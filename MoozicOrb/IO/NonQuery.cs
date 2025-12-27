using MoozicOrb.IO;
using System.Data;
using MySql.Data.MySqlClient;

// THIS QUERIES!!! needs to be changed!

namespace MoozicOrb.IO
{
    public class NonQuery
    {
        public DataTable Run(string nonQuery)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(DBConn1.ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand cmd = new MySqlCommand(nonQuery, connection))
                    {
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable);

                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle the exception as needed.
                Console.WriteLine(ex.Message);
                return null;
            }
        }

    }
}