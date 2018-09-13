using System;
using System.Configuration;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training; // -Version 0.12.0-preview
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction; // -Version 0.10.0-preview

namespace Trunked
{
    public enum ResultType
    {
        Barcode,
        Other
    }

    public class Result
    {
        public ResultType Type;
        public string Name;
        public string Probability;
    }
    
    public partial class _Default : Page
    {
        protected string trainingKey = ConfigurationManager.AppSettings["CustomVisionTrainingKey"];
        protected string predictionKey = ConfigurationManager.AppSettings["CustomVisionPredictionKey"];
        protected Guid projectID = new Guid(ConfigurationManager.AppSettings["CustomVisionProjectID"]);

        protected TrainingApi trainingApi;

        protected int minImagesPerTag = 5;

        protected string recognizedText = "";
        protected bool resultsFound;
        protected List<Dictionary<string, string>> bookDetailsList;

        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "VisionAPIServiceAccount.json");

            trainingApi = new TrainingApi() { ApiKey = trainingKey };
            trainingApi.HttpClient.Timeout = new TimeSpan(0, 30, 0);
        }

        protected void RecognizeButton_Click(object sender, EventArgs e)
        {
            if (ctrlFileUpload.HasFile)
            {
                string path = "";

                try
                {
                    string fileName = Path.GetFileName(ctrlFileUpload.FileName);

                    path = Server.MapPath("~/UploadedImages/") + fileName;

                    ctrlFileUpload.SaveAs(path);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Upload status: The file could not be uploaded. The following error occured: " + ex.Message;
                    lblStatus.Visible = true;
                }

                Result result = new Result();

                try
                {
                    result = DetermineObjectType(path);
                }
                catch (Microsoft.Rest.HttpOperationException ex)
                {
                    lblResults.Text = "ERROR: " + ex.Message + "<br />" + ex.Response.Content;
                }
                catch (Exception ex)
                {
                    lblResults.Text = ex + "<br />" + ex.Message + "<br />" + ex.InnerException;
                }

                if (result.Type == ResultType.Barcode)
                {
                    // Do barcode stuff
                    
                    // Just ocurred to me that since the QuaggaJS is javascript, we can't really use it how we would like
                    // from the server-side. Which sucks...
                }
                else if (result.Type == ResultType.Other)
                {
                    // Do custom vision stuff
                }
            }

            ctrlFileUpload.Dispose();
        }

        protected Result DetermineObjectType(string predictionImagePath)
        {
            PredictionEndpoint endpoint = new PredictionEndpoint() { ApiKey = predictionKey };
            MemoryStream predictionImage = new MemoryStream(File.ReadAllBytes(predictionImagePath));

            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction predictionResult = endpoint.PredictImage(projectID, predictionImage);

            Result result = new Result();

            result.Name = predictionResult.Predictions[0].TagName;
            result.Probability = predictionResult.Predictions[0].Probability.ToString();

            if (result.Name.Equals("Barcode"))
                result.Type = ResultType.Barcode;
            else
                result.Type = ResultType.Other;

            return result;
        }
    }
}