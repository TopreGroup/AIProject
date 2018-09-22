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
        protected readonly string baseURI = "https://www.googleapis.com/books/v1/volumes";

        public string resultText { get; set; }

        public List<Dictionary<string, string>> GetBookDetailsFromText(string bookText, string maxResults)
        {
            string uri = String.Format("{0}?q={1}&maxResults={2}", baseURI, bookText, maxResults);

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

            resultText += "<br />Number of books found: " + jsonObj["totalItems"].ToString() + "<br />";

            if (jsonObj["totalItems"].ToString().Equals("0"))
                return null;

            return HandleAPIResults(jsonObj);
        }

        public List<Dictionary<string, string>> GetBookDetailsFromISBN(string isbn)
        {
            string uri = String.Format("{0}?q=isbn:{1}", baseURI, isbn);

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

            resultText = "Number of books found: " + jsonObj["totalItems"].ToString() + "<br />";

            if (jsonObj["totalItems"].ToString().Equals("0"))
                return null;

            return HandleAPIResults(jsonObj);
        }

        public Dictionary<string, string> GetBookDetailsFromManualForm(string isbn, string title, string author, string publisher)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            string queryString = "";

            if (!String.IsNullOrEmpty(isbn))
                queryString += String.Format("isbn:{0}", isbn);
            else if (!String.IsNullOrEmpty(title))
                queryString += String.Format("{0}intitle:{1}", String.IsNullOrEmpty(isbn) ? "" : "&", title);
            else if (!String.IsNullOrEmpty(author))
                queryString += String.Format("&inauthor:{0}", author);
            else if (!String.IsNullOrEmpty(publisher))
                queryString += String.Format("&inpublisher:{0}", publisher);

            string uri = String.Format("{0}?q=", baseURI, queryString);

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

            resultText = "Number of books found: " + jsonObj["totalItems"].ToString() + "<br />";

            if (jsonObj["totalItems"].ToString().Equals("0"))
                return null;

            return results;
        }

        private List<Dictionary<string, string>> HandleAPIResults(JObject resultObj)
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

                JArray industryIdentifiersArray = (JArray)resultsArray[i]["volumeInfo"]["industryIdentifiers"];

                if (industryIdentifiersArray != null)
                {
                    foreach (JToken identifier in industryIdentifiersArray)
                    {
                        if (identifier["type"].ToString().Equals("ISBN_13"))
                        {
                            barcodeType = "ISBN_13";
                            isbn = identifier["identifier"].ToString();
                            break;
                        }

                        if (identifier["type"].ToString().Equals("ISBN_10"))
                        {
                            barcodeType = "ISBN_10";
                            isbn = identifier["identifier"].ToString();
                            break;
                        }
                    }
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

                if (subCategoriesArray != null)
                {
                    foreach (JToken subCategory in subCategoriesArray)
                        subCategories += subCategory.ToString() + ", ";

                    subCategories = subCategories.Substring(0, subCategories.Length - 2);
                }
                else
                    subCategories = "Unknown";

                string other = "<strong>Sub-Categories:</strong> " + subCategories + "<br />";
                other += "<strong>Pages:</strong> " + (resultsArray[i]["volumeInfo"]["pageCount"] == null ? "Unknown" : resultsArray[i]["volumeInfo"]["pageCount"].ToString()) + "<br />";
                other += "<strong>Rating:</strong> " + (resultsArray[i]["volumeInfo"]["averageRating"] == null ? "Unknown" : resultsArray[i]["volumeInfo"]["averageRating"].ToString()) + "<br />";

                bookDetails.Add("Other", other);

                bookDetailsList.Add(bookDetails);
            }

            return bookDetailsList;
        }

        public void CreateResultsTable(List<Dictionary<string, string>> results, Table tblResults)
        {
            List<string> headings = new List<string>()
            {
                "Cover", "ISBN", "Barcode Type", "Title", "Author(s)", "Publisher", "Other"
            };

            TableRow row = new TableRow();
            TableCell cell;

            foreach (string heading in headings)
            {
                cell = new TableCell();

                cell.Controls.Add(new LiteralControl(heading));
                cell.CssClass = "tblCell heading";

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
                        cellValue = (book["Thumbnail"].Equals("No thumbnail found") ? book["Thumbnail"] : "<img src=\"" + book["Thumbnail"] + "\" />");
                    else if (i == 2)
                        cellValue = book["ISBN"];
                    else if (i == 3)
                        cellValue = book["BarcodeType"];
                    else if (i == 4)
                        cellValue = book["Title"];
                    else if (i == 5)
                        cellValue = book["Author(s)"];
                    else if (i == 6)
                        cellValue = book["Publisher"] + "(" + book["PublishDate"] + ")";
                    else if (i == 7)
                        cellValue = book["Other"];

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