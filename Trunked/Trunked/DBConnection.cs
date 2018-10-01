using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace Trunked
{
    public class DBConnection
    {
        public string TrunkedDevDB { get; } = ConfigurationManager.ConnectionStrings["TrunkedDevDB"].ConnectionString;

        public void EstablishConection()
        {
            SqlConnection cnn = new SqlConnection(TrunkedDevDB);

            cnn.Open();
        }

        public int UpdateDatabase(string queryString, SqlConnection cnn)
        {
            int update = 0;
            try
            {
                SqlConnection conn = new SqlConnection(TrunkedDevDB);
                SqlCommand cmd = new SqlCommand(queryString, conn);
                conn.Open();
                update = cmd.ExecuteNonQuery();
                conn.Close();
                return update;
            } catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
                return -1;
            }
        }

        public int UpdateDatabase(SqlCommand command)
        {
            int update = 0;
            try
            {
                update = command.ExecuteNonQuery();

                return update;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
                return -1;
            }
        }

        //string clothingQuery = "INSERT INTO TrunkedModel (Tag, Details, ClothingType, ClothingSubType, ClothingBrand, ClothingSize, ClothingColour) Values(@tag, @details, @clothingType, @clothingSubType, @clothingBrand, @clothingSize, @clothingColour)";
        //string flixQuery = "INSERT INTO TrunkedModel (Tag, Details, FlixTitle, FlixGenre, FlixRating) Values(@tag, @details, @flixTitle, @flixGenre, @flixRating)";
        //string musicQuery = "INSERT INTO TrunkedModel (Tag, Details, MusicTitle, Musician, MusicGenre) Values(@tag, @details, @musicTitle, @musician, @musicGenre)";

        public void AddtoDatabase(string tag, string details, string ISBN, string bookTitle, string bookAuthor, string bookGenre, string bookPublisher)
        { 
            string query = "INSERT INTO TrunkedModel (Tag, Details, ISBN, BookTitle, BookAuthor, BookGenre, BookPublisher) Values(@tag, @details, @ISBN, @bookTitle, @bookAuthor, @bookGenre, @bookPublisher)";
            try
            {
                SqlConnection connect = new SqlConnection(TrunkedDevDB);
                connect.Open();

                SqlCommand cmd = new SqlCommand(query, connect);
                cmd.CreateParameter();
                cmd.Parameters.AddWithValue("tag", tag);
                cmd.Parameters.AddWithValue("details", details);
                cmd.Parameters.AddWithValue("ISBN", ISBN);
                cmd.Parameters.AddWithValue("bookTitle", bookTitle);
                cmd.Parameters.AddWithValue("bookAuthor", bookAuthor);
                cmd.Parameters.AddWithValue("bookGenre", bookGenre);
                cmd.Parameters.AddWithValue("bookPublisher", bookPublisher);

                var affectedRow = UpdateDatabase(cmd);
                connect.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }

        }
    }
}