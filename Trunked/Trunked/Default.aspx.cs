using System;
using System.Configuration;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using ImageResizer;
using System.Linq;

namespace Trunked
{
    public partial class _Default : Page
    {
        // This probably isn't necessary anymore since it is now resizing the images
        protected readonly string ERROR_IMAGESIZE = "BadRequestImageSizeBytes";
        protected readonly string MESSAGE_IMAGESIZE = "Image file is too large. Please choose an image smaller than 4MB";

        protected string trainingKey = ConfigurationManager.AppSettings["CustomVisionTrainingKey"];
        protected string predictionKey = ConfigurationManager.AppSettings["CustomVisionPredictionKey"];
        protected Guid projectID = new Guid(ConfigurationManager.AppSettings["CustomVisionProjectID"]);

        protected GoogleBooksAPI googleBooksAPI = new GoogleBooksAPI();
        protected CustomVision customVision = new CustomVision();
        BookRecognizer bookRecognizer = new BookRecognizer();
        protected DBConnection db = new DBConnection();

        protected string recognizedText = "";
        protected bool resultsFound;
        protected List<Dictionary<string, string>> bookDetailsList;

        protected string imagePath = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "GoogleServiceAccount.json");

            if (!String.IsNullOrEmpty(Request.QueryString["isbn"]))
            {
                List<Dictionary<string, string>> books = googleBooksAPI.GetBookDetailsFromISBN(Request.QueryString["isbn"]);

                if (books != null)
                    bookRecognizer.FormatBookResultsForConfirmation(books, tblResults, this);
            }
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
                    imagePath = path;

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
                        {
                            List<Dictionary<string, string>> books = googleBooksAPI.GetBookDetailsFromISBN(barcode.Text);

                            if (books != null)
                                bookRecognizer.FormatBookResultsForConfirmation(books, tblResults, this);
                            else
                                UpdateLabelText(lblStatus, "No similar books found. Please try again.");                            
                        }
                        else
                            UpdateLabelText(lblStatus, "Unable to decode barcode. Please try again.");

                        try
                        { 
                            customVision.TrainModel(path, "Barcode");
                        }
                        catch (Exception ex)
                        {
                            UpdateLabelText(lblStatus, "An error occurred while trying to train the model.<br />" + ex.Message);
                        }
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

