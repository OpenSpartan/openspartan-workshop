using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Diagnostics;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan.Workshop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomeView : Page
    {
        public HomeView()
        {
            this.InitializeComponent();
        }

        private async void btnOpenHaloWaypoint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            string targetHaloWaypointUrl = $"https://www.halowaypoint.com/halo-infinite/players/{HomeViewModel.Instance.Gamertag}";

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

        //private void scroller_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    UpdateScrollButtonsVisibility();
        //}

        //private void UpdateScrollButtonsVisibility()
        //{
        //    if (scroller.ScrollableWidth > 0)
        //    {
        //        ScrollForwardBtn.Visibility = Visibility.Visible;
        //    }
        //    else
        //    {
        //        ScrollForwardBtn.Visibility = Visibility.Collapsed;
        //    }
        //}

        //private void ScrollBackBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    scroller.ChangeView(scroller.HorizontalOffset - scroller.ViewportWidth, null, null);
        //}

        //private void ScrollForwardBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    scroller.ChangeView(scroller.HorizontalOffset + scroller.ViewportWidth, null, null);
        //}
    }
}
