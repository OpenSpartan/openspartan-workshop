CREATE UNIQUE INDEX IF NOT EXISTS IDX_ENGINE_GAME_VARIANTS 
ON EngineGameVariants (AssetId, VersionId);

CREATE UNIQUE INDEX IF NOT EXISTS IDX_GAME_VARIANTS 
ON GameVariants (AssetId, VersionId);

CREATE UNIQUE INDEX IF NOT EXISTS IDX_INVENTORY_ITEMS
ON InventoryItems (Path);

CREATE UNIQUE INDEX IF NOT EXISTS IDX_MAPS
ON Maps (AssetId, VersionId);

CREATE UNIQUE INDEX IF NOT EXISTS IDX_MATCH_STATS
ON MatchStats (MatchId);

CREATE UNIQUE INDEX IF NOT EXISTS IDX_OPERATION_REWARD_TRACKS
ON OperationRewardTracks (Path);

CREATE UNIQUE INDEX IF NOT EXISTS IDX_PLAYLISTS
ON Playlists (AssetId, VersionId);

CREATE UNIQUE INDEX IF NOT EXISTS IDX_PLAYLIST_MAP_MODE_PAIRS
ON PlaylistMapModePairs (AssetId, VersionId);

CREATE INDEX IF NOT EXISTS IDX_PLAYER_MATCH_STATS_MATCH_ID
ON PlayerMatchStats(MatchId);

-- Expression index on the JSON-extracted MatchInfo.StartTime so the
-- `WHERE StartTime <= $BoundaryTime` filter and the inner ORDER BY in
-- PlayerMatches.sql don't full-scan MatchStats and recompute json_extract
-- per row. The expression matches the form used by the query
-- (`json_extract(MS.MatchInfo, '$.StartTime')`) so SQLite's planner can
-- use the index for both the ORDER BY and the StartTime <= ? filter.
CREATE INDEX IF NOT EXISTS IDX_MATCH_STATS_START_TIME
ON MatchStats (json_extract(MatchInfo, '$.StartTime') DESC);