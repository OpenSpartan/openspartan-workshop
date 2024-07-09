using Microsoft.Data.Sqlite;
using OpenSpartan.Workshop.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;

namespace OpenSpartan.Workshop.Data
{
    internal static class Extensions
    {
        public static bool IsTableAvailable(this SqliteConnection connection, string tableName)
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                string queriesPath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "Queries");
                string filePath = Path.Combine(queriesPath, "VerifyTableAvailability.sql");

                string verifyTableAvailabilityQuery = File.ReadAllText(filePath, Encoding.UTF8);

                using var command = connection.CreateCommand();
                command.CommandText = verifyTableAvailabilityQuery;
                command.Parameters.AddWithValue("$id", tableName);

                using var reader = command.ExecuteReader();
                return reader.HasRows;
            }
            else
            {
                return false;
            }
        }

        public static bool BootstrapTable(this SqliteConnection connection, string tableName)
        {
            try
            {
                string assemblyLocation = Assembly.GetExecutingAssembly().Location;

                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    string queriesPath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "Queries", "Bootstrap");
                    string filePath = Path.Combine(queriesPath, $"{tableName}.sql");

                    string tableBootstrapQuery = File.ReadAllText(filePath, Encoding.UTF8);

                    using var command = connection.CreateCommand();
                    command.CommandText = tableBootstrapQuery;
                    _ = command.ExecuteReader();

                    return true;
                }
            }
            catch (IOException ex)
            {
                LogEngine.Log($"File operation failed for table {tableName}. {ex.Message}", Models.LogSeverity.Error);
            }
            catch (SqliteException ex)
            {
                LogEngine.Log($"Database operation failed for table {tableName}. {ex.Message}", Models.LogSeverity.Error);
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Could not bootstrap table {tableName}. {ex.Message}", Models.LogSeverity.Error);
            }

            return false;
        }

        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(items);

            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }
}
