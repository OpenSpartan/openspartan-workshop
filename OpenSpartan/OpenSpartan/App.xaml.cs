﻿using Microsoft.UI.Xaml;
using OpenSpartan.Data;
using OpenSpartan.Shared;
using OpenSpartan.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OpenSpartan
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

            var databaseBootstrapResult = DataHandler.BootstrapDatabase();
            var journalingMode = DataHandler.SetWALJournalingMode();

            if (journalingMode.ToLower() == "wal")
            {
                Debug.WriteLine("Successfully set WAL journaling mode.");
            }
            else
            {
                Debug.WriteLine("Could not set WAL journaling mode.");
            }

            var authResult = await UserContextManager.InitializePublicClientApplication();
            if (authResult != null)
            {
                var instantiationResult = UserContextManager.InitializeHaloClient(authResult);
                SplashScreenViewModel.Instance.IsBlocking = false;

                if (instantiationResult)
                {
                    Parallel.Invoke(async () => await UserContextManager.PopulateServiceRecordData(),
                        async () => await UserContextManager.PopulateCareerData(),
                        async () => await UserContextManager.PopulateCustomizationData(),
                        async () => await UserContextManager.PopulateDecorationData(),
                        async () => await UserContextManager.PopulateMedalData(),
                        async () =>
                        {
                            var matchRecordsOutcome = await UserContextManager.PopulateMatchRecordsData();
                            MatchesViewModel.Instance.MatchLoadingState = Models.MatchLoadingState.Completed;
                        },
                        async () => await UserContextManager.LoadBattlePassData());
                }
            }
        }

        internal Window m_window;
    }
}
