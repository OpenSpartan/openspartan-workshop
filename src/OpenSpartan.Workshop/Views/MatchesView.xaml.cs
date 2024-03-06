using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class MatchesView : Page
    {
        public MatchesView()
        {
            InitializeComponent();
            this.Loaded += MatchesView_Loaded;
        }

        private void MatchesView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ((MatchesViewModel)this.DataContext).NavigationRequested += MatchesView_NavigationRequested;
        }

        private void MatchesView_NavigationRequested(object sender, long e)
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
                await UserContextManager.DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    MatchesViewModel.Instance.MatchLoadingState = MetadataLoadingState.Completed;
                });
            }
        }
    }
}
