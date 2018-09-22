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

        public string TrainingKey { get; set; }
        public string PredictionKey { get; set; }
        public Guid ProjectID { get; set; }

        public void Init()
        {
            TrainingKey = ConfigurationManager.AppSettings["CustomVisionTrainingKey"];
            PredictionKey = ConfigurationManager.AppSettings["CustomVisionPredictionKey"];
            ProjectID = new Guid(ConfigurationManager.AppSettings["CustomVisionProjectID"]);

            TrainingApi = new TrainingApi() { ApiKey = TrainingKey };
            TrainingApi.HttpClient.Timeout = new TimeSpan(0, 30, 0);
        }

        public Result MakePrediction(string predictionImagePath)
        {
            PredictionEndpoint endpoint = new PredictionEndpoint() { ApiKey = PredictionKey };
            MemoryStream predictionImage = new MemoryStream(File.ReadAllBytes(predictionImagePath));

            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction predictionResult = endpoint.PredictImage(ProjectID, predictionImage);

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
            var tags = TrainingApi.GetTags(ProjectID);

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

                bookTag = TrainingApi.CreateTag(ProjectID, newTag);
            }
        }

        public void TrainModel(Result result, string imagePath)
        {
            var tags = TrainingApi.GetTags(ProjectID);

            Tag bookTag = null;

            foreach (Tag tag in tags)
            {
                if (tag.Name.Equals(result.Name))
                {
                    bookTag = tag;
                    break;
                }
            }

            using (var stream = File.Open(imagePath, FileMode.Open))
            {
                TrainingApi.CreateImagesFromData(ProjectID, stream, new List<string>() { bookTag.Id.ToString() });
            }

            var iteration = TrainingApi.TrainProject(ProjectID);

            while (iteration.Status == "Training")
            {
                Thread.Sleep(1000);

                iteration = TrainingApi.GetIteration(ProjectID, iteration.Id);
            }

            iteration.IsDefault = true;
            TrainingApi.UpdateIteration(ProjectID, iteration.Id, iteration);
        }
    }
}