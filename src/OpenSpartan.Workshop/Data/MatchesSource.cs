using CommunityToolkit.WinUI.Collections;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenSpartan.Workshop.Data
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes",
        Justification = "Instantiated by CommunityToolkit.WinUI.Collections.IncrementalLoadingCollection<TSource, TItem> via reflection on TSource.")]
    internal sealed class MatchesSource : IIncrementalSource<MatchTableEntity>
    {
        async Task<IEnumerable<MatchTableEntity>> IIncrementalSource<MatchTableEntity>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            if (HomeViewModel.Instance.Xuid == null)
            {
                return Enumerable.Empty<MatchTableEntity>();
            }

            // First page starts from "now"; subsequent pages start from the oldest
            // EndTime in the already-loaded list. Earlier this class also ran
            // UserContextManager.GetPlayerMatches in a constructor-initiated init
            // task that loaded 100 matches up front before the IncrementalLoading-
            // Collection did its own first GetPagedItemsAsync, so opening the
            // view double-fetched (100 + first page) before anything rendered.
            var matchList = MatchesViewModel.Instance.MatchList;

            string boundaryTime = matchList != null && matchList.Count > 0
                ? matchList.Min(a => a.EndTime).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
                : DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            return await DataHandler.GetMatchesAsync(
                $"xuid({HomeViewModel.Instance.Xuid})",
                boundaryTime,
                pageSize);
        }
    }
}
