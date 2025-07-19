using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.Extensions.Configuration;
using AnomalyDetectionApp.Models;
using AnomalyDetectionApp.Utils;

namespace AnomalyDetectionApp
{
    // Manages login anomaly detection
    public class LoginAnomalyDetector
    {
        private readonly MLContext _mlContext; // ML.NET context
        private ITransformer? _model;          // Trained model
        private readonly string _modelDir;     // Model directory
        private readonly string _modelPrefix;   // Model filename prefix
        private readonly double _iqrMultiplier; // Threshold configuration
        private readonly double _minThreshold;  // Minimum anomaly score

        // Column name constants
        private const string UserEncodedColumn = "UserEncoded";
        private const string ComputerEncodedColumn = "ComputerEncoded";
        private const string FeaturesVectorName = "Features";

        // Constructor with configuration
        public LoginAnomalyDetector(string modelDir, string modelPrefix, IConfiguration config)
        {
            _mlContext = new MLContext(seed: 0); // Initialize ML context
            _modelDir = "/data/models";                // Set model directory
            _modelPrefix = modelPrefix;          // Set model prefix
            
            // Initialize threshold values from config with defaults
            _iqrMultiplier = config.GetValue<double>("AnomalyThresholdSettings:IQRMultiplier", 1.5);
            _minThreshold = config.GetValue<double>("AnomalyThresholdSettings:MinThreshold", 0.8);
            
            // Create directory if needed
            if (!Directory.Exists(_modelDir))
                Directory.CreateDirectory(_modelDir);
        }

        // Trains and saves a model
        public void Train(string trainingDataPath)
        {
            Logger.Log("Starting Model Training", LogType.Title);
            
            // Get next model version number
            int modelVersion = GetNextModelVersion();
            // Create full model path
            string modelSavePath = Path.Combine(_modelDir, $"{_modelPrefix}{modelVersion}.zip");

            // Load and preprocess data
            IDataView? trainingDataView = LoadAndPreprocessData(trainingDataPath);
            if (trainingDataView == null) 
                return;

            // Build ML pipeline
            var pipeline = BuildTrainingPipeline(trainingDataView);

            Logger.Log("Fitting the anomaly detection model...", LogType.Info);
            try
            {
                // Train model
                _model = pipeline.Fit(trainingDataView);
                Logger.Log($"Model training completed. Saved as version {modelVersion}", LogType.Success);
                // Save model
                SaveModel(modelSavePath);
            }
            catch (Exception ex)
            {
                Logger.Log($"Model training failed: {ex.Message}", LogType.Error);
            }
        }

        // Generates report using specific model version
        public void GenerateStatefulReport(string predictionDataPath, int modelVersion)
        {
            // Create model path
            string modelPath = Path.Combine(_modelDir, $"{_modelPrefix}{modelVersion}.zip");
            
            // Check if model exists
            if (!File.Exists(modelPath))
            {
                Logger.Log($"Model file not found: {modelPath}", LogType.Error);
                return;
            }
            
            // Load model
            if (!LoadModel(modelPath)) 
                return;

            // Load prediction data
            var processedDataView = LoadAndPreprocessData(predictionDataPath);
            if (processedDataView == null) 
                return;

            // Get original features
            var originalFeatures = _mlContext.Data
                .CreateEnumerable<LoginFeatures>(processedDataView, reuseRowObject: false)
                .ToList();
                
            // Make predictions
            var predictions = _model!.Transform(processedDataView);
            
            // Get prediction results
            var anomalyResults = _mlContext.Data
                .CreateEnumerable<AnomalyPrediction>(predictions, reuseRowObject: false)
                .ToList();

            // Combine data and predictions
            var allResults = originalFeatures
                .Zip(anomalyResults, (features, prediction) => new LoginPredictionResult
                {
                    OriginalData = features,
                    Prediction = prediction
                })
                .ToList();

            // Run detection analyses
            DetectPcaAnomalies(allResults);
            DetectOwnershipAnomalies(allResults);
            DetectTimeBasedAnomalies(allResults);
        }

        // Gets next model version number
        private int GetNextModelVersion()
        {
            // Get existing models
            var existingModels = Directory.GetFiles(_modelDir, $"{_modelPrefix}*.zip");
            
            // Start at 1 if no models
            if (existingModels.Length == 0) 
                return 1;
                
            // Find highest version with null safety
            int maxVersion = 0;
            foreach (var filePath in existingModels)
            {
                // Get filename without extension
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // Skip null filenames
                if (fileName == null) continue;
                
                // Remove prefix to get version string
                string versionStr = fileName.Replace(_modelPrefix, "");
                
                // Try to parse version number
                if (int.TryParse(versionStr, out int version))
                {
                    if (version > maxVersion)
                        maxVersion = version;
                }
            }
            
            // Return next version number
            return maxVersion + 1;
        }