                            btnConfirm.Text = "Confirm: " + result.Name;

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
            }
            else
                UpdateLabelText(lblStatus, "Please select an image before clicking <strong><i>Recognize</i></strong>");

            lblStatus.Visible = String.IsNullOrEmpty(lblStatus.Text) ? false : true;

            ctrlFileUpload.Dispose();
        }

        protected void UpdateLabelText(Label label, string newText)
        {
            label.Text = "<p>" + newText + "</p>";
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

        protected void btnConfirmObject_Click(object sender, EventArgs e)
        {
            Button btnClicked = sender as Button;

            string item = btnClicked.Text.Substring(9); // 9 = "Confirm: "
            string clothingType = "";

            DBResult itemTypes = db.GetItemTypes();

            if (!itemTypes.Result.Contains(item))
            {
                DBResult clothingTypes = db.GetClothingTypes();

                if (clothingTypes.Result.Contains(item))
                {
                    clothingType = item;
                    item = "Clothing";
                }
                else
                    item = "Other";
            }

            PrepareManualForm(item, clothingType);
        }

        public void btnConfirmBook_Click(object sender, EventArgs e)
        {
            Button btnClicked = sender as Button;

            AddBookToDB(btnClicked.CommandName);
        }

        protected void AddBookToDB(string resultText)
        {
            string[] details = resultText.Split(new string[] { "|||" }, StringSplitOptions.None);

            string isbn = details[0];
            string title = details[1];
            string authors = details[2].Replace("<br />", ", ");
            authors = authors.Substring(0, authors.Length - 2);
            string publisher = details[3];
            string publishDate = details[4];
            string genre = details[5];

            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "ISBN", isbn },
                { "Title", title },
                { "Authors", authors },
                { "Genre", genre },
                { "Publisher", publisher },
                { "PublishDate", publishDate }
            };

            DBResult res = db.InsertBook(parameters);

            if (res.Code != -1)
            {
                Reset();
                pnlRecognition.Visible = false;
                pnlConfirmation.Visible = true;

                lblConfirmation.Text = String.Format("<strong>ISBN:</strong> {0}<br /><strong>Title:</strong> {1}<br /><strong>Author(s):</strong> {2}<br /><strong>Publisher:</strong> {3}<br /><strong>Publish Date:</strong> {4}<br /><strong>Genre:</strong> {5}", isbn, title, authors, publisher, publishDate, genre);

                GetImageAndTrainModel("Book");
            }
            else
                UpdateLabelText(lblStatus, "An error occurred while trying to add the book.<br />" + res.ErrorMessage);
        }

        protected void lnkbtnManualInput_Click(object sender, EventArgs e)
        {
            string item = "";
            Reset();

            if (sender.GetType().Name.Equals("Button"))
            {
                Button btnClicked = sender as Button;

                item = btnClicked.Text.Equals("Book not here?") ? "Book" : "";
            }

            PrepareManualForm(item, "");
        }

        protected void PrepareManualForm(string currentType, string clothingType)
        {
            pnlRecognition.Visible = false;
            pnlManual.Visible = true;

            // Hide all fields until user selects the type
            HideAllManualFields();

            List<string> itemTypes = new List<string>()
            {
                "Select the type of item"
            };

            DBResult dbItemTypes = db.GetItemTypes();

            if (dbItemTypes.Code != -1)
            {
                dbItemTypes.Result = dbItemTypes.Result.Where(s => !String.IsNullOrEmpty(s)).ToList();

                foreach (string itemType in dbItemTypes.Result)
                    itemTypes.Add(itemType);

                // For now, we'll have these until they are added to the DB
                if (!itemTypes.Contains("Book"))
                    itemTypes.Add("Book");

                if (!itemTypes.Contains("Clothing"))
                    itemTypes.Add("Clothing");

                if (!itemTypes.Contains("DVD"))
                    itemTypes.Add("DVD");

                if (!itemTypes.Contains("CD"))
                    itemTypes.Add("CD");

                if (!itemTypes.Contains("Vinyl"))
                    itemTypes.Add("Vinyl");

                itemTypes.Add("Other");

                ddlItemType.DataSource = itemTypes;
                ddlItemType.DataBind();

                ddlItemType.SelectedValue = "Select the type of item";

                if (itemTypes.Contains(currentType))
                {
                    ddlItemType.SelectedValue = currentType;
                    ddlItemType_SelectedIndexChanged(this, EventArgs.Empty);

                    if (currentType.Equals("Clothing") && !String.IsNullOrEmpty(clothingType))
                    {
                        if (ddlClothingType.Items.FindByText(clothingType) != null)
                        {
                            ddlClothingType.SelectedValue = clothingType;
                            ddlClothingType_SelectedIndexChanged(this, EventArgs.Empty);
                        }
                    }
                }
            }
            else
                UpdateLabelText(lblStatus, "An error occurred while trying to get the item types.<br />" + dbItemTypes.ErrorMessage);
        }

        protected void ddlItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Reset();

            ddlItemType.Items.Remove("Select the type of item");

            HideAllManualFields();

            if (ddlItemType.SelectedValue.Equals("Book"))
                PrepareBookForm();
            else if (ddlItemType.SelectedValue.Equals("Clothing"))
                PrepareClothingForm();
            else if (ddlItemType.SelectedValue.Equals("DVD"))
                PrepareDVDForm();
            else if (ddlItemType.SelectedValue.Equals("CD") || ddlItemType.SelectedValue.Equals("Vinyl"))
                PrepareMusicForm();
            else if (ddlItemType.SelectedValue.Equals("Other"))
                PrepareOtherForm();
        }

        protected void HideAllManualFields()
        {
            rowOtherItemType.Visible = false;
            rowOtherItemDescription.Visible = false;
            rowOtherItemDetails.Visible = false;
            rowISBN.Visible = false;
            rowTitle.Visible = false;
            rowAuthors.Visible = false;
            rowGenre.Visible = false;
            rowPublisher.Visible = false;
            rowPublishDate.Visible = false;
            rowBrand.Visible = false;
            rowClothingType.Visible = false;
            rowClothingSubType.Visible = false;
            rowClothingSize.Visible = false;
            rowClothingColour.Visible = false;
            rowRating.Visible = false;
            rowArtistBand.Visible = false;
        }

        protected void PrepareBookForm()
        {
            rowISBN.Visible = true;
            rowTitle.Visible = true;
            rowAuthors.Visible = true;
            rowGenre.Visible = true;
            rowPublisher.Visible = true;
            rowPublishDate.Visible = true;
        }

        protected void PrepareClothingForm()
        {
            List<string> clothingTypes = new List<string>()
            {
                "Select the type of clothing"
            };

            DBResult dbClothingTypes = db.GetClothingTypes();

            if (dbClothingTypes.Code != -1)
            {
                dbClothingTypes.Result = dbClothingTypes.Result.Where(s => !String.IsNullOrEmpty(s)).ToList();

                foreach (string clothingType in dbClothingTypes.Result)
                    clothingTypes.Add(clothingType);

                // For now, we'll have these until they are added to the DB
                if (!clothingTypes.Contains("Pants"))
                    clothingTypes.Add("Pants");

                if (!clothingTypes.Contains("Shirts"))
                    clothingTypes.Add("Shirts");

                if (!clothingTypes.Contains("Dresses"))
                    clothingTypes.Add("Dresses");

                if (!clothingTypes.Contains("Skirts"))
                    clothingTypes.Add("Skirts");

                if (!clothingTypes.Contains("Jumpers"))
                    clothingTypes.Add("Jumpers");

                if (!clothingTypes.Contains("Coats"))
                    clothingTypes.Add("Coats");

                if (!clothingTypes.Contains("Jackets"))
                    clothingTypes.Add("Jackets");

                if (!clothingTypes.Contains("Shoes"))
                    clothingTypes.Add("Shoes");

                if (!clothingTypes.Contains("Socks"))
                    clothingTypes.Add("Socks");

                if (!clothingTypes.Contains("Hats"))
                    clothingTypes.Add("Hats");

                if (!clothingTypes.Contains("Jewellery"))
                    clothingTypes.Add("Jewellery");

                clothingTypes.Add("Other");

                ddlClothingType.DataSource = clothingTypes;
                ddlClothingType.DataBind();

                ddlClothingType.SelectedValue = "Select the type of clothing";

                // Show all clothing related fields
                rowBrand.Visible = true;
                rowClothingType.Visible = true;
                rowClothingSubType.Visible = false;
                rowClothingSize.Visible = true;
                rowClothingColour.Visible = true;
            }
            else
                UpdateLabelText(lblStatus, "An error occurred while trying to get the clothing types.<br />" + dbClothingTypes.ErrorMessage);
        }

        protected void PrepareMusicForm()
        {
            rowTitle.Visible = true;
            rowArtistBand.Visible = true;
            rowGenre.Visible = true;
        }

        protected void PrepareDVDForm()
        {
            rowTitle.Visible = true;
            rowGenre.Visible = true;
            rowRating.Visible = true;
        }

        protected void PrepareOtherForm()
        {
            rowOtherItemType.Visible = true;
            rowOtherItemDescription.Visible = true;
            rowOtherItemDetails.Visible = true;
        }

        protected void ddlClothingType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ddlClothingType.Items.Remove("Select the type of clothing");

            rowClothingSubType.Visible = true;

            if (!ddlClothingType.SelectedValue.Equals("Other"))
            {
                ddlClothingSubType.Visible = true;
                lblNewLine.Visible = false;
                txtOtherClothingType.Visible = false;
                txtOtherClothingSubType.Visible = false;

                List<string> subTypes = new List<string>()
                {
                    "Select the most suitable subtype"
                };

                DBResult dbSubTypes = db.GetClothingSubTypes(ddlClothingType.SelectedValue);

                if (dbSubTypes.Code != -1)
                {
                    dbSubTypes.Result = dbSubTypes.Result.Where(s => !String.IsNullOrEmpty(s)).ToList();

                    foreach (string subType in dbSubTypes.Result)
                        subTypes.Add(subType);

                    // Again, only keeping these here until the DB has got some values in there
                    if (ddlClothingType.SelectedValue.Equals("Pants"))
                    {
                        subTypes.Add("Jeans");
                        subTypes.Add("Trousers");
                        subTypes.Add("Tracksuit Pants");
                        subTypes.Add("Shorts");
                        subTypes.Add("Short Shorts");
                        subTypes.Add("Leggings");
                    }
                    else if (ddlClothingType.SelectedValue.Equals("Shirts"))
                    {
                        subTypes.Add("T-Shirt");
                        subTypes.Add("Longsleeve T-Shirt");
                        subTypes.Add("Business shirt");
                        subTypes.Add("Polo");
                        subTypes.Add("Singlet");
                        subTypes.Add("Tanktop");
                    }
                    else if (ddlClothingType.SelectedValue.Equals("Dresses"))
                    {
                        subTypes.Add("Dress");
                        subTypes.Add("Sundress");
                        subTypes.Add("Maxi");
                        subTypes.Add("Wedding Dress");                        
                    }
                    else if (ddlClothingType.SelectedValue.Equals("Skirts"))
                    {
                        subTypes.Add("Normal Skirt");
                        subTypes.Add("Short Skirt");
                        subTypes.Add("A-Line Skirt");
                        subTypes.Add("Pencil Skirt");
                    }
                    else if (ddlClothingType.SelectedValue.Equals("Jumpers"))
                    {
                        subTypes.Add("Hoodie");
                        subTypes.Add("Sweater");
                        subTypes.Add("Windcheater");
                    }
                    else if (ddlClothingType.SelectedValue.Equals("Coats"))
                    {
                        subTypes.Add("Raincoat");                       
                        subTypes.Add("Trenchcoat");
                        subTypes.Add("Duster");
                    }
                    else if (ddlClothingType.SelectedValue.Equals("Jackets"))
                    {
                        subTypes.Add("Leather Jacket");
                        subTypes.Add("Suit Jacket");
                    }
                    else if (ddlClothingType.SelectedValue.Equals("Shoes"))
                    {
                        subTypes.Add("Sneakers");
                        subTypes.Add("Heels");
                        subTypes.Add("Boots");
                        subTypes.Add("Wedges");
                        subTypes.Add("Clogs");
                        subTypes.Add("Loafers");
                        subTypes.Add("Slippers");
                    }
                    else if (ddlClothingType.SelectedValue.Equals("Socks"))
                    {
                        subTypes.Add("Normal Socks");
                        subTypes.Add("Ankle Socks");
                        subTypes.Add("Thigh-High");
                        subTypes.Add("Toe Socks");
                    }
                    else if (ddlClothingType.SelectedValue.Equals("Hats"))
                    {
                        subTypes.Add("Baseball Cap");
                        subTypes.Add("Bowler");
                        subTypes.Add("Bucket");
                        subTypes.Add("Sun");
                        subTypes.Add("Beanie");
                        subTypes.Add("Cowboy");
                        subTypes.Add("Beret");
                    }
                    else if (ddlClothingType.SelectedValue.Equals("Jewellery"))
                    {
                        subTypes.Add("Earrings");
                        subTypes.Add("Necklace");
                        subTypes.Add("Anklet");
                        subTypes.Add("Cufflink");
                        subTypes.Add("Chain");
                        subTypes.Add("Pin");
                    }

                    subTypes.Add("Other");

                    ddlClothingSubType.DataSource = subTypes;
                    ddlClothingSubType.DataBind();

                    ddlClothingSubType.SelectedValue = "Select the most suitable subtype";
                }
                else
                    UpdateLabelText(lblStatus, "An error occurred while trying to get the clothing sub-types.<br />" + dbSubTypes.ErrorMessage);
            }
            else
            {
                lblNewLine.Visible = true;
                rowClothingSubType.Visible = true;
                ddlClothingSubType.Visible = false;
                txtOtherClothingType.Visible = true;
                txtOtherClothingSubType.Visible = true;
            }
        }

        protected void ddlClothingSubType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ddlClothingSubType.Items.Remove("Select the most suitable subtype");

            if (ddlClothingSubType.SelectedValue.Equals("Other"))
            {
                lblNewLineSub.Visible = true;
                txtOtherClothingSubType.Visible = true;
            }
            else
            {
                lblNewLineSub.Visible = false;
                txtOtherClothingSubType.Visible = false;
            }
        }

        protected void txtISBN_TextChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtISBN.Text))
            {
                List<Dictionary<string, string>> booksList = googleBooksAPI.GetBookDetailsFromISBN(txtISBN.Text);

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

            List<Dictionary<string, string>> booksList = googleBooksAPI.GetBookDetailsFromISBN(txtISBN.Text);

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
                        txtGenre.Text = book["Genre"];
                        txtPublisher.Text = book["Publisher"];
                        txtPublishDate.Text = book["PublishDate"];

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
                    string isbn = txtISBN.Text;
                    string title = txtTitle.Text;
                    string authors = txtAuthors.Text;
                    string publisher = txtPublisher.Text;
                    string publishDate = txtPublishDate.Text;
                    string genre = txtGenre.Text;
                    string type = ddlItemType.SelectedValue;

                    Dictionary<string, string> parameters = new Dictionary<string, string>()
                    {
                        { "ISBN", isbn },
                        { "Title", title },
                        { "Authors", authors },
                        { "Genre", genre },
                        { "Publisher", publisher },
                        { "PublishDate", publishDate }
                    };

                    DBResult res = db.InsertBook(parameters);

                    if (res.Code != -1)
                    {
                        Reset();
                        pnlManual.Visible = false;
                        pnlConfirmation.Visible = true;

                        lblConfirmation.Text = String.Format("<strong>ISBN:</strong> {0}<br /><strong>Title:</strong> {1}<br /><strong>Author(s):</strong> {2}<br /><strong>Publisher:</strong> {3}<br /><strong>Publish Date:</strong> {4}<br /><strong>Genre:</strong> {5}", isbn, title, authors, publisher, publishDate, genre);

                        GetImageAndTrainModel("Book");
                    }
                    else
                        UpdateLabelText(lblStatus, "An error occurred while trying to add the book.<br />" + res.ErrorMessage);
                }
                else
                    UpdateLabelText(lblStatus, "Please fill in <strong>Title</strong> and <strong>Author(s)</strong> at the minimum.");
            }
            else if (ddlItemType.SelectedValue.Equals("Clothing"))
            {
                if (ValidateClothingForm())
                {
                    string type = ddlClothingType.SelectedValue;
                    string subType = ddlClothingSubType.SelectedValue;
                    string brand = txtBrand.Text;
                    string size = txtClothingSize.Text;
                    string colour = txtClothingColour.Text;

                    Dictionary<string, string> parameters = new Dictionary<string, string>()
                    {
                        { "Type", type },
                        { "SubType", subType },
                        { "Brand", brand },
                        { "Size", size },
                        { "Colour", colour }
                    };

                    DBResult res = db.InsertClothing(parameters);

                    if (res.Code != -1)
                    {
                        Reset();
                        pnlManual.Visible = false;
                        pnlConfirmation.Visible = true;

                        lblConfirmation.Text = String.Format("<strong>Type:</strong> {0}<br /><strong>SubType:</strong> {1}<br /><strong>Brand:</strong> {2}<br /><strong>Size:</strong> {3}<br /><strong>Colour:</strong> {4}", type, subType, brand, size, colour);

                        GetImageAndTrainModel(type);
                    }
                    else
                        UpdateLabelText(lblStatus, "An error occurred while trying to add the item of clothing.<br />" + res.ErrorMessage);
                }
                else
                    UpdateLabelText(lblStatus, "Please fill in <strong>Clothing Type</strong> and <strong>Clothing SubType</strong> at the minimum.");
            }
            else if (ddlItemType.SelectedValue.Equals("DVD"))
            {
                if (ValidateDVDForm())
                {
                    string title = txtTitle.Text;
                    string genre = txtGenre.Text;
                    string rating = txtRating.Text;

                    Dictionary<string, string> parameters = new Dictionary<string, string>()
                    {
                        { "Title", title },
                        { "Genre", genre },
                        { "Rating", rating }
                    };

                    DBResult res = db.InsertDVD(parameters);

                    if (res.Code != -1)
                    {
                        Reset();
                        pnlManual.Visible = false;
                        pnlConfirmation.Visible = true;

                        lblConfirmation.Text = String.Format("<strong>Title:</strong> {0}<br /><strong>Genre:</strong> {1}<br /><strong>Rating:</strong> {2}", title, genre, rating);

                        GetImageAndTrainModel("DVD");
                    }
                    else
                        UpdateLabelText(lblStatus, "An error occurred while trying to add the DVD.<br />" + res.ErrorMessage);
                }
                else
                    UpdateLabelText(lblStatus, "Please fill in <strong>Title</strong> at the minimum.");
            }
            else if (ddlItemType.SelectedValue.Equals("CD") | ddlItemType.SelectedValue.Equals("Vinyl"))
            {
                if (ValidateMusicForm())
                {
                    string title = txtTitle.Text;
                    string musician = txtArtistBand.Text;
                    string genre = txtGenre.Text;

                    Dictionary<string, string> parameters = new Dictionary<string, string>()
                    {
                        { "Title", title },
                        { "Musician", musician },
                        { "Genre", genre }
                    };

                    DBResult res = db.InsertMusic(parameters);

                    if (res.Code != -1)
                    {
                        Reset();
                        pnlManual.Visible = false;
                        pnlConfirmation.Visible = true;

                        lblConfirmation.Text = String.Format("<strong>Title:</strong> {0}<br /><strong>Musician:</strong> {1}<br /><strong>Genre:</strong> {2}", title, musician, genre);

                        GetImageAndTrainModel(ddlItemType.SelectedValue);
                    }
                    else
                        UpdateLabelText(lblStatus, "An error occurred while trying to add the DVD.<br />" + res.ErrorMessage);
                }
                else
                    UpdateLabelText(lblStatus, "Please fill in <strong>Artist/Band Name</strong> and <strong>Album Name</strong> at the minimum.");
            }
            else if (ddlItemType.SelectedValue.Equals("Other"))
            {
                if (ValidateOtherForm())
                {
                    string type = txtOtherItemType.Text;
                    string details = txtOtherItemDetails.Text;
                    string description = txtOtherItemDescription.Text;

                    Dictionary<string, string> parameters = new Dictionary<string, string>()
                    {
                        { "Type", type },
                        { "Details", details },
                        { "Description", description }
                    };

                    DBResult res = db.InsertOther(parameters);

                    if (res.Code != -1)
                    {
                        Reset();
                        pnlManual.Visible = false;
                        pnlConfirmation.Visible = true;

                        lblConfirmation.Text = String.Format("<strong>Type:</strong> {0}<br /><strong>Details:</strong> {1}<br /><strong>Description:</strong> {2}", type, details, description);

                        GetImageAndTrainModel(type);
                    }
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
            if (ddlClothingType.SelectedValue.Equals("Other"))
                return !String.IsNullOrEmpty(txtOtherClothingType.Text) && !String.IsNullOrEmpty(txtOtherClothingSubType.Text);
            else if (!ddlClothingType.SelectedValue.Equals("Other") && ddlClothingSubType.SelectedValue.Equals("Other"))
                return !ddlClothingType.SelectedValue.Equals("Select the type of clothing") && !String.IsNullOrEmpty(txtOtherClothingSubType.Text);
            else
                return !ddlClothingType.SelectedValue.Equals("Select the type of clothing") && !ddlClothingSubType.SelectedValue.Equals("Select the most suitable subtype");
        }

        protected bool ValidateDVDForm()
        {
            return !String.IsNullOrEmpty(txtTitle.Text);
        }

        protected bool ValidateMusicForm()
        {
            return !String.IsNullOrEmpty(txtArtistBand.Text) && !String.IsNullOrEmpty(txtTitle.Text);
        }

        protected bool ValidateOtherForm()
        {
            return !String.IsNullOrEmpty(txtOtherItemType.Text) && !String.IsNullOrEmpty(txtOtherItemDescription.Text);
        }

        public void GetImageAndTrainModel(string tag)
        {
            // Images will always be saved to the temp folder
            string[] images = Directory.GetFiles(Server.MapPath("~/temp/"));

            string imageToTrainPath = "";

            // There should be exactly 2 images in the folder; the dummy image and the actual image to train
            if (images.Length == 2)
                imageToTrainPath = images[0].EndsWith("dummy.jpg") ? images[1] : images[0];

            if (!String.IsNullOrEmpty(imageToTrainPath))
            {
                try
                {
                    customVision.TrainModel(imageToTrainPath, tag);
                }
                catch (Exception ex)
                {
                    UpdateLabelText(lblStatus, "An error occurred while trying to train the model.<br />" + ex.Message);
                }
            }
        }
    }
}