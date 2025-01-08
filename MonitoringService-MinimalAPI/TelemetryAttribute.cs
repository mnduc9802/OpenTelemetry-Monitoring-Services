namespace MonitoringService
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TelemetryAttribute : Attribute
    {
        public string OperationName { get; }

        public TelemetryAttribute(string operationName)
        {
            OperationName = operationName;
        }
    }
}
