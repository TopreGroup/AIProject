using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using System.Web.UI;
using Newtonsoft.Json.Linq;

namespace Trunked
{
    public class GoogleBooksAPI
    {
        protected string BaseURI { get; set; }
        public string ResultText { get; set; }

        public GoogleBooksAPI()
        {
            BaseURI = "https://www.googleapis.com/books/v1/volumes";
        }

        public List<Dictionary<string, string>> GetBookDetailsFromText(string bookText, string maxResults)
        {
            string uri = String.Format("{0}?q={1}&maxResults={2}", BaseURI, bookText, maxResults);

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

            ResultText += "<br />Number of books found: " + jsonObj["totalItems"].ToString() + "<br />";

            if (jsonObj["totalItems"].ToString().Equals("0"))
                return null;

            return HandleAPIResults(jsonObj, "");
        }

        public List<Dictionary<string, string>> GetBookDetailsFromISBN(string isbn)
        {
            string uri = String.Format("{0}?q=isbn:{1}", BaseURI, isbn);

            WebRequest request = WebRequest.Create(uri);
            request.Method = "GET";

            WebResponse response = request.GetResponse();

            string jsonString;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            // Convert the response string to a JSON object
            JObject jsonObj = JObject.Parse(jsonString);

            ResultText = "Number of books found: " + jsonObj["totalItems"].ToString() + "<br />";

            if (jsonObj["totalItems"].ToString().Equals("0"))
                return null;

            return HandleAPIResults(jsonObj, isbn);
        }

        public List<Dictionary<string, string>> GetBookDetailsFromManualForm(string isbn, string title, string author, string publisher)
        {
            string queryString = "";

            if (!String.IsNullOrEmpty(isbn))
                queryString += String.Format("isbn:{0}", isbn);

            if (!String.IsNullOrEmpty(title))
                queryString += String.Format("{0}intitle:{1}", String.IsNullOrEmpty(queryString) ? "" : "&", title);

            if (!String.IsNullOrEmpty(author))
                queryString += String.Format("{0}inauthor:{1}", String.IsNullOrEmpty(queryString) ? "" : "&", author);

            if (!String.IsNullOrEmpty(publisher))
                queryString += String.Format("{0}inpublisher:{1}", String.IsNullOrEmpty(queryString) ? "" : "&", publisher);

            string uri = String.Format("{0}?q={1}", BaseURI, queryString);

            WebRequest request = WebRequest.Create(uri);
            request.Method = "GET";

            WebResponse response = request.GetResponse();

            string jsonString;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            // Convert the response string to a JSON object
            JObject jsonObj = JObject.Parse(jsonString);

            ResultText = "Number of books found: " + jsonObj["totalItems"].ToString() + "<br />";

            if (jsonObj["totalItems"].ToString().Equals("0"))
                return null;

            return HandleAPIResults(jsonObj, isbn);
        }

