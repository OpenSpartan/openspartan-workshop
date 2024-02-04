using CommunityToolkit.WinUI;
using Den.Dev.Orion.Converters;
using Microsoft.UI.Xaml;
using OpenSpartan.Workshop.Data;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

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

            var authResult = await UserContextManager.InitializePublicClientApplication();
            if (authResult != null)
            {
                var instantiationResult = UserContextManager.InitializeHaloClient(authResult);
                SplashScreenViewModel.Instance.IsBlocking = false;

                if (instantiationResult)
                {
                    // Only create the database and handle the initialization if we are able to
                    // properly authenticate and create a Halo client.
                    DataHandler.PlayerXuid = UserContextManager.HaloClient.Xuid;

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

                    Parallel.Invoke(async () => await UserContextManager.PopulateServiceRecordData(),
                        async () => await UserContextManager.PopulateCareerData(),
                        async () => await UserContextManager.PopulateUserInventory(),
                        async () => await UserContextManager.PopulateCustomizationData(),
                        async () => await UserContextManager.PopulateDecorationData(),
                        async () => await UserContextManager.PopulateMedalData(),
                        async () =>
                        {
                            var matchRecordsOutcome = await UserContextManager.PopulateMatchRecordsData();

                            if (matchRecordsOutcome)
                            {
                                await (MainWindow as MainWindow).DispatcherQueue.EnqueueAsync(() =>
                                {
                                    MatchesViewModel.Instance.MatchLoadingState = Models.MetadataLoadingState.Completed;
                                });
                            }
                        },
                        async () =>
                        {
                            await UserContextManager.PopulateBattlePassData();

                            await (MainWindow as MainWindow).DispatcherQueue.EnqueueAsync(() =>
                            {
                                BattlePassViewModel.Instance.BattlePassLoadingState = Models.MetadataLoadingState.Completed;
                            });
                        });
                }
            }
        }

        internal Window m_window;
    }
}
