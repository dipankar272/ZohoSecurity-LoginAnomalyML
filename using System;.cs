using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Xunit;

namespace AnomalyDetectionApp.Tests
{
    public class ProgramTest
    {
        public class TimeData
        {
            public float TimeInMinutes { get; set; }
        }

        public class ClusterPrediction
        {
            [ColumnName("PredictedLabel")]
            public uint PredictedClusterId;
            public float[]? Score;
        }

        [Fact]
        public void KMeansPipe_CreatesAndPredictsClusters()
        {
            // Arrange
            var ml = new MLContext(seed: 1);
            var data = new List<TimeData>
            {
                new TimeData { TimeInMinutes = 480 }, // 8:00 AM
                new TimeData { TimeInMinutes = 485 }, // 8:05 AM
                new TimeData { TimeInMinutes = 1320 }, // 10:00 PM
                new TimeData { TimeInMinutes = 1330 }, // 10:10 PM
            };
            var dv = ml.Data.LoadFromEnumerable(data);

            // Act
            var pipe = ml.Transforms.Concatenate("Features", "TimeInMinutes")
                .Append(ml.Clustering.Trainers.KMeans("Features", numberOfClusters: 2));
            var model = pipe.Fit(dv);
            var pred = model.Transform(dv);
            var clusters = ml.Data.CreateEnumerable<ClusterPrediction>(pred, reuseRowObject: false).ToList();

            // Assert
            Assert.NotNull(model);
            Assert.NotNull(clusters);
            Assert.Equal(data.Count, clusters.Count);
            Assert.All(clusters, c => Assert.True(c.PredictedClusterId == 1 || c.PredictedClusterId == 2));
        }
    }
}