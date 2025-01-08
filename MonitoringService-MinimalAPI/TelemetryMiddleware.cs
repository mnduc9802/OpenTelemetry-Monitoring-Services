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
            // Tạo operation name từ path và method
            var operationName = $"{context.Request.Method}_{context.Request.Path.Value?.TrimStart('/')}".Replace("/", "_");

            using var activity = _activitySource.StartActivity(operationName);
            var startTime = Stopwatch.GetTimestamp();

            // Lưu request body nếu cần
            string? requestBody = null;
            if (context.Request.Method != "GET" && context.Request.ContentLength > 0)
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(
                    context.Request.Body,
                    Encoding.UTF8,
                    leaveOpen: true
                );
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            try
            {
                // Common tags cho metrics
                var tags = new[] {
                    new KeyValuePair<string, object?>("endpoint", context.Request.Path),
                    new KeyValuePair<string, object?>("method", context.Request.Method),
                    new KeyValuePair<string, object?>("host", context.Request.Host.Value)
                };

                // Record request
                _metrics.RequestCounter.Add(1, tags);

                // Add trace context
                activity?.SetTag("http.method", context.Request.Method);
                activity?.SetTag("http.url", context.Request.Path);
                activity?.SetTag("http.host", context.Request.Host.Value);
                activity?.SetTag("http.user_agent", context.Request.Headers.UserAgent.ToString());

                if (requestBody != null)
                {
                    activity?.SetTag("http.request_body", requestBody);
                }

                // Capture response
                var originalBodyStream = context.Response.Body;
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                // Execute the request
                await _next(context);

                // Read response
                responseBody.Seek(0, SeekOrigin.Begin);
                var response = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);

                // Record duration
                var duration = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
                _metrics.RequestDuration.Record(duration, tags);

                // Add response info to trace
                activity?.SetTag("http.status_code", context.Response.StatusCode);
                activity?.SetTag("http.response_body", response);

                // Record success/failure
                if (context.Response.StatusCode < 400)
                {
                    _metrics.SuccessCounter.Add(1, tags);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                else
                {
                    _metrics.FailureCounter.Add(1, tags);
                    activity?.SetStatus(ActivityStatusCode.Error);
                }

                // Log request details
                _logger.LogInformation(
                    "API Request - Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Duration: {Duration}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    duration
                );
            }
            catch (Exception ex)
            {
                var errorTags = new[] {
                    new KeyValuePair<string, object?>("endpoint", context.Request.Path),
                    new KeyValuePair<string, object?>("error", ex.Message)
                };

                _metrics.FailureCounter.Add(1, errorTags);

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.message", ex.Message);
                activity?.SetTag("error.stack_trace", ex.StackTrace);

                _logger.LogError(ex,
                    "Error processing request - Method: {Method}, Path: {Path}",
                    context.Request.Method,
                    context.Request.Path
                );

                throw;
            }
        }
    }
}
