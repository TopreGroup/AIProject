using System;
using System.Collections.Generic;
using Google.Cloud.Vision.V1;
using System.Web.UI.WebControls;
using System.Web.UI;

namespace Trunked
{
    public class BookRecognizer
    {
        public string ReadTextFromImage(string imagePath)
        {
            var client = ImageAnnotatorClient.Create();
            var image = Google.Cloud.Vision.V1.Image.FromFile(imagePath);
            var response = client.DetectText(image);

            foreach (var annotation in response)
            {
                if (!String.IsNullOrEmpty(annotation.Description))
                    return annotation.Description;
            }

            return null;
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