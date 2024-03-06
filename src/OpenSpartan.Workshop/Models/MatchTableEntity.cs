using Den.Dev.Orion.Models.HaloInfinite;
using System;
using System.Collections.Generic;

namespace OpenSpartan.Workshop.Models
{
    internal sealed class MatchTableEntity
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

        public int? PostMatchCsr { get; set; }

        public int? PreMatchCsr { get; set; }

        public string? Tier { get; set; }

        public int? TierLevel { get; set; }

        public int? TierStart { get; set; }

        public string? NextTier { get; set; }

        public int? NextTierLevel { get; set; }

        public int? NextTierStart { get; set; }

        public int? InitialMeasurementMatches { get; set; }

        public int? MeasurementMatchesRemaining { get; set; }

        // Tiers are zero-indexed, so we need to add one to get the
        // readable tier level.
        public int? ActualTierLevel
        {
            get
            {
                return TierLevel + 1;
            }
        }

        public string? NextRankIdentifier
        {
            get
            {
                if (MeasurementMatchesRemaining > 0 && InitialMeasurementMatches > 0)
                {
                    return $"unranked_{InitialMeasurementMatches - MeasurementMatchesRemaining}";
                }
                else
                {
                    return $"{NextTier}_{NextTierLevel + 1}";
                }
            }
        }

        public string? RankIdentifier
        {
            get
            {
                if (MeasurementMatchesRemaining > 0 && InitialMeasurementMatches > 0)
                {
                    return $"unranked_{InitialMeasurementMatches - MeasurementMatchesRemaining}";
                }
                else
                {
                    return $"{Tier}_{TierLevel + 1}";
                }
            }
        }

        public int? CsrDelta
        {
            get
            {
                return PostMatchCsr - PreMatchCsr;
            } 
        }

        public bool IsRanked
        {
            get
            {
                return InitialMeasurementMatches > 0;
            }
        }

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
