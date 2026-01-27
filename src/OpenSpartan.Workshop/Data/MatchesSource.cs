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
        private readonly TaskCompletionSource<bool> _initializationComplete = new();
        private bool _isInitialized = false;

        public MatchesSource()
        {
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // Yield to allow the static initializer to complete before accessing MatchesViewModel.Instance
            await Task.Yield();

            try
            {
                await UserContextManager.GetPlayerMatches();
                _isInitialized = true;
                _initializationComplete.TrySetResult(true);
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Match initialization failed: {ex.Message}", LogSeverity.Error);
                _initializationComplete.TrySetResult(false);
            }
        }

        async Task<IEnumerable<MatchTableEntity>> IIncrementalSource<MatchTableEntity>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            // Wait for initialization on first page
            if (pageIndex == 0 && !_isInitialized)
            {
                await _initializationComplete.Task;
            }

            var matchList = MatchesViewModel.Instance.MatchList;
            if (matchList == null || matchList.Count == 0)
            {
                return Enumerable.Empty<MatchTableEntity>();
            }

            var boundaryTime = matchList.Min(a => a.EndTime)
                .ToUniversalTime()
                .ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            return await DataHandler.GetMatchesAsync(
                $"xuid({HomeViewModel.Instance.Xuid})",
                boundaryTime,
                pageSize);
        }
    }
}
