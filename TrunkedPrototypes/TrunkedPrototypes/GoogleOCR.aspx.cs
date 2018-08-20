using System;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using Google.Cloud.Vision.V1;

namespace TrunkedPrototypes
{
    public partial class GoogleOCR : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "VisionAPIServiceAccount.json");
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

                RecognizeImage(path);
            }

            ctrlFileUpload.Dispose();
        }

        protected void RecognizeImage(string imagePath)
        {
            var client = ImageAnnotatorClient.Create();
            var image = Google.Cloud.Vision.V1.Image.FromFile(imagePath);
            var response = client.DetectText(image);

            /**/
            int counter = 0;

            foreach (var annotation in response)
            {
                if (!String.IsNullOrEmpty(annotation.Description))
                {
                    TableRow row = new TableRow();
                    TableCell cell = new TableCell();

                    cell.Controls.Add(new LiteralControl(annotation.Description));

                    if (counter == 0)
                        cell.CssClass = "tblCell heading";
                    else
                        cell.CssClass = "tblCell";

                    row.Cells.Add(cell);

                    tblResults.Rows.Add(row);

                    counter++;
                }
            }
            //*/

            tblResults.Visible = true;
        }
    }
}