using Microsoft.UI.Xaml;
using NLog;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.IO;

namespace OpenSpartan.Workshop
{
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public Window MainWindow { get => m_window; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            App.Current.RequestedTheme = ApplicationTheme.Dark;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

            LogManager.Setup().LoadConfigurationFromFile("NLog.config");

            LoadSettings();

            var authResult = await UserContextManager.InitializeAllDataOnLaunch();
        }

        private async void LoadSettings()
        {
            var settingsPath = Path.Combine(Configuration.AppDataDirectory, Configuration.SettingsFileName);
            if (File.Exists(settingsPath))
            {
                SettingsViewModel.Instance.Settings = SettingsManager.LoadSettings();
                if (SettingsViewModel.Instance.Settings.SyncSettings)
                {
                    try
                    {
                        var settings = await UserContextManager.GetWorkshopSettings();
                        if (settings != null)
                        {
                            // For now, we only have two settings that are returned by the API, so
                            // no need to overwrite the entire object.
                            SettingsViewModel.Instance.Settings.Release = settings.Release;
                            SettingsViewModel.Instance.Settings.HeaderImagePath = settings.HeaderImagePath;
                        }    
                    }
                    catch (Exception ex)
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Could not load settings remotely. {ex.Message}\nWill use previously-configured settings..");
                    }
                }
            }
            else
            {
                SettingsViewModel.Instance.Settings = new Models.WorkshopSettings
                {
                    APIVersion = Configuration.DefaultAPIVersion,
                    HeaderImagePath = Configuration.DefaultHeaderImage,
                    Release = Configuration.DefaultRelease,
                    SyncSettings = true,
                    EnableLogging = false,
                };
            }
        }

        internal Window m_window;
    }
}
