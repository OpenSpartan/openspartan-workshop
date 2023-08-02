using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using OpenSpartan.Models;
using OpenSpartan.Shared;
using OpenSpartan.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
        }

        private async void BattlePassView_Loaded(object sender, RoutedEventArgs e)
        {
            var operations = await UserContextManager.GetOperations();

            if (operations != null)
            {
                foreach (var operation in operations.OperationRewardTracks)
                {
                    var compoundEvent = new OperationCompoundModel();
                    compoundEvent.RewardTrack = operation;

                    var operationDetails = await UserContextManager.GetEvent(operation.RewardTrackPath);
                    if (operationDetails != null)
                    {
                        compoundEvent.RewardTrackMetadata = operationDetails;

                        foreach (var rewardBucket in operationDetails.Ranks)
                        {
                            var freeInventoryRewards = await ExtractInventoryRewards(rewardBucket.Rank, rewardBucket.FreeRewards.InventoryRewards, true);
                            var freeCurrencyRewards = await ExtractCurrencyRewards(rewardBucket.Rank, rewardBucket.FreeRewards.CurrencyRewards, true);

                            var paidInventoryRewards = await ExtractInventoryRewards(rewardBucket.Rank, rewardBucket.PaidRewards.InventoryRewards, false);
                            var paidCurrencyRewards = await ExtractCurrencyRewards(rewardBucket.Rank, rewardBucket.PaidRewards.CurrencyRewards, false);

                            compoundEvent.Rewards = compoundEvent.Rewards.Concat(freeInventoryRewards)
                                                                         .Concat(freeCurrencyRewards)
                                                                         .Concat(paidInventoryRewards)
                                                                         .Concat(paidCurrencyRewards).ToList();

                            Debug.WriteLine($"{operation.RewardTrackPath} - Rank {rewardBucket.Rank} - Completed");
                        }

                        BattlePassViewModel.Instance.BattlePasses.Add(compoundEvent);
                    }


                }
            }
        }

        internal async Task<List<RewardMetaContainer>> ExtractCurrencyRewards(int rank, IEnumerable<CurrencyAmount> currencyItems, bool isFree)
        {
            List<RewardMetaContainer> rewardContainers = new List<RewardMetaContainer>();

            foreach (var currencyReward in currencyItems)
            {
                RewardMetaContainer container = new()
                {
                    Rank = rank,
                    IsFree = isFree,
                    Amount = currencyReward.Amount,
                    CurrencyDetails = await UserContextManager.GetInGameCurrency(currencyReward.CurrencyPath)
                };
                rewardContainers.Add(container);
            }

            return rewardContainers;
        }

        internal async Task<List<RewardMetaContainer>> ExtractInventoryRewards(int rank, IEnumerable<InventoryAmount> inventoryItems, bool isFree)
        {
            List<RewardMetaContainer> rewardContainers = new List<RewardMetaContainer>();

            foreach (var inventoryReward in inventoryItems)
            {
                RewardMetaContainer container = new()
                {
                    Rank = rank,
                    IsFree = isFree,
                    Amount = inventoryReward.Amount,
                    ItemDetails = await UserContextManager.GetInGameItem(inventoryReward.InventoryItemPath)
                };
                rewardContainers.Add(container);
            }

            return rewardContainers;
        }
    }
}
