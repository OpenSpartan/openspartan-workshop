using Microsoft.UI.Xaml;
using NLog;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.IO;

namespace OpenSpartan.Workshop
{
    public partial class App : Application
    {
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

            bool authResult = await UserContextManager.InitializeAllDataOnLaunch();
        }

        private async static void LoadSettings()
        {
            var settingsPath = Path.Combine(Configuration.AppDataDirectory, Configuration.SettingsFileName);
            if (File.Exists(settingsPath))
            {
                // Make sure that we default logging to true prior to settings
                // being loaded so that we can capture any errors that might be
                // happening with settings initialization.
                SettingsViewModel.Instance.Settings = SettingsManager.LoadSettings();

                if ((bool)SettingsViewModel.Instance.Settings.SyncSettings)
                {
                    try
                    {
                        var settings = await UserContextManager.GetWorkshopSettings();

                        if (settings != null)
                        {
                            SettingsViewModel.Instance.Settings.Release = settings.Release;
                            SettingsViewModel.Instance.Settings.HeaderImagePath = settings.HeaderImagePath;
                            SettingsViewModel.Instance.Settings.Build = settings.Build;
                            SettingsViewModel.Instance.Settings.Sandbox = settings.Sandbox;
                            SettingsViewModel.Instance.Settings.UseObanClearance = settings.UseObanClearance;
                        }    
                    }
                    catch (Exception ex)
                    {
                        LogEngine.Log($"Could not load settings remotely. {ex.Message}\nWill use previously-configured settings..", LogSeverity.Error);
                    }
                }
            }
            else
            {
                SettingsViewModel.Instance.Settings = new WorkshopSettings
                {
                    APIVersion = Configuration.DefaultAPIVersion,
                    HeaderImagePath = Configuration.DefaultHeaderImage,
                    Release = Configuration.DefaultRelease,
                    Sandbox = Configuration.DefaultSandbox,
                    Build = Configuration.DefaultBuild,
                    SyncSettings = true,
                    EnableLogging = false,
                    UseBroker = true,
                    UseObanClearance = false,
                };
            }
        }

        internal Window m_window;
    }
}
