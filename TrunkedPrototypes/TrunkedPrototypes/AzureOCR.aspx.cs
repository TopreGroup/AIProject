using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace TrunkedPrototypes
{
    public partial class AzureOCR : System.Web.UI.Page
    {
        const string uriBase = "https://australiaeast.api.cognitive.microsoft.com/vision/v2.0/ocr";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "VisionAPIServiceAccount.json");
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
                MakeOCRRequest(ctrlFileUpload.FileBytes);
            }
            ctrlFileUpload.Dispose();
        }

        public void MakeOCRRequest(byte[] byteData)
        {
            string requestParameters = "language=unk&detectOrientation=true";
            string uri = uriBase + "?" + requestParameters;

            WebRequest client = WebRequest.Create(uri);
            client.Headers.Add("Ocp-Apim-Subscription-Key", ConfigurationManager.AppSettings["AzureTrunkedKey"]);
            client.Method = "POST";
            client.ContentType = "application/octet-stream";

            string result;

            using (Stream dataStream = client.GetRequestStream())
            {
                dataStream.Write(byteData, 0, byteData.Length);
            }

            JObject responseObject = null;

            using (HttpWebResponse response = (HttpWebResponse)client.GetResponse())
            {
                using (var sr = new StreamReader(response.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                    responseObject = JObject.Parse(result);
                }
            }
            PrintResults(responseObject);
        }

        protected void PrintResults(JObject responseObject)
        {
            lblLanguage.Text = responseObject["language"].ToString();

            TableRow row = new TableRow();
            TableCell cell = new TableCell();

            cell.Controls.Add(new LiteralControl("Description"));
            cell.CssClass = "tblCell heading";
            row.Cells.Add(cell);

            tblResults.Rows.Add(row);

            JArray regionText = (JArray)responseObject["regions"];


            for (int j = 0; j < regionText.Count; j++)
            {
                row = new TableRow();
                cell = new TableCell();
                cell.Controls.Add(new LiteralControl(regionText.ToString()));
                cell.CssClass = "tblCell";

                row.Cells.Add(cell);

                tblResults.Rows.Add(row);
            }

            tblResults.Visible = true;
        }
    }
}