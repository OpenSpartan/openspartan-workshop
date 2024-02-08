using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using OpenSpartan.Workshop.Data;
using OpenSpartan.Workshop.Shared;
using OpenSpartan.Workshop.ViewModels;
using System.Diagnostics;
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

            var authResult = await UserContextManager.InitializeAllDataOnLaunch();
        }

        internal Window m_window;
    }
}
