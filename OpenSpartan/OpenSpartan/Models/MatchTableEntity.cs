using Den.Dev.Orion.Converters;
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
    }
}
