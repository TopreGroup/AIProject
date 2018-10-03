using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace Trunked
{
    public class DBResult
    {
        public int Code { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Result { get; set; }
    }

    public class DBConnection
    {
        public string TrunkedDB { get; set; }

        public DBConnection()
        {
            TrunkedDB = ConfigurationManager.ConnectionStrings["TrunkedDevDB"].ConnectionString;
        }

        public DBResult InsertBook(Dictionary<string, string> parameters)
        {
            DBResult result = new DBResult();

            try
            {
                using (var conn = new SqlConnection(TrunkedDB))
                using (var command = new SqlCommand("usp_insert_book", conn))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("@Tag", SqlDbType.VarChar).Value = "Book";
                    command.Parameters.Add("@Details", SqlDbType.VarChar).Value = null;
                    command.Parameters.Add("@ISBN", SqlDbType.VarChar).Value = parameters["ISBN"];
                    command.Parameters.Add("@BookTitle", SqlDbType.VarChar).Value = parameters["Title"];
                    command.Parameters.Add("@BookAuthors", SqlDbType.VarChar).Value = parameters["Authors"];
                    command.Parameters.Add("@BookGenre", SqlDbType.VarChar).Value = parameters["Genre"];
                    command.Parameters.Add("@BookPublisher", SqlDbType.VarChar).Value = parameters["Publisher"];
                    command.Parameters.Add("@BookPublishDate", SqlDbType.VarChar).Value = parameters["PublishDate"];

                    conn.Open();
                    result.Code = command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                result.Code = -1;
                result.ErrorMessage = e.Message;
            }

            return result;
        }

        public DBResult GetItemTypes()
        {
            DBResult result = new DBResult();

            try
            {
                using (var conn = new SqlConnection(TrunkedDB))
                using (var command = new SqlCommand("usp_get_itemTypes", conn))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    conn.Open();

                    List<string> itemTypes = new List<string>();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            itemTypes.Add(reader["Tag"].ToString());
                    }

                    result.Result = itemTypes;
                }
            }
            catch (Exception e)
            {
                result.Code = -1;
                result.ErrorMessage = e.Message;
                result.Result = null;
            }

            return result;
        }

        public DBResult GetClothingTypes()
        {
            DBResult result = new DBResult();

            try
            {
                using (var conn = new SqlConnection(TrunkedDB))
                using (var command = new SqlCommand("usp_get_clothingTypes", conn))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    conn.Open();

                    List<string> clothingTypes = new List<string>();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            clothingTypes.Add(reader["ClothingType"].ToString());
                    }

                    result.Result = clothingTypes;
                }
            }
            catch (Exception e)
            {
                result.Code = -1;
                result.ErrorMessage = e.Message;
                result.Result = null;
            }

            return result;
        }

        public DBResult GetClothingSubTypes(string clothingType)
        {
            DBResult result = new DBResult();

            try
            {
                using (var conn = new SqlConnection(TrunkedDB))
                using (var command = new SqlCommand("usp_get_clothingSubTypes", conn))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("@ClothingType", SqlDbType.VarChar).Value = clothingType;

                    conn.Open();

                    List<string> subTypes = new List<string>();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            subTypes.Add(reader["ClothingSubType"].ToString());
                    }

                    result.Result = subTypes;
                }
            }
            catch (Exception e)
            {
                result.Code = -1;
                result.ErrorMessage = e.Message;
                result.Result = null;
            }

            return result;
        }
    }
}