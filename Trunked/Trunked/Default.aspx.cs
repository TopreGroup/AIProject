using System;
using System.Configuration;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using ImageResizer;

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

        protected List<Dictionary<string, string>> booksList;

        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "GoogleServiceAccount.json");

            if (!String.IsNullOrEmpty(Request.QueryString["isbn"]))
                googleBooksAPI.CreateResultsTable(googleBooksAPI.GetBookDetailsFromISBN(Request.QueryString["isbn"]), tblResults);
        }

        protected void btnRecognize_Click(object sender, EventArgs e)
        {
            Reset();

            if (ctrlFileUpload.HasFile)
            {
                string path = "";

                try
                {
                    string fileName = Path.GetFileName(ctrlFileUpload.FileName);

                    path = Server.MapPath("~/temp/") + fileName;

                    ctrlFileUpload.SaveAs(path);

                    // Resizes the image to a better size for the decoder and also for the model
                    ImageBuilder.Current.Build(path, path, new ResizeSettings("width=768&height=1024"));
                }
                catch (Exception ex)
                {
                    UpdateLabelText(lblStatus, "The file could not be uploaded. The following error occured: " + ex.Message);
                }
                
                Result result = new Result();

                try
                {
                    result = customVision.MakePrediction(path);

                    lblRecognizedAs.Text = "<br />Object recognized as: <strong>" + result.Name + "</strong>";
                    pnlRecognizedAs.Visible = true;

                    if (result.Type == ResultType.Barcode)
                    {
                        Barcode barcode = BarcodeDecoder.Decode(path);

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
                            btnBookNotFound.Visible = true;
                            lblNewLines.Visible = true;

                            string imageText = BookRecognizer.ReadTextFromImage(path);

                            if (!String.IsNullOrWhiteSpace(imageText))
                            {
                                List<Dictionary<string, string>> bookDetails = googleBooksAPI.GetBookDetailsFromText(imageText, ConfigurationManager.AppSettings["GoogleBooksAPIMaxResults"]);

                                if (bookDetails != null)
                                    BookRecognizer.FormatBookResultsForSelection(bookDetails, tblResults);
                            }
                            else
                                UpdateLabelText(lblStatus, "Unable to recognize book cover. Please try again.");
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

            if (!String.IsNullOrEmpty(lblStatus.Text))
                lblStatus.Visible = true;

            ctrlFileUpload.Dispose();
        }

        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            // Do stuff?

            // customVision.TrainModel(result); 
        }

        protected void lnkbtnManualInput_Click(object sender, EventArgs e)
        {
            PrepareManualForm(false);
        }

        protected void btnBookNotFound_Click(object sender, EventArgs e)
        {
            PrepareManualForm(true);
        }

        protected void PrepareManualForm(bool book)
        {
            pnlRecognition.Visible = false;
            pnlManual.Visible = true;

            List<string> types = new List<string>();

            // Do a DB call and get a list of types/categories and add them to the list instead of hardcoding here
            types.Add("Select the type of item");
            types.Add("Book");
            types.Add("Clothing");
            types.Add("DVD");
            types.Add("CD");
            types.Add("Vinyl");

            ddlItemType.DataSource = types;
            ddlItemType.DataBind();

            if (book)
            {
                ddlItemType.SelectedValue = "Book";

            }
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

        protected void txtISBN_TextChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtISBN.Text))
            {                
                booksList = googleBooksAPI.GetBookDetailsFromISBN(txtISBN.Text);

                if (booksList != null)
                {
                    string authors = booksList[0]["Author(s)"].Replace("<br />", ",");
                    authors = authors.Substring(0, authors.Length - 1);

                    lblBookSuggestion.Text = "Is your book <strong>" + booksList[0]["Title"] + "</strong> by <strong>" + authors + "</strong>?";

                    btnYes.CommandName = booksList[0]["Title"] + "|||" + authors;

                    pnlSuggestion.Visible = true;
                }
            }
        }

        protected void btnYes_Click(object sender, EventArgs e)
        {
            string[] splitBook = btnYes.CommandName.Split(new string[] { "|||" }, StringSplitOptions.None);

            string correctTitle = splitBook[0];
            string correctAuthors = splitBook[1];

            pnlSuggestion.Visible = false;

            booksList = googleBooksAPI.GetBookDetailsFromISBN(txtISBN.Text);

            if (booksList != null)
            {
                foreach (Dictionary<string, string> book in booksList)
                {
                    string currentAuthors = book["Author(s)"].Replace("<br />", ",");
                    currentAuthors = currentAuthors.Substring(0, currentAuthors.Length - 1);

                    if (book["Title"].Equals(correctTitle) && currentAuthors.Equals(correctAuthors))
                    {
                        txtTitle.Text = book["Title"];
                        txtAuthors.Text = currentAuthors;
                        txtPublisher.Text = book["Publisher"];

                        break;
                    }
                }
            }
        }

        protected void btnNo_Click(object sender, EventArgs e)
        {
            pnlSuggestion.Visible = false;
        }

        protected void btnManualSubmit_Click(object sender, EventArgs e)
        {
            pnlManual.Visible = false;
            pnlConfirmation.Visible = true;

            string isbn = txtISBN.Text;
            string title = txtTitle.Text;
            string authors = txtAuthors.Text;
            string publisher = txtPublisher.Text;
            string type = ddlItemType.SelectedValue;

            btnBookNotFound.Visible = true;
            lblNewLines.Visible = true;

            List<Dictionary<string, string>> bookDetails = googleBooksAPI.GetBookDetailsFromText(title + authors + publisher, "1");

            if (bookDetails != null)
            {
                BookRecognizer.FormatBookResultsForSelection(bookDetails, tblResults);
            }

            // Either do a confirmation page or add to DB and then show results to user


        }
    }
}