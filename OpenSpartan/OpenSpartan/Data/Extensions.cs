using Microsoft.Data.Sqlite;
using System.IO;
using System.Reflection;
using System.Text;

namespace OpenSpartan.Data
{
    internal static class Extensions
    {
        public static bool IsTableAvailable(this SqliteConnection connection, string tableName)
        {
            var verifyTableAvailabilityQuery = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Queries", "VerifyTableAvailability.sql"), Encoding.UTF8);
            var command = connection.CreateCommand();
            command.CommandText = verifyTableAvailabilityQuery;
            command.Parameters.AddWithValue("$id", tableName);

            // Service record table
            using (var reader = command.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool BootstrapTable(this SqliteConnection connection, string tableName)
        {
            try
            {
                var tableBootstrapQuery = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Queries", "Bootstrap", $"{tableName}.sql"), Encoding.UTF8);

                var command = connection.CreateCommand();
                command.CommandText = tableBootstrapQuery;

                command.ExecuteReader();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
