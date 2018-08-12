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
    public partial class Barcode : System.Web.UI.Page
    {
        protected bool resultsFound;

        protected void Page_Load(object sender, EventArgs e)
        {
            resultsFound = false;
        }

        protected void ScanButton_Click(object sender, EventArgs e)
        {
            if (ctrlFileUpload.HasFile)
            {
                string path = "";

                try
                {
                    string fullFileName = Path.GetFileName(ctrlFileUpload.FileName);

                    string fileExtension = fullFileName.Substring(fullFileName.IndexOf("."));
                    string fileName = Guid.NewGuid().ToString();

                    path = Server.MapPath("~/UploadedImages/") + fileName + fileExtension;

                    ctrlFileUpload.SaveAs(path);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = "Upload status: The file could not be uploaded. The following error occured: " + ex.Message;
                    lblStatus.Visible = true;
                }

                ReadBarcode(path);
            }

            ctrlFileUpload.Dispose();
        }

        protected void ReadBarcode(string imagePath)
        {
            BarcodeReader reader = new BarcodeReader(ConfigurationManager.AppSettings["BarcodeScannerAPIKey"]);

            TextResult[] result = reader.DecodeFile(imagePath, "");

            if (result.Length > 0)
            {
                lblResult.Text = "Number of barcodes found in image: " + result.Length;

                CreateResultsTable(result);
            }
            else
                lblResult.Text = "No barcodes found in image";
        }

        protected void CreateResultsTable(TextResult[] results)
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

            foreach (var result in results)
            {
                string currentBarcode = result.BarcodeText;

                TableCell tc = tblResults.Rows[0].FindControl("lastColumn") as TableCell;

                List<Dictionary<string, string>> productDetails = GetProductDetailsGoogle(currentBarcode);

                // Not a book found on Google's Book API
                if (productDetails == null)
                {
                    continue;
                    //productDetails = GetProductDetails(currentBarcode);

                    //tc.Text = "Sold in Stores";
                }
                else
                    tc.Text = "Other";                    

                foreach (Dictionary<string, string> product in productDetails)
                {
                    row = new TableRow();

                    for (int i = 1; i <= headings.Count; i++)
                    {
                        cell = new TableCell();

                        string cellValue = "";

                        if (i == 1)
                            cellValue = currentBarcode;
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
            }

            if (resultsFound)
                tblResults.Visible = true;
        }

        protected List<Dictionary<string, string>> GetProductDetails(string barcode)
        {
            List<Dictionary<string, string>> productDetailsList = new List<Dictionary<string, string>>();            

            string barcodeLookupURI = String.Format("https://api.barcodelookup.com/v2/products?barcode={0}&formatted=y&key={1}", barcode, ConfigurationManager.AppSettings["BarcodeLookupAPIKey"]);

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

            lblResult.Text += "<br />Number of items found for this barcode" + ((JArray)jsonObj["products"]).Count;

            if (((JArray)jsonObj["products"]).Count == 0)
                return null;
            else
                resultsFound = true;

            for (int i = 0; i < ((JArray)jsonObj["products"]).Count; i++)
            {
                Dictionary<string, string> productDetails = new Dictionary<string, string>();

                productDetails.Add("BarcodeType", jsonObj["products"][i]["barcode_type"].ToString());
                productDetails.Add("Category", jsonObj["products"][i]["category"].ToString().Contains("Book") ? "Book" : jsonObj["products"]["barcode_type"].ToString());
                productDetails.Add("Title", jsonObj["products"][i]["product_name"].ToString());
                productDetails.Add("Author(s)", jsonObj["products"][i]["author"].ToString());
                productDetails.Add("Publisher", jsonObj["products"][i]["publisher"].ToString());

                string storesAndPrice = "";

                foreach (JToken store in jsonObj["products"][0]["stores"])
                    storesAndPrice += store["store_name"].ToString() + " (" + store["currency_code"].ToString() + " " + store["currency_symbol"].ToString() + store["store_price"].ToString() + ")<br />";

                productDetails.Add("LastColumn", storesAndPrice);

                productDetailsList.Add(productDetails);
            }

            return productDetailsList;
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