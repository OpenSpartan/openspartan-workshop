using Den.Dev.Grunt.Converters;
using Den.Dev.Grunt.Models.HaloInfinite;
using Microsoft.Data.Sqlite;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Collections.Concurrent;
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

        // SQL files under Queries/**/*.sql are immutable for the lifetime of the
        // process. The previous per-call File.ReadAllText was a synchronous disk
        // read on every DB method invocation — across a session that's hundreds
        // of redundant reads for the same files. Cache the contents on first read.
        private static readonly ConcurrentDictionary<string, string> _queryCache =
            new(StringComparer.OrdinalIgnoreCase);

        internal static string? SetWALJournalingMode()
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"Journaling mode modification exception: {ex}", LogSeverity.Error);
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"Database bootstrapping failure: {ex}", LogSeverity.Error);
                return false;
            }
        }

        private static void EnsureDatabaseDirectoryExists()
        {
            FileInfo file = new(DatabasePath);
            file.Directory?.Create();
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"Error inserting service record entry. {ex}", LogSeverity.Error);
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"Error inserting playlist CSR entry. {ex}", LogSeverity.Error);
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred obtaining unique match IDs. {ex}", LogSeverity.Error);
                return [];
            }
        }

        internal static RewardTrackMetadata? GetOperationResponseBody(string operationPath)
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
            catch (Exception ex) when (ex is SqliteException or IOException or JsonException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred obtaining operations from database. {ex}", LogSeverity.Error);
            }

            return null;
        }

        internal static int GetExistingMatchCount(IEnumerable<Guid> matchIds)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                connection.Open();

                using var command = connection.CreateCommand();

                // Build a parameterized IN list (@matchId0, @matchId1, ...). Previously
                // the GUIDs were string-interpolated into the SQL, which worked because
                // System.Guid.ToString() is hex+dashes only, but the pattern was the
                // wrong shape and would have broken the moment the input type changed.
                var paramNames = new List<string>();
                int index = 0;
                foreach (var id in matchIds)
                {
                    var name = $"@matchId{index++}";
                    paramNames.Add(name);
                    command.Parameters.AddWithValue(name, id.ToString());
                }

                if (paramNames.Count == 0)
                {
                    return 0;
                }

                command.CommandText = GetQuery("Select", "ExistingMatchCount")
                    .Replace("$MatchGUIDList", string.Join(", ", paramNames), StringComparison.InvariantCultureIgnoreCase);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var resultOrdinal = reader.GetOrdinal("ExistingMatchCount");
                    return reader.GetOrDefault(resultOrdinal, -1);
                }
                else
                {
                    LogEngine.Log("No rows returned for existing match metadata.", LogSeverity.Warning);
                }
            }
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred obtaining match records from database. {ex}", LogSeverity.Error);
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
                        var enrichedMedals = UserContextManager.EnrichMedalMetadata(matchEntry.PlayerTeamStats[0].Stats.CoreStats.Medals);
                        if (enrichedMedals != null)
                        {
                            matchEntry.PlayerTeamStats[0].Stats.CoreStats.Medals = enrichedMedals;
                        }
                    }

                    matches.Add(matchEntry);
                }

                if (matches.Count == 0)
                {
                    LogEngine.Log("No rows returned for player match IDs.", LogSeverity.Warning);
                }

                return matches;
            }
            catch (Exception ex) when (ex is SqliteException or IOException or JsonException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred obtaining matches. {ex}", LogSeverity.Error);
                return new List<MatchTableEntity>();
            }
        }

        internal static async Task<List<MatchTableEntity>> GetMatchesAsync(string playerXuid, string boundaryTime, int boundaryLimit)
        {
            return await GetMatchesInternalAsync(playerXuid, null, boundaryTime, boundaryLimit);
        }

        internal static async Task<List<MatchTableEntity>> GetMatchesWithMedalAsync(string playerXuid, long medalNameId, string boundaryTime, int boundaryLimit)
        {
            return await GetMatchesInternalAsync(playerXuid, medalNameId, boundaryTime, boundaryLimit);
        }

        private static async Task<List<MatchTableEntity>> GetMatchesInternalAsync(string playerXuid, long? medalNameId, string boundaryTime, int boundaryLimit)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath}");
                await connection.OpenAsync();

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

                using var reader = await command.ExecuteReaderAsync();
                List<MatchTableEntity> matches = [];
                while (await reader.ReadAsync())
                {
                    var matchEntry = ReadMatchTableEntity(reader);

                    if (matchEntry.PlayerTeamStats[0].Stats.CoreStats.Medals != null && matchEntry.PlayerTeamStats[0].Stats.CoreStats.Medals.Count > 0)
                    {
                        var enrichedMedals = UserContextManager.EnrichMedalMetadata(matchEntry.PlayerTeamStats[0].Stats.CoreStats.Medals);
                        if (enrichedMedals != null)
                        {
                            matchEntry.PlayerTeamStats[0].Stats.CoreStats.Medals = enrichedMedals;
                        }
                    }

                    matches.Add(matchEntry);
                }

                if (matches.Count == 0)
                {
                    LogEngine.Log("No rows returned for player match IDs.", LogSeverity.Warning);
                }

                return matches;
            }
            catch (Exception ex) when (ex is SqliteException or IOException or JsonException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred obtaining matches. {ex}", LogSeverity.Error);
                return new List<MatchTableEntity>();
            }
        }

        private static MatchTableEntity ReadMatchTableEntity(SqliteDataReader reader)
        {
            int matchOrdinal = reader.GetOrdinal("MatchId");
            int startTimeOrdinal = reader.GetOrdinal("StartTime");
            int endTimeOrdinal = reader.GetOrdinal("EndTime");
            int rankOrdinal = reader.GetOrdinal("Rank");
            int outcomeOrdinal = reader.GetOrdinal("Outcome");
            int gameVariantCategoryOrdinal = reader.GetOrdinal("GameVariantCategory");
            int mapOrdinal = reader.GetOrdinal("Map");
            int playlistOrdinal = reader.GetOrdinal("Playlist");
            int gameVariantOrdinal = reader.GetOrdinal("GameVariant");
            int durationOrdinal = reader.GetOrdinal("Duration");
            int lastTeamIdOrdinal = reader.GetOrdinal("LastTeamId");
            int teamsOrdinal = reader.GetOrdinal("Teams");
            int participationInfoOrdinal = reader.GetOrdinal("ParticipationInfo");
            int playerTeamStatsOrdinal = reader.GetOrdinal("PlayerTeamStats");
            int teamMmrOrdinal = reader.GetOrdinal("TeamMmr");
            int expectedDeathsOrdinal = reader.GetOrdinal("ExpectedDeaths");
            int expectedKillsOrdinal = reader.GetOrdinal("ExpectedKills");
            int expectedBronzeDeathsOrdinal = reader.GetOrdinal("ExpectedBronzeDeaths");
            int expectedBronzeKillsOrdinal = reader.GetOrdinal("ExpectedBronzeKills");
            int expectedSilverDeathsOrdinal = reader.GetOrdinal("ExpectedSilverDeaths");
            int expectedSilverKillsOrdinal = reader.GetOrdinal("ExpectedSilverKills");
            int expectedGoldDeathsOrdinal = reader.GetOrdinal("ExpectedGoldDeaths");
            int expectedGoldKillsOrdinal = reader.GetOrdinal("ExpectedGoldKills");
            int expectedPlatinumDeathsOrdinal = reader.GetOrdinal("ExpectedPlatinumDeaths");
            int expectedPlatinumKillsOrdinal = reader.GetOrdinal("ExpectedPlatinumKills");
            int expectedDiamondDeathsOrdinal = reader.GetOrdinal("ExpectedDiamondDeaths");
            int expectedDiamondKillsOrdinal = reader.GetOrdinal("ExpectedDiamondKills");
            int expectedOnyxDeathsOrdinal = reader.GetOrdinal("ExpectedOnyxDeaths");
            int expectedOnyxKillsOrdinal = reader.GetOrdinal("ExpectedOnyxKills");
            int postMatchOrdinal = reader.GetOrdinal("PostMatchCsr");
            int preMatchCsrOrdinal = reader.GetOrdinal("PreMatchCsr");
            int tierOrdinal = reader.GetOrdinal("Tier");
            int tierStartOrdinal = reader.GetOrdinal("TierStart");
            int tierLevelOrdinal = reader.GetOrdinal("TierLevel");
            int initialMeasurementMatchesOrdinal = reader.GetOrdinal("InitialMeasurementMatches");
            int measurementMatchesRemainingOrdinal = reader.GetOrdinal("MeasurementMatchesRemaining");
            int nextTierOrdinal = reader.GetOrdinal("NextTier");
            int nextTierLevelOrdinal = reader.GetOrdinal("NextTierLevel");
            int nextTierStartOrdinal = reader.GetOrdinal("NextTierStart");

            // Teams / ParticipationInfo / PlayerTeamStats are written together as one group
            // from the same PlayerMatchStats payload. If the Teams JSON column is null, the
            // other two are treated as unavailable too — preserving the behavior of the
            // pre-refactor code that gated all three on teamsOrdinal.
            bool statsAvailable = !reader.IsDBNull(teamsOrdinal);

            return new MatchTableEntity
            {
                MatchId = reader.GetOrDefault(matchOrdinal, string.Empty),
                StartTime = reader.IsDBNull(startTimeOrdinal) ? DateTimeOffset.UnixEpoch : reader.GetFieldValue<DateTimeOffset>(startTimeOrdinal).ToLocalTime(),
                EndTime = reader.IsDBNull(endTimeOrdinal) ? DateTimeOffset.UnixEpoch : reader.GetFieldValue<DateTimeOffset>(endTimeOrdinal).ToLocalTime(),
                Rank = reader.GetOrDefault(rankOrdinal, 0),
                Outcome = reader.GetOrDefault(outcomeOrdinal, Outcome.DidNotFinish),
                Category = reader.GetOrDefault(gameVariantCategoryOrdinal, GameVariantCategory.None),
                Map = reader.GetOrDefault(mapOrdinal, string.Empty),
                Playlist = reader.GetOrDefault(playlistOrdinal, string.Empty),
                GameVariant = reader.GetOrDefault(gameVariantOrdinal, string.Empty),
                Duration = reader.IsDBNull(durationOrdinal) ? TimeSpan.Zero : XmlConvert.ToTimeSpan(reader.GetFieldValue<string>(durationOrdinal)),
                LastTeamId = reader.GetNullable<int>(lastTeamIdOrdinal),
                Teams = statsAvailable ? JsonSerializer.Deserialize<List<Team>>(reader.GetFieldValue<string>(teamsOrdinal), serializerOptions) : null,
                ParticipationInfo = statsAvailable ? JsonSerializer.Deserialize<ParticipationInfo>(reader.GetFieldValue<string>(participationInfoOrdinal), serializerOptions) : null,
                PlayerTeamStats = statsAvailable ? JsonSerializer.Deserialize<List<PlayerTeamStat>>(reader.GetFieldValue<string>(playerTeamStatsOrdinal), serializerOptions) : null,
                TeamMmr = reader.GetNullable<float>(teamMmrOrdinal),
                ExpectedDeaths = reader.GetNullable<float>(expectedDeathsOrdinal),
                ExpectedKills = reader.GetNullable<float>(expectedKillsOrdinal),
                ExpectedBronzeDeaths = reader.GetNullable<float>(expectedBronzeDeathsOrdinal),
                ExpectedBronzeKills = reader.GetNullable<float>(expectedBronzeKillsOrdinal),
                ExpectedSilverDeaths = reader.GetNullable<float>(expectedSilverDeathsOrdinal),
                ExpectedSilverKills = reader.GetNullable<float>(expectedSilverKillsOrdinal),
                ExpectedGoldDeaths = reader.GetNullable<float>(expectedGoldDeathsOrdinal),
                ExpectedGoldKills = reader.GetNullable<float>(expectedGoldKillsOrdinal),
                ExpectedPlatinumDeaths = reader.GetNullable<float>(expectedPlatinumDeathsOrdinal),
                ExpectedPlatinumKills = reader.GetNullable<float>(expectedPlatinumKillsOrdinal),
                ExpectedDiamondDeaths = reader.GetNullable<float>(expectedDiamondDeathsOrdinal),
                ExpectedDiamondKills = reader.GetNullable<float>(expectedDiamondKillsOrdinal),
                ExpectedOnyxDeaths = reader.GetNullable<float>(expectedOnyxDeathsOrdinal),
                ExpectedOnyxKills = reader.GetNullable<float>(expectedOnyxKillsOrdinal),
                PostMatchCsr = reader.GetNullable<int>(postMatchOrdinal),
                PreMatchCsr = reader.GetNullable<int>(preMatchCsrOrdinal),
                Tier = reader.GetStringOrNull(tierOrdinal),
                TierStart = reader.GetNullable<int>(tierStartOrdinal),
                TierLevel = reader.GetNullable<int>(tierLevelOrdinal),
                InitialMeasurementMatches = reader.GetNullable<int>(initialMeasurementMatchesOrdinal),
                MeasurementMatchesRemaining = reader.GetNullable<int>(measurementMatchesRemainingOrdinal),
                NextTier = reader.GetStringOrNull(nextTierOrdinal),
                NextTierLevel = reader.GetNullable<int>(nextTierLevelOrdinal),
                NextTierStart = reader.GetNullable<int>(nextTierStartOrdinal),
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred obtaining match and stats availability. {ex}", LogSeverity.Error);
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred inserting player match and stats. {ex}", LogSeverity.Error);
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred inserting match and stats. {ex}", LogSeverity.Error);
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

                // Wrap the conditional asset upserts in a single transaction so a single
                // match's asset writes are atomic and we pay one fsync instead of up to four.
                using var transaction = connection.BeginTransaction();
                try
                {
                    if (!mapAvailable)
                    {
                        var map = await UserContextManager.SafeAPICall(async () => await UserContextManager.HaloClient.UgcDiscovery.GetMap(result.MatchInfo.MapVariant.AssetId.ToString(), result.MatchInfo.MapVariant.VersionId.ToString()));
                        if (map != null && map.Result != null && map.Response.Code == 200)
                        {
                            using var insertionCommand = connection.CreateCommand();
                            insertionCommand.Transaction = transaction;
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
                            var playlist = await UserContextManager.SafeAPICall(async () => await UserContextManager.HaloClient.UgcDiscovery.GetPlaylist(result.MatchInfo.Playlist.AssetId.ToString(), result.MatchInfo.Playlist.VersionId.ToString(), UserContextManager.HaloClient.ClearanceToken));
                            if (playlist != null && playlist.Result != null && playlist.Response.Code == 200)
                            {
                                using var insertionCommand = connection.CreateCommand();
                                insertionCommand.Transaction = transaction;
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
                            var playlistMmp = await UserContextManager.SafeAPICall(async () => await UserContextManager.HaloClient.UgcDiscovery.GetMapModePair(result.MatchInfo.PlaylistMapModePair.AssetId.ToString(), result.MatchInfo.PlaylistMapModePair.VersionId.ToString(), UserContextManager.HaloClient.ClearanceToken));
                            if (playlistMmp != null && playlistMmp.Result != null && playlistMmp.Response.Code == 200)
                            {
                                using var insertionCommand = connection.CreateCommand();
                                insertionCommand.Transaction = transaction;
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
                        var gameVariant = await UserContextManager.SafeAPICall(async () => await UserContextManager.HaloClient.UgcDiscovery.GetUgcGameVariant(result.MatchInfo.UgcGameVariant.AssetId.ToString(), result.MatchInfo.UgcGameVariant.VersionId.ToString()));
                        if (gameVariant != null && gameVariant.Result != null && gameVariant.Response.Code == 200)
                        {
                            targetGameVariant = gameVariant.Result;

                            using var insertionCommand = connection.CreateCommand();
                            insertionCommand.Transaction = transaction;
                            insertionCommand.CommandText = GetQuery("Insert", "GameVariants");
                            insertionCommand.Parameters.AddWithValue("$ResponseBody", gameVariant.Response.Message);

                            var insertionResult = await insertionCommand.ExecuteNonQueryAsync();

                            if (insertionResult > 0)
                            {
                                LogEngine.Log($"Stored game variant: {result.MatchInfo.UgcGameVariant.AssetId}/{result.MatchInfo.UgcGameVariant.VersionId}");
                            }

                            using var egvQueryCommand = connection.CreateCommand();
                            egvQueryCommand.Transaction = transaction;
                            egvQueryCommand.CommandText = "SELECT EXISTS(SELECT 1 FROM EngineGameVariants WHERE AssetId = @AssetId AND VersionId = @VersionId) AS ENGINEGAMEVARIANT_AVAILABLE";
                            egvQueryCommand.Parameters.AddWithValue("@AssetId", gameVariant.Result.EngineGameVariantLink.AssetId.ToString());
                            egvQueryCommand.Parameters.AddWithValue("@VersionId", gameVariant.Result.EngineGameVariantLink.VersionId.ToString());

                            using var egvReader = await egvQueryCommand.ExecuteReaderAsync();
                            if (await egvReader.ReadAsync())
                            {
                                engineGameVariantAvailable = egvReader.GetFieldValue<int>("ENGINEGAMEVARIANT_AVAILABLE") == 1;
                            }
                        }
                    }

                    if (!engineGameVariantAvailable && targetGameVariant != null)
                    {
                        var engineGameVariant = await UserContextManager.SafeAPICall(async () => await UserContextManager.HaloClient.UgcDiscovery.GetEngineGameVariant(targetGameVariant.EngineGameVariantLink.AssetId.ToString(), targetGameVariant.EngineGameVariantLink.VersionId.ToString()));

                        if (engineGameVariant != null && engineGameVariant.Result != null && engineGameVariant.Response.Code == 200)
                        {
                            using var egvInsertionCommand = connection.CreateCommand();
                            egvInsertionCommand.Transaction = transaction;
                            egvInsertionCommand.CommandText = GetQuery("Insert", "EngineGameVariants");
                            egvInsertionCommand.Parameters.AddWithValue("$ResponseBody", engineGameVariant.Response.Message);

                            var insertionResult = await egvInsertionCommand.ExecuteNonQueryAsync();

                            if (insertionResult > 0)
                            {
                                LogEngine.Log($"Stored engine game variant: {engineGameVariant.Result.AssetId}/{engineGameVariant.Result.VersionId}");
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                return true;
            }
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"Error updating match asset records. {ex}", LogSeverity.Error);
                return false;
            }
        }


        private static string GetQuery(string category, string target) =>
            _queryCache.GetOrAdd($"{category}/{target}", _ =>
                System.IO.File.ReadAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Queries", category, $"{target}.sql"),
                    Encoding.UTF8));

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
            catch (Exception ex) when (ex is SqliteException or IOException or JsonException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred obtaining medals from the database. {ex}", LogSeverity.Error);
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred updating operation reward tracks. {ex}", LogSeverity.Error);
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred updating inventory items. {ex}", LogSeverity.Error);
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred checking operation reward track availability. {ex}", LogSeverity.Error);
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
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred checking inventory item availability. {ex}", LogSeverity.Error);
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
            catch (Exception ex) when (ex is SqliteException or IOException or JsonException or InvalidOperationException)
            {
                LogEngine.Log($"An error occurred obtaining inventory items. {ex}", LogSeverity.Error);
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

                // Use transaction for batched inserts (100 items = 100x fewer transactions)
                using var transaction = connection.BeginTransaction();
                try
                {
                    foreach (var item in result.Items)
                    {
                        using var command = connection.CreateCommand();
                        command.Transaction = transaction;
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

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
            {
                LogEngine.Log($"Error inserting owned inventory items. {ex}", LogSeverity.Error);
                return false;
            }
        }
    }
}
