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

        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            // Do stuff?

            // customVision.TrainModel(result); 
        }

        protected void lnkbtnManualInput_Click(object sender, EventArgs e)
        {
            Reset();
            PrepareManualForm();
        }

        protected void PrepareManualForm()
        {
            pnlRecognition.Visible = false;
            pnlManual.Visible = true;

            // Hide all fields until user selects the type
            rowISBN.Visible = false;
            rowBookTitle.Visible = false;
            rowBookAuthors.Visible = false;
            rowBookPublisher.Visible = false;
            rowClothingBrand.Visible = false;
            rowClothingType.Visible = false;
            rowClothingSubType.Visible = false;
            rowClothingSize.Visible = false;

            List<string> itemTypes = new List<string>();

            // Maybe do a DB call and get a list of types/categories and add them to the list instead of hardcoding here.
            // That will make it difficult further down in "ddlItemType_SelectedIndexChanged()"
            itemTypes.Add("Select the type of item");
            itemTypes.Add("Book");
            itemTypes.Add("Clothing");
            itemTypes.Add("DVD");
            itemTypes.Add("CD");
            itemTypes.Add("Vinyl");

            ddlItemType.DataSource = itemTypes;
            ddlItemType.DataBind();

            ddlItemType.SelectedValue = "Select the type of item";

            List<string> clothingTypes = new List<string>();

            // Same thing with these ones
            clothingTypes.Add("Select the type of clothing");
            clothingTypes.Add("Pants");
            clothingTypes.Add("Shirts");
            clothingTypes.Add("Dresses/Skirts");
            clothingTypes.Add("Jumpers/Coats/Jackets");
            clothingTypes.Add("Shoes");
            clothingTypes.Add("Socks");
            clothingTypes.Add("Hats");
            clothingTypes.Add("Jewellery");

            ddlClothingType.DataSource = clothingTypes;
            ddlClothingType.DataBind();

            ddlClothingType.SelectedValue = "Select the type of clothing";            
        }

        protected void ddlItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ddlItemType.Items.Remove("Select the type of item");

            if (ddlItemType.SelectedValue.Equals("Book"))
            {
                // Hide all non-book related fields
                rowClothingBrand.Visible = false;
                rowClothingType.Visible = false;
                rowClothingSize.Visible = false;
                rowClothingSubType.Visible = false;

                // Show all book related fields
                rowISBN.Visible = true;
                rowBookTitle.Visible = true;
                rowBookAuthors.Visible = true;
                rowBookPublisher.Visible = true;
            }
            else if (ddlItemType.SelectedValue.Equals("Clothing"))
            {
                // Hide all non-book clothing fields
                rowISBN.Visible = false;
                rowBookTitle.Visible = false;
                rowBookAuthors.Visible = false;
                rowBookPublisher.Visible = false;

                // Show all book clothing fields
                rowClothingBrand.Visible = true;
                rowClothingType.Visible = true;
                rowClothingSubType.Visible = false;
                rowClothingSize.Visible = true;
            }
        }

        protected void ddlClothingType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ddlClothingType.Items.Remove("Select the type of clothing");

            rowClothingSubType.Visible = true;

            // Yep... and same thing with all of these ones

            if (ddlClothingType.SelectedValue.Equals("Pants"))
            {
                List<string> pantsSubTypes = new List<string>();

                pantsSubTypes.Add("Select the most suitable subtype");
                pantsSubTypes.Add("Jeans");
                pantsSubTypes.Add("Trousers");
                pantsSubTypes.Add("Tracksuit Pants");
                pantsSubTypes.Add("Shorts");
                pantsSubTypes.Add("Short Shorts");
                pantsSubTypes.Add("Leggings");

                ddlClothingSubType.DataSource = pantsSubTypes;
            }
            else if (ddlClothingType.SelectedValue.Equals("Shirts"))
            {
                List<string> shirtsSubTypes = new List<string>();

                shirtsSubTypes.Add("Select the most suitable subtype");
                shirtsSubTypes.Add("T-Shirt");
                shirtsSubTypes.Add("Longsleeve T-Shirt");
                shirtsSubTypes.Add("Business shirt");
                shirtsSubTypes.Add("Polo");
                shirtsSubTypes.Add("Singlet");
                shirtsSubTypes.Add("Tanktop");

                ddlClothingSubType.DataSource = shirtsSubTypes;
            }
            else if (ddlClothingType.SelectedValue.Equals("Dresses/Skirts"))
            {
                List<string> dressesSkirtsSubTypes = new List<string>();

                dressesSkirtsSubTypes.Add("Select the most suitable subtype");
                dressesSkirtsSubTypes.Add("Dress");
                dressesSkirtsSubTypes.Add("Sundress");
                dressesSkirtsSubTypes.Add("Maxi");
                dressesSkirtsSubTypes.Add("Wedding Dress");
                dressesSkirtsSubTypes.Add("A-Line Skirt");
                dressesSkirtsSubTypes.Add("Pencil Skirt");

                ddlClothingSubType.DataSource = dressesSkirtsSubTypes;
            }
            else if (ddlClothingType.SelectedValue.Equals("Jumpers/Coats/Jackets"))
            {
                List<string> jumpersCoatsJacketsSubTypes = new List<string>();

                jumpersCoatsJacketsSubTypes.Add("Select the most suitable subtype");
                jumpersCoatsJacketsSubTypes.Add("Hoodie");
                jumpersCoatsJacketsSubTypes.Add("Sweater");
                jumpersCoatsJacketsSubTypes.Add("Windcheater");
                jumpersCoatsJacketsSubTypes.Add("Raincoat");
                jumpersCoatsJacketsSubTypes.Add("Leather Jacket");
                jumpersCoatsJacketsSubTypes.Add("Suit Jacket");
                jumpersCoatsJacketsSubTypes.Add("Trenchcoat");
                jumpersCoatsJacketsSubTypes.Add("Duster");

                ddlClothingSubType.DataSource = jumpersCoatsJacketsSubTypes;
            }
            else if (ddlClothingType.SelectedValue.Equals("Shoes"))
            {
                List<string> shoesSubTypes = new List<string>();

                shoesSubTypes.Add("Select the most suitable subtype");
                shoesSubTypes.Add("Sneakers");
                shoesSubTypes.Add("Heels");
                shoesSubTypes.Add("Boots");
                shoesSubTypes.Add("Wedges");
                shoesSubTypes.Add("Clogs");
                shoesSubTypes.Add("Loafers");
                shoesSubTypes.Add("Slippers");

                ddlClothingSubType.DataSource = shoesSubTypes;
            }
            else if (ddlClothingType.SelectedValue.Equals("Shoes"))
            {
                List<string> shoesSubTypes = new List<string>();

                shoesSubTypes.Add("Select the most suitable subtype");
                shoesSubTypes.Add("Sneakers");
                shoesSubTypes.Add("Heels");
                shoesSubTypes.Add("Boots");
                shoesSubTypes.Add("Wedges");
                shoesSubTypes.Add("Clogs");
                shoesSubTypes.Add("Loafers");
                shoesSubTypes.Add("Slippers");

                ddlClothingSubType.DataSource = shoesSubTypes;
            }
            else if (ddlClothingType.SelectedValue.Equals("Socks"))
            {
                List<string> socksSubTypes = new List<string>();

                socksSubTypes.Add("Select the most suitable subtype");
                socksSubTypes.Add("Normal Socks");
                socksSubTypes.Add("Ankle Socks");
                socksSubTypes.Add("Thigh-High");
                socksSubTypes.Add("Toe Socks");

                ddlClothingSubType.DataSource = socksSubTypes;
            }
            else if (ddlClothingType.SelectedValue.Equals("Hats"))
            {
                List<string> hatsSubTypes = new List<string>();

                hatsSubTypes.Add("Select the most suitable subtype");
                hatsSubTypes.Add("Baseball Cap");
                hatsSubTypes.Add("Bowler");
                hatsSubTypes.Add("Bucket");
                hatsSubTypes.Add("Sun");
                hatsSubTypes.Add("Beanie");
                hatsSubTypes.Add("Cowboy");
                hatsSubTypes.Add("Beret");

                ddlClothingSubType.DataSource = hatsSubTypes;
            }
            else if (ddlClothingType.SelectedValue.Equals("Jewellery"))
            {
                List<string> jewellerySubTypes = new List<string>();

                jewellerySubTypes.Add("Select the most suitable subtype");
                jewellerySubTypes.Add("Earrings");
                jewellerySubTypes.Add("Necklace");
                jewellerySubTypes.Add("Anklet");
                jewellerySubTypes.Add("Cufflink");
                jewellerySubTypes.Add("Chain");
                jewellerySubTypes.Add("Pin");

                ddlClothingSubType.DataSource = jewellerySubTypes;
            }

            ddlClothingSubType.DataBind();

            ddlClothingSubType.SelectedValue = "Select the most suitable subtype";
        }

        protected void ddlClothingSubType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ddlClothingSubType.Items.Remove("Select the most suitable subtype");
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
            Reset();

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
                        txtBookTitle.Text = book["Title"];
                        txtBookAuthors.Text = currentAuthors;
                        txtBookPublisher.Text = book["Publisher"];

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
            Reset();

            if (ValidateBookForm())
            {
                pnlManual.Visible = false;
                pnlConfirmation.Visible = true;

                string isbn = txtISBN.Text;
                string title = txtBookTitle.Text;
                string authors = txtBookAuthors.Text;
                string publisher = txtBookPublisher.Text;
                string type = ddlItemType.SelectedValue;

                btnBookNotFound.Visible = true;
                lblNewLines.Visible = true;

                List<Dictionary<string, string>> bookDetails = googleBooksAPI.GetBookDetailsFromManualForm(isbn, title, authors, publisher);

                if (bookDetails != null)
                {
                    BookRecognizer.FormatBookResultsForConfirmation(bookDetails, tblResults);
                }

                // Either do a confirmation page or add to DB and then show results to user

            }
            else
                UpdateLabelText(lblStatus, "Please fill in <strong>Title</strong> and <strong>Author(s)</strong> at the minimum.");
        }

        protected bool ValidateBookForm()
        {
            return !String.IsNullOrEmpty(txtBookTitle.Text) && !String.IsNullOrEmpty(txtBookAuthors.Text);
        }
    }
}