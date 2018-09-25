using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Trunked
{
    public class DBConnection
    {
        public string TrunkedDevDB { get; } = ConfigurationManager.ConnectionStrings["TrunkedDevDB"].ConnectionString;
        
        private DBConnection(string queryString)
        {
            using (SqlConnection connection = new SqlConnection(
                TrunkedDevDB))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}