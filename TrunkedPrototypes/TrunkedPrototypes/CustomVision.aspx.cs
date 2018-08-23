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
    public partial class CustomVision : System.Web.UI.Page
    {
        const string uri = "https://southcentralus.api.cognitive.microsoft.com/customvision/v2.0/Prediction/7f793aca-ea51-48f0-a42f-be130c733b36/image?iterationId=52e0e274-b5db-419f-afb6-bfaac40920b4";

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



                MakePredictionRequest(ctrlFileUpload.FileBytes);
            }

            ctrlFileUpload.Dispose();
        }
       /* static byte[] GetImageAsByteArray(byte[] byteData)
        {
           // FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }*/
        public void MakePredictionRequest(byte[] byteData)
        {
           
            WebRequest client = WebRequest.Create(uri);
            client.Headers.Add("Prediction-Key", "ccc9e16826214050ac955205234b63ac");
            // Request headers - replace this example key with your valid subscription key.
            client.Method = "POSt";
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
            lblCaption.Text = responseObject["predictions"][0]["probability"].ToString();
            lblScore.Text = responseObject["predictions"][0]["tagName"].ToString();
            /*
            TableRow row = new TableRow();
            TableCell cell = new TableCell();

            cell.Controls.Add(new LiteralControl("Description"));
            cell.CssClass = "tblCell heading";
            row.Cells.Add(cell);

            tblResults.Rows.Add(row);

            JArray tagsArray = (JArray)responseObject["description"]["tags"];
            
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