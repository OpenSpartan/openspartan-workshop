CREATE TABLE PlayerMatchStats (
	ResponseBody TEXT,
	MatchId TEXT,
	PlayerStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Value')) VIRTUAL
)