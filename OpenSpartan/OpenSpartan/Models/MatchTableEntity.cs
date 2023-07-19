using Den.Dev.Orion.Models.HaloInfinite;
using System;

namespace OpenSpartan.Models
{
    internal class MatchTableEntity
    {
        public string MatchId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public string Duration { get; set; }
        public int Rank { get; set; }
        public Outcome Outcome { get; set; }
        public GameVariantCategory Category { get; set; }
        public string Map { get; set; }
        public string Playlist { get; set; }
        public string GameVariant { get; set; }
    }
}
