SELECT DISTINCT MatchStats.MatchId
FROM MatchStats
JOIN PlayerMatchStats ON MatchStats.MatchId = PlayerMatchStats.MatchId;