using System;
using System.Configuration;
using System.IO;
using System.Web.UI;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training; // -Version 0.12.0-preview
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction; // -Version 0.10.0-preview
using ImageResizer;

namespace Trunked
{
    public partial class _Default : Page
    {
        protected string trainingKey = ConfigurationManager.AppSettings["CustomVisionTrainingKey"];
        protected string predictionKey = ConfigurationManager.AppSettings["CustomVisionPredictionKey"];
        protected Guid projectID = new Guid(ConfigurationManager.AppSettings["CustomVisionProjectID"]);

        GoogleBooksAPI googleBooksAPI = new GoogleBooksAPI();
        protected TrainingApi trainingApi;

        protected string recognizedText = "";
        protected bool resultsFound;
        protected List<Dictionary<string, string>> bookDetailsList;

        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "GoogleServiceAccount.json");

            if (!String.IsNullOrEmpty(Request.QueryString["isbn"]))
                googleBooksAPI.CreateResultsTable(googleBooksAPI.GetBookDetailsFromISBN(Request.QueryString["isbn"]), tblResults);

            trainingApi = new TrainingApi() { ApiKey = trainingKey };
            trainingApi.HttpClient.Timeout = new TimeSpan(0, 30, 0);
        }

        protected void RecognizeButton_Click(object sender, EventArgs e)
        {
            tblObjectResults.Visible = false;

            if (ctrlFileUpload.HasFile)
            {
                string path = "";

                try
                {
                    string fileName = Path.GetFileName(ctrlFileUpload.FileName);

                    path = Server.MapPath("~/UploadedImages/") + fileName;

                    ctrlFileUpload.SaveAs(path);

                    // Resizes the image to a better size for the decoder and also for the model
                    ImageBuilder.Current.Build(path, path, new ResizeSettings("width=768&height=1024"));
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Upload status: The file could not be uploaded. The following error occured: " + ex.Message;
                    lblStatus.Visible = true;
                }
                
                Result result = new Result();

                try
                {
                    result = MakePrediction(path);

                    if (result.Type == ResultType.Barcode)
                    {
                        BarcodeDecoder barcodeDecoder = new BarcodeDecoder();

                        Barcode barcode = barcodeDecoder.Decode(path);

                        if (barcode != null)
                            googleBooksAPI.CreateResultsTable(googleBooksAPI.GetBookDetailsFromISBN(barcode.Text), tblResults);
                    }
                    else if (result.Type == ResultType.Other)
                    {
                        if (result.Name.Equals("Book"))
                        {
                            BookRecognizer bookRecognizer = new BookRecognizer();

                            string imageText = bookRecognizer.ReadTextFromImage(path);

                            if (!String.IsNullOrWhiteSpace(imageText))
                            {
                                List<Dictionary<string, string>> bookDetails = googleBooksAPI.GetBookDetailsFromText(imageText, ConfigurationManager.AppSettings["GoogleBooksAPIMaxResults"]);

                                if (bookDetails != null)
                                     bookRecognizer.FormatBookResultsForSelection(bookDetails, tblResults);
                            }
                            else
                                lblStatus.Text = imageText;
                        }
                        else
                        {
                            cllConfidence.Text = result.Probability;
                            cllItemScanned.Text = result.Name;

                            tblObjectResults.Visible = true;
                        }

                    }
                }
                catch (Microsoft.Rest.HttpOperationException ex)
                {
                    lblStatus.Text = "ERROR: " + ex.Message + "<br />" + ex.Response.Content;
                }
                catch (Exception ex)
                {
                    lblStatus.Text = ex + "<br />" + ex.Message + "<br />" + ex.InnerException;
                }
                
                File.Delete(path);
            }

            ctrlFileUpload.Dispose();
        }

        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            // Do stuff?
        }

        protected Result MakePrediction(string predictionImagePath)
        {
            PredictionEndpoint endpoint = new PredictionEndpoint() { ApiKey = predictionKey };
            MemoryStream predictionImage = new MemoryStream(File.ReadAllBytes(predictionImagePath));

            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction predictionResult = endpoint.PredictImage(projectID, predictionImage);

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