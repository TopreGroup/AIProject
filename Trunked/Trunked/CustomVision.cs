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
        public TrainingApi TrainingApi { get; set; }

        public string ObjectModelTrainingKey { get; set; }
        public string ObjectModelPredictionKey { get; set; }
        public Guid ObjectModelProjectID { get; set; }

        public string ClothingModelTrainingKey { get; set; }
        public string ClothingModelPredictionKey { get; set; }
        public Guid ClothingModelProjectID { get; set; }

        public CustomVision()
        {
            ObjectModelTrainingKey = ConfigurationManager.AppSettings["ObjectModelTrainingKey"];
            ObjectModelPredictionKey = ConfigurationManager.AppSettings["ObjectModelPredictionKey"];
            ObjectModelProjectID = new Guid(ConfigurationManager.AppSettings["ObjectModelProjectID"]);

            ClothingModelTrainingKey = ConfigurationManager.AppSettings["ClothingModelTrainingKey"];
            ClothingModelPredictionKey = ConfigurationManager.AppSettings["ClothingModelPredictionKey"];
            ClothingModelProjectID = new Guid(ConfigurationManager.AppSettings["ClothingModelProjectID"]);

            TrainingApi = new TrainingApi() { ApiKey = ObjectModelTrainingKey };
            TrainingApi.HttpClient.Timeout = new TimeSpan(0, 30, 0);
        }

        public Result MakePrediction(string predictionImagePath)
        {
            PredictionEndpoint endpoint = new PredictionEndpoint() { ApiKey = ObjectModelPredictionKey };
            MemoryStream predictionImage = new MemoryStream(File.ReadAllBytes(predictionImagePath));

            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction predictionResult = endpoint.PredictImage(ObjectModelProjectID, predictionImage);

            Result result = new Result
            {
                // First result in list is highest probability
                Name = predictionResult.Predictions[0].TagName,
                Probability = predictionResult.Predictions[0].Probability.ToString()
            };

            if (result.Name.Equals("Barcode"))
                result.Type = ResultType.Barcode;
            else
                result.Type = ResultType.Other;

            return result;
        }

        protected void CreateNewTag(string newTag)
        {
            var tags = TrainingApi.GetTags(ObjectModelProjectID);

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

                bookTag = TrainingApi.CreateTag(ObjectModelProjectID, newTag);
            }
        }

        public void TrainModel(string imagePath, string imageTag)
        {
            // Since apparently we can only have 10 iterations max
            DeleteEarliestIteration();

            var tags = TrainingApi.GetTags(ObjectModelProjectID);

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
                TrainingApi.CreateImagesFromData(ObjectModelProjectID, stream, new List<string>() { trainTag.Id.ToString() });
            }

            var iteration = TrainingApi.TrainProject(ObjectModelProjectID);

            while (iteration.Status == "Training")
            {
                Thread.Sleep(1000);

                iteration = TrainingApi.GetIteration(ObjectModelProjectID, iteration.Id);
            }

            iteration.IsDefault = true;
            TrainingApi.UpdateIteration(ObjectModelProjectID, iteration.Id, iteration);

            File.Delete(imagePath);
        }

        public void DeleteEarliestIteration()
        {
            var iterations = TrainingApi.GetIterations(ObjectModelProjectID);

            Iteration iterationToDelete = iterations[iterations.Count - 1];

            if (iterations.Count == 10)
                TrainingApi.DeleteIteration(ObjectModelProjectID, iterationToDelete.Id);
        }
    }
}