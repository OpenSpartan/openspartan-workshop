using Microsoft.Data.Sqlite;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace OpenSpartan.Data
{
    internal class DataHandler
    {
        internal static bool BootstrapDatabase(string databaseName)
        {
            try
            {
                string qualifiedDatabaseLocation = Path.Combine(Core.Configuration.CacheDirectory, "data", databaseName);

                // Let's make sure that we create the directory if it does not exist.
                FileInfo file = new FileInfo(qualifiedDatabaseLocation);
                file.Directory.Create();

                // Regardless of whether the file exists or not, a new database will be created
                // when the connection is initialized.
                using (var connection = new SqliteConnection($"Data Source={qualifiedDatabaseLocation}"))
                {
                    connection.Open();

                    var verifyTableAvailabilityQuery = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Queries", "VerifyTableAvailability.sql"), Encoding.UTF8);
                    var command = connection.CreateCommand();
                    command.CommandText = verifyTableAvailabilityQuery;
                    command.Parameters.AddWithValue("$id", "ServiceRecordSnapshots");

                    // Service record table
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var name = reader.GetString(0);

                                Debug.WriteLine($"Table detected: {name}");
                            }
                        }
                        else
                        {
                            // There is no table.
                            Debug.WriteLine("There is no table.");
                            var serviceRecordTableQuery = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Queries", "BootstrapServiceRecordTable.sql"), Encoding.UTF8);

                            command = connection.CreateCommand();
                            command.CommandText = serviceRecordTableQuery;

                            // TODO: Add some error validation here.
                            var boostrapReader = command.ExecuteReader();
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
