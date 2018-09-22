using System;
using System.Configuration;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Trunked
{
    public partial class _Default : Page
    {
        protected readonly string ERROR_IMAGESIZE = "BadRequestImageSizeBytes";
        protected readonly string MESSAGE_IMAGESIZE = "Image file is too large. Please choose an image smaller than 4MB";

        protected string trainingKey = ConfigurationManager.AppSettings["CustomVisionTrainingKey"];
        protected string predictionKey = ConfigurationManager.AppSettings["CustomVisionPredictionKey"];
        protected Guid projectID = new Guid(ConfigurationManager.AppSettings["CustomVisionProjectID"]);

        protected GoogleBooksAPI googleBooksAPI = new GoogleBooksAPI();
        protected CustomVision customVision = new CustomVision();

        protected string recognizedText = "";
        protected bool resultsFound;
        protected List<Dictionary<string, string>> bookDetailsList;

        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "GoogleServiceAccount.json");

            if (!String.IsNullOrEmpty(Request.QueryString["isbn"]))
                googleBooksAPI.CreateResultsTable(googleBooksAPI.GetBookDetailsFromISBN(Request.QueryString["isbn"]), tblResults);
        }

        protected void RecognizeButton_Click(object sender, EventArgs e)
        {
            Reset();

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
                    UpdateLabelText(lblStatus, "The file could not be uploaded. The following error occured: " + ex.Message);
                }

                Result result = new Result();

                try
                {
                    result = customVision.MakePrediction(path);

                    UpdateLabelText(lblRecognizedAs, "Object recognized as: <strong>" + result.Name + "</strong>");

                    if (result.Type == ResultType.Barcode)
                    {
                        BarcodeDecoder barcodeDecoder = new BarcodeDecoder();

                        Barcode barcode = barcodeDecoder.Decode(path);

                        if (barcode != null)
                            googleBooksAPI.CreateResultsTable(googleBooksAPI.GetBookDetailsFromISBN(barcode.Text), tblResults);
                        else
                            UpdateLabelText(lblStatus, "Unable to decode barcode. Please try again.");

                        // Eventually, should be able to move this from here to after this if block
                        customVision.TrainModel(result, path);
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
                                UpdateLabelText(lblStatus, "No text was found in the image uploaded. Please try again.");
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
                    JObject exContent = JObject.Parse(ex.Response.Content);

                    if (exContent["code"].ToString().Equals(ERROR_IMAGESIZE))
                        UpdateLabelText(lblStatus, MESSAGE_IMAGESIZE);
                }
                catch (Exception ex)
                {
                    UpdateLabelText(lblStatus, ex + "<br />" + ex.Message + "<br />" + ex.InnerException);
                }

                File.Delete(path);
            }
            else
                UpdateLabelText(lblStatus, "Please select an image before clicking <strong><i>Recognize</i></strong>");

            ctrlFileUpload.Dispose();
        }

        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            // Do stuff?

            // customVision.TrainModel(result); 
        }

        protected void UpdateLabelText(Label label, string newText)
        {
            label.Text = "<p>" + newText + "</p><br />";
        }

        protected void Reset()
        {
            lblStatus.Text = "";
            lblRecognizedAs.Text = "";

            tblObjectResults.Visible = false;

            cllItemScanned.Text = "";
            cllConfidence.Text = "";

            tblResults.Rows.Clear();
        }
    }
}