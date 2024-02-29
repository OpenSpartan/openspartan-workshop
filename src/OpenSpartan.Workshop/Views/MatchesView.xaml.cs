using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class MatchesView : Page
    {
        public MatchesView()
        {
            InitializeComponent();
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
