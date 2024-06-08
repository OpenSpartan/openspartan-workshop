using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System;

namespace OpenSpartan.Workshop.Views
{
    public sealed partial class BattlePassView : Page
    {
        public BattlePassView()
        {
            InitializeComponent();
        }

        private void NavigateBattlePassView(Type navPageType, NavigationTransitionInfo transitionInfo, string argument)
        {
            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null)
            {
                BattlePassContentFrame.Navigate(navPageType, argument, transitionInfo);
                BattlePassViewModel.Instance.CurrentlySelectedBattlepass = argument;
            }
        }

        private void NavigateEventView(Type navPageType, NavigationTransitionInfo transitionInfo, string argument)
        {
            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null)
            {
                EventContentFrame.Navigate(navPageType, argument, transitionInfo);
                BattlePassViewModel.Instance.CurrentlySelectedEvent = argument;
            }
        }

        private void nvBattlePassDetails_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is string)
            {
                if (args.InvokedItemContainer != null && ((OperationCompoundModel)nvBattlePassDetails.SelectedItem).RewardTrack.RewardTrackPath != BattlePassViewModel.Instance.CurrentlySelectedBattlepass)
                {
                    NavigateBattlePassView(typeof(BattlePassDetailView), args.RecommendedNavigationTransitionInfo, args.InvokedItemContainer.Tag.ToString());
                }
            }
            else if (args.InvokedItem is OperationCompoundModel)
            {
                if (((OperationCompoundModel)nvBattlePassDetails.SelectedItem).RewardTrack.RewardTrackPath != BattlePassViewModel.Instance.CurrentlySelectedBattlepass)
                {
                    NavigateBattlePassView(typeof(BattlePassDetailView), args.RecommendedNavigationTransitionInfo, ((OperationCompoundModel)args.InvokedItem).RewardTrack.RewardTrackPath);
                }
            }
        }

        private void nvEventDetails_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is string)
            {
                if (args.InvokedItemContainer != null && ((OperationCompoundModel)nvEventDetails.SelectedItem).RewardTrack.RewardTrackPath != BattlePassViewModel.Instance.CurrentlySelectedEvent)
                {
                    NavigateEventView(typeof(EventDetailView), args.RecommendedNavigationTransitionInfo, args.InvokedItemContainer.Tag.ToString());
                }
            }
            else if (args.InvokedItem is OperationCompoundModel)
            {
                if (((OperationCompoundModel)nvEventDetails.SelectedItem).RewardTrack.RewardTrackPath != BattlePassViewModel.Instance.CurrentlySelectedEvent)
                {
                    NavigateEventView(typeof(EventDetailView), args.RecommendedNavigationTransitionInfo, ((OperationCompoundModel)args.InvokedItem).RewardTrack.RewardTrackPath);
                }
            }
        }
    }
}
