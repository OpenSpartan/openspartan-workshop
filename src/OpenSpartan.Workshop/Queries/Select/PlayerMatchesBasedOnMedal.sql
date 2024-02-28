WITH RAW_MATCHES AS (
    SELECT
        MS.MatchId,
        MS.Teams,
        json_extract(MS.MatchInfo, '$.StartTime') AS StartTime,
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
		AND EXISTS (
        SELECT 1
        FROM json_each(PlayerTeamStats) AS PTS
        WHERE EXISTS (
				SELECT 1
				FROM json_each(PTS.value, '$.Stats.CoreStats.Medals') AS MedalEntry
				WHERE json_extract(MedalEntry.value, '$.NameId') = $MedalNameId
			)
		)
    ORDER BY
        StartTime DESC
),
MATCH_DETAILS AS (
    SELECT
        PMS.MatchId,
        json_extract(PE.value, '$.Result.TeamMmr') AS TeamMmr,
        json_extract(PE.value, '$.Result.Counterfactuals.SelfCounterfactuals.Deaths') AS ExpectedDeaths,
        json_extract(PE.value, '$.Result.Counterfactuals.SelfCounterfactuals.Kills') AS ExpectedKills
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
    MD.ExpectedKills AS ExpectedKills
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
    StartTime DESC;
