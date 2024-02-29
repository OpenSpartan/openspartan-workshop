using CommunityToolkit.Common;
using CommunityToolkit.WinUI;
using Den.Dev.Orion.Authentication;
using Den.Dev.Orion.Core;
using Den.Dev.Orion.Models;
using Den.Dev.Orion.Models.HaloInfinite;
using Den.Dev.Orion.Models.Security;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.UI.Xaml;
using NLog;
using OpenSpartan.Workshop.Core;
using OpenSpartan.Workshop.Data;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenSpartan.Workshop.Shared
{
    internal static class UserContextManager
    {
        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly HttpClient WorkshopHttpClient = new() 
        { 
            BaseAddress = new Uri(Configuration.SettingsEndpoint),
            DefaultRequestHeaders =
            {
                UserAgent = { ProductInfoHeaderValue.Parse($"{Configuration.PackageName}/{Configuration.Version}-{Configuration.BuildId}") }
            }
        };

        internal static CancellationTokenSource MatchLoadingCancellationTracker = new();
        internal static CancellationTokenSource BattlePassLoadingCancellationTracker = new();

        internal static MainWindow DispatcherWindow = ((Application.Current as App)?.MainWindow) as MainWindow;

        internal static HaloInfiniteClient HaloClient { get; set; }

        internal static XboxTicket XboxUserContext { get; set; }

        internal static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        internal static IntPtr GetMainWindowHandle()
        {
            return WinRT.Interop.WindowNative.GetWindowHandle(DispatcherWindow);
        }

        internal static async Task<AuthenticationResult> InitializePublicClientApplication()
        {
            BrokerOptions options = new(BrokerOptions.OperatingSystems.Windows)
            {
                Title = "OpenSpartan Workshop"
            };

            var storageProperties = new StorageCreationPropertiesBuilder(Core.Configuration.CacheFileName, Core.Configuration.AppDataDirectory).Build();

            var pca = PublicClientApplicationBuilder
                .Create(Core.Configuration.ClientID)
                .WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount)
                .WithParentActivityOrWindow(GetMainWindowHandle)
                .WithBroker(options)
                .Build();

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
                try
                {
                    authResult = await pca.AcquireTokenInteractive(Core.Configuration.Scopes)
                                                .WithAccount(accountToLogin)
                                                .ExecuteAsync();
                }
                catch (MsalClientException)
                {
                    // Authentication was not successsful, we have no token.
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Error("Authentication was not successful.");
                }
            }

            return authResult;
        }

        public static async Task<WorkshopSettings> GetWorkshopSettings()
        {
            string apiEndpoint = "clientsettings";

            WorkshopHttpClient.DefaultRequestHeaders.Clear();
            WorkshopHttpClient.DefaultRequestHeaders.Add("X-API-Version", Configuration.DefaultAPIVersion);

            HttpResponseMessage response = await WorkshopHttpClient.GetAsync(apiEndpoint);

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<WorkshopSettings>(await response.Content.ReadAsStringAsync(), SerializerOptions);
            }
            else
            {
                return null;
            }
        }

        public static async Task<HaloApiResultContainer<T, RawResponseContainer>> SafeAPICall<T>(Func<Task<HaloApiResultContainer<T, RawResponseContainer>>> orionAPICall)
        {
            try
            {
                var result = await orionAPICall();

                if (result.Response.Code == 401)
                {
                    var tokenResult = await ReAcquireTokens();

                    if (!tokenResult)
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Error("Could not reacquire tokens.");

                        return default;
                    }

                    return await orionAPICall();
                }

                return result;
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Failed to make Halo Infinite API call. {ex.Message}");
                return null;
            }
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
                ticket ??= await manager.RequestUserToken(authResult.AccessToken);
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
            }).GetAwaiter().GetResult();

            if (extendedTicket != null)
            {
                XboxUserContext = extendedTicket;

                HaloClient = new(haloToken.Token, extendedTicket.DisplayClaims.Xui[0].XUID, userAgent: $"{Configuration.PackageName}/{Configuration.Version}-{Configuration.BuildId}");

                string localClearance = string.Empty;
                Task.Run(async () =>
                {
                    var clearance = (await SafeAPICall(async () => { return await HaloClient.SettingsActiveClearance(SettingsViewModel.Instance.Settings.Release); })).Result;
                    if (clearance != null)
                    {
                        localClearance = clearance.FlightConfigurationId;
                        HaloClient.ClearanceToken = localClearance;
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Your clearance is {localClearance} and it's set in the client.");
                    }
                    else
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Error("Could not obtain the clearance.");
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
                var tasks = new List<Task>
                {
                    SafeAPICall(async () => await HaloClient.EconomyGetPlayerCareerRank(new List<string> { $"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})" }, "careerRank1")),
                    SafeAPICall(async () => await HaloClient.GameCmsGetCareerRanks("careerRank1"))
                };

                await Task.WhenAll(tasks);

                var careerTrackResult = (HaloApiResultContainer<RewardTrackResultContainer, RawResponseContainer>)tasks[0].GetResultOrDefault();
                var careerTrackContainerResult = (HaloApiResultContainer<CareerTrackContainer, RawResponseContainer>)tasks[1].GetResultOrDefault();

                if (careerTrackResult.Result != null && careerTrackResult.Response.Code == 200)
                {
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() => HomeViewModel.Instance.CareerSnapshot = careerTrackResult.Result);
                }

                if (careerTrackContainerResult.Result != null && (careerTrackContainerResult.Response.Code == 200 || careerTrackContainerResult.Response.Code == 304))
                {
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() => HomeViewModel.Instance.MaxRank = careerTrackContainerResult.Result.Ranks.Count);

                    if (HomeViewModel.Instance.CareerSnapshot != null)
                    {
                        var currentCareerStage = careerTrackContainerResult.Result.Ranks
                            .FirstOrDefault(c => c.Rank == HomeViewModel.Instance.CareerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank + 1);

                        if (currentCareerStage != null)
                        {
                            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                            {
                                HomeViewModel.Instance.Title = currentCareerStage.RankTitle.Value;
                                HomeViewModel.Instance.CurrentRankExperience = careerTrackResult.Result.RewardTracks[0].Result.CurrentProgress.PartialProgress;
                                HomeViewModel.Instance.RequiredRankExperience = currentCareerStage.XpRequiredForRank;

                                HomeViewModel.Instance.ExperienceTotalRequired = careerTrackContainerResult.Result.Ranks.Sum(item => item.XpRequiredForRank);

                                var relevantRanks = careerTrackContainerResult.Result.Ranks
                                    .Where(c => c.Rank <= HomeViewModel.Instance.CareerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank);
                                HomeViewModel.Instance.ExperienceEarnedToDate = relevantRanks.Sum(rank => rank.XpRequiredForRank) + careerTrackResult.Result.RewardTracks[0].Result.CurrentProgress.PartialProgress;
                            });

                            string qualifiedRankImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", currentCareerStage.RankLargeIcon);
                            string qualifiedAdornmentImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", currentCareerStage.RankAdornmentIcon);

                            EnsureDirectoryExists(qualifiedRankImagePath);
                            EnsureDirectoryExists(qualifiedAdornmentImagePath);

                            await DownloadAndSetImage(currentCareerStage.RankLargeIcon, qualifiedRankImagePath, () => HomeViewModel.Instance.RankImage = qualifiedRankImagePath);
                            await DownloadAndSetImage(currentCareerStage.RankAdornmentIcon, qualifiedAdornmentImagePath, () => HomeViewModel.Instance.AdornmentImage = qualifiedAdornmentImagePath);
                        }
                    }
                    else
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Error("Could not get the career snapshot - it's null in the view model.");
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void EnsureDirectoryExists(string path)
        {
            var file = new FileInfo(path);
            file.Directory.Create();
        }

        private static async Task DownloadAndSetImage(string imageName, string imagePath, Action setImageAction)
        {
            if (!System.IO.File.Exists(imagePath))
            {
                var image = await SafeAPICall(async () => await HaloClient.GameCmsGetImage(imageName));
                if (image.Result != null && image.Response.Code == 200)
                {
                    System.IO.File.WriteAllBytes(imagePath, image.Result);
                }
            }

            await DispatcherWindow.DispatcherQueue.EnqueueAsync(setImageAction);
        }

        internal static async Task<bool> PopulateServiceRecordData()
        {
            try
            {
                // Get initial service record details
                var serviceRecordResult = await SafeAPICall(async () =>
                {
                    return await HaloClient.StatsGetPlayerServiceRecord(HomeViewModel.Instance.Gamertag, Den.Dev.Orion.Models.HaloInfinite.LifecycleMode.Matchmade);
                });

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
                string backgroundPath = SettingsViewModel.Instance.Settings.HeaderImagePath;

                // Get initial service record details
                string qualifiedBackgroundImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", backgroundPath);

                if (!System.IO.File.Exists(qualifiedBackgroundImagePath))
                {
                    var backgroundImageResult = await SafeAPICall(async () =>
                    {
                        return await HaloClient.GameCmsGetImage(backgroundPath);
                    });

                    if (backgroundImageResult.Result != null && backgroundImageResult.Response.Code == 200)
                    {
                        // Let's make sure that we create the directory if it does not exist.
                        FileInfo file = new System.IO.FileInfo(qualifiedBackgroundImagePath);
                        file.Directory.Create();

                        System.IO.File.WriteAllBytes(qualifiedBackgroundImagePath, backgroundImageResult.Result);
                    }
                }

                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    HomeViewModel.Instance.SeasonalBackground = qualifiedBackgroundImagePath;
                });

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
                var customizationResult = await SafeAPICall(async () => await HaloClient.EconomyPlayerCustomization($"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})", "public"));

                if (customizationResult.Result == null || customizationResult.Response.Code != 200)
                    return false;

                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    HomeViewModel.Instance.ServiceTag = customizationResult.Result.Appearance.ServiceTag;
                });

                var emblemMapping = await SafeAPICall(async () => await HaloClient.GameCmsGetEmblemMapping());

                if (emblemMapping.Result == null || emblemMapping.Response.Code != 200)
                    return false;

                var emblem = await SafeAPICall(async () => await HaloClient.GameCmsGetItem(customizationResult.Result.Appearance.Emblem.EmblemPath, HaloClient.ClearanceToken));
                var backdrop = await SafeAPICall(async () => await HaloClient.GameCmsGetItem(customizationResult.Result.Appearance.BackdropImagePath, HaloClient.ClearanceToken));

                EmblemMapping nameplate = null;

                if (emblem.Result != null)
                {
                    nameplate = emblemMapping.Result.GetValueOrDefault(emblem.Result.CommonData.Id)?.GetValueOrDefault(customizationResult.Result.Appearance.Emblem.ConfigurationId.ToString())
                                   ?? new EmblemMapping() { EmblemCmsPath = emblem.Result.CommonData.DisplayPath.Media.MediaUrl.Path, NameplateCmsPath = string.Empty, TextColor = "#FFF" };
                }

                if (nameplate != null)
                {
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        HomeViewModel.Instance.IDBadgeTextColor = nameplate.TextColor;
                    });

                    string qualifiedNameplateImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", nameplate.NameplateCmsPath);
                    string qualifiedEmblemImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", nameplate.EmblemCmsPath);
                    string qualifiedBackdropImagePath = backdrop.Result != null ? Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", backdrop.Result.ImagePath.Media.MediaUrl.Path) : string.Empty;

                    FileInfo file = new(qualifiedNameplateImagePath); file.Directory.Create();
                    file = new(qualifiedEmblemImagePath); file.Directory.Create();
                    file = new(qualifiedBackdropImagePath); file.Directory.Create();

                    await DownloadAndSetImage(nameplate.NameplateCmsPath, qualifiedNameplateImagePath, () => HomeViewModel.Instance.Nameplate = qualifiedNameplateImagePath);
                    await DownloadAndSetImage(emblem.Result.CommonData.DisplayPath.Media.MediaUrl.Path, qualifiedEmblemImagePath, () => HomeViewModel.Instance.Emblem = qualifiedEmblemImagePath);
                    await DownloadAndSetImage(backdrop.Result.ImagePath.Media.MediaUrl.Path, qualifiedBackdropImagePath, () => HomeViewModel.Instance.Backdrop = qualifiedBackdropImagePath);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Could not populate customization data. {ex.Message}");
                return false;
            }
        }

        internal static async Task<bool> PopulateMatchRecordsData()
        {
            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
            {
                MatchesViewModel.Instance.MatchLoadingState = Models.MetadataLoadingState.Calculating;
            });

            MatchLoadingCancellationTracker.Cancel();
            MatchLoadingCancellationTracker = new CancellationTokenSource();

            try
            {
                List<Guid> ids = await GetPlayerMatchIds(XboxUserContext.DisplayClaims.Xui[0].XUID, MatchLoadingCancellationTracker.Token);

                if (ids != null && ids.Count > 0)
                {
                    var distinctMatchIds = ids.DistinctBy(x => x.ToString());

                    var existingMatches = DataHandler.GetMatchIds();

                    if (existingMatches != null)
                    {
                        var matchesToProcess = distinctMatchIds.Except(existingMatches);
                        if (matchesToProcess != null && matchesToProcess.Any())
                        {
                            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                            {
                                MatchesViewModel.Instance.MatchLoadingState = MetadataLoadingState.Loading;
                            });

                            var result = await UpdateMatchRecords(matchesToProcess, MatchLoadingCancellationTracker.Token);

                            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                            {
                                MatchesViewModel.Instance.MatchList = new IncrementalLoadingCollection<MatchesSource, MatchTableEntity>();
                            });

                            return result;
                        }
                        else
                        {
                            if (SettingsViewModel.Instance.EnableLogging) Logger.Info("No matches to update.");
                        }
                    }
                    else
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info("No matches found locally, so need to re-hydrate the database.");

                        var result = await UpdateMatchRecords(distinctMatchIds, MatchLoadingCancellationTracker.Token);

                        await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                        {
                            MatchesViewModel.Instance.MatchList = new IncrementalLoadingCollection<MatchesSource, MatchTableEntity>();
                        });

                        return result;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    MatchesViewModel.Instance.MatchLoadingParameter = "0";
                });

                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Error processing matches. {ex.Message}");
                return false;
            }
        }

        internal static async Task<bool> UpdateMatchRecords(IEnumerable<Guid> matchIds, CancellationToken cancellationToken)
        {
            try
            {
                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    MatchesViewModel.Instance.MatchLoadingState = MetadataLoadingState.Loading;
                });

                int matchCounter = 0;
                int matchesTotal = matchIds.Count();

                foreach (var matchId in matchIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    double completionProgress = (matchCounter++ / (double)matchesTotal) * 100.0;

                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        MatchesViewModel.Instance.MatchLoadingParameter = $"{matchId} ({matchCounter} out of {matchesTotal} - {completionProgress:#.00}%)";
                    });

                    var matchStatsAvailability = DataHandler.GetMatchStatsAvailability(matchId.ToString());

                    if (!matchStatsAvailability.MatchAvailable)
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Getting match stats for {matchId}...");

                        var matchStats = await GetMatchStats(matchId.ToString(), completionProgress);
                        if (matchStats == null)
                            continue;

                        var processedMatchAssetParameters = await DataHandler.UpdateMatchAssetRecords(matchStats.Result);

                        bool matchStatsInsertionResult = DataHandler.InsertMatchStats(matchStats.Response.Message);
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info(matchStatsInsertionResult
                            ? $"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Stored match data for {matchId} in the database."
                            : $"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Could not store match {matchId} stats in the database.");
                    }
                    else
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Match {matchId} already available. Not requesting new data.");
                    }

                    if (!matchStatsAvailability.StatsAvailable)
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Attempting to get player results for players for match {matchId}.");

                        var playerStatsSnapshot = await GetPlayerStats(matchId.ToString());
                        if (playerStatsSnapshot == null)
                            continue;

                        var playerStatsInsertionResult = DataHandler.InsertPlayerMatchStats(matchId.ToString(), playerStatsSnapshot.Response.Message);

                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info(playerStatsInsertionResult
                            ? $"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Stored player stats for {matchId}."
                            : $"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Could not store player stats for {matchId}.");
                    }
                    else
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Match {matchId} player stats already available. Not requesting new data.");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Error storing matches. {ex.Message}");
                return false;
            }
        }

        private static async Task<HaloApiResultContainer<MatchStats, RawResponseContainer>> GetMatchStats(string matchId, double completionProgress)
        {
            var matchStats = await SafeAPICall(async () => await HaloClient.StatsGetMatchStats(matchId));
            if (matchStats == null || matchStats.Result == null)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"[{completionProgress:#.00}%] [Error] Getting match stats failed for {matchId}.");
                return null;
            }

            return matchStats;
        }

        private static async Task<HaloApiResultContainer<MatchSkillInfo, RawResponseContainer>> GetPlayerStats(string matchId)
        {
            var matchStats = await HaloClient.StatsGetMatchStats(matchId);
            if (matchStats == null || matchStats.Result == null || matchStats.Result.Players == null)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"[Error] Could not obtain player stats for match {matchId} because the match metadata was unavailable.");
                return null;
            }

            // Anything that starts with "bid" is a bot and including that in the request for player stats will result in failure.
            var targetPlayers = matchStats.Result.Players.Select(p => p.PlayerId).Where(p => !p.StartsWith("bid")).ToList();

            var playerStatsSnapshot = await SafeAPICall(async () => await HaloClient.SkillGetMatchPlayerResult(matchId, targetPlayers!));
            if (playerStatsSnapshot == null || playerStatsSnapshot.Result == null || playerStatsSnapshot.Result.Value == null)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Could not obtain player stats for match {matchId}. Requested {targetPlayers.Count} XUIDs.");
                return null;
            }

            return playerStatsSnapshot;
        }

        private static async Task<List<Guid>> GetPlayerMatchIds(string xuid, CancellationToken cancellationToken)
        {
            List<Guid> matchIds = new();

            int queryCount = 25;
            int queryStart = 0;

            // We start with a value of 1 to bootstrap the process. This will then be overwritten.
            int lastResultCount = 1;

            while (lastResultCount > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var matches = await SafeAPICall(async () =>
                {
                    return await HaloClient.StatsGetMatchHistory($"xuid({xuid})", queryStart, queryCount, Den.Dev.Orion.Models.HaloInfinite.MatchType.All);
                });

                if (matches != null && matches.Result != null && matches.Result.Results != null && matches.Result.ResultCount > 0)
                {
                    lastResultCount = matches.Result.ResultCount;

                    // We want to extract individual match IDs first.
                    var matchIdBatch = matches.Result.Results.Select(item => item.MatchId).ToList();
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Got matches starting from {queryStart} up to {queryCount} entries. Last query yielded {matchIdBatch.Count} results.");
                    matchIds.AddRange(matchIdBatch);

                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        MatchesViewModel.Instance.MatchLoadingParameter = matchIds.Count.ToString();
                    });

                    //counter = counter - matchIdBatch.Count;
                    queryStart += matchIdBatch.Count;
                }
                else
                {
                    break;
                }
            }

            if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Ended indexing at {matchIds.Count} total matchmade games.");
            return matchIds;
        }

        internal static async Task<bool> ReAcquireTokens()
        {
            var authResult = await InitializePublicClientApplication();
            if (authResult != null)
            {
                var result = InitializeHaloClient(authResult);

                return result;
            }
            else
            {
                return false;
            }
        }

        internal static async void GetPlayerMatches()
        {
            if (HomeViewModel.Instance.Xuid != null)
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
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Error("Could not get the list of matches for the specified parameters.");
                }
            }
        }

        internal static async void PopulateMedalMatchData(long medalNameId)
        {
            if (HomeViewModel.Instance.Xuid != null)
            {
                List<MatchTableEntity> matches = null;
                string date = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

                if (MedalMatchesViewModel.Instance.MatchList.Count == 0)
                {
                    matches = DataHandler.GetMatchesWithMedal($"xuid({HomeViewModel.Instance.Xuid})", medalNameId, date, 100);
                }
                else
                {
                    date = MedalMatchesViewModel.Instance.MatchList.Min(a => a.StartTime).ToString("o", CultureInfo.InvariantCulture);
                    matches = DataHandler.GetMatchesWithMedal($"xuid({HomeViewModel.Instance.Xuid})", medalNameId, date, 10);
                }

                if (matches != null)
                {
                    var dispatcherWindow = ((Application.Current as App)?.MainWindow) as MainWindow;
                    await dispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        MedalMatchesViewModel.Instance.MatchList.AddRange(matches);
                    });
                }
                else
                {
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Error("Could not get the list of matches for the specified parameters.");
                }
            }
        }

        internal static async Task<bool> PopulateMedalData()
        {
            try
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Info("Getting medal metadata...");
                var medalData = await SafeAPICall(async () => await HaloClient.GameCmsGetMedalMetadata());

                if (medalData?.Result?.Medals == null || medalData.Result.Medals.Count == 0)
                    return false;

                var medals = DataHandler.GetMedals();
                if (medals == null)
                    return false;

                var compoundMedals = medals.Join(medalData.Result.Medals, earned => earned.NameId, references => references.NameId, (earned, references) => new Medal()
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

                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    MedalsViewModel.Instance.Medals = new System.Collections.ObjectModel.ObservableCollection<IGrouping<int, Medal>>(group);
                });

                string qualifiedMedalPath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", "medals");

                var spriteRequestResult = await SafeAPICall(async () => await HaloClient.GameCmsGetGenericWaypointFile(medalData.Result.Sprites.ExtraLarge.Path));

                var spriteContent = spriteRequestResult?.Result;
                if (spriteContent != null)
                {
                    using MemoryStream ms = new(spriteContent);
                    SkiaSharp.SKBitmap bmp = SkiaSharp.SKBitmap.Decode(ms);
                    using var pixmap = bmp.PeekPixels();

                    foreach (var medal in compoundMedals)
                    {
                        string medalImagePath = Path.Combine(qualifiedMedalPath, $"{medal.NameId}.png");
                        EnsureDirectoryExists(medalImagePath);

                        if (!System.IO.File.Exists(medalImagePath))
                        {
                            var row = (int)Math.Floor(medal.SpriteIndex / 16.0);
                            var column = (int)(medal.SpriteIndex % 16.0);
                            SkiaSharp.SKRectI rectI = SkiaSharp.SKRectI.Create(column * 256, row * 256, 256, 256);

                            var subset = pixmap.ExtractSubset(rectI);
                            using var data = subset.Encode(SkiaSharp.SKPngEncoderOptions.Default);
                            System.IO.File.WriteAllBytes(medalImagePath, data.ToArray());
                            if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Wrote medal to file: {medalImagePath}");
                        }
                    }

                    if (SettingsViewModel.Instance.EnableLogging) Logger.Info("Got medals.");
                }
            }
            catch (Exception ex)
            {
                if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Could not obtain medal metadata. Error: {ex.Message}");
                return false;
            }
            return true;
        }

        public static async Task<OperationRewardTrackSnapshot> GetOperations()
        {
            return (await SafeAPICall(async () =>
            {
                return await HaloClient.EconomyPlayerOperations($"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})", HaloClient.ClearanceToken);
            })).Result;
        }

        public static async Task<CurrencyDefinition> GetInGameCurrency(string currencyId)
        {
            return (await SafeAPICall(async () =>
            {
                return await HaloClient.GameCmsGetCurrency(currencyId, HaloClient.ClearanceToken);
            })).Result;
        }

        public static async Task<bool> PopulateBattlePassData(CancellationToken cancellationToken)
        {
            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
            {
                BattlePassViewModel.Instance.BattlePassLoadingState = Models.MetadataLoadingState.Loading;
            });

            var operations = await GetOperations();

            if (operations != null)
            {
                foreach (var operation in operations.OperationRewardTracks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        BattlePassViewModel.Instance.BattlePassLoadingParameter = operation.RewardTrackPath;
                    });

                    var compoundEvent = new OperationCompoundModel
                    {
                        RewardTrack = operation
                    };

                    // Check to see if there is a local copy. If there isn't, store the data.
                    if (!DataHandler.IsOperationRewardTrackAvailable(operation.RewardTrackPath))
                    {
                        var operationDetails = await SafeAPICall(async () =>
                        {
                            return await HaloClient.GameCmsGetEvent(operation.RewardTrackPath, HaloClient.ClearanceToken);
                        });

                        if (operationDetails != null && operationDetails.Result != null && operationDetails.Response.Code == 200)
                        {
                            // We need to store operation details locally.
                            DataHandler.UpdateOperationRewardTracks(operationDetails.Response.Message, operation.RewardTrackPath);

                            compoundEvent.RewardTrackMetadata = operationDetails.Result;

                            compoundEvent.Rewards = new(await GetFlattenedRewards(operationDetails.Result.Ranks, operation.CurrentProgress.Rank));
                            if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"{operation.RewardTrackPath} - Completed");

                            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                            {
                                BattlePassViewModel.Instance.BattlePasses.Add(compoundEvent);
                            });
                        }
                    }
                    // Otherwise, get the reward track directly from the database.
                    else
                    {
                        var operationDetails = DataHandler.GetOperationResponseBody(operation.RewardTrackPath);
                        compoundEvent.RewardTrackMetadata = operationDetails;
                        compoundEvent.Rewards = new(await GetFlattenedRewards(operationDetails.Ranks, operation.CurrentProgress.Rank));

                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"{operation.RewardTrackPath} (Local) - Completed");

                        await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                        {
                            BattlePassViewModel.Instance.BattlePasses.Add(compoundEvent);
                        });
                    }
                }

                return true;
            }
            else
            {
                return false;
            }

        }

        internal static async Task<bool> PopulateUserInventory()
        {
            var result = await SafeAPICall(async () =>
            {
                return await HaloClient.EconomyGetInventoryItems($"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})");
            });

            if (result != null && result.Result != null && result.Response.Code == 200)
            {
                var insertionResult = DataHandler.InsertOwnedInventoryItems(result.Result);
                return insertionResult;
            }
            return false;
        }

        internal static async Task<IEnumerable<IGrouping<int, RewardMetaContainer>>> GetFlattenedRewards(List<RankSnapshot> rankSnapshots, int currentPlayerRank)
        {
            List<RewardMetaContainer> rewards = new();

            foreach (var rewardBucket in rankSnapshots)
            {
                var freeInventoryRewards = await ExtractInventoryRewards(rewardBucket.Rank, currentPlayerRank, rewardBucket.FreeRewards.InventoryRewards, true);
                var freeCurrencyRewards = await ExtractCurrencyRewards(rewardBucket.Rank, currentPlayerRank, rewardBucket.FreeRewards.CurrencyRewards, true);

                var paidInventoryRewards = await ExtractInventoryRewards(rewardBucket.Rank, currentPlayerRank, rewardBucket.PaidRewards.InventoryRewards, false);
                var paidCurrencyRewards = await ExtractCurrencyRewards(rewardBucket.Rank, currentPlayerRank, rewardBucket.PaidRewards.CurrencyRewards, false);

                rewards.AddRange(freeInventoryRewards);
                rewards.AddRange(freeCurrencyRewards);
                rewards.AddRange(paidInventoryRewards);
                rewards.AddRange(paidCurrencyRewards);

                if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Rank {rewardBucket.Rank} - Completed");
            }

            return rewards.GroupBy(x => x.Ranks.Item1);
        }

        internal static async Task<List<RewardMetaContainer>> ExtractCurrencyRewards(int rank, int playerRank, IEnumerable<CurrencyAmount> currencyItems, bool isFree)
        {
            List<RewardMetaContainer> rewardContainers = new();

            foreach (var currencyReward in currencyItems)
            {
                RewardMetaContainer container = new()
                {
                    Ranks = new Tuple<int, int>(rank, playerRank),
                    IsFree = isFree,
                    Amount = currencyReward.Amount,
                    CurrencyDetails = await GetInGameCurrency(currencyReward.CurrencyPath)
                };

                if (container.CurrencyDetails != null)
                {
                    string currencyImageLocation = string.Empty;

                    switch (container.CurrencyDetails.Id.ToLower())
                    {
                        case "rerollcurrency":
                            currencyImageLocation = "progression/Currencies/1104-000-data-pad-e39bef84-2x2.png";
                            break;
                        case "xb":
                            currencyImageLocation = "progression/Currencies/1103-000-xp-boost-5e92621a-2x2.png";
                            break;
                        case "cr":
                            currencyImageLocation = "progression/Currencies/Credit_Coin-SM.png";
                            break;
                    }

                    container.ImagePath = currencyImageLocation;

                    string qualifiedImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", currencyImageLocation);

                    // Let's make sure that we create the directory if it does not exist.
                    EnsureDirectoryExists(qualifiedImagePath);

                    if (!System.IO.File.Exists(qualifiedImagePath))
                    {
                        var rankImage = await SafeAPICall(async () =>
                        {
                            return await HaloClient.GameCmsGetImage(currencyImageLocation);
                        });

                        if (rankImage.Result != null && rankImage.Response.Code == 200)
                        {
                            System.IO.File.WriteAllBytes(qualifiedImagePath, rankImage.Result);
                            if (SettingsViewModel.Instance.EnableLogging) Logger.Info("Stored local image: " + currencyImageLocation);
                        }
                    }
                }

                rewardContainers.Add(container);
            }

            return rewardContainers;
        }

        internal static async Task<List<RewardMetaContainer>> ExtractInventoryRewards(int rank, int playerRank, IEnumerable<InventoryAmount> inventoryItems, bool isFree)
        {
            List<RewardMetaContainer> rewardContainers = new();

            foreach (var inventoryReward in inventoryItems)
            {
                var inventoryItemLocallyAvailable = DataHandler.IsInventoryItemAvailable(inventoryReward.InventoryItemPath);

                RewardMetaContainer container = new()
                {
                    Ranks = new Tuple<int, int>(rank, playerRank),
                    IsFree = isFree,
                    Amount = inventoryReward.Amount,
                };

                if (inventoryItemLocallyAvailable)
                {
                    container.ItemDetails = DataHandler.GetInventoryItem(inventoryReward.InventoryItemPath);

                    if (container.ItemDetails != null)
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Trying to get local image for {container.ItemDetails.CommonData.Id} (entity: {inventoryReward.InventoryItemPath})");

                        var imageResult = await UpdateLocalImage("imagecache", container.ItemDetails.CommonData.DisplayPath.Media.MediaUrl.Path);

                        if (imageResult == true)
                        {
                            if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Stored local image: {container.ItemDetails.CommonData.DisplayPath.Media.MediaUrl.Path}");
                        }
                        else
                        {
                            if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Failed to store local image: {container.ItemDetails.CommonData.DisplayPath.Media.MediaUrl.Path}");
                        }
                    }
                    else
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Error("Inventory item is null.");
                    }
                }
                else
                {
                    var item = await SafeAPICall(async () =>
                    {
                        return await HaloClient.GameCmsGetItem(inventoryReward.InventoryItemPath, HaloClient.ClearanceToken);
                    });

                    if (item != null && item.Result != null)
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Trying to get local image for {item.Result.CommonData.Id} (entity: {inventoryReward.InventoryItemPath})");
                        var imageResult = await UpdateLocalImage("imagecache", item.Result.CommonData.DisplayPath.Media.MediaUrl.Path);

                        if (imageResult == true)
                        {
                            if (SettingsViewModel.Instance.EnableLogging) Logger.Info($"Stored local image: {item.Result.CommonData.DisplayPath.Media.MediaUrl.Path}");
                        }
                        else
                        {
                            if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Failed to store local image: {item.Result.CommonData.DisplayPath.Media.MediaUrl.Path}");
                        }

                        DataHandler.UpdateInventoryItems(item.Response.Message, inventoryReward.InventoryItemPath);
                        container.ItemDetails = item.Result;
                    }
                }


                if (container.ItemDetails != null)
                {
                    container.ImagePath = container.ItemDetails.CommonData.DisplayPath.Media.MediaUrl.Path;
                }
                else
                {
                    if (SettingsViewModel.Instance.EnableLogging) Logger.Error($"Could not set container item details for {inventoryReward.InventoryItemPath}");
                }

                rewardContainers.Add(container);
            }

            return rewardContainers;
        }

        internal static async Task<bool> UpdateLocalImage(string subDirectoryName, string imagePath)
        {

            string qualifiedImagePath = Path.Join(Core.Configuration.AppDataDirectory, subDirectoryName, imagePath);

            // Let's make sure that we create the directory if it does not exist.
            EnsureDirectoryExists(qualifiedImagePath);

            if (!System.IO.File.Exists(qualifiedImagePath))
            {
                var rankImage = await SafeAPICall(async () =>
                {
                    return await HaloClient.GameCmsGetImage(imagePath);
                });

                if (rankImage.Result != null && rankImage.Response.Code == 200)
                {
                    System.IO.File.WriteAllBytes(qualifiedImagePath, rankImage.Result);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // File already exists, so we can safely return true.
                return true;
            }
        }

        internal static async Task<bool> InitializeAllDataOnLaunch()
        {
            var authResult = await InitializePublicClientApplication();
            if (authResult != null)
            {
                var instantiationResult = InitializeHaloClient(authResult);

                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    SplashScreenViewModel.Instance.IsBlocking = false;
                });

                if (instantiationResult)
                {
                    HomeViewModel.Instance.Gamertag = XboxUserContext.DisplayClaims.Xui[0].Gamertag;
                    HomeViewModel.Instance.Xuid = XboxUserContext.DisplayClaims.Xui[0].XUID;

                    var databaseBootstrapResult = DataHandler.BootstrapDatabase();
                    var journalingMode = DataHandler.SetWALJournalingMode();

                    if (journalingMode.ToLower() == "wal")
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Info("Successfully set WAL journaling mode.");
                    }
                    else
                    {
                        if (SettingsViewModel.Instance.EnableLogging) Logger.Error("Could not set WAL journaling mode.");
                    }

                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        // Reset all collections to make sure that left-over data is not displayed.
                        BattlePassViewModel.Instance.BattlePasses = BattlePassViewModel.Instance.BattlePasses ?? new System.Collections.ObjectModel.ObservableCollection<OperationCompoundModel>();
                        MatchesViewModel.Instance.MatchList = MatchesViewModel.Instance.MatchList ?? new IncrementalLoadingCollection<MatchesSource, MatchTableEntity>();
                        MedalsViewModel.Instance.Medals = MedalsViewModel.Instance.Medals ?? new System.Collections.ObjectModel.ObservableCollection<IGrouping<int, Medal>>();
                    });

                    Parallel.Invoke(async () => await PopulateServiceRecordData(),
                        async () => await PopulateCareerData(),
                        async () => await PopulateUserInventory(),
                        async () => await PopulateCustomizationData(),
                        async () => await PopulateDecorationData(),
                        async () => await PopulateMedalData(),
                        async () =>
                        {
                            var matchRecordsOutcome = await PopulateMatchRecordsData();

                            if (matchRecordsOutcome)
                            {
                                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                                {
                                    MatchesViewModel.Instance.MatchLoadingState = Models.MetadataLoadingState.Completed;
                                    MatchesViewModel.Instance.MatchLoadingParameter = string.Empty;
                                });
                            }
                        },
                        async () =>
                        {
                            try
                            {
                                await PopulateBattlePassData(BattlePassLoadingCancellationTracker.Token);

                                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                                {
                                    BattlePassViewModel.Instance.BattlePassLoadingState = Models.MetadataLoadingState.Completed;
                                });
                            }
                            catch
                            {
                                BattlePassLoadingCancellationTracker = new CancellationTokenSource();
                            }
                        });

                    return true;
                }

                return false;
            }
            return false;
        }
    }
}
