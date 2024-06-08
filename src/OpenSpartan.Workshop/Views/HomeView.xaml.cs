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
