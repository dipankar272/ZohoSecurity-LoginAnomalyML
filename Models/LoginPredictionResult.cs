namespace AnomalyDetectionApp.Models
{
    // Combines original data with prediction
    public class LoginPredictionResult
    {
        public LoginFeatures? OriginalData { get; set; }   // Source data
        public AnomalyPrediction? Prediction { get; set; } // Prediction result
    }
}