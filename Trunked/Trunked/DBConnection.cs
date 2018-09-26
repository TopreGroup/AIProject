using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace Trunked
{
    public class DBConnection
    {
        public string TrunkedDevDB { get; } = ConfigurationManager.ConnectionStrings["TrunkedDevDB"].ConnectionString;

        //private void ExecuteQuery(string queryString)
        public void ExecuteQuery()
        {
            string queryString = "SELECT ID FROM dbo.TrunkedModel;";

            using (SqlConnection connection = new SqlConnection(
                TrunkedDevDB))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Connection.Open();
                command.ExecuteNonQuery();

                //https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand?view=netframework-4.7.2

            }
        }
    }
}