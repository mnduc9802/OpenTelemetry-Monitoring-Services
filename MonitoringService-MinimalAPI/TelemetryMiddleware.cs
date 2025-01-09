using System.Diagnostics;
using System.Text;

namespace MonitoringService
{
    public class TelemetryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MetricsRegistry _metrics;
        private readonly ActivitySource _activitySource;
        private readonly ILogger<TelemetryMiddleware> _logger;

        public TelemetryMiddleware(
            RequestDelegate next,
            MetricsRegistry metrics,
            ActivitySource activitySource,
            ILogger<TelemetryMiddleware> logger)
        {
            _next = next;
            _metrics = metrics;
            _activitySource = activitySource;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] Processing request: {context.Request.Method} {context.Request.Path}");

            var startTime = Stopwatch.GetTimestamp();

            var tags = new[] {
                new KeyValuePair<string, object?>("endpoint", context.Request.Path),
                new KeyValuePair<string, object?>("method", context.Request.Method)
            };

            try
            {
                _metrics.RequestCounter.Add(1, tags);
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] Added request counter metric");

                await _next(context);

                var duration = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
                _metrics.RequestDuration.Record(duration, tags);
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] Recorded duration: {duration}ms");

                if (context.Response.StatusCode < 400)
                {
                    _metrics.SuccessCounter.Add(1, tags);
                    Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] Added success counter");
                }
                else
                {
                    _metrics.FailureCounter.Add(1, tags);
                    Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] Added failure counter");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] Error in middleware: {ex.Message}");
                throw;
            }
        }
    }
}
