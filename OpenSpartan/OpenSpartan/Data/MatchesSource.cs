using CommunityToolkit.Common.Collections;
using OpenSpartan.Models;
using OpenSpartan.Shared;
using OpenSpartan.ViewModels;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenSpartan.Data
{
    public class MatchesSource : IIncrementalSource<MatchTableEntity>
    {
        private readonly List<MatchTableEntity> matches;

        public MatchesSource()
        {
            Task.Run(() =>
            {
                UserContextManager.GetPlayerMatches();
            });
        }

        Task<IEnumerable<MatchTableEntity>> IIncrementalSource<MatchTableEntity>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            var date = MatchesViewModel.Instance.MatchList.Min(a => a.StartTime).ToString("o", CultureInfo.InvariantCulture);
            var matches = Task.Run(() => (IEnumerable<MatchTableEntity>)DataHandler.GetMatches($"xuid({HomeViewModel.Instance.Xuid})", date, pageSize));

            return matches;
        }
    }
}
