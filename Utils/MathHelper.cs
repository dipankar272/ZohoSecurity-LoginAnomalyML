using System;
using System.Collections.Generic;
using System.Linq;

namespace AnomalyDetectionApp.Utils
{
    // Provides math utility functions
    public static class MathHelper
    {
        // Calculates standard deviation of a collection
        public static double CalculateStandardDeviation(IEnumerable<float> values)
        {
            // Check for valid input
            if (values == null || !values.Any() || values.Count() < 2)
                return 0.0;
            
            // Calculate mean
            double avg = values.Average();
            // Calculate sum of squares
            double sum = values.Sum(d => Math.Pow(d - avg, 2));
            // Return standard deviation
            return Math.Sqrt(sum / (values.Count() - 1));
        }

        // Calculates percentile in a sorted sequence
        public static double Percentile(double[] sequence, double percentile)
        {
            // Check for valid input
            if (sequence == null || sequence.Length == 0)
                return 0.0;
            
            // Calculate index position
            int n = sequence.Length;
            double index = percentile * (n - 1);
            int lower = (int)Math.Floor(index);
            double fraction = index - lower;

            // Handle edge cases
            if (lower + 1 >= n) 
                return sequence[lower];
            
            // Interpolate value
            return sequence[lower] + fraction * (sequence[lower + 1] - sequence[lower]);
        }
    }
}