        // Detects PCA-based anomalies
        private void DetectPcaAnomalies(List<LoginPredictionResult> allResults)
        {
            Logger.Log("Starting PCA-Based Anomaly Detection", LogType.Title);
            
            // Check minimum data requirement
            if (allResults == null || allResults.Count < 4) 
            {
                Logger.Log("Not enough data for dynamic threshold", LogType.Warning);
                return;
            }

            // Get and sort scores
            var scores = allResults.Select(r => (double)r.Prediction!.Score)
                                  .OrderBy(s => s)
                                  .ToArray();
            
            // Calculate quartiles
            double q1 = MathHelper.Percentile(scores, 0.25);
            double q3 = MathHelper.Percentile(scores, 0.75);
            double iqr = q3 - q1;
            // Calculate dynamic threshold
            double dynamicThreshold = Math.Max(
                q3 + _iqrMultiplier * iqr,
                _minThreshold
            );

            Logger.Log($"Final threshold: {dynamicThreshold:F2} (IQR×{_iqrMultiplier} or min {_minThreshold})", LogType.Info);

            // Find anomalies
            bool anomaliesFound = false;
            foreach (var result in allResults.OrderBy(r => r.OriginalData!.LoginDateTime))
            {
                if (result.Prediction!.Score > dynamicThreshold)
                {
                    Logger.Log($"Unusual activity: {result.OriginalData!.User}@{result.OriginalData.Computer} " +
                              $"at {result.OriginalData.LoginDateTime:yyyy-MM-dd HH:mm} " +
                              $"(Score: {result.Prediction.Score:F2})", LogType.Anomaly);
                    anomaliesFound = true;
                }
            }

            // No anomalies found
            if (!anomaliesFound)
            {
                Logger.Log("No PCA-based anomalies", LogType.Success);
            }
        }
        
        // Detects ownership anomalies
        private void DetectOwnershipAnomalies(List<LoginPredictionResult> allResults)
        {
            Logger.Log("Chronological System Ownership Log", LogType.Title);

            // Ownership tracking structures
            var monthlyOwnership = new Dictionary<string, Dictionary<string, string>>();
            var lastKnownOwner = new Dictionary<string, string>();

            // Group by month
            var monthlyGroups = allResults
                .GroupBy(r => r.OriginalData!.LoginDateTime.ToString("yyyy-MM"))
                .OrderBy(g => g.Key);

            // Process each month
            foreach (var monthGroup in monthlyGroups)
            {
                Console.WriteLine(); // Blank line between months
                string currentMonthStr = monthGroup.Key;
                Logger.Log($"Month: {DateTime.ParseExact(currentMonthStr, "yyyy-MM", CultureInfo.InvariantCulture):MMMM yyyy}", LogType.Info);

                // Find dominant user per computer
                var monthlyDominance = monthGroup
                    .GroupBy(r => r.OriginalData!.Computer)
                    .Select(g => new {
                        Computer = g.Key,
                        DominantUser = g.GroupBy(x => x.OriginalData!.User)
                                        .OrderByDescending(ug => ug.Count())
                                        .First().Key
                    });
                    
                // Track changes
                bool hasChanges = false;
                foreach (var item in monthlyDominance)
                {
                    // Initialize computer entry
                    if (!monthlyOwnership.ContainsKey(item.Computer))
                    {
                        monthlyOwnership[item.Computer] = new Dictionary<string, string>();
                    }
                    // Record monthly owner
                    monthlyOwnership[item.Computer][currentMonthStr] = item.DominantUser.ToLower();

                    // Check for ownership changes
                    if (!lastKnownOwner.ContainsKey(item.Computer))
                    {
                        Logger.Log($"NEW: {item.Computer} -> {item.DominantUser}", LogType.Info);
                        lastKnownOwner[item.Computer] = item.DominantUser.ToLower();
                        hasChanges = true;
                    }
                    else if (lastKnownOwner[item.Computer] != item.DominantUser.ToLower())
                    {
                        var previousOwner = lastKnownOwner[item.Computer];
                        Logger.Log($"CHANGE: {item.Computer} from {previousOwner} to {item.DominantUser}", LogType.Warning);
                        lastKnownOwner[item.Computer] = item.DominantUser.ToLower();
                        hasChanges = true;
                    }
                }
                 if (!hasChanges)
                {
                    Logger.Log("No ownership changes", LogType.Info);
                }
            }

            // Print ownership summary
            Logger.Log("Final Ownership Summary", LogType.Title);
            foreach (var entry in lastKnownOwner.OrderBy(kvp => kvp.Key))
            {
                Logger.Log($"{entry.Key} : {entry.Value}", LogType.Info);
            }

            // Detect ownership anomalies
            Logger.Log("Ownership Anomaly Detection", LogType.Title);
            bool ownershipAnomaliesFound = false;
            foreach (var result in allResults.OrderBy(r => r.OriginalData!.LoginDateTime))
            {
                var loginUser = result.OriginalData!.User.ToLower();
                var computer = result.OriginalData.Computer;
                var monthStr = result.OriginalData.LoginDateTime.ToString("yyyy-MM");

                // Check against monthly owner
                if (monthlyOwnership.TryGetValue(computer, out var computerMonthlyHistory) && 
                    computerMonthlyHistory.TryGetValue(monthStr, out var usualOwner) && 
                    loginUser != usualOwner)
                {
                    Logger.Log($"ANOMALY: {loginUser} on {computer} at {result.OriginalData.LoginDateTime:yyyy-MM-dd HH:mm} " +
                              $"(usual: {usualOwner})", LogType.Anomaly);
                    ownershipAnomaliesFound = true;
                }
            }
            if (!ownershipAnomaliesFound)
            {
                Logger.Log("No ownership anomalies", LogType.Success);
            }
        }
        
