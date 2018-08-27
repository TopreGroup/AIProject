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
    public partial class QuaggaJSBarcode : System.Web.UI.Page
    {
        protected bool resultsFound;

        protected void Page_Load(object sender, EventArgs e)
        {
            resultsFound = false;
        }

        protected void ScanButton_Click(object sender, EventArgs e)
        {
            
        }
    }
}