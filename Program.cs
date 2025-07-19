using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace AnomalyDetectionApp
{
    // Main application class
    class Program
    {
        // Entry point
        static void Main(string[] args)
        {
            // Load configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())  // Set base path for config
                .AddJsonFile("Config/AppSettings.json", optional: true)  // Load settings
                .AddEnvironmentVariables()  // Add this line

                .Build();
                
            // Get model settings
            string modelDir = config["ModelSaveDirectory"] ?? "Models/";
            string modelPrefix = config["ModelFilePrefix"] ?? "savedmodel";

            // Validate arguments
            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }

            // Get command and data path
            string command = args[0];
            string dataPath = args[1];

            // Create detector with config
            var detector = new LoginAnomalyDetector(modelDir, modelPrefix, config);

            // Validate data file
            if (!File.Exists(dataPath))
            {
                Console.WriteLine($"\n❌ [ERROR] File not found: {dataPath}");
                return;
            }

            // Execute command
            if (command == "train")
            {
                // Training command
                detector.Train(dataPath);
            }
            else if (command == "predict")
            {
                // Prediction command
                if (args.Length < 3)
                {
                    Console.WriteLine("\n❌ [ERROR] Model version required");
                    PrintUsage();
                    return;
                }
                
                // Parse model version
                if (!int.TryParse(args[2], out int modelVersion))
                {
                    Console.WriteLine("\n❌ [ERROR] Invalid model version");
                    return;
                }
                
                // Generate report
                detector.GenerateStatefulReport(dataPath, modelVersion);
            }
            else
            {
                // Invalid command
                Console.WriteLine($"\n❌ [ERROR] Unknown command: {command}");
                PrintUsage();
            }
        }

        // Prints usage instructions
        private static void PrintUsage()
        {
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("  Training:");
            Console.WriteLine("    dotnet run train <path-to-training-data.csv>");
            Console.WriteLine("");
            Console.WriteLine("  Prediction:");
            Console.WriteLine("    dotnet run predict <path-to-prediction-data.csv> <model-version>");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run train Data/TrainingData.csv");
            Console.WriteLine("  dotnet run predict Data/PredictionData.csv 3");
        }
    }
}