        // Detects time-based anomalies
        private void DetectTimeBasedAnomalies(List<LoginPredictionResult> allResults)
        {
            Logger.Log("Time-Based Anomaly Detection", LogType.Title);
            
            // Group by user
            var usersGrouped = allResults.GroupBy(x => x.OriginalData!.User).OrderBy(g => g.Key);
            foreach (var userGroup in usersGrouped)
            {
                string userId = userGroup.Key ?? "UNKNOWN_USER";
                Logger.Log($"User: {userId}", LogType.Info);
                int n = userGroup.Count();

                // Prepare time features
                var loginTimesForClustering = userGroup
                    .Select(x => new TimeFeature { TimeInMinutes = new float[] { x.OriginalData!.TimeOfDayInMinutes } })
                    .ToList();

                // Check minimum data requirements
                double minRequiredLogins = Math.Max(5, Math.Log(n) * 2); 
                var loginMinutes = loginTimesForClustering.Select(t => t.TimeInMinutes![0]).ToList();
                double timeStdDev = MathHelper.CalculateStandardDeviation(loginMinutes);

                // Skip if insufficient data
                if (n < minRequiredLogins)
                {
                    Logger.Log($"Insufficient data ({n} logins)", LogType.Warning);
                    continue;
                }
                // Skip if times are too consistent
                if (timeStdDev < 10.0)
                {
                    Logger.Log($"Times too consistent (StdDev: {timeStdDev:F2})", LogType.Info);
                    continue;
                }

                // Prepare data view
                var featuresDataView = _mlContext.Data.LoadFromEnumerable(loginTimesForClustering);
                
                // Calculate cluster count
                int k = Math.Min(3, Math.Max(2, (int)Math.Ceiling(Math.Log(n))));
                Logger.Log($"Using K={k} clusters", LogType.Info);
                
                // Configure K-Means
                var options = new KMeansTrainer.Options
                {
                    FeatureColumnName = nameof(TimeFeature.TimeInMinutes),
                    NumberOfClusters = k
                };

                ITransformer timeModel;
                try
                {
                    // Train time clustering model
                    timeModel = _mlContext.Clustering.Trainers.KMeans(options).Fit(featuresDataView);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Clustering failed: {ex.Message}", LogType.Error);
                    continue;
                }

                // Make predictions
                var timePredictions = timeModel.Transform(featuresDataView);
                var clusteredResults = _mlContext.Data.CreateEnumerable<TimeClusterPrediction>(timePredictions, reuseRowObject: false).ToList();

                // Count cluster members
                var clusterCounts = clusteredResults.GroupBy(p => p.PredictedClusterId)
                    .Select(g => new { ClusterId = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count).ToList();

                // Check cluster validity
                if (clusterCounts.Count < 2)
                {
                    Logger.Log("Insufficient clusters", LogType.Warning);
                    continue;
                }

                // Identify dominant cluster
                uint dominantClusterId = clusterCounts.First().ClusterId;
                var dominantLogins = userGroup.Where((_, idx) => clusteredResults[idx].PredictedClusterId == dominantClusterId).ToList();

                // Calculate average time
                if (dominantLogins.Any())
                {
                    float avgTimeMinutes = (float)dominantLogins.Average(x => x.OriginalData!.TimeOfDayInMinutes);
                    TimeSpan avgTime = TimeSpan.FromMinutes(avgTimeMinutes);
                    Logger.Log($"Usual login time: {avgTime:hh\\:mm}", LogType.Info);
                }

                // Find time anomalies
                bool timeAnomaliesFound = false;
                for (int i = 0; i < loginTimesForClustering.Count; i++)
                {
                    if (clusteredResults[i].PredictedClusterId != dominantClusterId)
                    {
                        Logger.Log($"ANOMALY: {userGroup.ElementAt(i).OriginalData!.LoginDateTime:yyyy-MM-dd HH:mm}", LogType.Anomaly);
                        timeAnomaliesFound = true;
                    }
                }
                if (!timeAnomaliesFound)
                {
                    Logger.Log("No time anomalies", LogType.Success);
                }
            }
        }
        
        // Builds training pipeline
        private IEstimator<ITransformer> BuildTrainingPipeline(IDataView trainingDataView)
        {
            // Create data processing pipeline
            var dataProcessPipeline = _mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: UserEncodedColumn, 
                    inputColumnName: nameof(LoginFeatures.User))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                    outputColumnName: ComputerEncodedColumn, 
                    inputColumnName: nameof(LoginFeatures.Computer)))
                .Append(_mlContext.Transforms.Concatenate(
                    FeaturesVectorName, 
                    UserEncodedColumn, 
                    ComputerEncodedColumn, 
                    nameof(LoginFeatures.TimeOfDayInMinutes), 
                    nameof(LoginFeatures.DayOfWeek)))
                .Append(_mlContext.Transforms.NormalizeMinMax(
                    FeaturesVectorName, 
                    FeaturesVectorName));
            
