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
AddOtel(builder, "gateway");
builder.Services.AddHttpClient();
var app = builder.Build();


app.MapPost("/api/order", async (HttpRequest req, IHttpClientFactory http, ILoggerFactory lf) =>
{
    var logger = lf.CreateLogger("gateway.order");
    var dto = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(req.Body) ?? new();

    var daprPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
    var url = $"http://localhost:{daprPort}/v1.0/invoke/ordersvc/method/api/order";

    var client = http.CreateClient();
    using var content = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json");
    var resp = await client.PostAsync(url, content);
    var text = await resp.Content.ReadAsStringAsync();

    logger.LogInformation("forwarded to ordersvc, status={StatusCode}, traceId={TraceId}", (int)resp.StatusCode, Activity.Current?.TraceId.ToString());
    return Results.Content(text, "application/json");
});

app.Run("http://0.0.0.0:8080");