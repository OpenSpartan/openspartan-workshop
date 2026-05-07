using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenSpartan.Workshop.Data
{
    public sealed class MedalMatchesSource : IIncrementalSource<MatchTableEntity>
    {
        public MedalMatchesSource()
        {
            if (MedalMatchesViewModel.Instance != null && MedalMatchesViewModel.Instance.Medal != null)
            {
                Task.Run(() =>
                {
                    UserContextManager.PopulateMedalMatchData(MedalMatchesViewModel.Instance.Medal.NameId);

                    UserContextManager.RunOnUI(() =>
                    {
                        MedalMatchesViewModel.Instance.MatchLoadingState = MetadataLoadingState.Completed;
                    });
                });
            }
        }

        async Task<IEnumerable<MatchTableEntity>> IIncrementalSource<MatchTableEntity>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            if (MedalMatchesViewModel.Instance.MatchList == null || MedalMatchesViewModel.Instance.MatchList.Count == 0)
            {
                return null;
            }

            var date = MedalMatchesViewModel.Instance.MatchList.Min(a => a.StartTime).ToString("o", CultureInfo.InvariantCulture);

            // Use the genuinely-async DataHandler entry point instead of wrapping the
            // synchronous variant in Task.Run, which burns a thread-pool thread per
            // page load.
            return await DataHandler.GetMatchesWithMedalAsync(
                $"xuid({HomeViewModel.Instance.Xuid})",
                MedalMatchesViewModel.Instance.Medal.NameId,
                date,
                pageSize);
        }
    }
}
