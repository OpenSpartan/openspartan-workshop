using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Views;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        private async void On_Navigated(object sender, NavigationEventArgs e)
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
                    var selectedItem = nvRoot.MenuItems
                        ?.OfType<NavigationViewItem>()
                        .FirstOrDefault(i => i.Tag?.Equals(ContentFrame.SourcePageType.FullName.ToString()) ?? false);

                    if (selectedItem != null)
                    {
                        nvRoot.SelectedItem = selectedItem;
                    }
                }
            }

            await CleanupFramesAsync();
        }

        private async Task CleanupFramesAsync()
        {
            await UserContextManager.DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
            {
                CleanupFrames();
            });
        }

        private void CleanupFrames()
        {
            for (int i = ContentFrame.BackStack.Count - 1; i >= 0; i--)
            {
                if (ContentFrame.BackStack[i].SourcePageType == typeof(MedalMatchesView))
                {
                    ContentFrame.BackStack.RemoveAt(i);
                }
            }
        }

        private void nvRoot_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                NavView_Navigate(typeof(Views.SettingsView), args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer?.Tag is string typeName)
            {
                Type navPageType = Type.GetType(typeName);
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
