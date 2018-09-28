using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace Trunked
{
    public class DBConnection
    {
        public string TrunkedDevDB { get; } = ConfigurationManager.ConnectionStrings["TrunkedDevDB"].ConnectionString;

        //private void ExecuteQuery(string queryString)
        public void EstablishConection()
        {
            //string queryString = "SELECT ID FROM dbo.TrunkedModel;";

            SqlConnection cnn = new SqlConnection(TrunkedDevDB);

            cnn.Open();

            cnn.Close();

        }
    }
}