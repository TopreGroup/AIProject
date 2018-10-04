using System.Drawing;
using ZXing;

namespace Trunked
{
    public static class BarcodeDecoder
    {
        public static Barcode Decode(string imagePath)
        {
            BarcodeReader reader = new BarcodeReader()
            {
                AutoRotate = false,
                TryInverted = true,
                Options =
                    {
                        TryHarder = true,
                        ReturnCodabarStartEnd = false,
                        PureBarcode = false
                    }
            };

            Bitmap barcodeBitmap = (Bitmap)Bitmap.FromFile(imagePath);

            var result = reader.Decode(barcodeBitmap);

            barcodeBitmap.Dispose();

            if (result != null)
            {
                Barcode barcode = new Barcode()
                {
                    Type = result.BarcodeFormat.ToString(),
                    Text = result.Text
                };

                return barcode;
            }
            else
                return null;      
        }
    }
}