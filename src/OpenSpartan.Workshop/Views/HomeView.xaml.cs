using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.ViewModels;
using System;
using Windows.System;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class HomeView : Page
    {
        public HomeView()
        {
            InitializeComponent();
            Loaded += HomeView_Loaded;
        }

        private void HomeView_Loaded(object sender, RoutedEventArgs e)
        {
            // The Overview section is marked x:DeferLoadStrategy="Lazy". Realize it
            // at low priority after the page is loaded so the visible above-the-fold
            // content (header + Career cards) can render first. FindName triggers
            // the realization for deferred elements.
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                _ = FindName(nameof(OverviewSection));
            });
        }

        private async void btnOpenHaloWaypoint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            string targetHaloWaypointUrl = $"{Configuration.HaloWaypointPlayerEndpoint}/{HomeViewModel.Instance.Gamertag}";

            var success = await Launcher.LaunchUriAsync(new System.Uri(targetHaloWaypointUrl));

            if (!success)
            {
                LogEngine.Log("Could not open the profile on Halo Waypoint.", Models.LogSeverity.Error);
            }
        }

        private async void btnRefreshServiceRecord_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await UserContextManager.PopulateCareerData();
            await UserContextManager.PopulateServiceRecordData();
        }
    }
}
