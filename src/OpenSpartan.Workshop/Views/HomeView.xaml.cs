using Microsoft.UI.Xaml.Controls;
using NLog;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
using System;
using Windows.System;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class HomeView : Page
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error("Could not open the profile on Halo Waypoint.");
            }
        }

        private async void btnRefreshServiceRecord_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await UserContextManager.PopulateCareerData();
            await UserContextManager.PopulateServiceRecordData();
        }
    }
}
