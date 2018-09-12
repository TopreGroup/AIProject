using System;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Generic;

namespace Trunked
{
    public partial class QuaggaJSBarcode : Page
    {
        GoogleBooksAPI googleBooksAPI = new GoogleBooksAPI();

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnGetBookInfo_Click(object sender, EventArgs e)
        {
            CreateResultsTable(hdnResult.Value);
            btnGetBookInfo.Visible=false;
        }

        protected void CreateResultsTable(string barcode)
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

            List<Dictionary<string, string>> bookDetails = googleBooksAPI.GetBookDetailsFromISBN(barcode);

            if (bookDetails != null)
            {
                lblResult.Text = googleBooksAPI.resultText;

                foreach (Dictionary<string, string> book in bookDetails)
                {
                    row = new TableRow();

                    for (int i = 1; i <= headings.Count; i++)
                    {
                        cell = new TableCell();

                        string cellValue = "";

                        if (i == 1)
                            cellValue = (book["Thumbnail"].Equals("No thumbnail found") ? book["Thumbnail"] : "<img src=\"" + book["Thumbnail"] + "\" />");
                        else if(i == 2)
                            cellValue = barcode;
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
}