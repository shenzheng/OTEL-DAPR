using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Text.Json;

static void AddOtel(WebApplicationBuilder builder, string serviceName)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r
            .AddService(serviceName: serviceName,
                serviceVersion: Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? "0.1.0",
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", Environment.GetEnvironmentVariable("DEPLOY_ENV") ?? "dev")
                })
        )
        .WithTracing(t => t
            .AddAspNetCoreInstrumentation(o => { o.RecordException = true; })
            .AddHttpClientInstrumentation(o => o.RecordException = true)
            .AddOtlpExporter()
        )
        .WithMetrics(m => m
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter()
        );

    builder.Logging.AddOpenTelemetry(o =>
    {
        o.IncludeFormattedMessage = true;
        o.IncludeScopes = true;
        o.ParseStateValues = true;
        o.AddOtlpExporter();
    });
}

var builder = WebApplication.CreateBuilder(args);
AddOtel(builder, "ordersvc");
builder.Services.AddHttpClient();
var app = builder.Build();


app.MapPost("/api/order", async (HttpContext ctx, IHttpClientFactory http, ILoggerFactory lf) =>
{
    var logger = lf.CreateLogger("ordersvc.create");
    var order = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(ctx.Request.Body) ?? new();
    var orderId = order.TryGetValue("id", out var id) ? id?.ToString() : Guid.NewGuid().ToString("n");

    var daprPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3501";
    var client = http.CreateClient();

    // 1) State 写入
    var stateUrl = $"http://localhost:{daprPort}/v1.0/state/statestore";
    var stateBody = new[] { new { key = orderId, value = order } };
    await client.PostAsync(stateUrl,
    new StringContent(JsonSerializer.Serialize(stateBody), System.Text.Encoding.UTF8, "application/json"));

    // 2) 调用支付
    var payUrl = $"http://localhost:{daprPort}/v1.0/invoke/paymentsvc/method/api/pay";
    var payResp = await client.PostAsync(payUrl,
    new StringContent(System.Text.Json.JsonSerializer.Serialize(new { id = orderId, amount = order.GetValueOrDefault("amount", 0) }), System.Text.Encoding.UTF8, "application/json"));

    if (!payResp.IsSuccessStatusCode)
    {
        logger.LogWarning("payment failed for {OrderId}, status={Status}", orderId, (int)payResp.StatusCode);
        return Results.Problem(statusCode: 502, title: "payment failed");
    }

    // 3) 发布事件
    var pubUrl = $"http://localhost:{daprPort}/v1.0/publish/pubsub/order.created";
    await client.PostAsync(pubUrl,
    new StringContent(JsonSerializer.Serialize(new { id = orderId }), System.Text.Encoding.UTF8, "application/json"));


    logger.LogInformation("order created {OrderId} trace={TraceId}", orderId, Activity.Current?.TraceId.ToString());
    return Results.Json(new { id = orderId, ok = true });
});


app.Run("http://0.0.0.0:8080");