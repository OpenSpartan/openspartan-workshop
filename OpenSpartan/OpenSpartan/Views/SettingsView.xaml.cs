using Microsoft.UI.Xaml.Controls;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan.Workshop.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsView : Page
    {
        public SettingsView()
        {
            this.InitializeComponent();
        }

        private async void btnLogOut_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to log out?", "OpenSpartan Workshop", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                try
                {
                    File.Delete(Path.Combine(Core.Configuration.AppDataDirectory, Core.Configuration.CacheFileName));

                    // Make sure that we stop loading matches, if any are currently in progress.
                    UserContextManager.MatchLoadingCancellationTracker.Cancel();

                    // Block the screen again.
                    SplashScreenViewModel.Instance.IsBlocking = true;

                    var authResult = await UserContextManager.InitializeAllDataOnLaunch();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not log out by deleting the credential cache file. {ex.Message}");
                }
            }
        }
    }
}
