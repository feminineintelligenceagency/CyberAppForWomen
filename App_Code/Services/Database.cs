using System.Configuration;
using System.Data.SqlClient;

namespace CyberApp_FIA.Services
{
    public static class Database
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static void ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            using (var connection = GetConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public static SqlDataReader ExecuteReader(string query, params SqlParameter[] parameters)
        {
            var connection = GetConnection();
            var command = new SqlCommand(query, connection);
            command.Parameters.AddRange(parameters);
            connection.Open();
            return command.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        }

        public static object ExecuteScalar(string query, params SqlParameter[] parameters)
        {
            using (var connection = GetConnection())
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters);
                    connection.Open();
                    return command.ExecuteScalar();
                }
            }
        }
    }
}