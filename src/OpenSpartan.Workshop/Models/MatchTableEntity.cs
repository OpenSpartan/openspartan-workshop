using Den.Dev.Orion.Models.HaloInfinite;
using System;
using System.Collections.Generic;

namespace OpenSpartan.Workshop.Models
{
    internal class MatchTableEntity
    {
        public string MatchId { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public List<Team> Teams { get; set; }

        public TimeSpan Duration { get; set; }

        public int? Rank { get; set; }

        public Outcome Outcome { get; set; }

        public GameVariantCategory Category { get; set; }

        public string Map { get; set; }

        public string Playlist { get; set; }

        public string GameVariant { get; set; }

        public int? LastTeamId { get; set; }

        public ParticipationInfo ParticipationInfo { get; set; }

        public List<PlayerTeamStat> PlayerTeamStats { get; set; }

        public float? TeamMmr { get; set; }

        public float? ExpectedDeaths { get; set; }

        public float? ExpectedKills { get; set; }

        public PerformanceMeasure? KillPerformance
        {
            get
            {
                return ExpectedKills == PlayerTeamStats[0].Stats.CoreStats.Kills
                    ? PerformanceMeasure.MetExpectations
                    : (ExpectedKills > PlayerTeamStats[0].Stats.CoreStats.Kills ? PerformanceMeasure.Underperformed : PerformanceMeasure.Outperformed);
            }
        }

        public PerformanceMeasure? DeathPerformance
        {
            get
            {
                return ExpectedDeaths == PlayerTeamStats[0].Stats.CoreStats.Deaths
                    ? PerformanceMeasure.MetExpectations
                    : (ExpectedDeaths > PlayerTeamStats[0].Stats.CoreStats.Deaths ? PerformanceMeasure.Outperformed : PerformanceMeasure.Underperformed);
            }
        }
    }
}
