using System.Diagnostics.Metrics;

namespace MonitoringService
{
    public class MetricsRegistry
    {
        private readonly Meter _meter;

        public Counter<long> RequestCounter { get; }
        public Histogram<double> RequestDuration { get; }
        public Counter<long> SuccessCounter { get; }
        public Counter<long> FailureCounter { get; }

        public MetricsRegistry(string meterName)
        {
            // Add version to differentiate metrics
            _meter = new Meter(meterName, "1.0.0");
            Console.WriteLine($"Created meter: {meterName} v1.0.0");

            RequestCounter = _meter.CreateCounter<long>(
                "api_requests_total", // Changed to underscore format
                description: "Total number of API requests",
                unit: "{requests}"
            );
            Console.WriteLine("Created RequestCounter");

            RequestDuration = _meter.CreateHistogram<double>(
                "api_request_duration_milliseconds", // Changed name
                unit: "ms",
                description: "Duration of API requests"
            );
            Console.WriteLine("Created RequestDuration");

            SuccessCounter = _meter.CreateCounter<long>(
                "api_requests_success_total", // Changed name
                description: "Number of successful requests",
                unit: "{requests}"
            );
            Console.WriteLine("Created SuccessCounter");

            FailureCounter = _meter.CreateCounter<long>(
                "api_requests_failure_total", // Changed name
                description: "Number of failed requests",
                unit: "{requests}"
            );
            Console.WriteLine("Created FailureCounter");
        }
    }
}
