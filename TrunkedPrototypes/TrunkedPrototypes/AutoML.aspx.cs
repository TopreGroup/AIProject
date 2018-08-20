using System;
using System.IO;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace TrunkedPrototypes
{
    public partial class AutoML : System.Web.UI.Page
    {
        private readonly string GCLOUD_ACCESS_TOKEN = "ya29.c.El_9BR1Erf_8kv5hfxIZm_qoGhN1LZhAg7nRfpg8Z55k7w9Rv0OeRdi8AWP4lgLFm_aSppIfmaTIHo7wSvnGMUU42iJaJm-MHP4L78V8McR_bs2_3VqpmETYS2rY_RSTbQ";
        private readonly string PROJECT_ID = "vision-poc-212402";
        private readonly string MODEL_ID = "ICN584807738399318221";

        protected bool resultsFound = false;

        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "AutoMLServiceAccount.json");
        }

        protected void RecognizeButton_Click(object sender, EventArgs e)
        {
            tblResults.Visible = false;
            resultsFound = false;

            if (ctrlFileUpload.HasFile)
            {
                string fullFileName = Path.GetFileName(ctrlFileUpload.FileName);

                string fileExtension = fullFileName.Substring(fullFileName.IndexOf("."));
                string fileName = Guid.NewGuid().ToString();

                string path = Server.MapPath("~/UploadedImages/") + fileName + fileExtension;

                ctrlFileUpload.SaveAs(path);

                Byte[] imageBytes = ctrlFileUpload.FileBytes;
                string base64ImageString = Convert.ToBase64String(imageBytes);                
                
                string jsonRequest = "{\"payload\":{\"image\":{\"imageBytes\":\"" + base64ImageString + "\"},}}";

                string uri = String.Format("https://automl.googleapis.com/v1beta1/projects/{0}/locations/us-central1/models/{1}:predict", PROJECT_ID, MODEL_ID);

                WebRequest request = WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentType = "application/json"; 
                request.Headers.Add("Authorization", "Bearer " + GCLOUD_ACCESS_TOKEN);

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(jsonRequest);
                }

                string result = "";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        result = sr.ReadToEnd();
                    }
                }

                JObject jsonResultObj = JObject.Parse(result);

                FormatResults(jsonResultObj);
            }

            ctrlFileUpload.Dispose();


            /*

              {
                 "payload": {
                    "image": {
                       "imageBytes": "YOUR_IMAGE_BYTE"
                    },
                 }
              }


                curl -X POST -H "Content-Type: application/json" \
                -H "Authorization: Bearer $(gcloud auth application-default print-access-token)" \
                https://automl.googleapis.com/v1beta1/projects/silver-charmer-212805/locations/us-central1/models/ICN6385355982086874655:predict -d @request.json

             */
        }

        protected void FormatResults(JObject results)
        {            
            List<string> headings = new List<string>()
            {
                "Label", "Confidence Score"
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


            int numResults;

            try
            {
                numResults = ((JArray)results["payload"]).Count;
            }
            catch
            {
                numResults = 0;
            }

            lblResult.Text = "<br />Number of labels identified: " + numResults;

            if (numResults > 0)
                resultsFound = true;

            for (int i = 0; i < numResults; i++)
            {
                row = new TableRow();

                for (int j = 1; j <= headings.Count; j++)
                {
                    cell = new TableCell();

                    string cellValue = "";

                    if (j == 1)
                        cellValue = results["payload"][i]["displayName"].ToString();
                    else if (j == 2)
                        cellValue = results["payload"][i]["classification"]["score"].ToString();

                    cell.Controls.Add(new LiteralControl(cellValue));
                    cell.CssClass = "tblCell";

                    row.Cells.Add(cell);
                }

                tblResults.Rows.Add(row);
            }

            if (resultsFound)
                tblResults.Visible = true;
        }
    }
}