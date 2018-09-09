using System;
using System.IO;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Google.Cloud.Vision.V1;

namespace TrunkedPrototypes
{
    public partial class RecognizeAndPrompt : System.Web.UI.Page
    {
        protected string recognizedText = "";
        protected bool resultsFound;
        protected List<Dictionary<string, string>> bookDetailsList;

        // The max number allowed by the API is 40 for some reason. 
        // Need to figure out what to do if the book doesn't get returned.
        protected int maxResults = 40;

        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "VisionAPIServiceAccount.json");

            if (!String.IsNullOrEmpty(Request.QueryString["isbn"]))
                CreateResultsTable(GetBookDetails(Request.QueryString["isbn"]));
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

                string imageText = ReadTextFromImage(path);

                if (!imageText.Equals("NO TEXT FOUND"))
                {
                    GetBookDetailsFromText(imageText);

                    if (resultsFound)
                        FormatBookResultsForSelection(bookDetailsList);
                }
                else
                    lblResults.Text = imageText;
            }

            ctrlFileUpload.Dispose();
        }

        protected string ReadTextFromImage(string imagePath)
        {
            var client = ImageAnnotatorClient.Create();
            var image = Google.Cloud.Vision.V1.Image.FromFile(imagePath);
            var response = client.DetectText(image);

            foreach (var annotation in response)
            {
                if (!String.IsNullOrEmpty(annotation.Description))
                    return annotation.Description;
            }

            return "NO TEXT FOUND";
        }

        protected void GetBookDetailsFromText(string bookText)
        {
            bookDetailsList = new List<Dictionary<string, string>>();

            string uri = String.Format("https://www.googleapis.com/books/v1/volumes?q={0}&maxResults={1}", bookText, maxResults);

            WebRequest request = WebRequest.Create(uri);
            request.Method = "GET";

            WebResponse response = request.GetResponse();

            string jsonString;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            JObject jsonObj = JObject.Parse(jsonString);

            lblResults.Text += "<br />Number of books found: " + jsonObj["totalItems"].ToString() + "<br />";

            if (jsonObj["totalItems"].ToString().Equals("0"))
                bookDetailsList = null;
            else
                resultsFound = true;

            if (resultsFound)
            {
                for (int i = 0; i < ((JArray)jsonObj["items"]).Count; i++)
                {
                    Dictionary<string, string> bookDetails = new Dictionary<string, string>();

                    bookDetails.Add("Title", jsonObj["items"][i]["volumeInfo"]["title"].ToString());

                    string authors = "";

                    JArray authorArray = (JArray)jsonObj["items"][i]["volumeInfo"]["authors"];

                    if (authorArray != null)
                    {
                        foreach (JToken author in authorArray)
                            authors += author.ToString() + "<br />";
                    }
                    else
                        authors = "Unknown";

                    bookDetails.Add("Author(s)", authors);

                    string isbn = "";

                    JArray industryIdentifiersArray = (JArray)jsonObj["items"][i]["volumeInfo"]["industryIdentifiers"];

                    if (industryIdentifiersArray != null)
                    {
                        foreach (JToken identifier in industryIdentifiersArray)
                        {
                            if (identifier["type"].ToString().Equals("ISBN_13"))
                            {
                                isbn = identifier["identifier"].ToString();
                                break;
                            }

                            if (identifier["type"].ToString().Equals("ISBN_10"))
                            {
                                isbn = identifier["identifier"].ToString();
                                break;
                            }
                        }
                    }

                    bookDetails.Add("ISBN", String.IsNullOrEmpty(isbn) ? "Unknown" : isbn);

                    string thumbnailURI = "No thumbnail found";

                    if (jsonObj["items"][i]["volumeInfo"]["imageLinks"] != null)
                    {
                        if (jsonObj["items"][i]["volumeInfo"]["imageLinks"]["thumbnail"] != null)
                            thumbnailURI = jsonObj["items"][i]["volumeInfo"]["imageLinks"]["thumbnail"].ToString();
                    }

                    bookDetails.Add("Thumbnail", thumbnailURI);

                    bookDetailsList.Add(bookDetails);
                }
            }
        }

        public void FormatBookResultsForSelection(List<Dictionary<string, string>> bookDetailsList)
        {
            List<string> headings = new List<string>()
            {
                "ISBN", "Cover", "Title", "Author(s)"
            };

            TableRow row = new TableRow();
            TableCell cell = new TableCell();

            foreach (string heading in headings)
            {
                cell = new TableCell();

                cell.Controls.Add(new LiteralControl(heading));
                cell.CssClass = "tblCell heading";

                row.Cells.Add(cell);
            }

            tblResults.Rows.Add(row);

            foreach (Dictionary<string, string> book in bookDetailsList)
            {
                row = new TableRow();

                for (int i = 0; i <= headings.Count - 1; i++)
                {
                    cell = new TableCell();

                    string cellValue = "";

                    if (i == 1)
                        cellValue = (book["Thumbnail"].Equals("No thumbnail found") ? book["Thumbnail"] : "<img src=\"" + book["Thumbnail"] + "\" />");
                    else if (i == 2)
                        cellValue = book["Title"];
                    else if (i == 3)
                        cellValue = book["Author(s)"];

                    if (i == 0)
                    {
                        Button btnSelectBook = new Button();

                        btnSelectBook.CssClass = "btn btn-primary btn-lg";
                        btnSelectBook.Text = book["ISBN"];
                        btnSelectBook.OnClientClick = "window.location = window.location + '?isbn=' + this.value;return false;";

                        if (book["ISBN"].Equals("Unknown"))
                            btnSelectBook.Enabled = false;

                        cell.Controls.Add(btnSelectBook);
                    }
                    else
                        cell.Controls.Add(new LiteralControl(cellValue));

                    cell.CssClass = "tblCell";
                    row.Cells.Add(cell);
                }

                tblResults.Rows.Add(row);
            }

            if (resultsFound)
                tblResults.Visible = true;
        }

        protected List<Dictionary<string, string>> GetBookDetails(string isbn)
        {
            List<Dictionary<string, string>> bookDetailsList = new List<Dictionary<string, string>>();

            string isbnLookupURI = String.Format(@"https://www.googleapis.com/books/v1/volumes?q=isbn:{0}", isbn);

            WebRequest request = WebRequest.Create(isbnLookupURI);
            request.Method = "GET";

            WebResponse response = request.GetResponse();

            string jsonString;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            JObject jsonObj = JObject.Parse(jsonString);

            if (jsonObj["totalItems"].ToString().Equals("0"))
                return null;
            else
                resultsFound = true;

            for (int i = 0; i < ((JArray)jsonObj["items"]).Count; i++)
            {
                Dictionary<string, string> bookDetails = new Dictionary<string, string>();

                bookDetails.Add("BarcodeType", "ISBN");
                bookDetails.Add("Category", "Book");
                bookDetails.Add("Title", jsonObj["items"][i]["volumeInfo"]["title"].ToString());

                string authors = "";

                JArray authorArray = (JArray)jsonObj["items"][i]["volumeInfo"]["authors"];

                if (authorArray != null)
                {
                    foreach (JToken author in authorArray)
                        authors += author.ToString() + "<br />";
                }
                else
                    authors = "Unknown";

                bookDetails.Add("Author(s)", authors);

                bookDetails.Add("Publisher", (jsonObj["items"][i]["volumeInfo"]["publisher"] == null ? "Unknown" : jsonObj["items"][i]["volumeInfo"]["publisher"].ToString()) + " (" + (jsonObj["items"][i]["volumeInfo"]["publishedDate"] == null ? "Unknown" : jsonObj["items"][i]["volumeInfo"]["publishedDate"].ToString()) + ")");

                string subCategories = "";

                JArray subCategoriesArray = (JArray)jsonObj["items"][i]["volumeInfo"]["categories"];

                if (subCategoriesArray != null)
                {
                    foreach (JToken subCategory in subCategoriesArray)
                        subCategories += subCategory.ToString() + ", ";

                    subCategories = subCategories.Substring(0, subCategories.Length - 2);
                }
                else
                    subCategories = "Unknown";

                string other = "<strong>Sub-Categories:</strong> " + subCategories + "<br />";
                other += "<strong>Pages:</strong> " + (jsonObj["items"][i]["volumeInfo"]["pageCount"] == null ? "Unknown" : jsonObj["items"][i]["volumeInfo"]["pageCount"].ToString()) + "<br />";
                other += "<strong>Rating:</strong> " + (jsonObj["items"][i]["volumeInfo"]["averageRating"] == null ? "Unknown" : jsonObj["items"][i]["volumeInfo"]["averageRating"].ToString()) + "<br />";

                bookDetails.Add("LastColumn", other);

                bookDetailsList.Add(bookDetails);
            }

            return bookDetailsList;
        }

        protected void CreateResultsTable(List<Dictionary<string, string>> results)
        {
            lblResults.Visible = false;

            List<string> headings = new List<string>()
            {
                "Barcode Number", "Barcode Type", "Category", "Title", "Author(s)", "Publisher", "LastColumn"
            };

            TableRow row = new TableRow();
            TableCell cell;

            foreach (string heading in headings)
            {
                cell = new TableCell();

                cell.Controls.Add(new LiteralControl(heading));
                cell.CssClass = "tblCell heading";

                if (heading.Equals("LastColumn"))
                    cell.ID = "lastColumn";

                row.Cells.Add(cell);
            }

            tblResults.Rows.Add(row);

            foreach (Dictionary<string, string> book in results)
            {
                row = new TableRow();

                for (int i = 1; i <= headings.Count; i++)
                {
                    cell = new TableCell();

                    string cellValue = "";

                    if (i == 1)
                        cellValue = Request.QueryString["isbn"];
                    else if (i == 2)
                        cellValue = book["BarcodeType"];
                    else if (i == 3)
                        cellValue = book["Category"];
                    else if (i == 4)
                        cellValue = book["Title"];
                    else if (i == 5)
                        cellValue = book["Author(s)"];
                    else if (i == 6)
                        cellValue = book["Publisher"];
                    else if (i == 7)
                        cellValue = book["LastColumn"];

                    cell.Controls.Add(new LiteralControl(cellValue));
                    cell.CssClass = "tblCell";

                    row.Cells.Add(cell);
                }

                tblResults.Rows.Add(row);
            }

            if (resultsFound)
                tblResults.Visible = true;
        }
    }
}