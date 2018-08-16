using System;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using Google.Cloud.Vision.V1;

namespace TrunkedPrototypes
{
    public partial class VisionAPI : System.Web.UI.Page
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
            var response = client.DetectLabels(image);

            int curRow = 1;
            int numCols = 2;

            TableRow row = new TableRow();
            TableCell cell = new TableCell();

            cell.Controls.Add(new LiteralControl("Description"));
            cell.CssClass = "tblCell heading";
            row.Cells.Add(cell);

            cell = new TableCell();

            cell.Controls.Add(new LiteralControl("Confidence Score"));
            cell.CssClass = "tblCell heading";
            row.Cells.Add(cell);

            tblResults.Rows.Add(row);

            /**/
            foreach (var annotation in response)
            {
                if (annotation.Description != null)
                {
                    row = new TableRow();

                    for (int i = 1; i <= numCols; i++)
                    {
                        cell = new TableCell();

                        string cellValue = "";

                        if (i == 1)
                            cellValue = annotation.Description;
                        else if (i == 2)
                            cellValue = annotation.Score.ToString();

                        cell.Controls.Add(new LiteralControl(cellValue));
                        cell.CssClass = "tblCell";

                        row.Cells.Add(cell);
                    }

                    tblResults.Rows.Add(row);
                }

                curRow++;
            }
            //*/

            tblResults.Visible = true;
        }
    }
}