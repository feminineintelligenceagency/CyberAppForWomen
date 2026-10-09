using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CyberApp_FIA.Services
{
    /// <summary>
    /// Low-level database helpers. Every query in the app should go through here
    /// and every value should be passed as a SqlParameter, never concatenated into the SQL string.
    ///
    /// </summary>
    public static class Database
    {
        private const string ConnectionStringName = "DefaultConnection";

        // Looked up on first use, with a clear error if Web.config is missing it.
        private static readonly Lazy<string> connectionString = new Lazy<string>(() =>
        {
            var cs = ConfigurationManager.ConnectionStrings[ConnectionStringName]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(cs))
            {
                throw new ConfigurationErrorsException(
                    "Missing connection string '" + ConnectionStringName + "'");
            }
            return cs;
        });

        public static string ConnectionString => connectionString.Value;

        /// <summary>Creates a new (closed) connection. Wrap it in a using block.</summary>
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        /// <summary>
        /// Builds a typed parameter. Null values become DBNull automatically.
        /// Example: Database.Param("@Email", SqlDbType.NVarChar, 256, email)
        /// </summary>
        public static SqlParameter Param(string name, SqlDbType type, int size, object value)
        {
            var p = new SqlParameter(name, type);
            if (size > 0) p.Size = size;
            p.Value = value ?? DBNull.Value;
            return p;
        }

        /// <summary>Typed parameter without a size (INT, BIT, DATETIME2, ...).</summary>
        public static SqlParameter Param(string name, SqlDbType type, object value)
        {
            return Param(name, type, 0, value);
        }

        /// <summary>Runs INSERT/UPDATE/DELETE. Returns the number of rows affected.</summary>
        public static int ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            using (var connection = GetConnection())
            using (var command = CreateCommand(connection, null, query, parameters))
            {
                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>Returns the first column of the first row, or null if there are no rows.</summary>
        public static object ExecuteScalar(string query, params SqlParameter[] parameters)
        {
            using (var connection = GetConnection())
            using (var command = CreateCommand(connection, null, query, parameters))
            {
                connection.Open();
                var result = command.ExecuteScalar();
                return result == DBNull.Value ? null : result;
            }
        }

        /// <summary>
        /// Returns an open reader; closing the reader closes the connection.
        /// The caller MUST wrap it in a using block or the connection leaks.
        /// Prefer Query / QuerySingle below, which can't leak.
        /// </summary>
        public static SqlDataReader ExecuteReader(string query, params SqlParameter[] parameters)
        {
            var connection = GetConnection();
            try
            {
                var command = CreateCommand(connection, null, query, parameters);
                connection.Open();
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Runs a SELECT and converts each row with <paramref name="map"/>.
        /// Example:
        ///   var emails = Database.Query("SELECT Email FROM dbo.Users", r => r.GetString(0));
        /// </summary>
        public static List<T> Query<T>(string query, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
        {
            var results = new List<T>();
            using (var connection = GetConnection())
            using (var command = CreateCommand(connection, null, query, parameters))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) results.Add(map(reader));
                }
            }
            return results;
        }

        /// <summary>Like Query, but returns only the first row, or default(T) (null) if none.</summary>
        public static T QuerySingle<T>(string query, Func<SqlDataReader, T> map, params SqlParameter[] parameters)
        {
            using (var connection = GetConnection())
            using (var command = CreateCommand(connection, null, query, parameters))
            {
                connection.Open();
                using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    return reader.Read() ? map(reader) : default(T);
                }
            }
        }


        /// <summary>
        /// Runs <paramref name="work"/> inside a transaction. If it throws, everything is
        /// rolled back, otherwise it is committed. Use the ExecuteNonQuery/ExecuteScalar
        /// overloads below that take (connection, transaction) inside the callback.
        /// </summary>
        public static void InTransaction(Action<SqlConnection, SqlTransaction> work)
        {
            InTransaction<object>((c, t) => { work(c, t); return null; });
        }

        /// <summary>Transaction version that returns a value.</summary>
        public static T InTransaction<T>(Func<SqlConnection, SqlTransaction, T> work)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var result = work(connection, transaction);
                        transaction.Commit();
                        return result;
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { /* connection may already be broken */ }
                        throw;
                    }
                }
            }
        }

        /// <summary>ExecuteNonQuery on an existing connection/transaction (use inside InTransaction).</summary>
        public static int ExecuteNonQuery(SqlConnection connection, SqlTransaction transaction,
                                          string query, params SqlParameter[] parameters)
        {
            using (var command = CreateCommand(connection, transaction, query, parameters))
            {
                return command.ExecuteNonQuery();
            }
        }

        /// <summary>ExecuteScalar on an existing connection/transaction (use inside InTransaction).</summary>
        public static object ExecuteScalar(SqlConnection connection, SqlTransaction transaction,
                                           string query, params SqlParameter[] parameters)
        {
            using (var command = CreateCommand(connection, transaction, query, parameters))
            {
                var result = command.ExecuteScalar();
                return result == DBNull.Value ? null : result;
            }
        }

        /// <summary>
        /// True when the error is a UNIQUE / PRIMARY KEY violation
        /// SQL Server error 2627 = constraint, 2601 = unique index.
        /// </summary>
        public static bool IsUniqueViolation(SqlException ex)
        {
            foreach (SqlError error in ex.Errors)
            {
                if (error.Number == 2627 || error.Number == 2601) return true;
            }
            return false;
        }


        /// <summary>
        /// Nullable string reader helper. Returns null if the column is NULL, otherwise returns the string.
        /// </summary>
        public static string GetStringOrNull(SqlDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        public static int? GetNullableInt(SqlDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? (int?)null : r.GetInt32(i);
        }

        public static DateTime? GetNullableDateTime(SqlDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? (DateTime?)null : DateTime.SpecifyKind(r.GetDateTime(i), DateTimeKind.Utc);
        }

        public static byte[] GetBytesOrNull(SqlDataReader r, string column)
        {
            int i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : (byte[])r.GetValue(i);
        }

        private static SqlCommand CreateCommand(SqlConnection connection, SqlTransaction transaction,
                                                string query, SqlParameter[] parameters)
        {
            var command = new SqlCommand(query, connection, transaction)
            {
                CommandTimeout = 30
            };
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    if (p.Value == null) p.Value = DBNull.Value;
                    command.Parameters.Add(p);
                }
            }
            return command;
        }
    }
}