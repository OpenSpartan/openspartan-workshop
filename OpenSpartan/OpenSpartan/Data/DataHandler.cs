using Den.Dev.Orion.Converters;
using Den.Dev.Orion.Models;
using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.Data.Sqlite;
using OpenSpartan.Models;
using OpenSpartan.Shared;
using OpenSpartan.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenSpartan.Data
{
    internal class DataHandler
    {
        internal static string DatabasePath => Path.Combine(Core.Configuration.AppDataDirectory, "data", Core.Configuration.DatabaseFileName);

        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new EmptyDateStringToNullJsonConverter(),
            },
        };

        internal static string SetWALJournalingMode()
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();
                using var command = connection.CreateCommand();

                command.CommandText = GetQuery("Bootstrap", "SetWALJournalingMode");
                using var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        return reader.GetString(0).Trim();
                    }
                }
                else
                {
                    Debug.WriteLine($"WAL journaling mode not set.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return null;
        }

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

                    if (!connection.IsTableAvailable("OperationRewardTracks"))
                    {
                        connection.BootstrapTable("OperationRewardTracks");
                    }

                    if (!connection.IsTableAvailable("InventoryItems"))
                    {
                        connection.BootstrapTable("InventoryItems");
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

        internal static List<Guid> GetMatchIds()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = GetQuery("Select", "DistinctMatchIds");

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                List<Guid> matchIds = new List<Guid>();
                                while (reader.Read())
                                {
                                    matchIds.Add(Guid.Parse(reader.GetString(0).Trim()));
                                }

                                return matchIds;
                            }
                            else
                            {
                                Debug.WriteLine($"No rows returned for distinct match IDs.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred obtaining unique match IDs. {ex.Message}");
            }

            return null;
        }

        internal static RewardTrackMetadata GetOperationResponseBody(string operationPath)
        {
            try
            {
                using SqliteConnection connection = new($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Select", "OperationResponseBody");
                command.Parameters.AddWithValue("$OperationPath", operationPath);

                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    RewardTrackMetadata response = new();

                    while (reader.Read())
                    {
                        response = JsonSerializer.Deserialize<RewardTrackMetadata>(reader.GetString(0).Trim(), serializerOptions);
                    }

                    return response;
                }
                else
                {
                    Debug.WriteLine($"No rows returned for distinct match IDs.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred obtaining unique match IDs. {ex.Message}");
            }

            return null;
        }

        internal static List<MatchTableEntity> GetMatches(string playerXuid, string boundaryTime, int boundaryLimit)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = GetQuery("Select", "PlayerMatches");
                        command.Parameters.AddWithValue("PlayerXuid", playerXuid);
                        command.Parameters.AddWithValue("BoundaryTime", boundaryTime);
                        command.Parameters.AddWithValue("BoundaryLimit", boundaryLimit);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                List<MatchTableEntity> matches = new();
                                while (reader.Read())
                                {
                                    var matchOrdinal = reader.GetOrdinal("MatchId");
                                    var startTimeOrdinal = reader.GetOrdinal("StartTime");
                                    var rankOrdinal = reader.GetOrdinal("Rank");
                                    var outcomeOrdinal = reader.GetOrdinal("Outcome");
                                    var gameVariantCategoryOrdinal = reader.GetOrdinal("GameVariantCategory");
                                    var mapOrdinal = reader.GetOrdinal("Map");
                                    var playlistOrdinal = reader.GetOrdinal("Playlist");
                                    var gameVariantOrdinal = reader.GetOrdinal("GameVariant");
                                    var durationOrdinal = reader.GetOrdinal("Duration");

                                    MatchTableEntity entity = new()
                                    {
                                        MatchId = reader.IsDBNull(matchOrdinal) ? string.Empty : reader.GetFieldValue<string>(matchOrdinal),
                                        StartTime = reader.IsDBNull(startTimeOrdinal) ? DateTimeOffset.UnixEpoch : reader.GetFieldValue<DateTimeOffset>(startTimeOrdinal).ToLocalTime(),
                                        Rank = reader.IsDBNull(rankOrdinal) ? 0 : reader.GetFieldValue<int>(rankOrdinal),
                                        Outcome = reader.IsDBNull(outcomeOrdinal) ? Outcome.DidNotFinish : reader.GetFieldValue<Outcome>(outcomeOrdinal),
                                        Category = reader.IsDBNull(gameVariantCategoryOrdinal) ? GameVariantCategory.None : reader.GetFieldValue<GameVariantCategory>(gameVariantCategoryOrdinal),
                                        Map = reader.IsDBNull(mapOrdinal) ? string.Empty : reader.GetFieldValue<string>(mapOrdinal),
                                        Playlist = reader.IsDBNull(playlistOrdinal) ? string.Empty : reader.GetFieldValue<string>(playlistOrdinal),
                                        GameVariant = reader.IsDBNull(gameVariantOrdinal) ? string.Empty : reader.GetFieldValue<string>(gameVariantOrdinal),
                                        Duration = reader.IsDBNull(durationOrdinal) ? string.Empty : reader.GetFieldValue<string>(durationOrdinal),
                                    };

                                    matches.Add(entity);
                                }

                                return matches;
                            }
                            else
                            {
                                Debug.WriteLine($"No rows returned for distinct match IDs.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred obtaining unique match IDs. {ex.Message}");
            }

            return null;
        }

        internal static async Task<bool> UpdateMatchRecords(IEnumerable<Guid> matchIds)
        {
            try
            {
                int matchAvailable = 0;
                int playerStatsAvailable = 0;

                HaloApiResultContainer<MatchStats, RawResponseContainer>? matchStats = null;

                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    int matchCounter = 1;
                    int matchesTotal = matchIds.Count();

                    foreach (var matchId in matchIds)
                    {
                        MatchesViewModel.Instance.MatchLoadingParameter = matchId.ToString();

                        var completionProgress = (double)matchCounter / (double)matchesTotal * 100.0;

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = $"SELECT EXISTS(SELECT 1 FROM MatchStats WHERE MatchId='{matchId}') AS MATCH_AVAILABLE, EXISTS(SELECT 1 FROM PlayerMatchStats WHERE MatchId='{matchId}') AS PLAYER_STATS_AVAILABLE;";

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        matchAvailable = reader.GetFieldValue<int>(0);
                                        playerStatsAvailable = reader.GetFieldValue<int>(1);
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Something went wrong. Could not communicate with the database to get match availability.");
                                }

                                matchCounter++;
                            }
                        }

                        // MATCH_AVAILABLE - if the value is zero, that means we do not have the match data.
                        if (matchAvailable == 0)
                        {
                            Debug.WriteLine($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Getting match stats for {matchId}...");
                            matchStats = await UserContextManager.HaloClient.StatsGetMatchStats(matchId.ToString());

                            if (matchStats != null && matchStats.Result != null)
                            {
                                var processedMatchAssetParameters = await UpdateMatchAssetRecords(matchStats.Result);
                                var insertMatchStatsQuery = System.IO.File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Queries", "Insert", "MatchStats.sql"), Encoding.UTF8);

                                using (var insertionCommand = connection.CreateCommand())
                                {
                                    insertionCommand.CommandText = insertMatchStatsQuery;
                                    insertionCommand.Parameters.AddWithValue("$ResponseBody", matchStats.Response.Message);

                                    var insertionResult = insertionCommand.ExecuteNonQuery();

                                    if (insertionResult > 0)
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
                        if (playerStatsAvailable == 0)
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

                                        using (var insertionCommand = connection.CreateCommand())
                                        {
                                            insertionCommand.CommandText = insertMatchStatsQuery;
                                            insertionCommand.Parameters.AddWithValue("$MatchId", matchId.ToString());
                                            insertionCommand.Parameters.AddWithValue("$ResponseBody", playerStatsSnapshot.Response.Message);

                                            var insertionResult = insertionCommand.ExecuteNonQuery();

                                            if (insertionResult > 0)
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
                bool mapAvailable = false;
                bool gameVariantAvailable = false;
                bool engineGameVariantAvailable = false;

                // These values are defaulted to 1 because it's possible for a match to not
                // have an associated playlist
                bool playlistAvailable = true;
                bool playlistMapModePairAvailable = true;

                UGCGameVariant targetGameVariant = null;

                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT EXISTS(SELECT 1 FROM Maps WHERE AssetId='{result.MatchInfo.MapVariant.AssetId}' AND VersionId='{result.MatchInfo.MapVariant.VersionId}') AS MAP_AVAILABLE," +
                                              $"EXISTS(SELECT 1 FROM GameVariants WHERE AssetId='{result.MatchInfo.UgcGameVariant.AssetId}' AND VersionId='{result.MatchInfo.UgcGameVariant.VersionId}') AS GAMEVARIANT_AVAILABLE";

                        if (result.MatchInfo.Playlist != null)
                        {
                            command.CommandText += $",EXISTS(SELECT 1 FROM Playlists WHERE AssetId='{result.MatchInfo.Playlist.AssetId}' AND VersionId='{result.MatchInfo.Playlist.VersionId}') AS PLAYLIST_AVAILABLE";
                        }

                        if (result.MatchInfo.PlaylistMapModePair != null)
                        {
                            command.CommandText += $",EXISTS(SELECT 1 FROM PlaylistMapModePairs WHERE AssetId='{result.MatchInfo.PlaylistMapModePair.AssetId}' AND VersionId='{result.MatchInfo.PlaylistMapModePair.VersionId}') AS PLAYLISTMAPMODEPAIR_AVAILABLE";
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    mapAvailable = Convert.ToBoolean(reader.GetFieldValue<int>(reader.GetOrdinal("MAP_AVAILABLE")));
                                    playlistAvailable = result.MatchInfo.Playlist != null ? Convert.ToBoolean(reader.GetFieldValue<int>(reader.GetOrdinal("PLAYLIST_AVAILABLE"))) : true;
                                    playlistMapModePairAvailable = result.MatchInfo.PlaylistMapModePair != null ? Convert.ToBoolean(reader.GetFieldValue<int>(reader.GetOrdinal("PLAYLISTMAPMODEPAIR_AVAILABLE"))) : true;
                                    gameVariantAvailable = Convert.ToBoolean(reader.GetFieldValue<int>(reader.GetOrdinal("GAMEVARIANT_AVAILABLE")));
                                }
                            }
                        }
                    }

                    if (!mapAvailable)
                    {
                        // Map is not available
                        var map = await UserContextManager.HaloClient.HIUGCDiscoveryGetMap(result.MatchInfo.MapVariant.AssetId.ToString(), result.MatchInfo.MapVariant.VersionId.ToString());
                        if (map != null && map.Result != null && map.Response.Code == 200)
                        {
                            using (var insertionCommand = connection.CreateCommand())
                            {
                                insertionCommand.CommandText = GetQuery("Insert", "Maps");
                                insertionCommand.Parameters.AddWithValue("$ResponseBody", map.Response.Message);

                                var insertionResult = insertionCommand.ExecuteNonQuery();

                                if (insertionResult > 0)
                                {
                                    Debug.WriteLine($"Stored map: {result.MatchInfo.MapVariant.AssetId}/{result.MatchInfo.MapVariant.VersionId}");
                                }
                            }
                        }
                    }

                    if (!playlistAvailable)
                    {
                        // Playlist is not available
                        var playlist = await UserContextManager.HaloClient.HIUGCDiscoveryGetPlaylist(result.MatchInfo.Playlist.AssetId.ToString(), result.MatchInfo.Playlist.VersionId.ToString(), UserContextManager.HaloClient.ClearanceToken);
                        if (playlist != null && playlist.Result != null && playlist.Response.Code == 200)
                        {
                            using (var insertionCommand = connection.CreateCommand())
                            {
                                insertionCommand.CommandText = GetQuery("Insert", "Playlists");
                                insertionCommand.Parameters.AddWithValue("$ResponseBody", playlist.Response.Message);

                                var insertionResult = insertionCommand.ExecuteNonQuery();

                                if (insertionResult > 0)
                                {
                                    Debug.WriteLine($"Stored playlist: {result.MatchInfo.Playlist.AssetId}/{result.MatchInfo.Playlist.VersionId}");
                                }
                            }
                        }
                    }

                    if (!playlistMapModePairAvailable)
                    {
                        // Playlist + map mode pair is not available
                        var playlistMmp = await UserContextManager.HaloClient.HIUGCDiscoveryGetMapModePair(result.MatchInfo.PlaylistMapModePair.AssetId.ToString(), result.MatchInfo.PlaylistMapModePair.VersionId.ToString(), UserContextManager.HaloClient.ClearanceToken);
                        if (playlistMmp != null && playlistMmp.Result != null && playlistMmp.Response.Code == 200)
                        {
                            using (var insertionCommand = connection.CreateCommand())
                            {
                                insertionCommand.CommandText = GetQuery("Insert", "PlaylistMapModePairs");
                                insertionCommand.Parameters.AddWithValue("$ResponseBody", playlistMmp.Response.Message);

                                var insertionResult = insertionCommand.ExecuteNonQuery();

                                if (insertionResult > 0)
                                {
                                    Debug.WriteLine($"Stored playlist + map mode pair: {result.MatchInfo.PlaylistMapModePair.AssetId}/{result.MatchInfo.PlaylistMapModePair.VersionId}");
                                }
                            }
                        }
                    }

                    if (!gameVariantAvailable)
                    {
                        // Game variant is not available
                        var gameVariant = await UserContextManager.HaloClient.HIUGCDiscoveryGetUgcGameVariant(result.MatchInfo.UgcGameVariant.AssetId.ToString(), result.MatchInfo.UgcGameVariant.VersionId.ToString());
                        if (gameVariant != null && gameVariant.Result != null && gameVariant.Response.Code == 200)
                        {
                            targetGameVariant = gameVariant.Result;

                            using (var insertionCommand = connection.CreateCommand())
                            {
                                insertionCommand.CommandText = GetQuery("Insert", "GameVariants");
                                insertionCommand.Parameters.AddWithValue("$ResponseBody", gameVariant.Response.Message);

                                var insertionResult = insertionCommand.ExecuteNonQuery();

                                if (insertionResult > 0)
                                {
                                    Debug.WriteLine($"Stored game variant: {result.MatchInfo.UgcGameVariant.AssetId}/{result.MatchInfo.UgcGameVariant.VersionId}");
                                }
                            }

                            using (var egvQueryCommand = connection.CreateCommand())
                            {
                                egvQueryCommand.CommandText = $"SELECT EXISTS(SELECT 1 FROM Maps WHERE AssetId='{gameVariant.Result.EngineGameVariantLink.AssetId}' AND VersionId='{gameVariant.Result.EngineGameVariantLink.VersionId}') AS ENGINEGAMEVARIANT_AVAILABLE";

                                using (var egvReader = egvQueryCommand.ExecuteReader())
                                {
                                    if (egvReader.HasRows)
                                    {
                                        while (egvReader.Read())
                                        {
                                            engineGameVariantAvailable = Convert.ToBoolean(egvReader.GetFieldValue<int>(egvReader.GetOrdinal("ENGINEGAMEVARIANT_AVAILABLE")));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!engineGameVariantAvailable && targetGameVariant != null)
                    {
                        var engineGameVariant = await UserContextManager.HaloClient.HIUGCDiscoveryGetEngineGameVariant(targetGameVariant.EngineGameVariantLink.AssetId.ToString(), targetGameVariant.EngineGameVariantLink.VersionId.ToString());

                        if (engineGameVariant != null && engineGameVariant.Result != null && engineGameVariant.Response.Code == 200)
                        {
                            using (var egvInsertionCommand = connection.CreateCommand())
                            {
                                egvInsertionCommand.CommandText = GetQuery("Insert", "EngineGameVariants");
                                egvInsertionCommand.Parameters.AddWithValue("$ResponseBody", engineGameVariant.Response.Message);

                                var insertionResult = egvInsertionCommand.ExecuteNonQuery();

                                if (insertionResult > 0)
                                {
                                    Debug.WriteLine($"Stored engine game variant: {engineGameVariant.Result.AssetId}/{engineGameVariant.Result.VersionId}");
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

        internal static List<Medal> GetMedals()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = GetQuery("Select", "LatestMedalsSnapshot");

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                List<Medal> matchIds = new();
                                while (reader.Read())
                                {
                                    matchIds.AddRange(JsonSerializer.Deserialize<List<Medal>>(reader.GetString(0)));
                                }

                                return matchIds;
                            }
                            else
                            {
                                Debug.WriteLine($"No rows returned for distinct match IDs.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred obtaining unique match IDs. {ex.Message}");
            }

            return null;
        }

        internal static bool UpdateOperationRewardTracks(string response, string path)
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            using var insertionCommand = connection.CreateCommand();

            insertionCommand.CommandText = GetQuery("Insert", "OperationRewardTracks");
            insertionCommand.Parameters.AddWithValue("$ResponseBody", response);
            insertionCommand.Parameters.AddWithValue("$Path", path);
            insertionCommand.Parameters.AddWithValue("$LastUpdated", DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));

            connection.Open();

            var insertionResult = insertionCommand.ExecuteNonQuery();

            if (insertionResult > 0)
            {
                Debug.WriteLine($"Stored reward track {path}.");
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool UpdateInventoryItems(string response, string path)
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            using var insertionCommand = connection.CreateCommand();

            insertionCommand.CommandText = GetQuery("Insert", "InventoryItems");
            insertionCommand.Parameters.AddWithValue("$ResponseBody", response);
            insertionCommand.Parameters.AddWithValue("$Path", path);
            insertionCommand.Parameters.AddWithValue("$LastUpdated", DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));

            connection.Open();

            var insertionResult = insertionCommand.ExecuteNonQuery();

            if (insertionResult > 0)
            {
                Debug.WriteLine($"Stored inventory item {path}.");
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool IsOperationRewardTrackAvailable(string path)
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            using var command = connection.CreateCommand();

            command.CommandText = $"SELECT EXISTS(SELECT 1 FROM OperationRewardTracks WHERE Path='{path}') AS OPERATION_AVAILABLE";

            connection.Open();

            using var reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    var operationAvailable = reader.GetFieldValue<int>(0);
                    if (operationAvailable > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        internal static bool IsInventoryItemAvailable(string path)
        {
            using var connection = new SqliteConnection($"Data Source={DatabasePath}");
            using var command = connection.CreateCommand();

            command.CommandText = $"SELECT EXISTS(SELECT 1 FROM InventoryItems WHERE Path='{path}') AS INVENTORY_ITEM_AVAILABLE";

            connection.Open();

            using var reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    var operationAvailable = reader.GetFieldValue<int>(0);
                    if (operationAvailable > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        internal static InGameItem GetInventoryItem(string path)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = GetQuery("Select", "InventoryItem");
                        command.Parameters.AddWithValue("$Path", path);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    return JsonSerializer.Deserialize<InGameItem>(reader.GetString(0), serializerOptions);
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"No rows returned for distinct match IDs.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred obtaining unique match IDs. {ex.Message}");
            }

            return null;
        }
    }
}
