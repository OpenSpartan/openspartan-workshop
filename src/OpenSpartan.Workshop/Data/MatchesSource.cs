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
    public sealed class MatchesSource : IIncrementalSource<MatchTableEntity>
    {
        public MatchesSource()
        {
            Task.Run(() =>
            {
                UserContextManager.GetPlayerMatches();
            });
        }

        Task<IEnumerable<MatchTableEntity>> IIncrementalSource<MatchTableEntity>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            if (MatchesViewModel.Instance.MatchList != null && MatchesViewModel.Instance.MatchList.Count > 0)
            {
                var date = MatchesViewModel.Instance.MatchList.Min(a => a.EndTime).ToString("o", CultureInfo.InvariantCulture);
                var universalDate = DateTime.Parse(date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                var matches = Task.Run(() => (IEnumerable<MatchTableEntity>)DataHandler.GetMatches($"xuid({HomeViewModel.Instance.Xuid})", universalDate, pageSize));

                return matches;
            }
            else
            {
                return Task.FromResult(Enumerable.Empty<MatchTableEntity>());
            }
        }
    }
}
