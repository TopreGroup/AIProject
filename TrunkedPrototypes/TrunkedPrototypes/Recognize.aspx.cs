using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Google.Cloud.Vision.V1;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;

namespace TrunkedPrototypes
{
    public partial class Recognize : System.Web.UI.Page
    {
        protected string trainingKey = "5577626c64a7499b823f8da3b9a4f1ac";
        protected string predictionKey = "eac5e5ce95d649f6806f6d1b464798c9";
        protected Guid projectID = new Guid("47a84c85-79e7-41b5-93b9-4e8f7eb1bee6");

        protected TrainingApi trainingApi;

        protected int minImagesPerTag = 5;

        protected string recognizedText = "";
        protected bool resultsFound;
        protected List<Dictionary<string, string>> bookDetailsList;

        protected void Page_Load(object sender, EventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Server.MapPath("~/Content/") + "VisionAPIServiceAccount.json");

            trainingApi = new TrainingApi() { ApiKey = trainingKey };
            trainingApi.HttpClient.Timeout = new TimeSpan(0, 30, 0);
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

                string imageText = ReadTextFromImage(path);

                if (!imageText.Equals("NO TEXT FOUND"))
                {
                    try
                    { 
                        GetBookDetailsFromText(imageText);

                        CreateTagsAndTrain(bookDetailsList);

                        MakePrediction(path);
                    }
                    catch (Microsoft.Rest.HttpOperationException ex)
                    {
                        lblResults.Text = "ERROR: " + ex.Message + "<br />" + ex.Response.Content;
                    }
                    catch (Exception ex)
                    {
                        lblResults.Text = ex + "<br />" + ex.Message + "<br />" + ex.InnerException;
                    }
                }
                else
                    lblResults.Text = imageText;
            }

