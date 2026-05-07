using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Threading.Tasks;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class MatchesView : Page
    {
        public MatchesView()
        {
            InitializeComponent();
            this.Loaded += MatchesView_Loaded;
            this.Unloaded += MatchesView_Unloaded;
        }

        private void MatchesView_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ((MatchesViewModel)this.DataContext).NavigationRequested -= MatchesView_NavigationRequested;
        }

        // Cap on the number of rows we'll auto-fill before handing control back
        // to the IncrementalLoadingCollection's scroll-triggered load. Generous
        // enough to overflow any reasonable monitor (~32px per row × 200 rows
        // = ~6400px of content) so the user always has scrollable content.
        private const int MaxAutoFillRows = 200;

        // Page size we ask for on each LoadMoreItemsAsync hop while filling.
        private const uint AutoFillPageSize = 20;

        private bool _autoFillInProgress;

        private async void MatchesView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ((MatchesViewModel)this.DataContext).NavigationRequested += MatchesView_NavigationRequested;

            // The CommunityToolkit DataGrid doesn't pull the first page from
            // ISupportIncrementalLoading the way ListView does, so we kick off
            // the initial fetch here when the bound list is empty. We then keep
            // pulling pages until the DataGrid's content overflows the viewport
            // — otherwise the user has nothing to scroll, and the collection's
            // own scroll-triggered load never fires (small window blocks paging).
            await EnsureMatchListFillsViewportAsync();
        }

        private async Task EnsureMatchListFillsViewportAsync()
        {
            if (_autoFillInProgress)
            {
                return;
            }

            var matchList = MatchesViewModel.Instance.MatchList;
            if (matchList == null)
            {
                return;
            }

            _autoFillInProgress = true;
            try
            {
                while (matchList.HasMoreItems && matchList.Count < MaxAutoFillRows)
                {
                    int before = matchList.Count;
                    await matchList.LoadMoreItemsAsync(AutoFillPageSize).AsTask();

                    // No items returned -> nothing more to load locally; stop.
                    if (matchList.Count == before)
                    {
                        break;
                    }

                    // Yield so the DataGrid can run its measure pass and update
                    // its inner ScrollViewer's ScrollableHeight; otherwise we'd
                    // always observe ScrollableHeight == 0 and over-fetch.
                    await Task.Yield();

                    var scroller = FindFirstDescendant<ScrollViewer>(this);
                    if (scroller != null && scroller.ScrollableHeight > 0)
                    {
                        // Content overflows the viewport. The user can scroll
                        // now, so the IncrementalLoadingCollection's own
                        // near-end-of-scroll trigger will take over for the
                        // remaining pages.
                        break;
                    }
                }
            }
            finally
            {
                _autoFillInProgress = false;
            }
        }

        private static T? FindFirstDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root is T match)
            {
                return match;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var found = FindFirstDescendant<T>(VisualTreeHelper.GetChild(root, i));
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void MatchesView_NavigationRequested(object? sender, long e)
        {
            // Once navigation starts, it's safe to assume that the match loading begins, so
            // we want to make sure that the infobar is properly displayed once the view is rendered.
            MedalMatchesViewModel.Instance.MatchLoadingState = Models.MetadataLoadingState.Loading;

            Frame.Navigate(typeof(MedalMatchesView), e);
        }

        private async void btnRefreshMatches_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var matchRecordsOutcome = await UserContextManager.PopulateMatchRecordsData();
            
            if (matchRecordsOutcome)
            {
                await UserContextManager.RunOnUI(() =>
                {
                    MatchesViewModel.Instance.MatchLoadingState = MetadataLoadingState.Completed;
                });
            }
        }
    }
}
