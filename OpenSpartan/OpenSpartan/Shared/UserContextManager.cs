using CommunityToolkit.WinUI;
using Den.Dev.Orion.Authentication;
using Den.Dev.Orion.Core;
using Den.Dev.Orion.Models;
using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.Data.Sqlite;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using OpenSpartan.Data;
using OpenSpartan.Models;
using OpenSpartan.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using MatchType = Den.Dev.Orion.Models.HaloInfinite.MatchType;

namespace OpenSpartan.Shared
{
    internal static class UserContextManager
    {
        internal static HaloInfiniteClient HaloClient { get; set; }

        internal static XboxTicket XboxUserContext { get; set; }

        internal static async Task<AuthenticationResult> InitializePublicClientApplication()
        {
            var storageProperties = new StorageCreationPropertiesBuilder(Core.Configuration.CacheFileName, Core.Configuration.AppDataDirectory).Build();

            var pca = PublicClientApplicationBuilder.Create(Core.Configuration.ClientID).WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount).Build();

            // This hooks up the cross-platform cache into MSAL
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(pca.UserTokenCache);

            IAccount accountToLogin = (await pca.GetAccountsAsync()).FirstOrDefault();

            AuthenticationResult authResult = null;

            try
            {
                authResult = await pca.AcquireTokenSilent(Core.Configuration.Scopes, accountToLogin)
                                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                authResult = await pca.AcquireTokenInteractive(Core.Configuration.Scopes)
                                            .WithAccount(accountToLogin)
                                            .ExecuteAsync();
            }

            return authResult;
        }

        internal static bool InitializeHaloClient(AuthenticationResult authResult)
        {
            HaloAuthenticationClient haloAuthClient = new();
            XboxAuthenticationClient manager = new();

            var ticket = new XboxTicket();
            var haloTicket = new XboxTicket();
            var extendedTicket = new XboxTicket();
            var haloToken = new SpartanToken();

            Task.Run(async () =>
            {
                ticket = await manager.RequestUserToken(authResult.AccessToken);
                if (ticket == null)
                {
                    ticket = await manager.RequestUserToken(authResult.AccessToken);
                }
            }).GetAwaiter().GetResult();

            Task.Run(async () =>
            {
                haloTicket = await manager.RequestXstsToken(ticket.Token);
            }).GetAwaiter().GetResult();

            Task.Run(async () =>
            {
                extendedTicket = await manager.RequestXstsToken(ticket.Token, false);
            }).GetAwaiter().GetResult();

            Task.Run(async () =>
            {
                haloToken = await haloAuthClient.GetSpartanToken(haloTicket.Token, 4);
                Debug.WriteLine("Your Halo token:");
                Debug.WriteLine(haloToken.Token);
            }).GetAwaiter().GetResult();

            if (extendedTicket != null)
            {
                XboxUserContext = extendedTicket;

                HaloClient = new(haloToken.Token, extendedTicket.DisplayClaims.Xui[0].XUID);

                string localClearance = string.Empty;
                Task.Run(async () =>
                {
                    var clearance = (await HaloClient.SettingsGetClearance("RETAIL", "UNUSED", "245613.23.06.01.1708-0", "1.4")).Result;
                    if (clearance != null)
                    {
                        localClearance = clearance.FlightConfigurationId;
                        HaloClient.ClearanceToken = localClearance;
                        Debug.WriteLine($"Your clearance is {localClearance} and it's set in the client.");
                    }
                    else
                    {
                        Debug.WriteLine("Could not obtain the clearance.");
                    }
                }).GetAwaiter().GetResult();

                return true;
            }
            else
            {
                return false;
            }
        }

