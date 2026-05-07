using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System;

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

        private async void MatchesView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ((MatchesViewModel)this.DataContext).NavigationRequested += MatchesView_NavigationRequested;

            // The CommunityToolkit DataGrid doesn't pull the first page from
            // ISupportIncrementalLoading the way ListView does, so we kick off
            // the initial fetch here when the bound list is empty. Subsequent
            // pages still load lazily when the user scrolls.
            var matchList = MatchesViewModel.Instance.MatchList;
            if (matchList != null && matchList.Count == 0 && matchList.HasMoreItems)
            {
                await matchList.LoadMoreItemsAsync(20).AsTask();
            }
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
