using Den.Dev.Orion.Models;
using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.Data.Sqlite;
using OpenSpartan.Models;
using OpenSpartan.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

                    var insertServiceRecordEntryQuery = System.IO.File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Queries", "Insert", "ServiceRecord.sql"), Encoding.UTF8);
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

        internal static async Task<bool> UpdateMatchRecords(IEnumerable<Guid> matchIds)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    int matchCounter = 1;
                    int matchesTotal = matchIds.Count();

                    foreach (var matchId in matchIds)
                    {
                        var completionProgress = (double)matchCounter / (double)matchesTotal * 100.0;

                        var command = connection.CreateCommand();
                        command.CommandText = $"SELECT EXISTS(SELECT 1 FROM MatchStats WHERE MatchId='{matchId}') AS MATCH_AVAILABLE, EXISTS(SELECT 1 FROM PlayerMatchStats WHERE MatchId='{matchId}') AS PLAYER_STATS_AVAILABLE;";

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    HaloApiResultContainer<MatchStats, RawResponseContainer>? matchStats = null;

                                    // MATCH_AVAILABLE - if the value is zero, that means we do not have the match data.
                                    if (reader.GetFieldValue<int>(0) == 0)
                                    {
                                        Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Getting match stats for {matchId}...");
                                        matchStats = await UserContextManager.HaloClient.StatsGetMatchStats(matchId.ToString());

                                        if (matchStats != null && matchStats.Result != null)
                                        {
                                            var insertMatchStatsQuery = System.IO.File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Queries", "Insert", "MatchStats.sql"), Encoding.UTF8);

                                            var insertionCommand = connection.CreateCommand();
                                            insertionCommand.CommandText = insertMatchStatsQuery;
                                            insertionCommand.Parameters.AddWithValue("$ResponseBody", matchStats.Response.Message);

                                            using (var matchReader = insertionCommand.ExecuteReader())
                                            {
                                                if (matchReader.RecordsAffected > 0)
                                                {
                                                    Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Stored match data for {matchId} in the database.");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Match stats were not available for {matchId}.");
                                            matchCounter++;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Match {matchId} already available. Not requesting new data.");
                                    }

                                    // PLAYER_STATS_AVAILABLE - if the value is zero, that means we do not have the match data.
                                    if (reader.GetFieldValue<int>(0) == 0)
                                    {
                                        if (matchStats == null)
                                        {
                                            matchStats = await UserContextManager.HaloClient.StatsGetMatchStats(matchId.ToString());
                                        }

                                        if (matchStats != null && matchStats.Result != null && matchStats.Result.Players != null)
                                        {
                                            // Anything that starts with "bid" is a bot and including that in the request for player stats will result in failure.
                                            var targetPlayers = matchStats.Result.Players.Select(p => p.PlayerId).Where(p => !p.StartsWith("bid")).ToList();

                                            Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Attempting to get player results for players for match {matchId}.");

                                            var playerStatsSnapshot = await UserContextManager.HaloClient.SkillGetMatchPlayerResult(matchId.ToString(), targetPlayers!);

                                            if (playerStatsSnapshot != null && playerStatsSnapshot.Result != null && playerStatsSnapshot.Result.Value != null)
                                            {
                                                Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Got stats for {playerStatsSnapshot.Result.Value.Count} players.");

                                                if (playerStatsSnapshot.Response != null)
                                                {
                                                    var insertMatchStatsQuery = System.IO.File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Queries", "Insert", "PlayerMatchStats.sql"), Encoding.UTF8);

                                                    var insertionCommand = connection.CreateCommand();
                                                    insertionCommand.CommandText = insertMatchStatsQuery;
                                                    insertionCommand.Parameters.AddWithValue("$MatchId", matchId.ToString());
                                                    insertionCommand.Parameters.AddWithValue("$ResponseBody", playerStatsSnapshot.Response.Message);

                                                    using (var matchReader = insertionCommand.ExecuteReader())
                                                    {
                                                        if (matchReader.RecordsAffected > 0)
                                                        {
                                                            Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Stored player stats data for {matchId} in the database.");
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Could not obtain player stats for match {matchId}. Requested {targetPlayers.Count} XUIDs.");
                                            }
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Could not obtain player stats for match {matchId} because the match metadata was unavailable.");
                                        }

                                    }
                                    else
                                    {
                                        Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Match {matchId} player stats already available. Not requesting new data.");
                                    }

                                }
                            }
                            else
                            {
                                Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Something went wrong. Could not communicate with the database to get match availability.");
                            }

                            matchCounter++;
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
