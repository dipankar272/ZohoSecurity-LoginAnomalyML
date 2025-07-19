using Microsoft.ML.Data;

namespace AnomalyDetectionApp.Models
{
    // Represents raw login data from CSV
    public class LoginData
    {
        [LoadColumn(0)] // Load from first CSV column
        public string? User { get; set; }      // User name
        
        [LoadColumn(1)] // Load from second CSV column
        public string? Computer { get; set; }  // Computer name
        
        [LoadColumn(2)] // Load from third CSV column
        public string? Time { get; set; }      // Login time
        
        [LoadColumn(3)] // Load from fourth CSV column
        public string? Date { get; set; }      // Login date
    }
}