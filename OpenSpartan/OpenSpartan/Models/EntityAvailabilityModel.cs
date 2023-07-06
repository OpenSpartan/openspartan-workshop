using System.ComponentModel.DataAnnotations.Schema;

namespace OpenSpartan.Models
{
    internal class EntityAvailabilityModel
    {
        [Column("MATCH_AVAILABLE")]
        public bool MatchAvailable { get; set; }

        [Column("PLAYER_STATS_AVAILABLE")]
        public bool PlayerStatsAvailable { get; set; }
    }
}
