SELECT json_extract(CoreStats, '$.Medals') Medals FROM ServiceRecordSnapshots
ORDER BY SnapshotTimestamp DESC
LIMIT 1