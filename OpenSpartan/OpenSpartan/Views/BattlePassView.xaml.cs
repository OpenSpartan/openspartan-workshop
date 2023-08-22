using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using OpenSpartan.Shared;
using System;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BattlePassView : Page
    {
        public BattlePassView()
        {
            this.InitializeComponent();
            this.Loaded += BattlePassView_Loaded;

            //nvBattlePassDetails.SelectedItem = nvBattlePassDetails.MenuItems.OfType<NavigationViewItem>().First();
            //ContentFrame.Navigate(typeof(Views.BattlePassView), null, new EntranceNavigationTransitionInfo());

            ContentFrame.Navigated += On_Navigated;
        }

        private async void BattlePassView_Loaded(object sender, RoutedEventArgs e)
        {
            var result = await UserContextManager.LoadBattlePassData();
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
                // Select the nav view item that corresponds to the page being navigated to.
                //nvBattlePassDetails.SelectedItem = nvBattlePassDetails.MenuItems
                //            .OfType<NavigationViewItem>()
                //            .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));
        }


        private void NavView_Navigate(Type navPageType, NavigationTransitionInfo transitionInfo, string argument)
        {
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null)
            {
                ContentFrame.Navigate(navPageType, argument, transitionInfo);
            }
        }

        private void nvBattlePassDetails_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                NavView_Navigate(typeof(BattlePassDetailView), args.RecommendedNavigationTransitionInfo, args.InvokedItemContainer.Tag.ToString());
            }
        }
    }
}
