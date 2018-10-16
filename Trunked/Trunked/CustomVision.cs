using System;
using System.Configuration;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training; // -Version 0.12.0-preview
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction; // -Version 0.10.0-preview

namespace Trunked
{
    public class CustomVision
    {
        public TrainingApi ClassifierTrainingApi { get; set; }
        public TrainingApi ClothingTrainingApi { get; set; }

        public string ClassifierModelTrainingKey { get; set; }
        public string ClassifierModelPredictionKey { get; set; }
        public Guid ClassifierModelProjectID { get; set; }

        public string ClothingModelTrainingKey { get; set; }
        public string ClothingModelPredictionKey { get; set; }
        public Guid ClothingModelProjectID { get; set; }

        public CustomVision()
        {
            ClassifierModelTrainingKey = ConfigurationManager.AppSettings["ObjectModelTrainingKey"];
            ClassifierModelPredictionKey = ConfigurationManager.AppSettings["ObjectModelPredictionKey"];
            ClassifierModelProjectID = new Guid(ConfigurationManager.AppSettings["ObjectModelProjectID"]);

            ClothingModelTrainingKey = ConfigurationManager.AppSettings["ClothingModelTrainingKey"];
            ClothingModelPredictionKey = ConfigurationManager.AppSettings["ClothingModelPredictionKey"];
            ClothingModelProjectID = new Guid(ConfigurationManager.AppSettings["ClothingModelProjectID"]);

            ClassifierTrainingApi = new TrainingApi() { ApiKey = ClassifierModelTrainingKey };
            ClassifierTrainingApi.HttpClient.Timeout = new TimeSpan(0, 30, 0);

            ClothingTrainingApi = new TrainingApi() { ApiKey = ClothingModelTrainingKey };
            ClothingTrainingApi.HttpClient.Timeout = new TimeSpan(0, 30, 0);
        }

        public Result GetClassification(string predictionImagePath)
        {
            PredictionEndpoint endpoint = new PredictionEndpoint() { ApiKey = ClassifierModelPredictionKey };
            MemoryStream predictionImage = new MemoryStream(File.ReadAllBytes(predictionImagePath));

            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction predictionResult = endpoint.PredictImage(ClassifierModelProjectID, predictionImage);

            Result result = new Result
            {
                // First result in list is highest probability
                Name = predictionResult.Predictions[0].TagName,
                Probability = predictionResult.Predictions[0].Probability.ToString()
            };

            if (result.Name.Equals("Barcode"))
                result.Classification = Classifiers.Barcode;
            else if (result.Name.Equals("Clothing"))
                result.Classification = Classifiers.Clothing;
            else
                result.Classification = Classifiers.Other;

            return result;
        }

        public Result GetClothingPrediction(string predictionImagePath)
        {
            PredictionEndpoint endpoint = new PredictionEndpoint() { ApiKey = ClothingModelPredictionKey };
            MemoryStream predictionImage = new MemoryStream(File.ReadAllBytes(predictionImagePath));

            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction predictionResult = endpoint.PredictImage(ClothingModelProjectID, predictionImage);

            Result result = new Result
            {
                // First result in list is highest probability
                Name = predictionResult.Predictions[0].TagName,
                Probability = predictionResult.Predictions[0].Probability.ToString()
            };

            return result;
        }

        protected void CreateNewTag(string newTag)
        {
            var tags = ClassifierTrainingApi.GetTags(ClassifierModelProjectID);

            bool tagExists = false;
            Tag bookTag = null;

            foreach (Tag tag in tags)
            {
                if (tag.Name.Equals(newTag))
                {
                    tagExists = true;
                    bookTag = tag;
                    break;
                }
            }

            if (!tagExists)
            {
                // Save image in tag folder
                // Check number of images in folder
                // If > 5, proceed with creating tag and training

                bookTag = ClassifierTrainingApi.CreateTag(ClassifierModelProjectID, newTag);
            }
        }

        public void TrainClassifierModel(string imagePath, string imageTag)
        {
            // Since apparently we can only have 10 iterations max
            DeleteEarliestIteration(false);

            var tags = ClassifierTrainingApi.GetTags(ClassifierModelProjectID);

            Tag trainTag = null;

            foreach (Tag tag in tags)
            {
                if (tag.Name.Equals(imageTag))
                {
                    trainTag = tag;
                    break;
                }
            }

            using (var stream = File.Open(imagePath, FileMode.Open))
            {
                ClassifierTrainingApi.CreateImagesFromData(ClassifierModelProjectID, stream, new List<string>() { trainTag.Id.ToString() });
            }

            var iteration = ClassifierTrainingApi.TrainProject(ClassifierModelProjectID);

            while (iteration.Status == "Training")
            {
                Thread.Sleep(1000);

                iteration = ClassifierTrainingApi.GetIteration(ClassifierModelProjectID, iteration.Id);
            }

            iteration.IsDefault = true;
            ClassifierTrainingApi.UpdateIteration(ClassifierModelProjectID, iteration.Id, iteration);

            File.Delete(imagePath);
        }

        public void TrainClothingModel(string imagePath, string imageTag)
        {
            // Since apparently we can only have 10 iterations max
            DeleteEarliestIteration(true);

            var tags = ClothingTrainingApi.GetTags(ClothingModelProjectID);

            Tag trainTag = null;

            foreach (Tag tag in tags)
            {
                if (tag.Name.Equals(imageTag))
                {
                    trainTag = tag;
                    break;
                }
            }

            using (var stream = File.Open(imagePath, FileMode.Open))
            {
                ClothingTrainingApi.CreateImagesFromData(ClothingModelProjectID, stream, new List<string>() { trainTag.Id.ToString() });
            }

            var iteration = ClothingTrainingApi.TrainProject(ClothingModelProjectID);

            while (iteration.Status == "Training")
            {
                Thread.Sleep(1000);

                iteration = ClothingTrainingApi.GetIteration(ClothingModelProjectID, iteration.Id);
            }

            iteration.IsDefault = true;
            ClothingTrainingApi.UpdateIteration(ClothingModelProjectID, iteration.Id, iteration);

            File.Delete(imagePath);
        }

        public void DeleteEarliestIteration(bool isClothing)
        {
            if (isClothing)
            {
                var iterations = ClothingTrainingApi.GetIterations(ClothingModelProjectID);

                Iteration iterationToDelete = iterations[iterations.Count - 1];

                if (iterations.Count == 10)
                    ClothingTrainingApi.DeleteIteration(ClothingModelProjectID, iterationToDelete.Id);
            }
            else
            {
                var iterations = ClassifierTrainingApi.GetIterations(ClassifierModelProjectID);

                Iteration iterationToDelete = iterations[iterations.Count - 1];

                if (iterations.Count == 10)
                    ClassifierTrainingApi.DeleteIteration(ClassifierModelProjectID, iterationToDelete.Id);
            }
        }
    }
}