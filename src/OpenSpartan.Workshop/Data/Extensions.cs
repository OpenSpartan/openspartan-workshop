using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OpenSpartan.Workshop.Data
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
            using var reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                return true;
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
