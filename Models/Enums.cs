namespace AnomalyDetectionApp.Models
{
    // Defines different types of log messages
    public enum LogType
    {
        Title,    // Section titles
        Info,     // General information
        Warning,  // Non-critical warnings
        Error,    // Critical errors
        Anomaly,  // Detected anomalies
        Success   // Successful operations
    }
}