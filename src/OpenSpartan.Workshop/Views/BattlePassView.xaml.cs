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

        private void NavView_Navigate(Type navPageType, NavigationTransitionInfo transitionInfo, string argument)
        {
            // Only navigate if the selected page isn't currently loaded.
            if (navPageType is not null)
            {
                ContentFrame.Navigate(navPageType, argument, transitionInfo);
                BattlePassViewModel.Instance.CurrentlySelectedBattlepass = argument;
            }
        }

        private void nvBattlePassDetails_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is string)
            {
                if (args.InvokedItemContainer != null && ((OperationCompoundModel)nvBattlePassDetails.SelectedItem).RewardTrack.RewardTrackPath != BattlePassViewModel.Instance.CurrentlySelectedBattlepass)
                {
                    NavView_Navigate(typeof(BattlePassDetailView), args.RecommendedNavigationTransitionInfo, args.InvokedItemContainer.Tag.ToString());
                }
            }
            else if (args.InvokedItem is OperationCompoundModel)
            {
                if (((OperationCompoundModel)nvBattlePassDetails.SelectedItem).RewardTrack.RewardTrackPath != BattlePassViewModel.Instance.CurrentlySelectedBattlepass)
                {
                    NavView_Navigate(typeof(BattlePassDetailView), args.RecommendedNavigationTransitionInfo, ((OperationCompoundModel)args.InvokedItem).RewardTrack.RewardTrackPath);
                }
            }
        }
    }
}
