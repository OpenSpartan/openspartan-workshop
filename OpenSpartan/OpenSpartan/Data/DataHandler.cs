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
using System.Text.RegularExpressions;
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

                    if (!connection.IsTableAvailable("Maps"))
                    {
                        connection.BootstrapTable("Maps");
                    }

                    if (!connection.IsTableAvailable("GameVariants"))
                    {
                        connection.BootstrapTable("GameVariants");
                    }

                    if (!connection.IsTableAvailable("Playlists"))
                    {
                        connection.BootstrapTable("Playlists");
                    }

                    if (!connection.IsTableAvailable("PlaylistMapModePairs"))
                    {
                        connection.BootstrapTable("PlaylistMapModePairs");
                    }

                    if (!connection.IsTableAvailable("EngineGameVariants"))
                    {
                        connection.BootstrapTable("EngineGameVariants");
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
                                            var processedMatchAssetParameters = UpdateMatchAssetRecords(matchStats.Result);
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

        private static async Task<bool> UpdateMatchAssetRecords(MatchStats result)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT EXISTS(SELECT 1 FROM Maps WHERE AssetId='{result.MatchInfo.MapVariant.AssetId}' AND VersionId='{result.MatchInfo.MapVariant.VersionId}') AS MAP_AVAILABLE," +
                                          $"EXISTS(SELECT 1 FROM Playlists WHERE AssetId='{result.MatchInfo.Playlist.AssetId}' AND VersionId='{result.MatchInfo.Playlist.VersionId}') AS PLAYLIST_AVAILABLE," +
                                          $"EXISTS(SELECT 1 FROM PlaylistMapModePairs WHERE AssetId='{result.MatchInfo.PlaylistMapModePair.AssetId}' AND VersionId='{result.MatchInfo.PlaylistMapModePair.VersionId}') AS PLAYLISTMAPMODEPAIR_AVAILABLE," +
                                          $"EXISTS(SELECT 1 FROM GameVariants WHERE AssetId='{result.MatchInfo.UgcGameVariant.AssetId}' AND VersionId='{result.MatchInfo.UgcGameVariant.VersionId}') AS GAMEVARIANT_AVAILABLE;";

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                if (reader.GetFieldValue<int>(reader.GetOrdinal("MAP_AVAILABLE")) == 0)
                                {
                                    // Map is not available
                                    var map = await UserContextManager.HaloClient.HIUGCDiscoveryGetMap(result.MatchInfo.MapVariant.AssetId.ToString(), result.MatchInfo.MapVariant.VersionId.ToString());
                                    if (map != null && map.Result != null && map.Response.Code == 200)
                                    {
                                        var insertionCommand = connection.CreateCommand();
                                        insertionCommand.CommandText = GetQuery("Insert", "Maps");
                                        insertionCommand.Parameters.AddWithValue("$ResponseBody", map.Response.Message);

                                        using (var matchReader = insertionCommand.ExecuteReader())
                                        {
                                            if (matchReader.RecordsAffected > 0)
                                            {
                                                Debug.WriteLine($"Stored map: {result.MatchInfo.MapVariant.AssetId}/{result.MatchInfo.MapVariant.VersionId}");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"Map exists: {result.MatchInfo.MapVariant.AssetId}/{result.MatchInfo.MapVariant.VersionId}");
                                }

                                if (reader.GetFieldValue<int>(reader.GetOrdinal("PLAYLIST_AVAILABLE")) == 0)
                                {
                                    // Playlist is not available
                                    var playlist = await UserContextManager.HaloClient.HIUGCDiscoveryGetPlaylist(result.MatchInfo.Playlist.AssetId.ToString(), result.MatchInfo.Playlist.VersionId.ToString(), UserContextManager.HaloClient.ClearanceToken);
                                    if (playlist != null && playlist.Result != null && playlist.Response.Code == 200)
                                    {
                                        var insertionCommand = connection.CreateCommand();
                                        insertionCommand.CommandText = GetQuery("Insert", "Playlists");
                                        insertionCommand.Parameters.AddWithValue("$ResponseBody", playlist.Response.Message);

                                        using (var matchReader = insertionCommand.ExecuteReader())
                                        {
                                            if (matchReader.RecordsAffected > 0)
                                            {
                                                Debug.WriteLine($"Stored playlist: {result.MatchInfo.Playlist.AssetId}/{result.MatchInfo.Playlist.VersionId}");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"Playlist exists: {result.MatchInfo.Playlist.AssetId}/{result.MatchInfo.Playlist.VersionId}");
                                }

                                if (reader.GetFieldValue<int>(reader.GetOrdinal("PLAYLISTMAPMODEPAIR_AVAILABLE")) == 0)
                                {
                                    // Playlist + map mode pair is not available
                                    var playlistMmp = await UserContextManager.HaloClient.HIUGCDiscoveryGetMapModePair(result.MatchInfo.PlaylistMapModePair.AssetId.ToString(), result.MatchInfo.PlaylistMapModePair.VersionId.ToString(), UserContextManager.HaloClient.ClearanceToken);
                                    if (playlistMmp != null && playlistMmp.Result != null && playlistMmp.Response.Code == 200)
                                    {
                                        var insertionCommand = connection.CreateCommand();
                                        insertionCommand.CommandText = GetQuery("Insert", "PlaylistMapModePairs");
                                        insertionCommand.Parameters.AddWithValue("$ResponseBody", playlistMmp.Response.Message);

                                        using (var matchReader = insertionCommand.ExecuteReader())
                                        {
                                            if (matchReader.RecordsAffected > 0)
                                            {
                                                Debug.WriteLine($"Stored playlist + map mode pair: {result.MatchInfo.PlaylistMapModePair.AssetId}/{result.MatchInfo.PlaylistMapModePair.VersionId}");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"Playlist + map mode pair exists: {result.MatchInfo.PlaylistMapModePair.AssetId}/{result.MatchInfo.PlaylistMapModePair.VersionId}");
                                }

                                if (reader.GetFieldValue<int>(reader.GetOrdinal("GAMEVARIANT_AVAILABLE")) == 0)
                                {
                                    // Game variant is not available
                                    var gameVariant = await UserContextManager.HaloClient.HIUGCDiscoveryGetUgcGameVariant(result.MatchInfo.UgcGameVariant.AssetId.ToString(), result.MatchInfo.UgcGameVariant.VersionId.ToString());
                                    if (gameVariant != null && gameVariant.Result != null && gameVariant.Response.Code == 200)
                                    {
                                        var insertionCommand = connection.CreateCommand();
                                        insertionCommand.CommandText = GetQuery("Insert", "GameVariants");
                                        insertionCommand.Parameters.AddWithValue("$ResponseBody", gameVariant.Response.Message);

                                        using (var matchReader = insertionCommand.ExecuteReader())
                                        {
                                            if (matchReader.RecordsAffected > 0)
                                            {
                                                Debug.WriteLine($"Stored game variant: {result.MatchInfo.UgcGameVariant.AssetId}/{result.MatchInfo.UgcGameVariant.VersionId}");
                                            }
                                        }

                                        var engineGameVariantExistenceString = $"SELECT EXISTS(SELECT 1 FROM Maps WHERE AssetId='{result.MatchInfo.MapVariant.AssetId}' AND VersionId='{result.MatchInfo.MapVariant.VersionId}') AS MAP_AVAILABLE";
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"Game variant exists: {result.MatchInfo.UgcGameVariant.AssetId}/{result.MatchInfo.UgcGameVariant.VersionId}");
                                }
                            }
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

        private static string GetQuery(string category, string target)
        {
            return System.IO.File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Queries", category, $"{target}.sql"), Encoding.UTF8);
        }
    }
}