            // Get feature vector information
            var processedSchema = dataProcessPipeline.Fit(trainingDataView).Transform(trainingDataView).Schema;

            // Configure PCA parameters
            if (processedSchema[FeaturesVectorName].Type is VectorDataViewType vectorType)
            {
                var featureCount = vectorType.Size;
                int rank = Math.Max(2, (int)Math.Sqrt(featureCount));
                int oversampling = (int)Math.Ceiling(0.5 * rank);
                Logger.Log($"PCA: Rank={rank}, Oversampling={oversampling}", LogType.Info);

                // Add PCA trainer
                return dataProcessPipeline.Append(_mlContext.AnomalyDetection.Trainers.RandomizedPca(
                    FeaturesVectorName, 
                    rank: rank, 
                    oversampling: oversampling));
            }
            else
            {
                throw new InvalidOperationException("Invalid feature vector type");
            }
        }
        
        // Loads and preprocesses data
        private IDataView? LoadAndPreprocessData(string dataPath)
        {
            try
            {
                // Load raw data
                IDataView rawDataView = _mlContext.Data.LoadFromTextFile<LoginData>(
                    dataPath, 
                    hasHeader: true, 
                    separatorChar: ',');
                
                // Parse and preprocess data
                var parsedData = _mlContext.Data.CreateEnumerable<LoginData>(rawDataView, reuseRowObject: false)
                    .Select(login =>
                    {
                        // Parse date/time
                        bool success = DateTime.TryParseExact($"{login.Date} {login.Time}",
                            new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd H:mm" }, 
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None, 
                            out var parsedDateTime);
                            
                        // Validate data
                        if (!success || string.IsNullOrEmpty(login.User) || string.IsNullOrEmpty(login.Computer)) 
                            return null;
                            
                        // Create features
                        return new LoginFeatures
                        {
                            User = login.User,
                            Computer = login.Computer,
                            LoginDateTime = parsedDateTime,
                            TimeOfDayInMinutes = (float)parsedDateTime.TimeOfDay.TotalMinutes,
                            DayOfWeek = (float)parsedDateTime.DayOfWeek
                        };
                    })
                    .Where(x => x != null).ToList();

                // Check for valid data
                if (!parsedData.Any())
                {
                    Logger.Log("No valid data rows", LogType.Error);
                    return null;
                }

                // Create data view
                return _mlContext.Data.LoadFromEnumerable(parsedData!);
            }
            catch (Exception ex)
            {
                Logger.Log($"Data loading error: {ex.Message}", LogType.Error);
                return null;
            }
        }

        // Saves model to file
        private void SaveModel(string modelSavePath)
        {
            if (_model == null) 
                return;
                
            // Save model
            _mlContext.Model.Save(_model, null, modelSavePath);
            Logger.Log($"Model saved: {modelSavePath}", LogType.Info);
        }

        // Loads model from file
        private bool LoadModel(string modelLoadPath)
        {
            Logger.Log("Loading Model", LogType.Title);
            try
            {
                // Load model
                _model = _mlContext.Model.Load(modelLoadPath, out _);
                Logger.Log("Model loaded", LogType.Success);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Model load failed: {ex.Message}", LogType.Error);
                return false;
            }
        }
    }
}