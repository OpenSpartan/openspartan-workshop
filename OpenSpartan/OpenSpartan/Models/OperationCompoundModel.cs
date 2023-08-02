using Den.Dev.Orion.Models.HaloInfinite;
using System.Collections.Generic;

namespace OpenSpartan.Models
{
    internal class OperationCompoundModel
    {
        public OperationCompoundModel()
        {
            Rewards = new List<RewardMetaContainer>();
        }

        public RewardTrack RewardTrack { get; set; }

        public RewardTrackMetadata RewardTrackMetadata { get; set; }

        public List<RewardMetaContainer> Rewards { get; set; }
    }
}
