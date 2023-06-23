using Microsoft.Identity.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            var pca = PublicClientApplicationBuilder.Create(Authentication.Configuration.ClientID).WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows)).Build();

            IAccount accountToLogin = (await pca.GetAccountsAsync()).FirstOrDefault();
            if (accountToLogin == null)
            {
                accountToLogin = PublicClientApplication.OperatingSystemAccount;
            }

            try
            {
                var authResult = await pca.AcquireTokenSilent(Authentication.Configuration.Scopes, accountToLogin)
                                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                var authResult = await pca.AcquireTokenInteractive(Authentication.Configuration.Scopes)
                                            .WithAccount(accountToLogin)
                                            .WithParentActivityOrWindow(WinRT.Interop.WindowNative.GetWindowHandle(this))
                                            .ExecuteAsync();                          
            }
        }

        private void nvRoot_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                ContentFrame.Navigate(typeof(Views.SettingsView), null, args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null && (args.InvokedItemContainer.Tag != null))
            {
                Type newPage = Type.GetType(args.InvokedItemContainer.Tag.ToString());
                ContentFrame.Navigate(
                       newPage,
                       null,
                       args.RecommendedNavigationTransitionInfo
                       );
            }
        }
    }
}