            ctrlFileUpload.Dispose();
        }

        protected string ReadTextFromImage(string imagePath)
        {
            var client = ImageAnnotatorClient.Create();
            var image = Google.Cloud.Vision.V1.Image.FromFile(imagePath);
            var response = client.DetectText(image);

            foreach (var annotation in response)
            {
                if (!String.IsNullOrEmpty(annotation.Description))
                    return annotation.Description;
            }

            return "NO TEXT FOUND";
        }

        protected void GetBookDetailsFromText(string bookText)
        {
            bookDetailsList = new List<Dictionary<string, string>>();

            string uri = String.Format("https://www.googleapis.com/books/v1/volumes?q={0}", bookText);

            WebRequest request = WebRequest.Create(uri);
            request.Method = "GET";

            WebResponse response = request.GetResponse();

            string jsonString;
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }

            JObject jsonObj = JObject.Parse(jsonString);

            lblResults.Text += "<br />Number of books found: " + jsonObj["totalItems"].ToString() + "<br />";

            if (jsonObj["totalItems"].ToString().Equals("0"))
                bookDetailsList = null;
            else
                resultsFound = true;

            for (int i = 0; i < ((JArray)jsonObj["items"]).Count; i++)
            {
                Dictionary<string, string> bookDetails = new Dictionary<string, string>();

                bookDetails.Add("Title", jsonObj["items"][i]["volumeInfo"]["title"].ToString());

                string authors = "";

                JArray authorArray = (JArray)jsonObj["items"][i]["volumeInfo"]["authors"];

                foreach (JToken author in authorArray)
                    authors += author.ToString() + ", ";

                authors = authors.Substring(0, authors.Length - 2);

                bookDetails.Add("Author(s)", authors);

                string isbn = "";

                JArray industryIdentifiersArray = (JArray)jsonObj["items"][i]["volumeInfo"]["industryIdentifiers"];

                foreach (JToken identifier in industryIdentifiersArray)
                {
                    if (identifier["type"].ToString().Equals("ISBN_13"))
                    {
                        isbn = identifier["identifier"].ToString();
                        break;
                    }
                }

                bookDetails.Add("ISBN", isbn);

                bookDetails.Add("Publisher", jsonObj["items"][i]["volumeInfo"]["publisher"].ToString() + " (" + jsonObj["items"][i]["volumeInfo"]["publishedDate"].ToString() + ")");

                bookDetails.Add("Thumbnail", jsonObj["items"][i]["volumeInfo"]["imageLinks"]["thumbnail"].ToString());

                string subCategories = "";

                JArray subCategoriesArray = (JArray)jsonObj["items"][i]["volumeInfo"]["categories"];

                if (subCategoriesArray != null)
                {
                    foreach (JToken subCategory in subCategoriesArray)
                        subCategories += subCategory.ToString() + ", ";

                    subCategories = subCategories.Substring(0, subCategories.Length - 2);
                }
                else
                    subCategories = "Unknown";

                string other = "<strong>Sub-Categories:</strong> " + subCategories + "<br />";
                other += "<strong>Pages:</strong> " + (jsonObj["items"][i]["volumeInfo"]["pageCount"] == null ? "Unknown" : jsonObj["items"][i]["volumeInfo"]["pageCount"].ToString()) + "<br />";
                other += "<strong>Rating:</strong> " + (jsonObj["items"][i]["volumeInfo"]["averageRating"] == null ? "Unknown" : jsonObj["items"][i]["volumeInfo"]["averageRating"].ToString()) + "<br />";

                bookDetails.Add("LastColumn", other);

                bookDetailsList.Add(bookDetails);
            }
        }

        protected void CreateTagsAndTrain(List<Dictionary<string, string>> bookDetailsList)
        {
            foreach (Dictionary<string, string> book in bookDetailsList)
            {
                string newTag = book["ISBN"] + "|" + book["Title"] + "|" + book["Author(s)"];

                var tags = trainingApi.GetTags(projectID);
                bool tagExists = false;
                Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models.Tag bookTag = null;

                foreach (var tag in tags)
                {
                    if (tag.Name.Equals(newTag))
                    {
                        tagExists = true;
                        bookTag = tag;
                        break;
                    }
                }

                if (!tagExists)
                {
                    bookTag = trainingApi.CreateTag(projectID, newTag);
                }

                for (int i = 0; i < minImagesPerTag; i++)
                {
                    WebRequest request = WebRequest.Create(book["Thumbnail"]);
                    WebResponse response = request.GetResponse();

                    using (var stream = response.GetResponseStream())
                    {
                        trainingApi.CreateImagesFromData(projectID, stream, new List<string>() { bookTag.Id.ToString() });
                    }
                }
            }

            var iteration = trainingApi.TrainProject(projectID);

            while (iteration.Status == "Training")
            {
                Thread.Sleep(1000);

                iteration = trainingApi.GetIteration(projectID, iteration.Id);
            }

            iteration.IsDefault = true;
            trainingApi.UpdateIteration(projectID, iteration.Id, iteration);            
        }

        protected void MakePrediction(string predictionImagePath)
        {
            PredictionEndpoint endpoint = new PredictionEndpoint() { ApiKey = predictionKey };
            MemoryStream predictionImage = new MemoryStream(File.ReadAllBytes(predictionImagePath));

            CreateResultsTable(endpoint.PredictImage(projectID, predictionImage));
        }

        protected void CreateResultsTable(Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImagePrediction result)
        {
            lblResults.Visible = false;

            List<string> headings = new List<string>()
            {
                "ISBN", "Title", "Author(s)", "Probability"
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

            foreach (var prediction in result.Predictions)
            {
                string isbn = prediction.TagName.Split('|')[0];
                string title = prediction.TagName.Split('|')[1];
                string authors = prediction.TagName.Split('|')[2];

                row = new TableRow();

                for (int i = 1; i <= headings.Count; i++)
                {
                    cell = new TableCell();

                    string cellValue = "";

                    if (i == 1)
                        cellValue = title;
                    else if (i == 2)
                        cellValue = authors;
                    else if (i == 3)
                        cellValue = prediction.Probability.ToString();

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