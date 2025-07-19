using AnomalyDetectionApp.Models;

namespace AnomalyDetectionApp.Utils
{
    // Provides centralized logging functionality
    public static class Logger
    {
        // Logs messages with different types and formatting
        public static void Log(string message, LogType type)
        {
            // Add blank line before titles for readability
            if (type == LogType.Title) 
                Console.WriteLine();
            
            // Format message based on log type
            Console.WriteLine(type switch {
                LogType.Title => $"--- {message.ToUpper()} ---",       // Title formatting
                LogType.Info => $"ℹ️  [INFO] {message}",              // Information message
                LogType.Warning => $"⚠️  [WARNING] {message}",        // Warning message
                LogType.Error => $"❌ [ERROR] {message}",             // Error message
                LogType.Anomaly => $"🚨 [ANOMALY] {message}",        // Anomaly detection
                LogType.Success => $"✅ [SUCCESS] {message}",         // Success message
                _ => message                                          // Default formatting
            });
        }
    }
}