        internal static async Task<bool> PopulateCareerData()
        {
            try
            {
                // Get career details.
                var careerTrackResult = await HaloClient.EconomyGetPlayerCareerRank(new List<string>() { $"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})" }, "careerRank1");

                if (careerTrackResult.Result != null && careerTrackResult.Response.Code == 200)
                {
                    HomeViewModel.Instance.CareerSnapshot = careerTrackResult.Result;
                }

                var careerTrackContainerResult = await HaloClient.GameCmsGetCareerRanks("careerRank1");

                if (careerTrackContainerResult.Result != null && careerTrackContainerResult.Response.Code == 200)
                {
                    HomeViewModel.Instance.MaxRank = careerTrackContainerResult.Result.Ranks.Count;

                    // The rank here is incremented by one because of off-by-one counting when ranks are established. The introductory rank apparently is counted differently in the index
                    // compared to the full set of ranks in the reward track.
                    var currentCareerStage = (from c in careerTrackContainerResult.Result.Ranks where c.Rank == HomeViewModel.Instance.CareerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank + 1 select c).FirstOrDefault();
                    if (currentCareerStage != null)
                    {
                        HomeViewModel.Instance.Title = currentCareerStage.RankTitle.Value;
                        HomeViewModel.Instance.CurrentRankExperience = careerTrackResult.Result.RewardTracks[0].Result.CurrentProgress.PartialProgress;
                        HomeViewModel.Instance.RequiredRankExperience = currentCareerStage.XpRequiredForRank;

                        // Let's also compute secondary values that can tell us how far the user is from the Hero title.
                        HomeViewModel.Instance.ExperienceTotalRequired = careerTrackContainerResult.Result.Ranks.Sum(item => item.XpRequiredForRank);

                        var relevantRanks = (from c in careerTrackContainerResult.Result.Ranks where c.Rank <= HomeViewModel.Instance.CareerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank select c);
                        HomeViewModel.Instance.ExperienceEarnedToDate = relevantRanks.Sum(rank => rank.XpRequiredForRank) + careerTrackResult.Result.RewardTracks[0].Result.CurrentProgress.PartialProgress;

                        string qualifiedRankImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", currentCareerStage.RankLargeIcon);
                        string qualifiedAdornmentImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", currentCareerStage.RankAdornmentIcon);

                        // Let's make sure that we create the directory if it does not exist.
                        System.IO.FileInfo file = new System.IO.FileInfo(qualifiedRankImagePath);
                        file.Directory.Create();

                        file = new System.IO.FileInfo(qualifiedAdornmentImagePath);
                        file.Directory.Create();

                        if (!System.IO.File.Exists(qualifiedRankImagePath))
                        {
                            var rankImage = await HaloClient.GameCmsGetImage(currentCareerStage.RankLargeIcon);
                            if (rankImage.Result != null && rankImage.Response.Code == 200)
                            {
                                System.IO.File.WriteAllBytes(qualifiedRankImagePath, rankImage.Result);
                            }
                        }
                        HomeViewModel.Instance.RankImage = qualifiedRankImagePath;

                        if (!System.IO.File.Exists(qualifiedAdornmentImagePath))
                        {
                            var adornmentImage = await HaloClient.GameCmsGetImage(currentCareerStage.RankAdornmentIcon);
                            if (adornmentImage.Result != null && adornmentImage.Response.Code == 200)
                            {
                                System.IO.File.WriteAllBytes(qualifiedAdornmentImagePath, adornmentImage.Result);
                            }
                        }
                        HomeViewModel.Instance.AdornmentImage = qualifiedAdornmentImagePath;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static async Task<bool> PopulateServiceRecordData()
        {
            try
            {
                HomeViewModel.Instance.Gamertag = XboxUserContext.DisplayClaims.Xui[0].Gamertag;
                HomeViewModel.Instance.Xuid = XboxUserContext.DisplayClaims.Xui[0].XUID;

                // Get initial service record details
                var serviceRecordResult = await HaloClient.StatsGetPlayerServiceRecord(HomeViewModel.Instance.Gamertag, Den.Dev.Orion.Models.HaloInfinite.LifecycleMode.Matchmade);

                if (serviceRecordResult.Result != null && serviceRecordResult.Response.Code == 200)
                {
                    HomeViewModel.Instance.ServiceRecord = serviceRecordResult.Result;

                    DataHandler.InsertServiceRecordEntry(serviceRecordResult.Response.Message);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static async Task<bool> PopulateDecorationData()
        {
            try
            {
                string backgroundPath = "progression/Switcher/Season_Switcher_S4_IN.png";
                // Get initial service record details
                string qualifiedBackgroundImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", backgroundPath);

                if (!System.IO.File.Exists(qualifiedBackgroundImagePath))
                {
                    var backgroundImageResult = await HaloClient.GameCmsGetImage(backgroundPath);

                    if (backgroundImageResult.Result != null && backgroundImageResult.Response.Code == 200)
                    {
                        // Let's make sure that we create the directory if it does not exist.
                        System.IO.FileInfo file = new System.IO.FileInfo(qualifiedBackgroundImagePath);
                        file.Directory.Create();

                        System.IO.File.WriteAllBytes(qualifiedBackgroundImagePath, backgroundImageResult.Result);
                    }
                }

                HomeViewModel.Instance.SeasonalBackground = qualifiedBackgroundImagePath;

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static async Task<bool> PopulateCustomizationData()
        {
            try
            {
                var customizationResult = await HaloClient.EconomyPlayerCustomization($"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})", "public");

                if (customizationResult.Result != null && customizationResult.Response.Code == 200)
                {
                    HomeViewModel.Instance.ServiceTag = customizationResult.Result.Appearance.ServiceTag;

                    var emblemMapping = await HaloClient.GameCmsGetEmblemMapping();

                    if (emblemMapping.Result != null && emblemMapping.Response.Code == 200)
                    {
                        var emblem = await HaloClient.GameCmsGetItem(customizationResult.Result.Appearance.Emblem.EmblemPath, HaloClient.ClearanceToken);
                        var backdrop = await HaloClient.GameCmsGetItem(customizationResult.Result.Appearance.BackdropImagePath, HaloClient.ClearanceToken);

                        var nameplate = (from n in emblemMapping.Result where n.Key == emblem.Result.CommonData.Id select n).FirstOrDefault();
                        var configuration = (from c in nameplate.Value where c.Key.ToString() == customizationResult.Result.Appearance.Emblem.ConfigurationId.ToString() select c).FirstOrDefault();

                        if (!configuration.Equals(default))
                        {
                            HomeViewModel.Instance.IDBadgeTextColor = configuration.Value.TextColor;

                            string qualifiedNameplateImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", configuration.Value.NameplateCmsPath);
                            string qualifiedEmblemImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", configuration.Value.EmblemCmsPath);
                            string qualifiedBackdropImagePath = backdrop.Result != null ? Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", backdrop.Result.ImagePath.Media.MediaUrl.Path) : string.Empty;

                            // Let's make sure that we create the directory if it does not exist.
                            FileInfo file = new FileInfo(qualifiedNameplateImagePath);
                            file.Directory.Create();

                            file = new System.IO.FileInfo(qualifiedEmblemImagePath);
                            file.Directory.Create();

                            file = new System.IO.FileInfo(qualifiedBackdropImagePath);
                            file.Directory.Create();

                            if (!System.IO.File.Exists(qualifiedNameplateImagePath))
                            {
                                var nameplateData = await HaloClient.GameCmsGetGenericWaypointFile(configuration.Value.NameplateCmsPath);

                                if (nameplateData.Result != null && nameplateData.Response.Code == 200)
                                {
                                    System.IO.File.WriteAllBytes(qualifiedNameplateImagePath, nameplateData.Result);
                                }
                            }
                            HomeViewModel.Instance.Nameplate = qualifiedNameplateImagePath;

                            if (!System.IO.File.Exists(qualifiedEmblemImagePath))
                            {
                                var emblemData = await HaloClient.GameCmsGetGenericWaypointFile(configuration.Value.EmblemCmsPath);

                                if (emblemData.Result != null && emblemData.Response.Code == 200)
                                {
                                    System.IO.File.WriteAllBytes(qualifiedEmblemImagePath, emblemData.Result);
                                }
                            }
                            HomeViewModel.Instance.Emblem = qualifiedEmblemImagePath;

                            if (!System.IO.File.Exists(qualifiedBackdropImagePath))
                            {
                                var backdropData = await HaloClient.GameCmsGetImage(backdrop.Result.ImagePath.Media.MediaUrl.Path);

                                if (backdropData.Result != null && backdropData.Response.Code == 200)
                                {
                                    System.IO.File.WriteAllBytes(qualifiedBackdropImagePath, backdropData.Result);
                                }
                            }
                            HomeViewModel.Instance.Backdrop = qualifiedBackdropImagePath;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        internal static async Task<bool> PopulateMatchRecordsData()
        {
            MatchesViewModel.Instance.MatchLoadingState = Models.MatchLoadingState.Calculating;

            try
            {
                //var currentMatchmadeRecords = DataHandler.GetCountOfMatchRecords();
                var ids = await GetPlayerMatchIds(XboxUserContext.DisplayClaims.Xui[0].XUID);
                if (ids != null && ids.Count > 0)
                {
                    var distinctMatchIds = ids.DistinctBy(x => x.ToString());

                    var existingMatches = DataHandler.GetMatchIds();

                    if (existingMatches != null)
                    {
                        var matchesToProcess = distinctMatchIds.Except(existingMatches);
                        if (matchesToProcess != null && matchesToProcess.Count() > 0)
                        {
                            MatchesViewModel.Instance.MatchLoadingState = Models.MatchLoadingState.Loading;
                            return await DataHandler.UpdateMatchRecords(matchesToProcess);
                        }
                        else
                        {
                            Debug.WriteLine("No matches to update.");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("No matches found locally, so need to re-hydrate the database.");
                        return await DataHandler.UpdateMatchRecords(distinctMatchIds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private static async Task<List<Guid>> GetPlayerMatchIds(string xuid)
        {
            List<Guid> matchIds = new();

            int queryCount = 25;
            int queryStart = 0;

            // We start with a value of 1 to bootstrap the process. This will then be overwritten.
            int lastResultCount = 1;

            while (lastResultCount > 0)
            {
                var matches = await HaloClient.StatsGetMatchHistory($"xuid({xuid})", queryStart, queryCount, MatchType.All);
                if (matches != null && matches.Result != null && matches.Result.Results != null && matches.Result.ResultCount > 0)
                {
                    lastResultCount = matches.Result.ResultCount;

                    // We want to extract individual match IDs first.
                    var matchIdBatch = matches.Result.Results.Select(item => item.MatchId).ToList();
                    Debug.WriteLine($"Got matches starting from {queryStart} up to {queryCount} entries. Last query yielded {matchIdBatch.Count} results.");
                    matchIds.AddRange(matchIdBatch);

                    MatchesViewModel.Instance.MatchLoadingParameter = matchIds.Count.ToString();

                    //counter = counter - matchIdBatch.Count;
                    queryStart = queryStart + matchIdBatch.Count;
                }
                else
                {
                    break;
                }
            }

            Debug.WriteLine($"Clocked at {matchIds.Count} total matchmade games.");
            return matchIds;
        }

        internal static async void GetPlayerMatches()
        {
            List<MatchTableEntity> matches = null;
            string date = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            if (MatchesViewModel.Instance.MatchList.Count == 0)
            {
                matches = DataHandler.GetMatches($"xuid({HomeViewModel.Instance.Xuid})", date, 100);
            }
            else
            {
                date = MatchesViewModel.Instance.MatchList.Min(a => a.StartTime).ToString("o", CultureInfo.InvariantCulture);
                matches = DataHandler.GetMatches($"xuid({HomeViewModel.Instance.Xuid})", date, 10);
            }

            if (matches != null)
            {
                var dispatcherWindow = ((Application.Current as App)?.MainWindow) as MainWindow;
                await dispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    MatchesViewModel.Instance.MatchList.AddRange(matches);
                });
            }
            else
            {
                Debug.WriteLine("Could not get the list of matches for the specified parameters.");
            }
        }

        internal static async Task<bool> PopulateMedalData()
        {
            try
            {
                Debug.WriteLine("Getting medal metadata...");
                var medalReferences = (await HaloClient.GameCmsGetMedalMetadata()).Result;

                if (medalReferences.Medals != null && medalReferences.Medals.Count > 0)
                {
                    var medals = DataHandler.GetMedals();

                    if (medals != null)
                    {
                        // There is likely a delta in medals here because the end-result doesn't actually
                        // account for quite a few medals, such as the ones from Infection game modes.
                        var compoundMedals = medals.Join(medalReferences.Medals, earned => earned.NameId, references => references.NameId, (earned, references) => new Medal()
                        {
                            Count = earned.Count,
                            Description = references.Description,
                            DifficultyIndex = references.DifficultyIndex,
                            Name = references.Name,
                            NameId = references.NameId,
                            PersonalScore = references.PersonalScore,
                            SortingWeight = references.SortingWeight,
                            SpriteIndex = references.SpriteIndex,
                            TotalPersonalScoreAwarded = earned.TotalPersonalScoreAwarded,
                            TypeIndex = references.TypeIndex,
                        }).ToList();

                        var group = compoundMedals.OrderByDescending(x => x.Count).GroupBy(x => x.TypeIndex);
                        MedalsViewModel.Instance.Medals = new System.Collections.ObjectModel.ObservableCollection<IGrouping<int, Medal>>(group);

                        string qualifiedMedalPath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", "medals");

                        var spriteContent = (await HaloClient.GameCmsGetGenericWaypointFile(medalReferences.Sprites.ExtraLarge.Path)).Result;
                        using MemoryStream ms = new(spriteContent);
                        SkiaSharp.SKBitmap bmp = SkiaSharp.SKBitmap.Decode(ms);
                        using var pixmap = bmp.PeekPixels();

                        foreach (var medal in compoundMedals)
                        {
                            string medalImagePath = Path.Combine(qualifiedMedalPath, $"{medal.NameId}.png");
                            if (!System.IO.File.Exists(medalImagePath))
                            {
                                FileInfo file = new FileInfo(medalImagePath);
                                file.Directory.Create();

                                // The spritesheet for medals is 16x16, so we want to make sure that we extract the right medals.
                                var row = (int)Math.Floor(medal.SpriteIndex / 16.0);
                                var column = (int)(medal.SpriteIndex % 16.0);

                                SkiaSharp.SKRectI rectI = SkiaSharp.SKRectI.Create(column * 256, row * 256, 256, 256);

                                var subset = pixmap.ExtractSubset(rectI);
                                using (var data = subset.Encode(SkiaSharp.SKPngEncoderOptions.Default))
                                {
                                    System.IO.File.WriteAllBytes(medalImagePath, data.ToArray());
                                    Debug.WriteLine($"Wrote medal to file: {medalImagePath}");
                                }

                            }
                        }

                        Debug.WriteLine("Got medals.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not obtain medal metadata. Error: {ex.Message}");
                return false;
            }
            return true;
        }

        public static async Task<OperationRewardTrackSnapshot> GetOperations()
        {
            return (await HaloClient.EconomyPlayerOperations($"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})")).Result;
        }

        public static async Task<CurrencyDefinition> GetInGameCurrency(string currencyId)
        {
            return (await HaloClient.GameCmsGetCurrency(currencyId, HaloClient.ClearanceToken)).Result;
        }

        public static async Task<bool> LoadBattlePassData()
        {
            var operations = await GetOperations();

            if (operations != null)
            {
                foreach (var operation in operations.OperationRewardTracks)
                {
                    var compoundEvent = new OperationCompoundModel
                    {
                        RewardTrack = operation
                    };

                    // Check to see if there is a local copy. If there isn't, store the data.
                    if (!DataHandler.IsOperationRewardTrackAvailable(operation.RewardTrackPath))
                    {
                        var operationDetails = await HaloClient.GameCmsGetEvent(operation.RewardTrackPath, HaloClient.ClearanceToken);

                        if (operationDetails != null && operationDetails.Result != null && operationDetails.Response.Code == 200)
                        {
                            // We need to store operation details locally.
                            DataHandler.UpdateOperationRewardTracks(operationDetails.Response.Message, operation.RewardTrackPath);

                            compoundEvent.RewardTrackMetadata = operationDetails.Result;

                            compoundEvent.Rewards = await GetFlattenedRewards(operationDetails.Result.Ranks);
                            Debug.WriteLine($"{operation.RewardTrackPath} - Completed");

                            BattlePassViewModel.Instance.BattlePasses.Add(compoundEvent);
                        }
                    }
                    // Otherwise, get the reward track directly from the database.
                    else
                    {
                        var operationDetails = DataHandler.GetOperationResponseBody(operation.RewardTrackPath);
                        compoundEvent.RewardTrackMetadata = operationDetails;
                        compoundEvent.Rewards = await GetFlattenedRewards(operationDetails.Ranks);

                        Debug.WriteLine($"{operation.RewardTrackPath} (Local) - Completed");

                        BattlePassViewModel.Instance.BattlePasses.Add(compoundEvent);
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        internal static async Task<List<RewardMetaContainer>> GetFlattenedRewards(List<RankSnapshot> rankSnapshots)
        {
            List<RewardMetaContainer> rewards = new();

            foreach (var rewardBucket in rankSnapshots)
            {
                var freeInventoryRewards = await ExtractInventoryRewards(rewardBucket.Rank, rewardBucket.FreeRewards.InventoryRewards, true);
                var freeCurrencyRewards = await ExtractCurrencyRewards(rewardBucket.Rank, rewardBucket.FreeRewards.CurrencyRewards, true);

                var paidInventoryRewards = await ExtractInventoryRewards(rewardBucket.Rank, rewardBucket.PaidRewards.InventoryRewards, false);
                var paidCurrencyRewards = await ExtractCurrencyRewards(rewardBucket.Rank, rewardBucket.PaidRewards.CurrencyRewards, false);

                rewards.AddRange(freeInventoryRewards);
                rewards.AddRange(freeCurrencyRewards);
                rewards.AddRange(paidInventoryRewards);
                rewards.AddRange(paidCurrencyRewards);

                Debug.WriteLine($"Rank {rewardBucket.Rank} - Completed");
            }

            return rewards;
        }

        internal static async Task<List<RewardMetaContainer>> ExtractCurrencyRewards(int rank, IEnumerable<CurrencyAmount> currencyItems, bool isFree)
        {
            List<RewardMetaContainer> rewardContainers = new List<RewardMetaContainer>();

            foreach (var currencyReward in currencyItems)
            {
                RewardMetaContainer container = new()
                {
                    Rank = rank,
                    IsFree = isFree,
                    Amount = currencyReward.Amount,
                    CurrencyDetails = await GetInGameCurrency(currencyReward.CurrencyPath)
                };
                rewardContainers.Add(container);
            }

            return rewardContainers;
        }

        internal static async Task<List<RewardMetaContainer>> ExtractInventoryRewards(int rank, IEnumerable<InventoryAmount> inventoryItems, bool isFree)
        {
            List<RewardMetaContainer> rewardContainers = new List<RewardMetaContainer>();

            foreach (var inventoryReward in inventoryItems)
            {
                var inventoryItemLocallyAvailable = DataHandler.IsInventoryItemAvailable(inventoryReward.InventoryItemPath);

                RewardMetaContainer container = new()
                {
                    Rank = rank,
                    IsFree = isFree,
                    Amount = inventoryReward.Amount,
                };

                if (inventoryItemLocallyAvailable)
                {
                    container.ItemDetails = DataHandler.GetInventoryItem(inventoryReward.InventoryItemPath);
                }
                else 
                {
                    var item = await HaloClient.GameCmsGetItem(inventoryReward.InventoryItemPath, HaloClient.ClearanceToken);
                    if (item != null && item.Result != null)
                    {
                        string qualifiedImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", item.Result.CommonData.DisplayPath.Media.MediaUrl.Path);

                        // Let's make sure that we create the directory if it does not exist.
                        System.IO.FileInfo file = new System.IO.FileInfo(qualifiedImagePath);
                        file.Directory.Create();

                        if (!System.IO.File.Exists(qualifiedImagePath))
                        {
                            var rankImage = await HaloClient.GameCmsGetImage(item.Result.CommonData.DisplayPath.Media.MediaUrl.Path);
                            if (rankImage.Result != null && rankImage.Response.Code == 200)
                            {
                                System.IO.File.WriteAllBytes(qualifiedImagePath, rankImage.Result);
                                Debug.WriteLine("Stored local image: " + item.Result.CommonData.DisplayPath.Media.MediaUrl.Path);
                            }
                        }

                        DataHandler.UpdateInventoryItems(item.Response.Message, inventoryReward.InventoryItemPath);
                        container.ItemDetails = item.Result;
                    }
                }

                rewardContainers.Add(container);
            }

            return rewardContainers;
        }
    }
}
