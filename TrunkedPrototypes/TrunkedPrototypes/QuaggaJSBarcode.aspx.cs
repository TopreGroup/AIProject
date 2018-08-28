using System;
using System.IO;
using System.Net;
using System.Web.UI;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Dynamsoft.Barcode;

namespace TrunkedPrototypes
{
    public partial class QuaggaJSBarcode : System.Web.UI.Page
    {
        protected bool resultsFound;

        protected void Page_Load(object sender, EventArgs e)
        {
            resultsFound = false;
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

            TableCell tc = tblResults.Rows[0].FindControl("lastColumn") as TableCell;

            List<Dictionary<string, string>> productDetails = GetProductDetailsGoogle(barcode);

            foreach (Dictionary<string, string> product in productDetails)
            {
                row = new TableRow();

                for (int i = 1; i <= headings.Count; i++)
                {
                    cell = new TableCell();

                    string cellValue = "";

                    if (i == 1)
                        cellValue = barcode;
                    else if (i == 2)
                        cellValue = product["BarcodeType"];
                    else if (i == 3)
                        cellValue = product["Category"];
                    else if (i == 4)
                        cellValue = product["Title"];
                    else if (i == 5)
                        cellValue = product["Author(s)"];
                    else if (i == 6)
                        cellValue = product["Publisher"];
                    else if (i == 7)
                        cellValue = product["LastColumn"];

                    cell.Controls.Add(new LiteralControl(cellValue));
                    cell.CssClass = "tblCell";

                    row.Cells.Add(cell);
                }

                tblResults.Rows.Add(row);
            }

            if (resultsFound)
                tblResults.Visible = true;
        }

        protected List<Dictionary<string, string>> GetProductDetailsGoogle(string barcode)
        {
            List<Dictionary<string, string>> productDetailsList = new List<Dictionary<string, string>>();

            string barcodeLookupURI = String.Format("https://www.googleapis.com/books/v1/volumes?q=isbn:{0}", barcode);

            WebRequest request = WebRequest.Create(barcodeLookupURI);
            request.Method = "GET";

            WebResponse response = request.GetResponse();

            string jsonString;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            JObject jsonObj = JObject.Parse(jsonString);

            lblResult.Text += "<br />Number of books found for this barcode: " + jsonObj["totalItems"].ToString();

            if (jsonObj["totalItems"].ToString().Equals("0"))
                return null;
            else
                resultsFound = true;

            for (int i = 0; i < ((JArray)jsonObj["items"]).Count; i++)
            {
                Dictionary<string, string> productDetails = new Dictionary<string, string>();

                productDetails.Add("BarcodeType", "ISBN");
                productDetails.Add("Category", "Book");
                productDetails.Add("Title", jsonObj["items"][i]["volumeInfo"]["title"].ToString());

                string authors = "";

                JArray authorArray = (JArray)jsonObj["items"][i]["volumeInfo"]["authors"];

                foreach (JToken author in authorArray)
                    authors += author.ToString() + "<br />";

                productDetails.Add("Author(s)", authors);

                productDetails.Add("Publisher", jsonObj["items"][i]["volumeInfo"]["publisher"].ToString() + " (" + jsonObj["items"][i]["volumeInfo"]["publishedDate"].ToString() + ")");

                string subCategories = "";

                JArray subCategoriesArray = (JArray)jsonObj["items"][i]["volumeInfo"]["categories"];

                foreach (JToken subCategory in subCategoriesArray)
                    subCategories += subCategory.ToString() + ", ";

                subCategories = subCategories.Substring(0, subCategories.Length - 2);

                string other = "<strong>Sub-Categories:</strong> " + subCategories + "<br />";
                other += "<strong>Pages:</strong> " + jsonObj["items"][i]["volumeInfo"]["pageCount"].ToString() + "<br />";
                other += "<strong>Rating:</strong> " + jsonObj["items"][i]["volumeInfo"]["averageRating"].ToString() + "<br />";

                productDetails.Add("LastColumn", other);

                productDetailsList.Add(productDetails);
            }

            return productDetailsList;
        }
    }
}