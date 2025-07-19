using Microsoft.ML.Data;

namespace AnomalyDetectionApp.Models
{
    // Output from time clustering
    public class TimeClusterPrediction
    {
        [ColumnName("PredictedLabel")] // Cluster ID
        public uint PredictedClusterId; 
        
        [ColumnName("Score")] // Distances to centroids
        public float[]? Distance { get; set; } 
    }
}