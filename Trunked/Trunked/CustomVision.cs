using System;
using System.Configuration;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using System.Web.UI;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training; // -Version 0.12.0-preview
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
    }
}