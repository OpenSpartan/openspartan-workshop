using Microsoft.UI.Xaml;
using OpenSpartan.Data;
using OpenSpartan.Shared;
using OpenSpartan.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
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

            var databaseBootstrapResult = DataHandler.BootstrapDatabase("local.db");

            var authResult = await UserContextManager.InitializePublicClientApplication();
            if (authResult != null)
            {
                var instantiationResult = UserContextManager.InitializeHaloClient(authResult);
                SplashScreenViewModel.Instance.IsBlocking = false;

                if (instantiationResult)
                {
                    var serviceRecordOutcome = UserContextManager.PopulateServiceRecordData();

                    var careerOutcome = UserContextManager.PopulateCareerData();

                    var customizationOutcome = UserContextManager.PopulateCustomizationData();

                    var decorationOutcome = UserContextManager.PopulateDecorationData();
                }
            }
        }        

        private Window m_window;
    }
}
