using System;
using System.Collections.Generic;
using Google.Cloud.Vision.V1;
using System.Web.UI.WebControls;
using System.Web.UI;

namespace Trunked
{
    public class OCR
    {
        public string ReadTextFromImage(string imagePath)
        {
            var client = ImageAnnotatorClient.Create();
            var image = Google.Cloud.Vision.V1.Image.FromFile(imagePath);
            var response = client.DetectText(image);

            foreach (var annotation in response)
            {
                if (!String.IsNullOrEmpty(annotation.Description))
                    return annotation.Description;
            }

            return null;
        }
    }
}