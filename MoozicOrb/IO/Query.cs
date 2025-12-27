using MySql.Data.MySqlClient;
using System.Data;
using System.Linq;

namespace MoozicOrb.IO
{
    public class Query
    {
        public DataTable Run(string query)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(DBConn1.ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
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
                return null;
            }
        }
    }
}