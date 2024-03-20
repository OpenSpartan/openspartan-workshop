using Den.Dev.Orion.Converters;
using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.Data.Sqlite;
using NLog;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace OpenSpartan.Workshop.Data
{
    internal sealed class DataHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        internal static string DatabasePath => Path.Combine(Core.Configuration.AppDataDirectory, "data", $"{HomeViewModel.Instance.Xuid}.db");

        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new EmptyDateStringToNullJsonConverter(),
                new XmlDurationToTimeSpanJsonConverter(),
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
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"WAL journaling mode not set.");
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Journaling mode modification exception: {ex.Message}");
            }

            return null;
        }

        internal static bool BootstrapDatabase()
        {
            try
            {
                EnsureDatabaseDirectoryExists();

                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                BootstrapTableIfNotExists(connection, "ServiceRecordSnapshots");
                BootstrapTableIfNotExists(connection, "PlayerMatchStats");
                BootstrapTableIfNotExists(connection, "MatchStats");
                BootstrapTableIfNotExists(connection, "Maps");
                BootstrapTableIfNotExists(connection, "GameVariants");
                BootstrapTableIfNotExists(connection, "Playlists");
                BootstrapTableIfNotExists(connection, "PlaylistMapModePairs");
                BootstrapTableIfNotExists(connection, "EngineGameVariants");
                BootstrapTableIfNotExists(connection, "OperationRewardTracks");
                BootstrapTableIfNotExists(connection, "InventoryItems");
                BootstrapTableIfNotExists(connection, "OwnedInventoryItems");

                SetupIndices(connection);

                return true;
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Database bootstrapping failure: {ex.Message}");
                return false;
            }
        }

        private static void EnsureDatabaseDirectoryExists()
        {
            FileInfo file = new(DatabasePath);
            file.Directory.Create();
        }

        private static void BootstrapTableIfNotExists(SqliteConnection connection, string tableName)
        {
            if (!connection.IsTableAvailable(tableName))
            {
                connection.BootstrapTable(tableName);
            }
        }

        private static void SetupIndices(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = GetQuery("Bootstrap", "Indexes");

            int outcome = command.ExecuteNonQuery();

            if (outcome > 0)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Info("Indices provisioned.");
            }
            else
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Warn("Indices could not be set up. If this is not the first run, then those are likely already configured.");
            }
        }

        internal static bool InsertServiceRecordEntry(string serviceRecordJson)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = GetQuery("Insert", "ServiceRecord"); ;
                command.Parameters.AddWithValue("$ResponseBody", serviceRecordJson);
                command.Parameters.AddWithValue("$SnapshotTimestamp", DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));

                using var reader = command.ExecuteReader();
                if (reader.RecordsAffected > 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Error inserting service record entry. {ex.Message}");
                return false;
            }
        }

        internal static List<Guid> GetMatchIds()
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Select", "DistinctMatchIds");

                using var reader = command.ExecuteReader();
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
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Warn($"No rows returned for distinct match IDs.");
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"An error occurred obtaining unique match IDs. {ex.Message}");
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
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Warn($"No rows returned for operations.");
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"An error occurred obtaining operations from database. {ex.Message}");
            }

            return null;
        }

        internal static List<MatchTableEntity> GetMatches(string playerXuid, string boundaryTime, int boundaryLimit)
        {
            return GetMatchesInternal(playerXuid, null, boundaryTime, boundaryLimit);
        }

        internal static List<MatchTableEntity> GetMatchesWithMedal(string playerXuid, long medalNameId, string boundaryTime, int boundaryLimit)
        {
            return GetMatchesInternal(playerXuid, medalNameId, boundaryTime, boundaryLimit);
        }

        private static List<MatchTableEntity> GetMatchesInternal(string playerXuid, long? medalNameId, string boundaryTime, int boundaryLimit)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                if (medalNameId.HasValue)
                {
                    command.CommandText = GetQuery("Select", "PlayerMatchesBasedOnMedal");
                    command.Parameters.AddWithValue("MedalNameId", medalNameId.Value);
                }
                else
                {
                    command.CommandText = GetQuery("Select", "PlayerMatches");
                }

                command.Parameters.AddWithValue("PlayerXuid", playerXuid);
                command.Parameters.AddWithValue("BoundaryTime", boundaryTime);
                command.Parameters.AddWithValue("BoundaryLimit", boundaryLimit);

                using var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    List<MatchTableEntity> matches = [];
                    while (reader.Read())
                    {
                        var matchEntry = ReadMatchTableEntity(reader);
                        
                        if (matchEntry.PlayerTeamStats[0].Stats.CoreStats.Medals != null && matchEntry.PlayerTeamStats[0].Stats.CoreStats.Medals.Count > 0)
                        {
                            matchEntry.PlayerTeamStats[0].Stats.CoreStats.Medals = UserContextManager.EnrichMedalMetadata(matchEntry.PlayerTeamStats[0].Stats.CoreStats.Medals);
                        }

                        matches.Add(matchEntry);
                    }

                    return matches;
                }
                else
                {
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Warn($"No rows returned for player match IDs.");
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"An error occurred obtaining matches. {ex.Message}");
            }

            return null;
        }

        private static MatchTableEntity ReadMatchTableEntity(SqliteDataReader reader)
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
            var lastTeamIdOrdinal = reader.GetOrdinal("LastTeamId");
            var teamsOrdinal = reader.GetOrdinal("Teams");
            var participationInfoOrdinal = reader.GetOrdinal("ParticipationInfo");
            var playerTeamStatsOrdinal = reader.GetOrdinal("PlayerTeamStats");
            var teamMmrOrdinal = reader.GetOrdinal("TeamMmr");
            var expectedDeathsOrdinal = reader.GetOrdinal("ExpectedDeaths");
            var expectedKillsOrdinal = reader.GetOrdinal("ExpectedKills");
            var postMatchOrdinal = reader.GetOrdinal("PostMatchCsr");
            var preMatchCsrOrdinal = reader.GetOrdinal("PreMatchCsr");
            var tierOrdinal = reader.GetOrdinal("Tier");
            var tierStartOrdinal = reader.GetOrdinal("TierStart");
            var tierLevelOrdinal = reader.GetOrdinal("TierLevel");
            var initialMeasurementMatchesOrdinal = reader.GetOrdinal("InitialMeasurementMatches");
            var measurementMatchesRemainingOrdinal = reader.GetOrdinal("MeasurementMatchesRemaining");
            var nextTierOrdinal = reader.GetOrdinal("NextTier");
            var nextTierLevelOrdinal = reader.GetOrdinal("NextTierLevel");
            var nextTierStartOrdinal = reader.GetOrdinal("NextTierStart");

            return new MatchTableEntity
            {
                MatchId = reader.IsDBNull(matchOrdinal) ? string.Empty : reader.GetFieldValue<string>(matchOrdinal),
                StartTime = reader.IsDBNull(startTimeOrdinal) ? DateTimeOffset.UnixEpoch : reader.GetFieldValue<DateTimeOffset>(startTimeOrdinal).ToLocalTime(),
                Rank = reader.IsDBNull(rankOrdinal) ? 0 : reader.GetFieldValue<int>(rankOrdinal),
                Outcome = reader.IsDBNull(outcomeOrdinal) ? Outcome.DidNotFinish : reader.GetFieldValue<Outcome>(outcomeOrdinal),
                Category = reader.IsDBNull(gameVariantCategoryOrdinal) ? GameVariantCategory.None : reader.GetFieldValue<GameVariantCategory>(gameVariantCategoryOrdinal),
                Map = reader.IsDBNull(mapOrdinal) ? string.Empty : reader.GetFieldValue<string>(mapOrdinal),
                Playlist = reader.IsDBNull(playlistOrdinal) ? string.Empty : reader.GetFieldValue<string>(playlistOrdinal),
                GameVariant = reader.IsDBNull(gameVariantOrdinal) ? string.Empty : reader.GetFieldValue<string>(gameVariantOrdinal),
                Duration = reader.IsDBNull(durationOrdinal) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(reader.GetFieldValue<string>(durationOrdinal)),
                LastTeamId = reader.IsDBNull(durationOrdinal) ? null : reader.GetFieldValue<int>(lastTeamIdOrdinal),
                Teams = reader.IsDBNull(teamsOrdinal) ? null : JsonSerializer.Deserialize<List<Team>>(reader.GetFieldValue<string>(teamsOrdinal), serializerOptions),
                ParticipationInfo = reader.IsDBNull(teamsOrdinal) ? null : JsonSerializer.Deserialize<ParticipationInfo>(reader.GetFieldValue<string>(participationInfoOrdinal), serializerOptions),
                PlayerTeamStats = reader.IsDBNull(teamsOrdinal) ? null : JsonSerializer.Deserialize<List<PlayerTeamStat>>(reader.GetFieldValue<string>(playerTeamStatsOrdinal), serializerOptions),
                TeamMmr = reader.IsDBNull(teamMmrOrdinal) ? null : reader.GetFieldValue<float>(teamMmrOrdinal),
                ExpectedDeaths = reader.IsDBNull(expectedDeathsOrdinal) ? null : reader.GetFieldValue<float>(expectedDeathsOrdinal),
                ExpectedKills = reader.IsDBNull(expectedKillsOrdinal) ? null : reader.GetFieldValue<float>(expectedKillsOrdinal),
                PostMatchCsr = reader.IsDBNull(postMatchOrdinal) ? null : reader.GetFieldValue<int>(postMatchOrdinal),
                PreMatchCsr = reader.IsDBNull(preMatchCsrOrdinal) ? null : reader.GetFieldValue<int>(preMatchCsrOrdinal),
                Tier = reader.IsDBNull(tierOrdinal) ? null : reader.GetFieldValue<string>(tierOrdinal),
                TierStart = reader.IsDBNull(tierStartOrdinal) ? null : reader.GetFieldValue<int>(tierStartOrdinal),
                TierLevel = reader.IsDBNull(tierLevelOrdinal) ? null : reader.GetFieldValue<int>(tierLevelOrdinal),
                InitialMeasurementMatches = reader.IsDBNull(initialMeasurementMatchesOrdinal) ? null : reader.GetFieldValue<int>(initialMeasurementMatchesOrdinal),
                MeasurementMatchesRemaining = reader.IsDBNull(measurementMatchesRemainingOrdinal) ? null : reader.GetFieldValue<int>(measurementMatchesRemainingOrdinal),
                NextTier = reader.IsDBNull(nextTierOrdinal) ? null : reader.GetFieldValue<string>(nextTierOrdinal),
                NextTierLevel = reader.IsDBNull(nextTierLevelOrdinal) ? null : reader.GetFieldValue<int>(nextTierLevelOrdinal),
                NextTierStart = reader.IsDBNull(nextTierStartOrdinal) ? null : reader.GetFieldValue<int>(nextTierStartOrdinal),
            };
        }


        internal static (bool MatchAvailable, bool StatsAvailable) GetMatchStatsAvailability(string matchId)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = GetQuery("Select", "MatchStatsAvailability");
                    command.Parameters.AddWithValue("$MatchId", matchId);

                    using var reader = command.ExecuteReader();
                    if (reader.HasRows && reader.Read())
                    {
                        return (Convert.ToBoolean(reader.GetFieldValue<int>(0)), Convert.ToBoolean(reader.GetFieldValue<int>(1)));
                    }
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"An error occurred obtaining match and stats availability. {ex.Message}");
            }

            return (false, false); // Default values if the data retrieval fails
        }

        internal static bool InsertPlayerMatchStats(string matchId, string statsBody)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var insertionCommand = connection.CreateCommand();
                insertionCommand.CommandText = GetQuery("Insert", "PlayerMatchStats");
                insertionCommand.Parameters.AddWithValue("$MatchId", matchId);
                insertionCommand.Parameters.AddWithValue("$ResponseBody", statsBody);

                var insertionResult = insertionCommand.ExecuteNonQuery();

                if (insertionResult > 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"An error occurred inserting player match and stats. {ex.Message}");
            }

            return false;
        }

        internal static bool InsertMatchStats (string matchBody)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var insertionCommand = connection.CreateCommand();
                insertionCommand.CommandText = GetQuery("Insert", "MatchStats");
                insertionCommand.Parameters.AddWithValue("$ResponseBody", matchBody);

                var insertionResult = insertionCommand.ExecuteNonQuery();

                if (insertionResult > 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"An error occurred inserting match and stats. {ex.Message}");
            }

            return false;
        }

        internal static async Task<bool> UpdateMatchAssetRecords(MatchStats result)
        {
            try
            {
                bool mapAvailable = false;
                bool gameVariantAvailable = false;
                bool engineGameVariantAvailable = false;

                bool playlistAvailable = true;
                bool playlistMapModePairAvailable = true;

                UGCGameVariant targetGameVariant = null;

                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                await connection.OpenAsync();
                
                string query = "SELECT EXISTS(SELECT 1 FROM Maps WHERE AssetId = $MapAssetId AND VersionId = $MapVersionId) AS MAP_AVAILABLE, " +
                      "EXISTS(SELECT 1 FROM GameVariants WHERE AssetId = $GameVariantAssetId AND VersionId = $GameVariantVersionId) AS GAMEVARIANT_AVAILABLE";

                if (result.MatchInfo.Playlist != null)
                {
                    query += ", EXISTS(SELECT 1 FROM Playlists WHERE AssetId = $PlaylistAssetId AND VersionId = $PlaylistVersionId) AS PLAYLIST_AVAILABLE";
                }

                if (result.MatchInfo.PlaylistMapModePair != null)
                {
                    query += ", EXISTS(SELECT 1 FROM PlaylistMapModePairs WHERE AssetId = $PlaylistMapModePairAssetId AND VersionId = $PlaylistMapModePairVersionId) AS PLAYLISTMAPMODEPAIR_AVAILABLE";
                }

                using (SqliteCommand command = new(query, connection))
                {
                    command.Parameters.AddWithValue("$MapAssetId", result.MatchInfo.MapVariant.AssetId.ToString());
                    command.Parameters.AddWithValue("$MapVersionId", result.MatchInfo.MapVariant.VersionId.ToString());
                    command.Parameters.AddWithValue("$GameVariantAssetId", result.MatchInfo.UgcGameVariant.AssetId.ToString());
                    command.Parameters.AddWithValue("$GameVariantVersionId", result.MatchInfo.UgcGameVariant.VersionId.ToString());

                    if (result.MatchInfo.Playlist != null)
                    {
                        command.Parameters.AddWithValue("$PlaylistAssetId", result.MatchInfo.Playlist.AssetId.ToString());
                        command.Parameters.AddWithValue("$PlaylistVersionId", result.MatchInfo.Playlist.VersionId.ToString());
                    }

                    if (result.MatchInfo.PlaylistMapModePair != null)
                    {
                        command.Parameters.AddWithValue("$PlaylistMapModePairAssetId", result.MatchInfo.PlaylistMapModePair.AssetId.ToString());
                        command.Parameters.AddWithValue("$PlaylistMapModePairVersionId", result.MatchInfo.PlaylistMapModePair.VersionId.ToString());
                    }

                    using var reader = await command.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        mapAvailable = await reader.GetFieldValueAsync<int>("MAP_AVAILABLE") == 1;
                        playlistAvailable = result.MatchInfo.Playlist != null && await reader.GetFieldValueAsync<int>("PLAYLIST_AVAILABLE") == 1;
                        playlistMapModePairAvailable = result.MatchInfo.PlaylistMapModePair != null && await reader.GetFieldValueAsync<int>("PLAYLISTMAPMODEPAIR_AVAILABLE") == 1;
                        gameVariantAvailable = await reader.GetFieldValueAsync<int>("GAMEVARIANT_AVAILABLE") == 1;
                    }
                }

                if (!mapAvailable)
                {
                    var map = await UserContextManager.SafeAPICall(async () => await UserContextManager.HaloClient.HIUGCDiscoveryGetMap(result.MatchInfo.MapVariant.AssetId.ToString(), result.MatchInfo.MapVariant.VersionId.ToString()));
                    if (map != null && map.Result != null && map.Response.Code == 200)
                    {
                        using var insertionCommand = connection.CreateCommand();
                        insertionCommand.CommandText = GetQuery("Insert", "Maps");
                        insertionCommand.Parameters.AddWithValue("$ResponseBody", map.Response.Message);

                        var insertionResult = await insertionCommand.ExecuteNonQueryAsync();

                        if (insertionResult > 0)
                        {
                            if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Stored map: {result.MatchInfo.MapVariant.AssetId}/{result.MatchInfo.MapVariant.VersionId}");
                        }
                    }
                }

                if (!playlistAvailable)
                {
                    if (result.MatchInfo.Playlist != null)
                    {
                        var playlist = await UserContextManager.SafeAPICall(async () => await UserContextManager.HaloClient.HIUGCDiscoveryGetPlaylist(result.MatchInfo.Playlist.AssetId.ToString(), result.MatchInfo.Playlist.VersionId.ToString(), UserContextManager.HaloClient.ClearanceToken));
                        if (playlist != null && playlist.Result != null && playlist.Response.Code == 200)
                        {
                            using var insertionCommand = connection.CreateCommand();
                            insertionCommand.CommandText = GetQuery("Insert", "Playlists");
                            insertionCommand.Parameters.AddWithValue("$ResponseBody", playlist.Response.Message);

                            var insertionResult = await insertionCommand.ExecuteNonQueryAsync();

                            if (insertionResult > 0)
                            {
                                if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Stored playlist: {result.MatchInfo.Playlist.AssetId}/{result.MatchInfo.Playlist.VersionId}");
                            }
                        }
                    }
                }

                if (!playlistMapModePairAvailable)
                {
                    if (result.MatchInfo.PlaylistMapModePair != null)
                    {
                        var playlistMmp = await UserContextManager.SafeAPICall(async () => await UserContextManager.HaloClient.HIUGCDiscoveryGetMapModePair(result.MatchInfo.PlaylistMapModePair.AssetId.ToString(), result.MatchInfo.PlaylistMapModePair.VersionId.ToString(), UserContextManager.HaloClient.ClearanceToken));
                        if (playlistMmp != null && playlistMmp.Result != null && playlistMmp.Response.Code == 200)
                        {
                            using var insertionCommand = connection.CreateCommand();
                            insertionCommand.CommandText = GetQuery("Insert", "PlaylistMapModePairs");
                            insertionCommand.Parameters.AddWithValue("$ResponseBody", playlistMmp.Response.Message);

                            var insertionResult = await insertionCommand.ExecuteNonQueryAsync();

                            if (insertionResult > 0)
                            {
                                if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Stored playlist + map mode pair: {result.MatchInfo.PlaylistMapModePair.AssetId}/{result.MatchInfo.PlaylistMapModePair.VersionId}");
                            }
                        }
                    }
                }

                if (!gameVariantAvailable)
                {
                    var gameVariant = await UserContextManager.SafeAPICall(async () => await UserContextManager.HaloClient.HIUGCDiscoveryGetUgcGameVariant(result.MatchInfo.UgcGameVariant.AssetId.ToString(), result.MatchInfo.UgcGameVariant.VersionId.ToString()));
                    if (gameVariant != null && gameVariant.Result != null && gameVariant.Response.Code == 200)
                    {
                        targetGameVariant = gameVariant.Result;

                        using (var insertionCommand = connection.CreateCommand())
                        {
                            insertionCommand.CommandText = GetQuery("Insert", "GameVariants");
                            insertionCommand.Parameters.AddWithValue("$ResponseBody", gameVariant.Response.Message);

                            var insertionResult = await insertionCommand.ExecuteNonQueryAsync();

                            if (insertionResult > 0)
                            {
                                if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Stored game variant: {result.MatchInfo.UgcGameVariant.AssetId}/{result.MatchInfo.UgcGameVariant.VersionId}");
                            }
                        }

                        using var egvQueryCommand = connection.CreateCommand();
                        egvQueryCommand.CommandText = $"SELECT EXISTS(SELECT 1 FROM EngineGameVariants WHERE AssetId='{gameVariant.Result.EngineGameVariantLink.AssetId}' AND VersionId='{gameVariant.Result.EngineGameVariantLink.VersionId}') AS ENGINEGAMEVARIANT_AVAILABLE";

                        using var egvReader = await egvQueryCommand.ExecuteReaderAsync();
                        if (await egvReader.ReadAsync())
                        {
                            engineGameVariantAvailable = egvReader.GetFieldValue<int>("ENGINEGAMEVARIANT_AVAILABLE") == 1;
                        }
                    }
                }

                if (!engineGameVariantAvailable && targetGameVariant != null)
                {
                    var engineGameVariant = await UserContextManager.SafeAPICall(async () => await UserContextManager.HaloClient.HIUGCDiscoveryGetEngineGameVariant(targetGameVariant.EngineGameVariantLink.AssetId.ToString(), targetGameVariant.EngineGameVariantLink.VersionId.ToString()));

                    if (engineGameVariant != null && engineGameVariant.Result != null && engineGameVariant.Response.Code == 200)
                    {
                        using var egvInsertionCommand = connection.CreateCommand();
                        egvInsertionCommand.CommandText = GetQuery("Insert", "EngineGameVariants");
                        egvInsertionCommand.Parameters.AddWithValue("$ResponseBody", engineGameVariant.Response.Message);

                        var insertionResult = await egvInsertionCommand.ExecuteNonQueryAsync();

                        if (insertionResult > 0)
                        {
                            if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Stored engine game variant: {engineGameVariant.Result.AssetId}/{engineGameVariant.Result.VersionId}");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Error updating match stats. {ex.Message}");
                return false;
            }
        }


        private static string GetQuery(string category, string target)
        {
            return System.IO.File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Queries", category, $"{target}.sql"), Encoding.UTF8);
        }

        internal static List<Medal> GetMedals()
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Select", "LatestMedalsSnapshot");

                using var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    List<Medal> matchIds = [];
                    while (reader.Read())
                    {
                        matchIds.AddRange(JsonSerializer.Deserialize<List<Medal>>(reader.GetString(0)));
                    }

                    return matchIds;
                }
                else
                {
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Warn($"No rows returned for medals.");
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"An error occurred obtaining medals from the database. {ex.Message}");
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
                if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Stored reward track {path}.");
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
                if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Stored inventory item {path}.");
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
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Select", "InventoryItem");
                command.Parameters.AddWithValue("$Path", path);

                using var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        return JsonSerializer.Deserialize<InGameItem>(reader.GetString(0), serializerOptions);
                    }
                }
                else
                {
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"No rows returned for inventory items query.");
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"An error occurred obtaining inventory items. {ex.Message}");
            }

            return null;
        }

        internal static async Task<bool> InsertOwnedInventoryItems(PlayerInventory result)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                await connection.OpenAsync();

                var command = GetQuery("Insert", "OwnedInventoryItems");

                foreach (var item in result.Items)
                {
                    using var insertionCommand = connection.CreateCommand();
                    insertionCommand.CommandText = command;
                    insertionCommand.Parameters.AddWithValue("$Amount", item.Amount);
                    insertionCommand.Parameters.AddWithValue("$ItemId", item.ItemId);
                    insertionCommand.Parameters.AddWithValue("$ItemPath", item.ItemPath);
                    insertionCommand.Parameters.AddWithValue("$ItemType", item.ItemType);
                    insertionCommand.Parameters.AddWithValue("$FirstAcquiredDate", item.FirstAcquiredDate.ISO8601Date);

                    var insertionResult = await insertionCommand.ExecuteNonQueryAsync();

                    if (insertionResult > 0)
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Stored owned inventory item {item.ItemId}.");
                    }
                    else
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Could not store owned inventory item {item.ItemId}.");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Error inserting owned inventory items. {ex.Message}");
                return false;
            }
        }

    }
}
