using System;
using System.IO;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Google.Cloud.Vision.V1;

namespace Trunked
{
    public partial class RecognizeBook : System.Web.UI.Page
    {
        GoogleBooksAPI googleBooksAPI = new GoogleBooksAPI();

        protected string recognizedText = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Need to set up a Google account for Trunked with billing, etc. to get a service account from there (instead of using my trial one)
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "GoogleServiceAccount.json");

            if (!String.IsNullOrEmpty(Request.QueryString["isbn"]))
                CreateResultsTable(googleBooksAPI.GetBookDetailsFromISBN(Request.QueryString["isbn"]));
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
                    List<Dictionary<string, string>> bookDetails = googleBooksAPI.GetBookDetailsFromText(imageText, ConfigurationManager.AppSettings["GoogleBooksAPIMaxResults"]);

                    if (bookDetails != null)
                        FormatBookResultsForSelection(bookDetails);
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

            tblResults.Visible = true;
        }


        protected void CreateResultsTable(List<Dictionary<string, string>> results)
        {
            lblResults.Visible = false;

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
                        cellValue = Request.QueryString["isbn"];
                    else if (i == 3)
                        cellValue = book["BarcodeType"];
                    else if (i == 4)
                        cellValue = book["Title"];
                    else if (i == 5)
                        cellValue = book["Author(s)"];
                    else if (i == 6)
                        cellValue = book["Publisher"];
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