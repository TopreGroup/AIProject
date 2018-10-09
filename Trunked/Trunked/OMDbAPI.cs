using System;
using System.Net;
using System.IO;
using System.Web.UI;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Trunked
{
    public class OMDbAPI
    {
        protected string baseURI { get; set; }
        protected string APIKey { get; set; }
        public string ResultText { get; set; }

        public OMDbAPI()
        {
            baseURI = "http://www.omdbapi.com";
            APIKey = ConfigurationManager.AppSettings["OMDbAPIKey"];
        }

        public List<Dictionary<string, string>> GetMovieDetailsFromText(string text)
        {
            string uri = String.Format("{0}?s={1}&apikey={2}", baseURI, text, APIKey);

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

            if (jsonObj["Response"].ToString().Equals("False") || jsonObj["Error"].ToString().Equals("Movie not found!"))
                return null;

            JArray moviesArray = (JArray)jsonObj["Search"];

            ResultText += "<br />Number of movies found: " + moviesArray.Count + "<br />";

            return HandleAPIResults(moviesArray);
        }

        private List<Dictionary<string, string>> HandleAPIResults(JArray moviesArray)
        {
            List<Dictionary<string, string>> movieDetailsList = new List<Dictionary<string, string>>();

            // Loop through all books returned
            for (int i = 0; i < moviesArray.Count; i++)
            {
                Dictionary<string, string> movieDetails = new Dictionary<string, string>();

                movieDetails.Add("Type", (moviesArray[i]["Type"] == null ? "Unknown" : moviesArray[i]["Type"].ToString()));
                movieDetails.Add("Title", (moviesArray[i]["Title"] == null ? "Unknown" : moviesArray[i]["Title"].ToString()));
                movieDetails.Add("Year", (moviesArray[i]["Year"] == null ? "Unknown" : moviesArray[i]["Year"].ToString()));
                movieDetails.Add("imdbID", (moviesArray[i]["imdbID"] == null ? "Unknown" : moviesArray[i]["imdbID"].ToString()));
                movieDetails.Add("Poster", (moviesArray[i]["Poster"] == null ? "Unknown" : moviesArray[i]["Poster"].ToString()));

                movieDetailsList.Add(movieDetails);
            }

            return movieDetailsList;
        }

        public void FormatMovieResultsForSelection(List<Dictionary<string, string>> movieDetails, Table tblResults)
        {
            List<string> headings = new List<string>()
            {
                "IMDB ID", "Poster", "Type", "Title", "Year"
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

            foreach (Dictionary<string, string> movie in movieDetails)
            {
                row = new TableRow();

                for (int i = 0; i <= headings.Count - 1; i++)
                {
                    cell = new TableCell();

                    string cellValue = "";

                    if (i == 1)
                        cellValue = (movie["Poster"].Equals("Unknown") ? movie["Poster"] : "<img src=\"" + movie["Poster"] + "\" />");
                    else if (i == 2)
                        cellValue = movie["Type"];
                    else if (i == 3)
                        cellValue = movie["Title"];
                    else if (i == 4)
                        cellValue = movie["Year"];

                    if (i == 0)
                    {
                        Button btnSelectBook = new Button();

                        btnSelectBook.CssClass = "btn btn-primary btn-lg";
                        btnSelectBook.Text = movie["imdbID"];
                        btnSelectBook.OnClientClick = "window.location = window.location + '?imdbid=' + this.value;return false;";

                        if (movie["imdbID"].Equals("Unknown"))
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
    }
}