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
        BookRecognizer bookRecognizer = new BookRecognizer();

        protected List<Dictionary<string, string>> booksList;

        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "GoogleServiceAccount.json");

            if (!String.IsNullOrEmpty(Request.QueryString["isbn"]))
                bookRecognizer.FormatBookResultsForConfirmation(googleBooksAPI.GetBookDetailsFromISBN(Request.QueryString["isbn"]), tblResults, this);
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
                            bookRecognizer.FormatBookResultsForConfirmation(googleBooksAPI.GetBookDetailsFromISBN(barcode.Text), tblResults, this);
                        else
                            UpdateLabelText(lblStatus, "Unable to decode barcode. Please try again.");

                        // Eventually, should be able to move this from here to after this if block
                        customVision.TrainModel(result, path);
                    }
                    else if (result.Type == ResultType.Other)
                    {
                        if (result.Name.Equals("Book"))
                        {
                            string imageText = bookRecognizer.ReadTextFromImage(path);

                            if (!String.IsNullOrWhiteSpace(imageText))
                            {
                                List<Dictionary<string, string>> bookDetails = googleBooksAPI.GetBookDetailsFromText(imageText, ConfigurationManager.AppSettings["GoogleBooksAPIMaxResults"]);

                                if (bookDetails != null)
                                {
                                    bookRecognizer.FormatBookResultsForSelection(bookDetails, tblResults);

                                    btnBookNotFound.Visible = true;
                                    lblNewLines.Visible = true;
                                }
                                else
                                    UpdateLabelText(lblStatus, "Unable to recognize book cover. Please upload a different image or click the link above to add it manually");
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

                    lblStatus.Visible = true;
                }
                catch (Exception ex)
                {
                    UpdateLabelText(lblStatus, ex + "<br />" + ex.Message + "<br />" + ex.InnerException);
                    lblStatus.Visible = true;
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
            label.Visible = true;
        }

        protected void Reset()
        {
            pnlRecognizedAs.Visible = false;
            lblNewLines.Visible = false;
            btnBookNotFound.Visible = false;
            lblStatus.Visible = true;
            tblObjectResults.Visible = false;

            lblStatus.Text = "";
            cllItemScanned.Text = "";
            cllConfidence.Text = "";

            tblResults.Rows.Clear();
        }

        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            // Do stuff?

            // customVision.TrainModel(result); 
        }

        public void btnConfirmBook_Click(object sender, EventArgs e)
        {
            Button btnClicked = sender as Button;

            AddToDB(btnClicked.CommandName);
        }

        protected void AddToDB(string resultText)
        {
            string[] details = resultText.Split(new string[] { "|||" }, StringSplitOptions.None);

            string isbn = details[0];
            string title = details[1];
            string authors = details[2];
            string publisher = details[3];
            string publishDate = details[4];
            string genre = details[5];

            // ADD TO DATABASE HERE

            Reset();
            pnlRecognition.Visible = false;
            pnlConfirmation.Visible = true;

            lblConfirmation.Text = String.Format("<strong>ISBN:</strong> {0}<br /><strong>Title:</strong> {1}<br /><strong>Author(s):</strong> {2}<br /><strong>Publisher:</strong> {3}<br /><strong>Publish Date:</strong> {4}<br /><strong>Genre:</strong> {5}", isbn, title, authors, publisher, publishDate, genre);
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
            rowOtherItemType.Visible = false;
            rowOtherItemDescription.Visible = false;
            rowOtherItemDetails.Visible = false;
            rowISBN.Visible = false;
            rowTitle.Visible = false;
            rowAuthors.Visible = false;
            rowPublisher.Visible = false;
            rowGenre.Visible = false;
            rowBrand.Visible = false;
            rowClothingType.Visible = false;
            rowClothingSubType.Visible = false;
            rowClothingSize.Visible = false;
            rowClothingColour.Visible = false;
            rowRating.Visible = false;
            rowAlbum.Visible = false;
            rowArtistBand.Visible = false;

            List<string> itemTypes = new List<string>();

            // Maybe do a DB call and get a list of types/categories and add them to the list instead of hardcoding here.
            // That will make it difficult further down in "ddlItemType_SelectedIndexChanged()"
            itemTypes.Add("Select the type of item");
            itemTypes.Add("Book");
            itemTypes.Add("Clothing");
            itemTypes.Add("DVD");
            itemTypes.Add("CD");
            itemTypes.Add("Vinyl");
            itemTypes.Add("Other");

            ddlItemType.DataSource = itemTypes;
            ddlItemType.DataBind();

            ddlItemType.SelectedValue = "Select the type of item";
        }

        protected void ddlItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Reset();

            ddlItemType.Items.Remove("Select the type of item");

            if (ddlItemType.SelectedValue.Equals("Book"))
            {
                // Hide all non-book related fields
                rowOtherItemType.Visible = false;
                rowOtherItemDescription.Visible = false;
                rowOtherItemDetails.Visible = false;
                rowBrand.Visible = false;
                rowClothingType.Visible = false;
                rowClothingSubType.Visible = false;
                rowClothingSize.Visible = false;
                rowClothingColour.Visible = false;
                rowRating.Visible = false;
                rowAlbum.Visible = false;
                rowArtistBand.Visible = false;

                // Show all book related fields
                rowISBN.Visible = true;
                rowTitle.Visible = true;
                rowAuthors.Visible = true;
                rowPublisher.Visible = true;
                rowGenre.Visible = true;
            }
            else if (ddlItemType.SelectedValue.Equals("Clothing"))
            {
                List<string> clothingTypes = new List<string>();

                // Same thing with these ones as the Item Types
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

                // Hide all non-clothing related fields
                rowOtherItemType.Visible = false;
                rowOtherItemDescription.Visible = false;
                rowOtherItemDetails.Visible = false;
                rowISBN.Visible = false;
                rowTitle.Visible = false;
                rowAuthors.Visible = false;
                rowPublisher.Visible = false;
                rowGenre.Visible = false;
                rowRating.Visible = false;
                rowAlbum.Visible = false;
                rowArtistBand.Visible = false;

                // Show all clothing related fields
                rowBrand.Visible = true;
                rowClothingType.Visible = true;
                rowClothingSubType.Visible = false;
                rowClothingSize.Visible = true;
                rowClothingColour.Visible = true;
            }
            else if (ddlItemType.SelectedValue.Equals("DVD"))
            {
                // Hide all non-DVD related fields
                rowISBN.Visible = false;
                rowAuthors.Visible = false;
                rowPublisher.Visible = false;
                rowBrand.Visible = false;
                rowClothingType.Visible = false;
                rowClothingSubType.Visible = false;
                rowClothingSize.Visible = false;
                rowClothingColour.Visible = false;
                rowOtherItemType.Visible = false;
                rowOtherItemDescription.Visible = false;
                rowOtherItemDetails.Visible = false;
                rowAlbum.Visible = false;
                rowArtistBand.Visible = false;

                // Show all DVD related fields
                rowTitle.Visible = true;
                rowGenre.Visible = true;
                rowRating.Visible = true;
            }
            else if (ddlItemType.SelectedValue.Equals("CD") || ddlItemType.SelectedValue.Equals("Vinyl"))
            {
                // Hide all non-CD related fields
                rowISBN.Visible = false;
                rowTitle.Visible = false;
                rowAuthors.Visible = false;
                rowPublisher.Visible = false;                
                rowBrand.Visible = false;
                rowClothingType.Visible = false;
                rowClothingSubType.Visible = false;
                rowClothingSize.Visible = false;
                rowClothingColour.Visible = false;
                rowRating.Visible = false;
                rowOtherItemType.Visible = false;
                rowOtherItemDescription.Visible = false;
                rowOtherItemDetails.Visible = false;

                // Show all CD related fields
                rowAlbum.Visible = true;
                rowArtistBand.Visible = true;
                rowGenre.Visible = true;
            }
            else if (ddlItemType.SelectedValue.Equals("Other"))
            {
                // Hide all non-other related fields
                rowISBN.Visible = false;
                rowTitle.Visible = false;
                rowAuthors.Visible = false;
                rowPublisher.Visible = false;
                rowGenre.Visible = false;
                rowBrand.Visible = false;
                rowClothingType.Visible = false;
                rowClothingSubType.Visible = false;
                rowClothingSize.Visible = false;
                rowClothingColour.Visible = false;
                rowRating.Visible = false;
                rowAlbum.Visible = false;
                rowArtistBand.Visible = false;

                // Show all related fields
                rowOtherItemType.Visible = true;
                rowOtherItemDescription.Visible = true;
                rowOtherItemDetails.Visible = true;
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
                        txtTitle.Text = book["Title"];
                        txtAuthors.Text = currentAuthors;
                        txtPublisher.Text = book["Publisher"];
                        txtGenre.Text = book["Genre"];

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

            
            if (ddlItemType.SelectedValue.Equals("Book"))
            {
                if (ValidateBookForm())
                {
                    pnlManual.Visible = false;
                    pnlConfirmation.Visible = true;

                    string isbn = txtISBN.Text;
                    string title = txtTitle.Text;
                    string authors = txtAuthors.Text;
                    string publisher = txtPublisher.Text;
                    string genre = txtGenre.Text;
                    string type = ddlItemType.SelectedValue;

                    // Maybe don't bother with getting results from the API. If they enter manually we should assume it's a real book?
                    List<Dictionary<string, string>> bookDetails = googleBooksAPI.GetBookDetailsFromManualForm(isbn, title, authors, publisher);

                    if (bookDetails != null)
                        bookRecognizer.FormatBookResultsForConfirmation(bookDetails, tblResults, this);

                    // Either do a confirmation page or add to DB and then show results to user ??

                }
                else
                    UpdateLabelText(lblStatus, "Please fill in <strong>Title</strong> and <strong>Author(s)</strong> at the minimum.");
            }
            else if (ddlItemType.SelectedValue.Equals("Clothing"))
            {
                if (ValidateClothingForm())
                {
                    pnlManual.Visible = false;
                    pnlConfirmation.Visible = true;

                    string brand = txtBrand.Text;
                    string type = ddlClothingType.SelectedValue;
                    string subType = ddlClothingSubType.SelectedValue;
                    string size = txtClothingSize.Text;
                    string colour = txtClothingColour.Text;

                    // Add to DB and show results to user

                }
                else
                    UpdateLabelText(lblStatus, "Please fill in <strong>Clothing Type</strong> and <strong>Clothing SubType</strong> at the minimum.");
            }
            else if (ddlItemType.SelectedValue.Equals("DVD"))
            {
                if (ValidateDVDForm())
                {
                    pnlManual.Visible = false;
                    pnlConfirmation.Visible = true;

                    string title = txtTitle.Text;
                    string genre = txtGenre.Text;
                    string rating = txtRating.Text;

                    // Add to DB and show results to user

                }
                else
                    UpdateLabelText(lblStatus, "Please fill in <strong>Title</strong> at the minimum.");
            }
            else if (ddlItemType.SelectedValue.Equals("CD") | ddlItemType.SelectedValue.Equals("Vinyl"))
            {
                if (ValidateCDVinylForm())
                {
                    pnlManual.Visible = false;
                    pnlConfirmation.Visible = true;

                    string artistBand = txtArtistBand.Text;
                    string album = txtAlbum.Text;
                    string genre = txtGenre.Text;

                    // Add to DB and show results to user

                }
                else
                    UpdateLabelText(lblStatus, "Please fill in <strong>Artist/Band Name</strong> and <strong>Album Name</strong> at the minimum.");
            }
            else if (ddlItemType.SelectedValue.Equals("Other"))
            {
                if (ValidateOtherForm())
                {
                    string type = txtOtherItemType.Text;
                    string description = txtOtherItemDescription.Text;
                    string details = txtOtherItemDetails.Text;
                    
                    // Add to DB and show results to user
                }
                else
                    UpdateLabelText(lblStatus, "Please fill in <strong>Item Type</strong> and <strong>Description</strong> at the minimum.");
            }
            else if (ddlItemType.SelectedValue.Equals("Select the type of item"))
                UpdateLabelText(lblStatus, "Please select the <strong>Item Type</strong>.");
        }

        protected bool ValidateBookForm()
        {
            return !String.IsNullOrEmpty(txtTitle.Text) && !String.IsNullOrEmpty(txtAuthors.Text);
        }

        protected bool ValidateClothingForm()
        {
            return !ddlClothingType.SelectedValue.Equals("Select the type of clothing") && !ddlClothingSubType.SelectedValue.Equals("Select the most suitable subtype");
        }

        protected bool ValidateDVDForm()
        {
            return !String.IsNullOrEmpty(txtTitle.Text);
        }

        protected bool ValidateCDVinylForm()
        {
            return !String.IsNullOrEmpty(txtArtistBand.Text) && !String.IsNullOrEmpty(txtAlbum.Text);
        }

        protected bool ValidateOtherForm()
        {
            return !String.IsNullOrEmpty(txtOtherItemType.Text) && !String.IsNullOrEmpty(txtOtherItemDescription.Text);
        }
    }
}