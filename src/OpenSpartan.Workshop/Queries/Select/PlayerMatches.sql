WITH RAW_MATCHES AS (
    SELECT
        MS.MatchId,
        MS.Teams,
        json_extract(MS.MatchInfo, '$.StartTime') AS StartTime,
        json_extract(MS.MatchInfo, '$.EndTime') AS EndTime,
        json_extract(MS.MatchInfo, '$.Duration') AS Duration,
        json_extract(MS.MatchInfo, '$.GameVariantCategory') AS GameVariantCategory,
        json_extract(MS.MatchInfo, '$.MapVariant.AssetId') AS Map,
        json_extract(MS.MatchInfo, '$.Playlist.AssetId') AS Playlist,
        json_extract(MS.MatchInfo, '$.UgcGameVariant.AssetId') AS UgcGameVariant,
        json_extract(value, '$.Rank') AS "Rank",
        json_extract(value, '$.Outcome') AS Outcome,
        json_extract(value, '$.LastTeamId') AS LastTeamId,
        json_extract(value, '$.ParticipationInfo') AS ParticipationInfo,
        json_extract(value, '$.PlayerTeamStats') AS PlayerTeamStats
    FROM
        MatchStats MS
    JOIN
        json_each(MS.Players) PE ON json_extract(PE.value, '$.PlayerId') = $PlayerXuid
    WHERE
        MS.MatchInfo IS NOT NULL
    ORDER BY
        StartTime DESC
),
MATCH_DETAILS AS (
    SELECT
        PMS.MatchId,
        json_extract(PE.value, '$.Result.TeamMmr') AS TeamMmr,
        json_extract(PE.value, '$.Result.Counterfactuals.SelfCounterfactuals.Deaths') AS ExpectedDeaths,
        json_extract(PE.value, '$.Result.Counterfactuals.SelfCounterfactuals.Kills') AS ExpectedKills,
        json_extract(PE.value, '$.Result.RankRecap.PostMatchCsr.Value') AS PostMatchCsr,
		json_extract(PE.value, '$.Result.RankRecap.PreMatchCsr.Value') AS PreMatchCsr,
		json_extract(PE.value, '$.Result.RankRecap.PostMatchCsr.Tier') AS Tier,
		json_extract(PE.value, '$.Result.RankRecap.PostMatchCsr.SubTier') AS TierLevel,
        json_extract(PE.value, '$.Result.RankRecap.PostMatchCsr.TierStart') AS TierStart,
        json_extract(PE.value, '$.Result.RankRecap.PostMatchCsr.NextTier') AS NextTier,
		json_extract(PE.value, '$.Result.RankRecap.PostMatchCsr.NextSubTier') AS NextTierLevel,
		json_extract(PE.value, '$.Result.RankRecap.PostMatchCsr.NextTierStart') AS NextTierStart,
		json_extract(PE.value, '$.Result.RankRecap.PostMatchCsr.InitialMeasurementMatches') AS InitialMeasurementMatches,
		json_extract(PE.value, '$.Result.RankRecap.PostMatchCsr.MeasurementMatchesRemaining') AS MeasurementMatchesRemaining
    FROM
        PlayerMatchStats PMS
    JOIN
        json_each(PMS.PlayerStats) PE ON json_extract(PE.value, '$.Id') = $PlayerXuid
    WHERE
        PMS.PlayerStats IS NOT NULL
),
SELECTIVE_MATCHES AS (
    SELECT
        MatchId,
        Teams,
        StartTime,
        EndTime,
        Duration,
        "Rank",
        Outcome,
        LastTeamId,
        ParticipationInfo,
        PlayerTeamStats,
        GameVariantCategory,
        Map,
        Playlist,
        UgcGameVariant
    FROM
        RAW_MATCHES
    WHERE
        StartTime <= $BoundaryTime
    LIMIT
        $BoundaryLimit
)
SELECT
    SM.MatchId,
    SM.Teams,
    SM.StartTime,
    SM.EndTime,
    SM.Duration,
    SM."Rank",
    SM.Outcome,
    SM.LastTeamId,
    SM.ParticipationInfo,
    SM.PlayerTeamStats,
    SM.GameVariantCategory,
    M.PublicName AS Map,
    P.PublicName AS Playlist,
    GV.PublicName AS GameVariant,
    MD.TeamMmr AS TeamMmr,
    MD.ExpectedDeaths AS ExpectedDeaths,
    MD.ExpectedKills AS ExpectedKills,
    MD.PostMatchCsr AS PostMatchCsr,
	MD.PreMatchCsr AS PreMatchCsr,
	MD.Tier AS Tier,
	MD.TierLevel AS TierLevel,
    MD.NextTier AS NextTier,
    MD.TierStart AS TierStart,
	MD.NextTierLevel AS NextTierLevel,
	MD.NextTierStart AS NextTierStart,
	MD.InitialMeasurementMatches AS InitialMeasurementMatches,
	MD.MeasurementMatchesRemaining AS MeasurementMatchesRemaining
FROM
    SELECTIVE_MATCHES SM
LEFT JOIN
    Maps M ON M.AssetId = SM.Map
LEFT JOIN
    Playlists P ON P.AssetId = SM.Playlist
LEFT JOIN
    GameVariants GV ON GV.AssetId = SM.UgcGameVariant
LEFT JOIN
    MATCH_DETAILS MD ON MD.MatchId = SM.MatchId
GROUP BY
    SM.MatchId
ORDER BY
    EndTime DESC;
