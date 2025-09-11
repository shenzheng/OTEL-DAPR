using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
AddOtel(builder, "paymentsvc");
var app = builder.Build();

app.MapPost("/api/pay", (Dictionary<string, object> payload) =>
{
    if (Random.Shared.NextDouble() < 0.1)
    {
        return Results.Problem(statusCode: 500, title: "payment gateway error");
    }
    return Results.Json(new { paid = true, id = payload.GetValueOrDefault("id", string.Empty) });
});

app.Run("http://0.0.0.0:8080");