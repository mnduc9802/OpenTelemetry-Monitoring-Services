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
            _meter = new Meter(meterName);

            RequestCounter = _meter.CreateCounter<long>(
                "api.requests.total",
                description: "Total number of API requests"
            );

            RequestDuration = _meter.CreateHistogram<double>(
                "api.request.duration",
                unit: "ms",
                description: "Duration of API requests"
            );

            SuccessCounter = _meter.CreateCounter<long>(
                "api.requests.success",
                description: "Number of successful requests"
            );

            FailureCounter = _meter.CreateCounter<long>(
                "api.requests.failure",
                description: "Number of failed requests"
            );
        }
    }
}