        private List<Dictionary<string, string>> HandleAPIResults(JObject resultObj, string actualISBN)
        {
            List<Dictionary<string, string>> bookDetailsList = new List<Dictionary<string, string>>();

            JArray resultsArray = (JArray)resultObj["items"];

            // Loop through all books returned
            for (int i = 0; i < resultsArray.Count; i++)
            {
                Dictionary<string, string> bookDetails = new Dictionary<string, string>();

                bookDetails.Add("Title", resultsArray[i]["volumeInfo"]["title"].ToString());

                string authors = "";

                JArray authorArray = (JArray)resultsArray[i]["volumeInfo"]["authors"];

                if (authorArray != null)
                {
                    foreach (JToken author in authorArray)
                        authors += author.ToString() + "<br />";
                }
                else
                    authors = "Unknown";

                bookDetails.Add("Author(s)", authors);

                string barcodeType = "";
                string isbn = "";

                if (String.IsNullOrEmpty(actualISBN))
                {
                    JArray industryIdentifiersArray = (JArray)resultsArray[i]["volumeInfo"]["industryIdentifiers"];

                    if (industryIdentifiersArray != null)
                    {
                        foreach (JToken identifier in industryIdentifiersArray)
                        {
                            if (identifier["type"].ToString().Equals("ISBN_13"))
                            {
                                barcodeType = identifier["type"].ToString();
                                isbn = identifier["identifier"].ToString();
                                break;
                            }

                            if (identifier["type"].ToString().Equals("ISBN_10"))
                            {
                                barcodeType = identifier["type"].ToString();
                                isbn = identifier["identifier"].ToString();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    isbn = actualISBN;

                    if (isbn.Length == 10)
                        barcodeType = "ISBN_10";
                    else if (isbn.Length == 13)
                        barcodeType = "ISBN_13";
                }

                bookDetails.Add("BarcodeType", String.IsNullOrEmpty(barcodeType) ? "Unknown" : barcodeType);
                bookDetails.Add("ISBN", String.IsNullOrEmpty(isbn) ? "Unknown" : isbn);

                bookDetails.Add("Publisher", (resultsArray[i]["volumeInfo"]["publisher"] == null ? "Unknown" : resultsArray[i]["volumeInfo"]["publisher"].ToString()));
                bookDetails.Add("PublishDate", (resultsArray[i]["volumeInfo"]["publishedDate"] == null ? "Unknown" : resultsArray[i]["volumeInfo"]["publishedDate"].ToString()));

                string thumbnailURI = "No thumbnail found";

                if (resultsArray[i]["volumeInfo"]["imageLinks"] != null)
                {
                    if (resultsArray[i]["volumeInfo"]["imageLinks"]["thumbnail"] != null)
                        thumbnailURI = resultsArray[i]["volumeInfo"]["imageLinks"]["thumbnail"].ToString();
                }

                bookDetails.Add("Thumbnail", thumbnailURI);

                string subCategories = "";

                JArray subCategoriesArray = (JArray)resultsArray[i]["volumeInfo"]["categories"];

                string genre = "Unknown";

                if (subCategoriesArray != null)
                {
                    foreach (JToken subCategory in subCategoriesArray)
                        subCategories += subCategory.ToString() + ", ";

                    subCategories = subCategories.Substring(0, subCategories.Length - 2);

                    genre = subCategoriesArray[0].ToString();
                }
                else
                    subCategories = "Unknown";

                bookDetails.Add("Genre", genre);

                string other = "<strong>Sub-Categories:</strong> " + subCategories + "<br />";
                other += "<strong>Pages:</strong> " + (resultsArray[i]["volumeInfo"]["pageCount"] == null ? "Unknown" : resultsArray[i]["volumeInfo"]["pageCount"].ToString()) + "<br />";
                other += "<strong>Rating:</strong> " + (resultsArray[i]["volumeInfo"]["averageRating"] == null ? "Unknown" : resultsArray[i]["volumeInfo"]["averageRating"].ToString()) + "<br />";

                bookDetails.Add("Other", other);

                bookDetailsList.Add(bookDetails);
            }

            return bookDetailsList;
        }

        public void FormatBookResultsForSelection(List<Dictionary<string, string>> bookDetailsList, Table tblResults)
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

            tblResults.Visible = true;
        }

        public void FormatBookResultsForConfirmation(List<Dictionary<string, string>> bookDetailsList, Table tblResults, _Default def)
        {
            List<string> headings = new List<string>()
            {
                "ISBN", "Cover", "Title", "Author(s)", "Publisher", "Genre", "Confirm"
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

                    if (i == 0)
                        cellValue = book["ISBN"];
                    if (i == 1)
                        cellValue = (book["Thumbnail"].Equals("No thumbnail found") ? book["Thumbnail"] : "<img src=\"" + book["Thumbnail"] + "\" />");
                    else if (i == 2)
                        cellValue = book["Title"];
                    else if (i == 3)
                        cellValue = book["Author(s)"];
                    else if (i == 4)
                        cellValue = book["Publisher"] + " (" + book["PublishDate"] + ")";
                    else if (i == 5)
                        cellValue = book["Genre"];

                    if (i == 6)
                    {
                        Button btnConfirmBook = new Button();

                        btnConfirmBook.CssClass = "btn btn-primary btn-lg";
                        btnConfirmBook.Text = "Confirm";

                        // Might have to do this part as a query string. But even then, might be difficult to get all book details into the js

                        btnConfirmBook.CommandName = String.Format("{0}|||{1}|||{2}|||{3}|||{4}|||{5}", book["ISBN"], book["Title"], book["Author(s)"], book["Publisher"], book["PublishDate"], book["Genre"]);

                        btnConfirmBook.Click += new EventHandler(def.btnConfirmBook_Click);

                        cell.Controls.Add(btnConfirmBook);
                    }
                    else
                        cell.Controls.Add(new LiteralControl(cellValue));

                    cell.CssClass = "tblCell";
                    row.Cells.Add(cell);
                }

                tblResults.Rows.Add(row);
            }

            tblResults.Visible = true;
        }
    }
}