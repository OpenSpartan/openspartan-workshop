using Microsoft.UI.Xaml;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
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
            
            LoadSettings();

            var authResult = await UserContextManager.InitializeAllDataOnLaunch();
        }

        private void LoadSettings()
        {
            var settingsPath = Path.Combine(Configuration.AppDataDirectory, Configuration.SettingsFileName);
            if (File.Exists(settingsPath))
            {
                SettingsViewModel.Instance.Settings = SettingsManager.LoadSettings();
            }
            else
            {
                SettingsViewModel.Instance.Settings = new Models.WorkshopSettings
                {
                    APIVersion = Configuration.DefaultAPIVersion,
                    Audience = Configuration.DefaultAudience,
                    Build = Configuration.DefaultBuild,
                    HeaderImagePath = Configuration.DefaultHeaderImage,
                    Release = Configuration.DefaultRelease,
                    SyncSettings = true,
                    Sandbox = Configuration.DefaultSandbox,
                };
            }
        }

        internal Window m_window;
    }
}
