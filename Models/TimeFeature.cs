using Microsoft.ML.Data;

namespace AnomalyDetectionApp.Models
{
    // Input for time clustering
    public class TimeFeature
    {
        [VectorType(1)] // Single-value vector
        public float[]? TimeInMinutes { get; set; } // Time feature
    }
}