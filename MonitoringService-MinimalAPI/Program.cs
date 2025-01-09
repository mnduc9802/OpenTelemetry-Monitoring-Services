using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using System.Diagnostics;
using MonitoringService;

var builder = WebApplication.CreateBuilder(args);

// Service configuration
var serviceName = "APIMonitor";
var serviceVersion = "1.0.0";

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
    .AddTelemetrySdk()
    .AddEnvironmentVariableDetector();

var activitySource = new ActivitySource(serviceName);
var metricsRegistry = new MetricsRegistry("APIMetrics");

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource(serviceName)
            .SetResourceBuilder(resourceBuilder)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
    })
    .WithMetrics(metricsProviderBuilder =>
    {
        metricsProviderBuilder
            .SetResourceBuilder(resourceBuilder)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("APIMetrics") // Phải match với tên trong MetricsRegistry
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://otel-collector:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                options.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
                options.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<Activity>
                {
                    MaxQueueSize = 2048,
                    ScheduledDelayMilliseconds = 1000, // Giảm xuống để test nhanh hơn
                    ExporterTimeoutMilliseconds = 30000,
                    MaxExportBatchSize = 512,
                };
            });
    });

// Register monitoring services
builder.Services.AddSingleton(activitySource);
builder.Services.AddSingleton(metricsRegistry);

var app = builder.Build();

// Add API endpoint
app.MapGet("/api/sample", async context =>
{
    using var activity = activitySource.StartActivity("SampleEndpoint");
    activity?.SetTag("http.method", "GET");
    activity?.SetTag("http.path", "/api/sample");

    var response = new
    {
        Message = "Hello, this is a sample endpoint!",
        Timestamp = DateTime.UtcNow
    };

    // Log custom events or metrics if necessary
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Sample endpoint was called at {Time}", DateTime.UtcNow);

    await context.Response.WriteAsJsonAsync(response);
});

// Add telemetry middleware - sẽ bắt mọi request
app.UseMiddleware<TelemetryMiddleware>();

app.Run();