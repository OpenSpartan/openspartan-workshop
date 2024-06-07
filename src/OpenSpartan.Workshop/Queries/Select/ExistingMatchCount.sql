SELECT COUNT(*) AS ExistingMatchCount
FROM MatchStats
WHERE MatchId IN ($MatchGUIDList);