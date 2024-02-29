using CommunityToolkit.Common.Collections;
using CommunityToolkit.WinUI;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenSpartan.Workshop.Data
{
    public class MedalMatchesSource : IIncrementalSource<MatchTableEntity>
    {
        public MedalMatchesSource()
        {
            if (MedalMatchesViewModel.Instance != null && MedalMatchesViewModel.Instance.Medal != null)
            {
                Task.Run(() =>
                {
                    UserContextManager.PopulateMedalMatchData(MedalMatchesViewModel.Instance.Medal.NameId);

                    UserContextManager.DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        MedalMatchesViewModel.Instance.MatchLoadingState = MetadataLoadingState.Completed;
                    });
                });
            }
        }

        Task<IEnumerable<MatchTableEntity>> IIncrementalSource<MatchTableEntity>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            if (MedalMatchesViewModel.Instance.MatchList != null && MedalMatchesViewModel.Instance.MatchList.Count > 0)
            {
                var date = MedalMatchesViewModel.Instance.MatchList.Min(a => a.StartTime).ToString("o", CultureInfo.InvariantCulture);
                var matches = Task.Run(() => (IEnumerable<MatchTableEntity>)DataHandler.GetMatchesWithMedal($"xuid({HomeViewModel.Instance.Xuid})", MedalMatchesViewModel.Instance.Medal.NameId, date, pageSize));

                return matches;
            }
            else
            {
                return null;
            }
        }
    }
}
