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
    public partial class GoogleBooksOCR : System.Web.UI.Page
    {
        protected string recognizedText = "";
        protected bool resultsFound;
        protected List<Dictionary<string, string>> bookDetailsList;

        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "VisionAPIServiceAccount.json");
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

            string uri = String.Format("https://www.googleapis.com/books/v1/volumes?q={0}", bookText);

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

            for (int i = 0; i < ((JArray)jsonObj["items"]).Count; i++)
            {
                Dictionary<string, string> bookDetails = new Dictionary<string, string>();

                bookDetails.Add("Title", jsonObj["items"][i]["volumeInfo"]["title"].ToString());

                string authors = "";

                JArray authorArray = (JArray)jsonObj["items"][i]["volumeInfo"]["authors"];

                foreach (JToken author in authorArray)
                    authors += author.ToString() + "<br />";

                bookDetails.Add("Author(s)", authors);

                string isbn = "";

                JArray industryIdentifiersArray = (JArray)jsonObj["items"][i]["volumeInfo"]["industryIdentifiers"];

                foreach (JToken identifier in industryIdentifiersArray)
                {
                    if (identifier["type"].ToString().Equals("ISBN_13"))
                    {
                        isbn = identifier["identifier"].ToString();
                        break;
                    }
                }

                bookDetails.Add("ISBN", isbn);

                bookDetails.Add("Publisher", jsonObj["items"][i]["volumeInfo"]["publisher"].ToString() + " (" + jsonObj["items"][i]["volumeInfo"]["publishedDate"].ToString() + ")");

                bookDetails.Add("Thumbnail", jsonObj["items"][i]["volumeInfo"]["imageLinks"]["thumbnail"].ToString());

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
        }

        public void FormatBookResultsForSelection(List<Dictionary<string, string>> bookDetailsList)
        {
            List<string> headings = new List<string>()
            {
                "ISBN", "Thumbnail", "Title", "Author(s)"
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
                        cellValue = "<img src=\"" + book["Thumbnail"] + "\" />";
                    else if (i == 2)
                        cellValue = book["Title"];
                    else if (i == 3)
                        cellValue = book["Author(s)"];

                    if (i == 0)
                    {
                        Button btnSelectBook = new Button();

                        btnSelectBook.CssClass = "btn btn-primary btn-lg";
                        btnSelectBook.Text = book["ISBN"];
                        btnSelectBook.Click += (s, e) => 
                        {
                            Button button = s as Button;

                            CreateResultsTable(button.Text);
                        };

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

        protected void CreateResultsTable(string isbn)
        {
            lblResults.Visible = false;

            tblResults = new Table();

            List<string> headings = new List<string>()
            {
                "ISBN", "Category", "Title", "Author(s)", "Publisher", "LastColumn"
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

            foreach (Dictionary<string, string> book in bookDetailsList)
            {
                if (!book["ISBN"].Equals(isbn))
                    continue;

                row = new TableRow();

                for (int i = 1; i <= headings.Count; i++)
                {
                    cell = new TableCell();

                    string cellValue = "";

                    if (i == 1)
                        cellValue = isbn;
                    else if (i == 2)
                        cellValue = book["Category"];
                    else if (i == 3)
                        cellValue = book["Title"];
                    else if (i == 4)
                        cellValue = book["Author(s)"];
                    else if (i == 5)
                        cellValue = book["Publisher"];
                    else if (i == 6)
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