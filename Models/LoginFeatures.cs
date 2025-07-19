using System;

namespace AnomalyDetectionApp.Models
{
    // Contains processed features for ML models
    public class LoginFeatures
    {
        public string User { get; set; } = "";          // User name
        public string Computer { get; set; } = "";      // Computer name
        public DateTime LoginDateTime { get; set; }     // Combined date/time
        public float TimeOfDayInMinutes { get; set; }   // Time in minutes since midnight
        public float DayOfWeek { get; set; }            // Numeric day of week (0-6)
    }
}