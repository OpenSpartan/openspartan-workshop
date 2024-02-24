WITH RAW_MATCHES AS (
    SELECT
        MatchId,
        Teams,
        json_extract(MatchInfo, '$.StartTime') StartTime,
        json_extract(MatchInfo, '$.Duration') Duration,
        json_extract(MatchInfo, '$.GameVariantCategory') GameVariantCategory,
        json_extract(MatchInfo, '$.MapVariant.AssetId') Map,
        json_extract(MatchInfo, '$.Playlist.AssetId') Playlist,
        json_extract(MatchInfo, '$.UgcGameVariant.AssetId') UgcGameVariant,
        json_extract(value, '$.Rank') "Rank",
        json_extract(value, '$.Outcome') Outcome,
        json_extract(value, '$.LastTeamId') LastTeamId,
        json_extract(value, '$.ParticipationInfo') ParticipationInfo,
        json_extract(value, '$.PlayerTeamStats') PlayerTeamStats
    FROM
        MatchStats, json_each(Players)
    WHERE
        json_extract(json_each.value, '$.PlayerId') = $PlayerXuid
    ORDER BY
        StartTime DESC
),
MATCH_DETAILS AS (
    SELECT
        MatchId,
        json_extract(value, '$.Result.TeamMmr') TeamMmr
    FROM
        PlayerMatchStats, json_each(PlayerStats)
    WHERE
        json_extract(json_each.value, '$.Id') = $PlayerXuid
),
SELECTIVE_MATCHES AS (
    SELECT
        *
    FROM
        RAW_MATCHES
    WHERE
        StartTime <= $BoundaryTime
    LIMIT
        $BoundaryLimit
)
SELECT
    SM.MatchId,
    Teams,
    StartTime,
    Duration,
    "Rank",
    Outcome,
    LastTeamId,
    ParticipationInfo,
    PlayerTeamStats,
    GameVariantCategory,
    M.PublicName Map,
    P.PublicName Playlist,
    GV.PublicName GameVariant,
    MD.TeamMmr TeamMmr
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
