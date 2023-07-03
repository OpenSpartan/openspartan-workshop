CREATE TABLE MatchStats (
   ResponseBody TEXT,
   MatchId Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.MatchId')) VIRTUAL,
   MatchInfo Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.MatchInfo')) VIRTUAL,
   Teams Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Teams')) VIRTUAL,
   Players Text GENERATED ALWAYS AS (json_extract(ResponseBody, '$.Players')) VIRTUAL
);