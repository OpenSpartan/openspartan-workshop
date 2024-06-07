CREATE TABLE PlaylistCSRSnapshots (
   ResponseBody TEXT,
   PlaylistId TEXT,
   PlaylistVersion TEXT,
   SnapshotTimestamp DATETIME,
   Value Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Value')) VIRTUAL
);