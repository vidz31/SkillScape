using System;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace SkillScape.MLTrainer
{
    public class QuizData
    {
        [LoadColumn(0)]
        public string CombinedAnswers { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string RecommendedDomain { get; set; } = string.Empty;
    }

    public class QuizPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedDomain { get; set; } = string.Empty;
        public float[] Score { get; set; } = Array.Empty<float>();
    }

    public class SalaryData
    {
        [LoadColumn(0)]
        public string Domain { get; set; } = string.Empty;

        [LoadColumn(1)]
        public float YearsOfExperience { get; set; }

        [LoadColumn(2)]
        public float Salary { get; set; }
    }

    public class SalaryPrediction
    {
        [ColumnName("Score")]
        public float PredictedSalary { get; set; }
    }

    // ─── Placement Readiness Models ───────────────────────────────────────────
    public class PlacementData
    {
        [LoadColumn(0)] public float DsaScore { get; set; }
        [LoadColumn(1)] public float ProgrammingScore { get; set; }
        [LoadColumn(2)] public float ProblemSolvingScore { get; set; }
        [LoadColumn(3)] public float AptitudeScore { get; set; }
        [LoadColumn(4)] public float SoftSkillsScore { get; set; }
        [LoadColumn(5)] public float SystemDesignScore { get; set; }
        [LoadColumn(6)] public float DatabasesScore { get; set; }
        [LoadColumn(7)] public float ReadinessScore { get; set; }
    }

    public class PlacementPrediction
    {
        [ColumnName("Score")]
        public float ReadinessScore { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var mlContext = new MLContext(seed: 1);
            
            // 1. Train Career Predictor
            TrainCareerPredictor(mlContext);

            // 2. Train Salary Predictor
            // TrainSalaryPredictor(mlContext);

            // 3. Train Placement Readiness Predictor (NEW)
            TrainPlacementReadinessPredictor(mlContext);

            TestPredictor(mlContext);
            TestPlacementPredictor(mlContext);
        }

        static void TestPredictor(MLContext mlContext)
        {
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Testing Predictor...");
            string modelPath = Path.Combine(Environment.CurrentDirectory, "CareerPredictor.zip");
            ITransformer model = mlContext.Model.Load(modelPath, out var schema);
            
            var engine = mlContext.Model.CreatePredictionEngine<QuizData, QuizPrediction>(model);

            // Let's test different combinations of text from the quiz dataset
            var sample1 = new QuizData { CombinedAnswers = "Designing brand logos Creating typography systems Choosing color palettes Sketching wireframes Focusing on esthetics Running user research sessions" };
            var sample2 = new QuizData { CombinedAnswers = "Processing natural language Training predictive models Implementing deep learning Tuning neural networks Analyzing market competition" };
            var sample3 = new QuizData { CombinedAnswers = "Building statistical models Managing application state Architecting the entire application flow Ensuring zero downtime Organizing classroom activities" };

            Console.WriteLine($"Sample 1 Predicted: {engine.Predict(sample1).PredictedDomain}");
            Console.WriteLine($"Sample 2 Predicted: {engine.Predict(sample2).PredictedDomain}");
            Console.WriteLine($"Sample 3 Predicted: {engine.Predict(sample3).PredictedDomain}");
        }

        static void TestPlacementPredictor(MLContext mlContext)
        {
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Testing Placement Readiness Predictor...");
            string modelPath = Path.Combine(Environment.CurrentDirectory, "PlacementReadinessPredictor.zip");
            if (!File.Exists(modelPath))
            {
                Console.WriteLine("PlacementReadinessPredictor.zip not found. Run training first.");
                return;
            }
            ITransformer model = mlContext.Model.Load(modelPath, out var schema);
            var engine = mlContext.Model.CreatePredictionEngine<PlacementData, PlacementPrediction>(model);

            // Strong candidate
            var strong = new PlacementData { DsaScore=90, ProgrammingScore=85, ProblemSolvingScore=88, AptitudeScore=80, SoftSkillsScore=75, SystemDesignScore=70, DatabasesScore=80 };
            // Average candidate
            var avg = new PlacementData { DsaScore=60, ProgrammingScore=65, ProblemSolvingScore=55, AptitudeScore=60, SoftSkillsScore=70, SystemDesignScore=50, DatabasesScore=60 };
            // Weak candidate
            var weak = new PlacementData { DsaScore=30, ProgrammingScore=35, ProblemSolvingScore=25, AptitudeScore=40, SoftSkillsScore=50, SystemDesignScore=20, DatabasesScore=35 };

            Console.WriteLine($"Strong Candidate Readiness: {engine.Predict(strong).ReadinessScore:F1}%");
            Console.WriteLine($"Average Candidate Readiness: {engine.Predict(avg).ReadinessScore:F1}%");
            Console.WriteLine($"Weak Candidate Readiness: {engine.Predict(weak).ReadinessScore:F1}%");
        }

        static void TrainCareerPredictor(MLContext mlContext)
        {
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Training Career Predictor...");
            string dataPath = Path.Combine(Environment.CurrentDirectory, "..", "DataGeneration", "quiz_training_data.csv");
            string modelPath = Path.Combine(Environment.CurrentDirectory, "CareerPredictor.zip");

            Console.WriteLine("Loading data...");
            IDataView dataView = mlContext.Data.LoadFromTextFile<QuizData>(dataPath, hasHeader: true, separatorChar: '\t');

            var splitInfo = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            Console.WriteLine("Building pipeline...");
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "Label", inputColumnName: nameof(QuizData.RecommendedDomain))
                .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(QuizData.CombinedAnswers)))
                .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var model = pipeline.Fit(splitInfo.TrainSet);

            var predictions = model.Transform(splitInfo.TestSet);
            var metrics = mlContext.MulticlassClassification.Evaluate(predictions, "Label", "Score");

            Console.WriteLine($"Macro Accuracy: {metrics.MacroAccuracy:P2}");
            Console.WriteLine($"Micro Accuracy: {metrics.MicroAccuracy:P2}");

            mlContext.Model.Save(model, dataView.Schema, modelPath);
            Console.WriteLine($"Career Predictor saved to {modelPath}");
        }

        static void TrainSalaryPredictor(MLContext mlContext)
        {
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Training Salary Predictor...");
            string dataPath = Path.Combine(Environment.CurrentDirectory, "..", "DataGeneration", "salary_training_data.csv");
            string modelPath = Path.Combine(Environment.CurrentDirectory, "SalaryPredictor.zip");

            Console.WriteLine("Loading data...");
            IDataView dataView = mlContext.Data.LoadFromTextFile<SalaryData>(dataPath, hasHeader: true, separatorChar: ',');

            var splitInfo = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            Console.WriteLine("Building pipeline...");
            // One-hot encode the categorical 'Domain' feature, then concatenate with numerical 'YearsOfExperience'
            var pipeline = mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "DomainEncoded", inputColumnName: nameof(SalaryData.Domain))
                .Append(mlContext.Transforms.Concatenate("Features", "DomainEncoded", nameof(SalaryData.YearsOfExperience)))
                // Note: The Salary is the label
                .Append(mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(SalaryData.Salary)))
                .Append(mlContext.Regression.Trainers.FastTree());

            var model = pipeline.Fit(splitInfo.TrainSet);

            var predictions = model.Transform(splitInfo.TestSet);
            var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");

            Console.WriteLine($"R-Squared: {metrics.RSquared:F2}");
            Console.WriteLine($"Root Mean Squared Error: {metrics.RootMeanSquaredError:F2}");

            mlContext.Model.Save(model, dataView.Schema, modelPath);
            Console.WriteLine($"Salary Predictor saved to {modelPath}");
        }

        /// <summary>
        /// Trains an SDCA Regression model to predict placement readiness score (0-100)
        /// from 7 skill category input features.
        /// </summary>
        static void TrainPlacementReadinessPredictor(MLContext mlContext)
        {
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Training Placement Readiness Predictor...");

            string dataPath = Path.Combine(Environment.CurrentDirectory, "..", "DataGeneration", "placement_training_data.csv");
            string modelPath = Path.Combine(Environment.CurrentDirectory, "PlacementReadinessPredictor.zip");

            if (!File.Exists(dataPath))
            {
                Console.WriteLine($"Training data not found at: {dataPath}");
                return;
            }

            Console.WriteLine("Loading placement training data...");
            IDataView dataView = mlContext.Data.LoadFromTextFile<PlacementData>(
                dataPath, hasHeader: true, separatorChar: ',');

            var splitInfo = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

            Console.WriteLine("Building regression pipeline...");
            // Concatenate all 7 feature columns, then apply SDCA regression
            var pipeline = mlContext.Transforms.Concatenate(
                    "Features",
                    nameof(PlacementData.DsaScore),
                    nameof(PlacementData.ProgrammingScore),
                    nameof(PlacementData.ProblemSolvingScore),
                    nameof(PlacementData.AptitudeScore),
                    nameof(PlacementData.SoftSkillsScore),
                    nameof(PlacementData.SystemDesignScore),
                    nameof(PlacementData.DatabasesScore))
                .Append(mlContext.Transforms.CopyColumns(
                    outputColumnName: "Label",
                    inputColumnName: nameof(PlacementData.ReadinessScore)))
                .Append(mlContext.Regression.Trainers.Sdca(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    maximumNumberOfIterations: 100));

            Console.WriteLine("Fitting model...");
            var model = pipeline.Fit(splitInfo.TrainSet);

            var predictions = model.Transform(splitInfo.TestSet);
            var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");

            Console.WriteLine($"Placement Predictor R-Squared:  {metrics.RSquared:F4}");
            Console.WriteLine($"Placement Predictor RMSE:       {metrics.RootMeanSquaredError:F2}");
            Console.WriteLine($"Placement Predictor MAE:        {metrics.MeanAbsoluteError:F2}");

            mlContext.Model.Save(model, dataView.Schema, modelPath);
            Console.WriteLine($"PlacementReadinessPredictor saved to {modelPath}");
        }
    }
}
