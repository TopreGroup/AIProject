using System;
using System.Configuration;
using System.IO;
using System.Web.UI;
using System.Collections.Generic;

namespace Trunked
{
    public partial class _Default : Page
    {
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
            tblObjectResults.Visible = false;

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
                    result = customVision.MakePrediction(path);

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
    }
}