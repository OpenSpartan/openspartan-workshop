using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Diagnostics;
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
                Debug.WriteLine("Could not open the profile on Halo Waypoint.");
            }
        }

        private async void btnRefreshServiceRecord_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await UserContextManager.PopulateCareerData();
            await UserContextManager.PopulateServiceRecordData();
        }
    }
}
