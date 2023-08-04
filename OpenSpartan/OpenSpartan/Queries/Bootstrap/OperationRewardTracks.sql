CREATE TABLE OperationRewardTracks (
	ResponseBody TEXT,
	Path TEXT,
	LastUpdated DATETIME,
	TrackId Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.TrackId')) VIRTUAL,
	XpPerRank Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.XpPerRank')) VIRTUAL,
	HideIfNotOwned Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.HideIfNotOwned')) VIRTUAL,
	Ranks Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Ranks')) VIRTUAL,
	Name Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Name')) VIRTUAL,
	Description Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Description')) VIRTUAL,
	OperationNumber Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.OperationNumber')) VIRTUAL,
	DateRange Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.DateRange')) VIRTUAL,
	IsRitual Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.IsRitual')) VIRTUAL,
	SummaryImagePath Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.SummaryImagePath')) VIRTUAL,
	WeekNumber Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.WeekNumber')) VIRTUAL,
	BackgroundImagePath Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.BackgroundImagePath')) VIRTUAL
);