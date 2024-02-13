﻿CREATE TABLE EngineGameVariants (
	ResponseBody TEXT,
	CustomData Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.CustomData')) VIRTUAL,
	Tags Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Tags')) VIRTUAL,
	AssetId Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.AssetId')) VIRTUAL,
	VersionId Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.VersionId')) VIRTUAL,
	PublicName Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.PublicName')) VIRTUAL,
	Description Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Description')) VIRTUAL,
	Files Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Files')) VIRTUAL,
	Contributors Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Contributors')) VIRTUAL,
	AssetHome Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.AssetHome')) VIRTUAL,
	AssetStats Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.AssetStats')) VIRTUAL,
	InspectionResult Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.InspectionResult')) VIRTUAL,
	CloneBehavior Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.CloneBehavior')) VIRTUAL,
	"Order" Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Order')) VIRTUAL,
	PublishedDate Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.PublishedDate')) VIRTUAL,
	VersionNumber Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.VersionNumber')) VIRTUAL,
	Admin Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Admin')) VIRTUAL
);