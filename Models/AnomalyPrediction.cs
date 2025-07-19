using Microsoft.ML.Data;

namespace AnomalyDetectionApp.Models
{
    // Contains PCA anomaly detection results
    public class AnomalyPrediction
    {
        [ColumnName("Score")] // Anomaly score
        public float Score { get; set; } 
        
        [ColumnName("PredictedLabel")] // Model prediction
        public bool IsAnomalyFromModel { get; set; } 
        
        [VectorType] // Feature vector used
        public float[]? Features { get; set; } 
    }
}
