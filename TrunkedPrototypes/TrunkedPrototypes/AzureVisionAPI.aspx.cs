using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;

namespace TrunkedPrototypes
{
    public partial class AzureVisionAPI : System.Web.UI.Page
    {
        const string uriBase = "https://australiaeast.api.cognitive.microsoft.com/vision/v1.0/analyze";

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

                MakeAnalysisRequestAsync(ctrlFileUpload.FileBytes);
            }

            ctrlFileUpload.Dispose();
        }

        public void MakeAnalysisRequestAsync(byte[] byteData)
        {
            string requestParameters = "visualFeatures=Categories,Description,Color&language=en";
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
            lblCaption.Text = responseObject["description"]["captions"][0]["text"].ToString();
            lblScore.Text = responseObject["description"]["captions"][0]["confidence"].ToString();

            TableRow row = new TableRow();
            TableCell cell = new TableCell();

            cell.Controls.Add(new LiteralControl("Description"));
            cell.CssClass = "tblCell heading";
            row.Cells.Add(cell);

            tblResults.Rows.Add(row);

            JArray tagsArray = (JArray)responseObject["description"]["tags"];
            /**/
            for (int j = 0; j < tagsArray.Count; j++)
            {
                    row = new TableRow();

                        cell = new TableCell();

                        cell.Controls.Add(new LiteralControl(tagsArray[j].ToString()));
                        cell.CssClass = "tblCell";

                        row.Cells.Add(cell);

                    tblResults.Rows.Add(row);
            }
            //*/

            tblResults.Visible = true;
        }
    }
}