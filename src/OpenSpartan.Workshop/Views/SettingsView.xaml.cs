using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using NLog;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Diagnostics;
using System.IO;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class SettingsView : Page
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public SettingsView()
        {
            InitializeComponent();
        }

        private async void btnLogOut_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ContentDialog deleteFileDialog = new ContentDialog
            {
                Title = "Log out",
                Content = "Are you sure you want to log out?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No"
            };

            ContentDialogResult result = await deleteFileDialog.ShowAsync();

            // Delete the file if the user clicked the primary button.
            /// Otherwise, do nothing.
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    File.Delete(Path.Combine(Core.Configuration.AppDataDirectory, Core.Configuration.CacheFileName));

                    // Make sure that we stop loading matches, if any are currently in progress.
                    UserContextManager.MatchLoadingCancellationTracker.Cancel();

                    await UserContextManager.DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        SplashScreenViewModel.Instance.IsBlocking = true;

                        // Reset the player details.
                        HomeViewModel.Instance.Xuid = null;
                        HomeViewModel.Instance.Gamertag = null;

                        // Clear out the user's previous data.
                        BattlePassViewModel.Instance.BattlePasses = null;
                        MatchesViewModel.Instance.MatchList = null;
                        MedalsViewModel.Instance.Medals = null;
                    });

                    var authResult = await UserContextManager.InitializeAllDataOnLaunch();
                }
                catch (Exception ex)
                {
                    if ((bool)SettingsViewModel.Instance.EnableLogging) Logger.Error($"Could not log out by deleting the credential cache file. {ex.Message}");
                }
            }
        }

        private void btnViewFiles_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = Configuration.AppDataDirectory,
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
