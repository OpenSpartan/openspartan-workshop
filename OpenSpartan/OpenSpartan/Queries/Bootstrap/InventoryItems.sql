CREATE TABLE InventoryItems (
	ResponseBody TEXT,
	Path TEXT,
	LastUpdated DATETIME,
	Id Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.CommonData.Id')) VIRTUAL,
	Quality Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.CommonData.Quality')) VIRTUAL
);