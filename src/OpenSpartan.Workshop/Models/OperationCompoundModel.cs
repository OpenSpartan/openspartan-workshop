using Den.Dev.Orion.Models.HaloInfinite;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenSpartan.Workshop.Models
{
    internal sealed class OperationCompoundModel
    {
        public OperationCompoundModel()
        {
            Rewards = [];
        }

        public RewardTrack? RewardTrack { get; set; }

        public RewardTrackMetadata? RewardTrackMetadata { get; set; }

        public ObservableCollection<IGrouping<int, RewardMetaContainer>>? Rewards { get; set; }

        public SeasonRewardTrack? SeasonRewardTrack { get; set; }
    }
}
