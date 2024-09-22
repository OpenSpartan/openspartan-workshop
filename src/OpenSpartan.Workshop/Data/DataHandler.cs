using Den.Dev.Orion.Converters;
using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.Data.Sqlite;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace OpenSpartan.Workshop.Data
{
    internal static class DataHandler
    {
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
                if (reader.Read())
                {
                    return reader.GetString(0).Trim();
                }

                LogEngine.Log($"WAL journaling mode not set.", LogSeverity.Error);
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Journaling mode modification exception: {ex.Message}", LogSeverity.Error);
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
                BootstrapTableIfNotExists(connection, "PlaylistCSRSnapshots");

                SetupIndices(connection);

                return true;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Database bootstrapping failure: {ex.Message}", LogSeverity.Error);
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
                LogEngine.Log("Indices provisioned.");
            }
            else
            {
                LogEngine.Log("Indices could not be set up. If this is not the first run, then those are likely already configured.", LogSeverity.Warning);
            }
        }

        internal static bool InsertServiceRecordEntry(string serviceRecordJson)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Insert", "ServiceRecord");
                command.Parameters.AddWithValue("$ResponseBody", serviceRecordJson);
                command.Parameters.AddWithValue("$SnapshotTimestamp", DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));

                int recordsAffected = command.ExecuteNonQuery();
                return recordsAffected > 1;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Error inserting service record entry. {ex.Message}", LogSeverity.Error);
                return false;
            }
        }

        internal static bool InsertPlaylistCSRSnapshot(string playlistId, string playlistVersion, string playlistCsrJson)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Insert", "PlaylistCSR");
                command.Parameters.AddWithValue("$ResponseBody", playlistCsrJson);
                command.Parameters.AddWithValue("$PlaylistId", playlistId);
                command.Parameters.AddWithValue("$PlaylistVersion", playlistVersion);
                command.Parameters.AddWithValue("$SnapshotTimestamp", DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));

                int recordsAffected = command.ExecuteNonQuery();
                return recordsAffected > 1;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Error inserting playlist CSR entry. {ex.Message}", LogSeverity.Error);
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
                List<Guid> matchIds = [];
                while (reader.Read())
                {
                    matchIds.Add(reader.GetGuid(0));
                }

                if (matchIds.Count == 0)
                {
                    LogEngine.Log("No rows returned for distinct match IDs.", LogSeverity.Warning);
                }

                return matchIds;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred obtaining unique match IDs. {ex.Message}", LogSeverity.Error);
                return [];
            }
        }

        internal static RewardTrackMetadata GetOperationResponseBody(string operationPath)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Select", "OperationResponseBody");
                command.Parameters.AddWithValue("$OperationPath", operationPath);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    string jsonString = reader.GetString(0).Trim();
                    return JsonSerializer.Deserialize<RewardTrackMetadata>(jsonString, serializerOptions);
                }

                LogEngine.Log("No rows returned for operations.", LogSeverity.Warning);
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred obtaining operations from database. {ex.Message}", LogSeverity.Error);
            }

            return null;
        }

        internal static int GetExistingMatchCount(IEnumerable<Guid> matchIds)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                // Build the command text with the matchIds directly embedded (literal query)
                string matchIdsString = string.Join(", ", matchIds.Select(g => $"'{g}'"));
                string query = GetQuery("Select", "ExistingMatchCount").Replace("$MatchGUIDList", matchIdsString, StringComparison.InvariantCultureIgnoreCase);

                using var command = connection.CreateCommand();
                command.CommandText = query;

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var resultOrdinal = reader.GetOrdinal("ExistingMatchCount");
                    return reader.IsDBNull(resultOrdinal) ? -1 : reader.GetFieldValue<int>(resultOrdinal);
                }
                else
                {
                    LogEngine.Log("No rows returned for existing match metadata.", LogSeverity.Warning);
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred obtaining match records from database. {ex.Message}", LogSeverity.Error);
            }

            return -1;
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
                    command.Parameters.AddWithValue("$MedalNameId", medalNameId.Value);
                }
                else
                {
                    command.CommandText = GetQuery("Select", "PlayerMatches");
                }

                command.Parameters.AddWithValue("$PlayerXuid", playerXuid);
                command.Parameters.AddWithValue("$BoundaryTime", boundaryTime);
                command.Parameters.AddWithValue("$BoundaryLimit", boundaryLimit);

                using var reader = command.ExecuteReader();
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

                if (matches.Count == 0)
                {
                    LogEngine.Log("No rows returned for player match IDs.", LogSeverity.Warning);
                }

                return matches;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred obtaining matches. {ex.Message}", LogSeverity.Error);
                return new List<MatchTableEntity>();
            }
        }

        private static MatchTableEntity ReadMatchTableEntity(SqliteDataReader reader)
        {
            var matchOrdinal = reader.GetOrdinal("MatchId");
            var startTimeOrdinal = reader.GetOrdinal("StartTime");
            var endTimeOrdinal = reader.GetOrdinal("EndTime");
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
            var expectedBronzeDeathsOrdinal = reader.GetOrdinal("ExpectedBronzeDeaths");
            var expectedBronzeKillsOrdinal = reader.GetOrdinal("ExpectedBronzeKills");
            var expectedSilverDeathsOrdinal = reader.GetOrdinal("ExpectedSilverDeaths");
            var expectedSilverKillsOrdinal = reader.GetOrdinal("ExpectedSilverKills");
            var expectedGoldDeathsOrdinal = reader.GetOrdinal("ExpectedGoldDeaths");
            var expectedGoldKillsOrdinal = reader.GetOrdinal("ExpectedGoldKills");
            var expectedPlatinumDeathsOrdinal = reader.GetOrdinal("ExpectedPlatinumDeaths");
            var expectedPlatinumKillsOrdinal = reader.GetOrdinal("ExpectedPlatinumKills");
            var expectedDiamondDeathsOrdinal = reader.GetOrdinal("ExpectedDiamondDeaths");
            var expectedDiamondKillsOrdinal = reader.GetOrdinal("ExpectedDiamondKills");
            var expectedOnyxDeathsOrdinal = reader.GetOrdinal("ExpectedOnyxDeaths");
            var expectedOnyxKillsOrdinal = reader.GetOrdinal("ExpectedOnyxKills");
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
                EndTime = reader.IsDBNull(startTimeOrdinal) ? DateTimeOffset.UnixEpoch : reader.GetFieldValue<DateTimeOffset>(endTimeOrdinal).ToLocalTime(),
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
                ExpectedBronzeDeaths = reader.IsDBNull(expectedBronzeDeathsOrdinal) ? null : reader.GetFieldValue<float>(expectedBronzeDeathsOrdinal),
                ExpectedBronzeKills = reader.IsDBNull(expectedBronzeKillsOrdinal) ? null : reader.GetFieldValue<float>(expectedBronzeKillsOrdinal),
                ExpectedSilverDeaths = reader.IsDBNull(expectedSilverDeathsOrdinal) ? null : reader.GetFieldValue<float>(expectedSilverDeathsOrdinal),
                ExpectedSilverKills = reader.IsDBNull(expectedSilverKillsOrdinal) ? null : reader.GetFieldValue<float>(expectedSilverKillsOrdinal),
                ExpectedGoldDeaths = reader.IsDBNull(expectedGoldDeathsOrdinal) ? null : reader.GetFieldValue<float>(expectedGoldDeathsOrdinal),
                ExpectedGoldKills = reader.IsDBNull(expectedGoldKillsOrdinal) ? null : reader.GetFieldValue<float>(expectedGoldKillsOrdinal),
                ExpectedPlatinumDeaths = reader.IsDBNull(expectedPlatinumDeathsOrdinal) ? null : reader.GetFieldValue<float>(expectedPlatinumDeathsOrdinal),
                ExpectedPlatinumKills = reader.IsDBNull(expectedPlatinumKillsOrdinal) ? null : reader.GetFieldValue<float>(expectedPlatinumKillsOrdinal),
                ExpectedDiamondDeaths = reader.IsDBNull(expectedDiamondDeathsOrdinal) ? null : reader.GetFieldValue<float>(expectedDiamondDeathsOrdinal),
                ExpectedDiamondKills = reader.IsDBNull(expectedDiamondKillsOrdinal) ? null : reader.GetFieldValue<float>(expectedDiamondKillsOrdinal),
                ExpectedOnyxDeaths = reader.IsDBNull(expectedOnyxDeathsOrdinal) ? null : reader.GetFieldValue<float>(expectedOnyxDeathsOrdinal),
                ExpectedOnyxKills = reader.IsDBNull(expectedOnyxKillsOrdinal) ? null : reader.GetFieldValue<float>(expectedOnyxKillsOrdinal),
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

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Select", "MatchStatsAvailability");
                command.Parameters.AddWithValue("$MatchId", matchId);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return (reader.GetBoolean(0), reader.GetBoolean(1));
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred obtaining match and stats availability. {ex.Message}", LogSeverity.Error);
            }

            return (false, false); // Default values if the data retrieval fails
        }

        internal static bool InsertPlayerMatchStats(string matchId, string statsBody)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Insert", "PlayerMatchStats");
                command.Parameters.AddWithValue("$MatchId", matchId);
                command.Parameters.AddWithValue("$ResponseBody", statsBody);

                var rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred inserting player match and stats. {ex.Message}", LogSeverity.Error);
                return false;
            }
        }

        internal static bool InsertMatchStats(string matchBody)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Insert", "MatchStats");
                command.Parameters.AddWithValue("$ResponseBody", matchBody);

                var rowsAffected = command.ExecuteNonQuery();

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred inserting match and stats. {ex.Message}", LogSeverity.Error);
                return false;
            }
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

                // Construct the initial query
                var queryBuilder = new StringBuilder();
                queryBuilder.Append("SELECT ");
                queryBuilder.Append("EXISTS(SELECT 1 FROM Maps WHERE AssetId = $MapAssetId AND VersionId = $MapVersionId) AS MAP_AVAILABLE, ");
                queryBuilder.Append("EXISTS(SELECT 1 FROM GameVariants WHERE AssetId = $GameVariantAssetId AND VersionId = $GameVariantVersionId) AS GAMEVARIANT_AVAILABLE");

                // Conditionally add more parts to the query based on available parameters
                if (result.MatchInfo.Playlist != null)
                {
                    queryBuilder.Append(", EXISTS(SELECT 1 FROM Playlists WHERE AssetId = $PlaylistAssetId AND VersionId = $PlaylistVersionId) AS PLAYLIST_AVAILABLE");
                }

                if (result.MatchInfo.PlaylistMapModePair != null)
                {
                    queryBuilder.Append(", EXISTS(SELECT 1 FROM PlaylistMapModePairs WHERE AssetId = $PlaylistMapModePairAssetId AND VersionId = $PlaylistMapModePairVersionId) AS PLAYLISTMAPMODEPAIR_AVAILABLE");
                }

                // Execute the constructed query
                using (var command = new SqliteCommand(queryBuilder.ToString(), connection))
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

                // Update assets if they are not available
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
                            LogEngine.Log($"Stored map: {result.MatchInfo.MapVariant.AssetId}/{result.MatchInfo.MapVariant.VersionId}");
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
                                LogEngine.Log($"Stored playlist: {result.MatchInfo.Playlist.AssetId}/{result.MatchInfo.Playlist.VersionId}");
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
                                LogEngine.Log($"Stored playlist + map mode pair: {result.MatchInfo.PlaylistMapModePair.AssetId}/{result.MatchInfo.PlaylistMapModePair.VersionId}");
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

                        using var insertionCommand = connection.CreateCommand();
                        insertionCommand.CommandText = GetQuery("Insert", "GameVariants");
                        insertionCommand.Parameters.AddWithValue("$ResponseBody", gameVariant.Response.Message);

                        var insertionResult = await insertionCommand.ExecuteNonQueryAsync();

                        if (insertionResult > 0)
                        {
                            LogEngine.Log($"Stored game variant: {result.MatchInfo.UgcGameVariant.AssetId}/{result.MatchInfo.UgcGameVariant.VersionId}");
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
                            LogEngine.Log($"Stored engine game variant: {engineGameVariant.Result.AssetId}/{engineGameVariant.Result.VersionId}");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Error updating match asset records. {ex.Message}", LogSeverity.Error);
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

                List<Medal> medals = [];

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    medals.AddRange(JsonSerializer.Deserialize<List<Medal>>(reader.GetString(0)));
                }

                if (medals.Count == 0)
                {
                    LogEngine.Log($"No rows returned for medals.", LogSeverity.Warning);
                }

                return medals;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred obtaining medals from the database. {ex.Message}", LogSeverity.Error);
                return null;
            }
        }

        internal static bool UpdateOperationRewardTracks(string response, string path)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Insert", "OperationRewardTracks");
                command.Parameters.AddWithValue("$ResponseBody", response);
                command.Parameters.AddWithValue("$Path", path);
                command.Parameters.AddWithValue("$LastUpdated", DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));

                var insertionResult = command.ExecuteNonQuery();

                if (insertionResult > 0)
                {
                    LogEngine.Log($"Stored reward track {path}.");
                    return true;
                }
                else
                {
                    LogEngine.Log($"Could not store reward track {path}.", LogSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred updating operation reward tracks. {ex.Message}", LogSeverity.Error);
            }

            return false;
        }

        internal static bool UpdateInventoryItems(string response, string path)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = GetQuery("Insert", "InventoryItems");
                command.Parameters.AddWithValue("$ResponseBody", response);
                command.Parameters.AddWithValue("$Path", path);
                command.Parameters.AddWithValue("$LastUpdated", DateTimeOffset.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));

                var insertionResult = command.ExecuteNonQuery();

                if (insertionResult > 0)
                {
                    LogEngine.Log($"Stored inventory item {path}.");
                    return true;
                }
                else
                {
                    LogEngine.Log($"Could not store inventory item {path}.", LogSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred updating inventory items. {ex.Message}", LogSeverity.Error);
            }

            return false;
        }

        internal static bool IsOperationRewardTrackAvailable(string path)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT EXISTS(SELECT 1 FROM OperationRewardTracks WHERE Path = @Path) AS OPERATION_AVAILABLE";
                command.Parameters.AddWithValue("@Path", path);

                var result = command.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) > 0;
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred checking operation reward track availability. {ex.Message}", LogSeverity.Error);
            }

            return false;
        }

        internal static bool IsInventoryItemAvailable(string path)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT EXISTS(SELECT 1 FROM InventoryItems WHERE Path = @Path) AS INVENTORY_ITEM_AVAILABLE";
                command.Parameters.AddWithValue("@Path", path);

                var result = command.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) > 0;
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred checking inventory item availability. {ex.Message}", LogSeverity.Error);
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
                if (reader.Read())
                {
                    return JsonSerializer.Deserialize<InGameItem>(reader.GetString(0), serializerOptions);
                }
                else
                {
                    LogEngine.Log($"No rows returned for inventory items query.");
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred obtaining inventory items. {ex.Message}", LogSeverity.Error);
            }

            return null;
        }

        internal static async Task<bool> InsertOwnedInventoryItems(PlayerInventory result)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                await connection.OpenAsync();

                var commandText = GetQuery("Insert", "OwnedInventoryItems");

                foreach (var item in result.Items)
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = commandText;
                    command.Parameters.AddWithValue("$Amount", item.Amount);
                    command.Parameters.AddWithValue("$ItemId", item.ItemId);
                    command.Parameters.AddWithValue("$ItemPath", item.ItemPath);
                    command.Parameters.AddWithValue("$ItemType", item.ItemType);
                    command.Parameters.AddWithValue("$FirstAcquiredDate", item.FirstAcquiredDate.ISO8601Date);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        LogEngine.Log($"Stored owned inventory item {item.ItemId}.");
                    }
                    else
                    {
                        LogEngine.Log($"Could not store owned inventory item {item.ItemId}.", LogSeverity.Error);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Error inserting owned inventory items. {ex.Message}", LogSeverity.Error);
                return false;
            }
        }
    }
}
