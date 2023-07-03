CREATE TABLE ServiceRecordSnapshots (
	ResponseBody TEXT,
	SnapshotTimestamp DATETIME,
	Subqueries Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Subqueries')) VIRTUAL,
	TimePlayed Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.TimePlayed')) VIRTUAL,
	MatchesCompleted Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.MatchesCompleted')) VIRTUAL,
	Wins Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Wins')) VIRTUAL,
	Losses Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Losses')) VIRTUAL,
	Ties Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Ties')) VIRTUAL,
	CoreStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.CoreStats')) VIRTUAL,
	BombStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.BombStats')) VIRTUAL,
	CaptureTheFlagStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.CaptureTheFlagStats')) VIRTUAL,
	EliminationStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.EliminationStats')) VIRTUAL,
	ExtractionStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.ExtractionStats')) VIRTUAL,
	InfectionStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.InfectionStats')) VIRTUAL,
	OddballStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.OddballStats')) VIRTUAL,
	ZonesStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.ZonesStats')) VIRTUAL,
	StockpileStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.StockpileStats')) VIRTUAL
)