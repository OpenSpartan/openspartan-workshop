using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using OpenSpartan.Workshop.ViewModels;
using OpenSpartan.Workshop.Views;
using System;
using System.Linq;

namespace OpenSpartan.Workshop
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;

            AppWindow.SetIcon("CustomImages/OpenSpartan.Workshop.ico");

            nvRoot.SelectedItem = nvRoot.MenuItems.OfType<NavigationViewItem>().First();
            ContentFrame.Navigate(typeof(Views.HomeView), null, new EntranceNavigationTransitionInfo());

            ContentFrame.Navigated += On_Navigated;
        }

        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            nvRoot.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(Views.SettingsView))
            {
                // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
                nvRoot.SelectedItem = (NavigationViewItem)nvRoot.SettingsItem;
            }
            else if (ContentFrame.SourcePageType != null)
            {
                // Select the nav view item that corresponds to the page being navigated to.
                if (ContentFrame.SourcePageType != typeof(MedalMatchesView))
                {
                    nvRoot.SelectedItem = nvRoot.MenuItems
                                .OfType<NavigationViewItem>()
                                .First(i => i.Tag.Equals(ContentFrame.SourcePageType.FullName.ToString()));
                }
            }
        }

        private void nvRoot_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                NavView_Navigate(typeof(Views.SettingsView), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                Type navPageType = Type.GetType(args.InvokedItemContainer.Tag.ToString());
                NavView_Navigate(navPageType, args.RecommendedNavigationTransitionInfo);
            }
        }

        private void NavView_Navigate(Type navPageType, NavigationTransitionInfo transitionInfo)
        {
            // Get the page type before navigation so you can prevent duplicate
            // entries in the backstack.
            Type preNavPageType = ContentFrame.CurrentSourcePageType;

            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null && !Type.Equals(preNavPageType, navPageType))
            {
                ContentFrame.Navigate(navPageType, null, transitionInfo);
            }
        }

        private void nvRoot_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }
    }
}
