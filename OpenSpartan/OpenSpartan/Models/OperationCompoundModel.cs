using Den.Dev.Orion.Models.HaloInfinite;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenSpartan.Workshop.Models
{
    internal class OperationCompoundModel
    {
        public OperationCompoundModel()
        {
            Rewards = new();
        }

        public RewardTrack RewardTrack { get; set; }

        public RewardTrackMetadata RewardTrackMetadata { get; set; }

        public ObservableCollection<IGrouping<int, RewardMetaContainer>> Rewards { get; set; }
    }
}
