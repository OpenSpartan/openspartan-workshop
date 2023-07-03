using Microsoft.Data.Sqlite;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace OpenSpartan.Data
{
    internal class DataHandler
    {
        internal static string DatabasePath => Path.Combine(Core.Configuration.AppDataDirectory, "data", Core.Configuration.DatabaseFileName);

        internal static bool BootstrapDatabase()
        {
            try
            {
                // Let's make sure that we create the directory if it does not exist.
                FileInfo file = new FileInfo(DatabasePath);
                file.Directory.Create();

                // Regardless of whether the file exists or not, a new database will be created
                // when the connection is initialized.
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    if (!connection.IsTableAvailable("ServiceRecordSnapshots"))
                    {
                        connection.BootstrapTable("ServiceRecordSnapshots");
                    }

                    if (!connection.IsTableAvailable("PlayerMatchStats"))
                    {
                        connection.BootstrapTable("PlayerMatchStats");
                    }

                    if (!connection.IsTableAvailable("MatchStats"))
                    {
                        connection.BootstrapTable("MatchStats");
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

        internal static bool InsertServiceRecordEntry(string serviceRecordJson)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    var insertServiceRecordEntryQuery = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Queries", "Insert", "ServiceRecord.sql"), Encoding.UTF8);
                    var command = connection.CreateCommand();
                    command.CommandText = insertServiceRecordEntryQuery;
                    command.Parameters.AddWithValue("$ResponseBody", serviceRecordJson);
                    command.Parameters.AddWithValue("$SnapshotTimestamp", DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.RecordsAffected > 1)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
