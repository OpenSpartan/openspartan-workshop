using CommunityToolkit.WinUI;
using Den.Dev.Orion.Authentication;
using Den.Dev.Orion.Core;
using Den.Dev.Orion.Models;
using Den.Dev.Orion.Models.HaloInfinite;
using Den.Dev.Orion.Models.Security;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using NLog;
using OpenSpartan.Workshop.Data;
using OpenSpartan.Workshop.Models;
using OpenSpartan.Workshop.ViewModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenSpartan.Workshop.Core
{
    internal static class UserContextManager
    {
        private static MedalMetadata MedalMetadata;

        private const int MatchesPerPage = 25;

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
        internal static CancellationTokenSource ServiceRecordCancellationTracker = new();
        internal static CancellationTokenSource ExchangeCancellationTracker = new();

        internal static MainWindow DispatcherWindow = ((Application.Current as App)?.MainWindow) as MainWindow;

        internal static HaloInfiniteClient HaloClient { get; set; }

        internal static XboxTicket XboxUserContext { get; set; }

        internal static Dictionary<string, CurrencyDefinition> CurrencyDefinitions = [];

        internal static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        internal static nint GetMainWindowHandle()
        {
            return WinRT.Interop.WindowNative.GetWindowHandle(DispatcherWindow);
        }

        internal static async Task<MedalMetadata?> PrepopulateMedalMetadata()
        {
            try
            {
                LogEngine.Log($"Attempting to populate medal metadata...");

                var metadata = await SafeAPICall(() => HaloClient.GameCmsGetMedalMetadata());

                if (metadata?.Result != null)
                {
                    LogEngine.Log($"Medal metadata populated successfully.");
                    return metadata.Result;
                }
                else
                {
                    LogEngine.Log($"Failed to populate medal metadata: No valid result received.", LogSeverity.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Could not populate medal metadata: {ex.Message}", LogSeverity.Error);
                return null;
            }
        }

        internal static async Task<AuthenticationResult> InitializePublicClientApplication()
        {
            var storageProperties = new StorageCreationPropertiesBuilder(Configuration.CacheFileName, Configuration.AppDataDirectory).Build();

            var pcaBootstrap = PublicClientApplicationBuilder
                .Create(Configuration.ClientID)
                .WithDefaultRedirectUri()
                .WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount);

            if (SettingsViewModel.Instance.UseBroker)
            {
                BrokerOptions options = new(BrokerOptions.OperatingSystems.Windows)
                {
                    Title = "OpenSpartan Workshop"
                };

                pcaBootstrap.WithParentActivityOrWindow(GetMainWindowHandle).WithBroker(options);
            }

            var pca = pcaBootstrap.Build();

            // This hooks up the cross-platform cache into MSAL
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(pca.UserTokenCache);

            IAccount accountToLogin = (await pca.GetAccountsAsync()).FirstOrDefault();

            AuthenticationResult authResult = null;

            try
            {
                authResult = await pca.AcquireTokenSilent(Configuration.Scopes, accountToLogin)
                                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    authResult = await pca.AcquireTokenInteractive(Configuration.Scopes)
                                                .WithAccount(accountToLogin)
                                                .ExecuteAsync();
                }
                catch (MsalClientException ex)
                {
                    // Authentication was not successsful, we have no token.
                    LogEngine.Log($"Authentication was not successful. {ex.Message}", LogSeverity.Error);
                }
            }

            return authResult;
        }

        public static async Task<WorkshopSettings> GetWorkshopSettings()
        {
            WorkshopHttpClient.DefaultRequestHeaders.Add("X-API-Version", Configuration.DefaultAPIVersion);

            HttpResponseMessage response = await WorkshopHttpClient.GetAsync(new Uri(Configuration.SettingsEndpoint));

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
                HaloApiResultContainer<T, RawResponseContainer> result = await orionAPICall();

                if (result != null && result.Response != null && result.Response.Code == 401)
                {
                    if (await ReAcquireTokens())
                    {
                        result = await orionAPICall();
                    }
                    else
                    {
                        LogEngine.Log("Could not reacquire tokens.", LogSeverity.Error);
                        return default;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Failed to make Halo Infinite API call. {ex.Message}", LogSeverity.Error);
                return default;
            }
        }

        internal static async Task<bool> InitializeHaloClient(AuthenticationResult authResult)
        {
            try
            {
                HaloAuthenticationClient haloAuthClient = new();
                XboxAuthenticationClient manager = new();

                var ticket = await manager.RequestUserToken(authResult.AccessToken) ?? await manager.RequestUserToken(authResult.AccessToken);

                if (ticket == null)
                {
                    LogEngine.Log("Failed to obtain Xbox user token.", LogSeverity.Error);
                    return false;
                }

                var haloTicketTask = manager.RequestXstsToken(ticket.Token);
                var extendedTicketTask = manager.RequestXstsToken(ticket.Token, false);

                var haloTicket = await haloTicketTask;
                var extendedTicket = await extendedTicketTask;

                if (haloTicket == null)
                {
                    LogEngine.Log("Failed to obtain Halo XSTS token.", LogSeverity.Error);
                    return false;
                }

                var haloToken = await haloAuthClient.GetSpartanToken(haloTicket.Token, 4);

                if (extendedTicket != null)
                {
                    XboxUserContext = extendedTicket;

                    HaloClient = new HaloInfiniteClient(haloToken.Token, extendedTicket.DisplayClaims.Xui[0].XUID, userAgent: $"{Configuration.PackageName}/{Configuration.Version}-{Configuration.BuildId}");

                    PlayerClearance? clearance = null;

                    if (SettingsViewModel.Instance.Settings.UseObanClearance)
                    {
                        clearance = (await SafeAPICall(async () => await HaloClient.SettingsActiveFlight(SettingsViewModel.Instance.Settings.Sandbox, SettingsViewModel.Instance.Settings.Build, SettingsViewModel.Instance.Settings.Release)))?.Result;
                    }
                    else
                    {
                        clearance = (await SafeAPICall(async () => await HaloClient.SettingsActiveClearance(SettingsViewModel.Instance.Settings.Release)))?.Result;
                    }

                    if (clearance != null && !string.IsNullOrWhiteSpace(clearance.FlightConfigurationId))
                    {
                        HaloClient.ClearanceToken = clearance.FlightConfigurationId;
                        LogEngine.Log($"Your clearance is {clearance.FlightConfigurationId} and it's set in the client.");
                        return true;
                    }
                    else
                    {
                        LogEngine.Log("Could not obtain the clearance.", LogSeverity.Error);
                        return false;
                    }
                }
                else
                {
                    LogEngine.Log("Extended ticket is null. Cannot authenticate.", LogSeverity.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Error initializing Halo client: {ex.Message}", LogSeverity.Error);
                return false;
            }
        }

        internal static async Task<bool> PopulateCareerData()
        {
            try
            {
                var xuid = XboxUserContext.DisplayClaims.Xui[0].XUID;

                // Fetch career data asynchronously
                var careerRankTask = SafeAPICall(() => HaloClient.EconomyGetPlayerCareerRank([$"xuid({xuid})"], "careerRank1"));
                var rankCollectionTask = SafeAPICall(() => HaloClient.GameCmsGetCareerRanks("careerRank1"));

                // Await both tasks concurrently
                await Task.WhenAll(careerRankTask, rankCollectionTask);

                // Extract results from tasks
                var careerTrackResult = careerRankTask.Result;
                var careerTrackContainerResult = rankCollectionTask.Result;

                // Process career track result
                if (careerTrackResult != null && careerTrackResult.Response.Code == 200)
                {
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() => HomeViewModel.Instance.CareerSnapshot = careerTrackResult.Result);
                }

                // Process career track container result
                if (careerTrackContainerResult != null && (careerTrackContainerResult.Response.Code == 200 || careerTrackContainerResult.Response.Code == 304))
                {
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(async () =>
                    {
                        HomeViewModel.Instance.MaxRank = careerTrackContainerResult.Result.Ranks.Count;

                        if (HomeViewModel.Instance.CareerSnapshot != null)
                        {
                            var currentRank = HomeViewModel.Instance.CareerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank + 1;
                            var currentCareerStage = careerTrackContainerResult.Result.Ranks.FirstOrDefault(c => c.Rank == currentRank);

                            if (currentCareerStage != null)
                            {
                                HomeViewModel.Instance.Title = $"{currentCareerStage.TierType} {currentCareerStage.RankTitle.Value} {currentCareerStage.RankTier.Value}";
                                HomeViewModel.Instance.CurrentRankExperience = careerTrackResult.Result.RewardTracks[0].Result.CurrentProgress.PartialProgress;
                                HomeViewModel.Instance.RequiredRankExperience = currentCareerStage.XpRequiredForRank;

                                HomeViewModel.Instance.ExperienceTotalRequired = careerTrackContainerResult.Result.Ranks.Sum(item => item.XpRequiredForRank);

                                var relevantRanks = careerTrackContainerResult.Result.Ranks.TakeWhile(c => c.Rank < currentRank);
                                HomeViewModel.Instance.ExperienceEarnedToDate = relevantRanks.Sum(rank => rank.XpRequiredForRank) + careerTrackResult.Result.RewardTracks[0].Result.CurrentProgress.PartialProgress;

                                // Handle known bug in the Halo Infinite CMS for rank images
                                if (currentCareerStage.RankLargeIcon == "career_rank/CelebrationMoment/219_Cadet_Onyx_III.png")
                                {
                                    currentCareerStage.RankLargeIcon = "career_rank/CelebrationMoment/19_Cadet_Onyx_III.png";
                                }

                                string qualifiedRankImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", currentCareerStage.RankLargeIcon);
                                string qualifiedAdornmentImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", currentCareerStage.RankAdornmentIcon);

                                EnsureDirectoryExists(qualifiedRankImagePath);
                                EnsureDirectoryExists(qualifiedAdornmentImagePath);

                                await DownloadAndSetImage(currentCareerStage.RankLargeIcon, qualifiedRankImagePath, () => HomeViewModel.Instance.RankImage = qualifiedRankImagePath);
                                await DownloadAndSetImage(currentCareerStage.RankAdornmentIcon, qualifiedAdornmentImagePath, () => HomeViewModel.Instance.AdornmentImage = qualifiedAdornmentImagePath);
                            }
                        }
                        else
                        {
                            LogEngine.Log("Could not get the career snapshot - it's null in the view model.", LogSeverity.Error);
                        }
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred: {ex.Message}", LogSeverity.Error);
                return false;
            }
        }

        internal static void EnsureDirectoryExists(string path)
        {
            var file = new FileInfo(path);
            file.Directory.Create();
        }

        private static async Task DownloadAndSetImage(string serviceImagePath, string localImagePath, Action setImageAction = null, bool isOnWaypoint = false)
        {
            try
            {
                // Check if local image file exists
                if (System.IO.File.Exists(localImagePath))
                {
                    if (setImageAction != null)
                    {
                        await DispatcherWindow.DispatcherQueue.EnqueueAsync(setImageAction);
                    }
                    return;
                }

                HaloApiResultContainer<byte[], RawResponseContainer> image = null;

                Func<Task<HaloApiResultContainer<byte[], RawResponseContainer>>> apiCall = isOnWaypoint ?
                    async () => await HaloClient.GameCmsGetGenericWaypointFile(serviceImagePath) :
                    async () => await HaloClient.GameCmsGetImage(serviceImagePath);

                image = await SafeAPICall(apiCall);

                // Check if the image retrieval was successful
                if (image != null && image.Result != null && image.Response.Code == 200)
                {
                    // In case the folder does not exist, make sure we create it.
                    FileInfo file = new(localImagePath);
                    file.Directory.Create();

                    await System.IO.File.WriteAllBytesAsync(localImagePath, image.Result);

                    if (setImageAction != null)
                    {
                        await DispatcherWindow.DispatcherQueue.EnqueueAsync(setImageAction);
                    }
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Failed to download and set image '{serviceImagePath}' to '{localImagePath}'. Error: {ex.Message}", LogSeverity.Error);
            }
        }

        internal static async Task<bool> PopulateServiceRecordData()
        {
            await ServiceRecordCancellationTracker.CancelAsync();
            ServiceRecordCancellationTracker = new CancellationTokenSource();

            try
            {
                // Get initial service record details
                var serviceRecordResult = await SafeAPICall(() =>
                    HaloClient.StatsGetPlayerServiceRecord($"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})", LifecycleMode.Matchmade));

                if (serviceRecordResult != null && serviceRecordResult.Response.Code == 200)
                {
                    // Update UI with service record details
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        HomeViewModel.Instance.ServiceRecord = serviceRecordResult.Result;
                        RankedViewModel.Instance.RankedLoadingState = MetadataLoadingState.Loading;
                        RankedViewModel.Instance.Playlists.Clear();
                    });

                    // Insert service record entry into the database
                    DataHandler.InsertServiceRecordEntry(serviceRecordResult.Response.Message);

                    // Process ranked playlists asynchronously on the thread pool
                    _ = Task.Run(async () =>
                    {
                        await ProcessRankedPlaylists(serviceRecordResult.Result, ServiceRecordCancellationTracker.Token);
                    }, ServiceRecordCancellationTracker.Token);

                    return true;
                }
                else
                {
                    LogEngine.Log($"Failed to retrieve service record for xuid({XboxUserContext.DisplayClaims.Xui[0].XUID}). Response code: {serviceRecordResult?.Response.Code}", LogSeverity.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"An error occurred while populating service record data: {ex.Message}", LogSeverity.Error);
                return false;
            }
        }

        private static async Task ProcessRankedPlaylists(PlayerServiceRecord serviceRecord, CancellationToken token)
        {
            try
            {
                foreach (var playlist in serviceRecord.Subqueries.PlaylistAssetIds)
                {
                    token.ThrowIfCancellationRequested();

                    var playlistConfigurationResult = await SafeAPICall(() =>
                        HaloClient.GameCmsGetMultiplayerPlaylistConfiguration($"{playlist}.json"));

                    if (playlistConfigurationResult?.Result?.HasCsr == true)
                    {
                        var playlistCsr = await SafeAPICall(() =>
                            HaloClient.SkillGetPlaylistCsr(playlist.ToString(), new List<string> { $"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})" }));

                        if (playlistCsr?.Result?.Value?.Count > 0)
                        {
                            DataHandler.InsertPlaylistCSRSnapshot(playlist.ToString(), playlistConfigurationResult.Result.UgcPlaylistVersion.ToString(), playlistCsr.Response.Message);

                            var playlistMetadata = await SafeAPICall(() =>
                                HaloClient.HIUGCDiscoveryGetPlaylist(playlist.ToString(), playlistConfigurationResult.Result.UgcPlaylistVersion.ToString(), HaloClient.ClearanceToken));

                            if (playlistMetadata?.Result != null && !token.IsCancellationRequested)
                            {
                                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                                {
                                    if (!RankedViewModel.Instance.Playlists.Any(x => x.Id == playlist))
                                    {
                                        RankedViewModel.Instance.Playlists.Add(new PlaylistCSRSnapshot
                                        {
                                            Name = playlistMetadata.Result.PublicName,
                                            Id = playlist,
                                            Version = playlistConfigurationResult.Result.UgcPlaylistVersion,
                                            Snapshot = playlistCsr.Result.Value[0] // Assuming we only get data for one player
                                        });
                                    }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Error processing ranked playlists: {ex.Message}", LogSeverity.Error);
            }

            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
            {
                RankedViewModel.Instance.RankedLoadingState = MetadataLoadingState.Completed;
            });
        }

        internal static async Task<bool> PopulateDecorationData()
        {
            try
            {
                string backgroundPath = SettingsViewModel.Instance.Settings.HeaderImagePath;
                string cachedImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", backgroundPath);

                await DownloadAndSetImage(backgroundPath, cachedImagePath);

                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    HomeViewModel.Instance.SeasonalBackground = cachedImagePath;
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
                    nameplate = emblemMapping.Result.GetValueOrDefault(emblem.Result.CommonData.Id)?.GetValueOrDefault(customizationResult.Result.Appearance.Emblem.ConfigurationId.ToString(CultureInfo.InvariantCulture))
                                   ?? new EmblemMapping() { EmblemCmsPath = emblem.Result.CommonData.DisplayPath.Media.MediaUrl.Path, NameplateCmsPath = string.Empty, TextColor = "#FFF" };
                }

                if (nameplate != null)
                {
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        HomeViewModel.Instance.IDBadgeTextColor = nameplate.TextColor;
                    });

                    string qualifiedNameplateImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", nameplate.NameplateCmsPath);
                    string qualifiedEmblemImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", nameplate.EmblemCmsPath);
                    string qualifiedBackdropImagePath = backdrop.Result != null ? Path.Combine(Configuration.AppDataDirectory, "imagecache", backdrop.Result.ImagePath.Media.MediaUrl.Path) : string.Empty;

                    FileInfo file = new(qualifiedNameplateImagePath); file.Directory.Create();
                    file = new(qualifiedEmblemImagePath); file.Directory.Create();
                    file = new(qualifiedBackdropImagePath); file.Directory.Create();

                    // The nameplate image is downloaded from the Waypoint APIs.
                    await DownloadAndSetImage(nameplate.NameplateCmsPath, qualifiedNameplateImagePath, () => HomeViewModel.Instance.Nameplate = qualifiedNameplateImagePath, true);
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
                LogEngine.Log($"Could not populate customization data. {ex.Message}", LogSeverity.Error);
                return false;
            }
        }

        internal static async Task<bool> PopulateMatchRecordsData()
        {
            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
            {
                MatchesViewModel.Instance.MatchLoadingState = MetadataLoadingState.Calculating;
            });

            await MatchLoadingCancellationTracker.CancelAsync();
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
                                MatchesViewModel.Instance.MatchList = [];
                            });

                            return result;
                        }
                        else
                        {
                            LogEngine.Log("No matches to update.");
                        }
                    }
                    else
                    {
                        LogEngine.Log("No matches found locally, so need to re-hydrate the database.");

                        var result = await UpdateMatchRecords(distinctMatchIds, MatchLoadingCancellationTracker.Token);

                        await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                        {
                            MatchesViewModel.Instance.MatchList = [];
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

                LogEngine.Log($"Error processing matches. {ex.Message}", LogSeverity.Error);
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

                ParallelOptions parallelOptions = new()
                {
                    MaxDegreeOfParallelism = 4
                };

                await Parallel.ForEachAsync(matchIds, parallelOptions, async (matchId, token) =>
                {
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        cancellationToken.ThrowIfCancellationRequested();

                        double completionProgress = matchCounter++ / (double)matchesTotal * 100.0;

                        await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                        {
                            MatchesViewModel.Instance.MatchLoadingParameter = $"{matchId} ({matchCounter} out of {matchesTotal} - {completionProgress:#.00}%)";
                        });

                        var matchStatsAvailability = DataHandler.GetMatchStatsAvailability(matchId.ToString());

                        if (!matchStatsAvailability.MatchAvailable)
                        {
                            LogEngine.Log($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Getting match stats for {matchId}...");

                            var matchStats = await GetMatchStats(matchId.ToString(), completionProgress);
                            if (matchStats == null)
                                return;

                            var processedMatchAssetParameters = await DataHandler.UpdateMatchAssetRecords(matchStats.Result);

                            bool matchStatsInsertionResult = DataHandler.InsertMatchStats(matchStats.Response.Message);
                            LogEngine.Log(matchStatsInsertionResult
                                ? $"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Stored match data for {matchId} in the database."
                                : $"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Could not store match {matchId} stats in the database.");
                        }
                        else
                        {
                            LogEngine.Log($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Match {matchId} already available. Not requesting new data.");
                        }

                        if (!matchStatsAvailability.StatsAvailable)
                        {
                            LogEngine.Log($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Attempting to get player results for players for match {matchId}.");

                            var playerStatsSnapshot = await GetPlayerStats(matchId.ToString());
                            if (playerStatsSnapshot == null)
                                return;

                            var playerStatsInsertionResult = DataHandler.InsertPlayerMatchStats(matchId.ToString(), playerStatsSnapshot.Response.Message);

                            LogEngine.Log(playerStatsInsertionResult
                                ? $"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Stored player stats for {matchId}."
                                : $"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Could not store player stats for {matchId}.");
                        }
                        else
                        {
                            LogEngine.Log($"[{completionProgress:#.00}%] [{matchCounter}/{matchesTotal}] Match {matchId} player stats already available. Not requesting new data.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Because processing is parallelized, we don't quite want to error our right away and
                        // stop processing other matches, so instead we will log an exception locally for investigation.
                        LogEngine.Log($"Error storing {matchId} at {matchCounter}. {ex.Message}", LogSeverity.Error);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Error storing matches. {ex.Message}", LogSeverity.Error);
                return false;
            }
        }

        private static async Task<HaloApiResultContainer<MatchStats, RawResponseContainer>> GetMatchStats(string matchId, double completionProgress)
        {
            int retryCount = 0;
            const int maxRetries = 3;
            HaloApiResultContainer<MatchStats, RawResponseContainer> matchStats = null;

            while (retryCount < maxRetries)
            {
                matchStats = await SafeAPICall(async () => await HaloClient.StatsGetMatchStats(matchId));
                if (matchStats != null && matchStats.Result != null)
                {
                    return matchStats;
                }

                retryCount++;
                if (retryCount < maxRetries)
                {
                    LogEngine.Log($"[{completionProgress:#.00}%] [Warning] Getting match stats from the Halo Infinite API failed for {matchId}. Retrying... ({retryCount}/{maxRetries})", LogSeverity.Warning);
                    await Task.Delay(1000); // Optional: delay between retries
                }
            }

            LogEngine.Log($"[{completionProgress:#.00}%] [Error] Getting match stats from the Halo Infinite API failed for {matchId} after {maxRetries} attempts.", LogSeverity.Error);
            return null;
        }

        private static async Task<HaloApiResultContainer<MatchSkillInfo, RawResponseContainer>> GetPlayerStats(string matchId)
        {
            var matchStats = await HaloClient.StatsGetMatchStats(matchId);
            if (matchStats == null || matchStats.Result == null || matchStats.Result.Players == null)
            {
                LogEngine.Log($"[Error] Could not obtain player stats from the Halo Infinite API for match {matchId} because the match metadata was unavailable.", LogSeverity.Error);
                return null;
            }

            // Anything that starts with "bid" is a bot and including that in the request for player stats will result in failure.
            var targetPlayers = matchStats.Result.Players.Select(p => p.PlayerId).Where(p => !p.StartsWith("bid")).ToList();

            var playerStatsSnapshot = await SafeAPICall(async () => await HaloClient.SkillGetMatchPlayerResult(matchId, targetPlayers!));
            if (playerStatsSnapshot == null || playerStatsSnapshot.Result == null || playerStatsSnapshot.Result.Value == null)
            {
                LogEngine.Log($"Could not obtain player stats from the Halo Infinite API for match {matchId}. Requested {targetPlayers.Count} XUIDs.", LogSeverity.Error);
                return null;
            }

            return playerStatsSnapshot;
        }

        private static async Task<List<Guid>> GetPlayerMatchIds(string xuid, CancellationToken cancellationToken)
        {
            List<Guid> matchIds = [];
            int queryStart = 0;

            var tasks = new ConcurrentBag<Task<List<Guid>>>();

            int fullyMatchedBatches = 0;
            int matchThreshold = 4;

            // If EnableLooseMatchSearch is enabled, we need to also check that the
            // threshold for successful matches is not hit.
            while (true && (SettingsViewModel.Instance.EnableLooseMatchSearch ? fullyMatchedBatches < matchThreshold : true))
            {
                cancellationToken.ThrowIfCancellationRequested();

                tasks.Add(GetMatchBatchAsync(xuid, queryStart, MatchesPerPage));

                queryStart += MatchesPerPage;

                if (tasks.Count == 4)
                {
                    var completedTasks = await Task.WhenAll(tasks);
                    tasks.Clear();

                    // Flatten the batches and add to the overall match list
                    foreach (var batch in completedTasks)
                    {
                        matchIds.AddRange(batch);

                        if (SettingsViewModel.Instance.EnableLooseMatchSearch)
                        {
                            var matchingMatchesInDb = DataHandler.GetExistingMatchCount(batch);
                            if (matchingMatchesInDb == batch.Count)
                            {
                                fullyMatchedBatches++;
                            }
                        }
                    }

                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        MatchesViewModel.Instance.MatchLoadingParameter = matchIds.Count.ToString(CultureInfo.InvariantCulture);
                    });

                    if (completedTasks.LastOrDefault()?.Count == 0)
                    {
                        // No more matches to fetch, break out of the loop
                        break;
                    }
                }
            }

            if ((bool)SettingsViewModel.Instance.EnableLogging)
            {
                LogEngine.Log($"Ended indexing at {matchIds.Count} total matchmade games.");
            }
            return matchIds;
        }

        /// <summary>
        /// Gets the matches asynchronously from the Halo Infinite API.
        /// </summary>
        /// <param name="xuid"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private static async Task<List<Guid>> GetMatchBatchAsync(string xuid, int start, int count)
        {
            List<Guid> successfulMatches = [];
            List<(string xuid, int start, int count)> retryQueue = [];

            var matches = await SafeAPICall(async () => await HaloClient.StatsGetMatchHistory($"xuid({xuid})", start, count, Den.Dev.Orion.Models.HaloInfinite.MatchType.All));

            if (matches.Response.Code == 200)
            {
                successfulMatches.AddRange(matches?.Result?.Results?.Select(item => item.MatchId) ?? Enumerable.Empty<Guid>());
            }
            else
            {
                if ((bool)SettingsViewModel.Instance.EnableLogging)
                {
                    LogEngine.Log($"Error getting match stats through the search endpoint. Adding to retry queue. XUID: {xuid}, START: {start}, COUNT: {count}. Response code: {matches.Response.Code}. Response message: {matches.Response.Message}", LogSeverity.Error);
                }
                retryQueue.Add((xuid, start, count));
            }

            // Process retry queue after processing successful requests
            foreach (var retryRequest in retryQueue)
            {
                await ProcessRetry(retryRequest, successfulMatches);
            }

            return successfulMatches;
        }

        private static async Task ProcessRetry((string xuid, int start, int count) retryRequest, List<Guid> successfulMatches)
        {
            var retryAttempts = 0;
            HaloApiResultContainer<MatchHistoryResponse, RawResponseContainer> retryMatches;

            do
            {
                retryMatches = await SafeAPICall(async () => await HaloClient.StatsGetMatchHistory($"xuid({retryRequest.xuid})", retryRequest.start, retryRequest.count, Den.Dev.Orion.Models.HaloInfinite.MatchType.All));

                if (retryMatches.Response.Code == 200)
                {
                    successfulMatches.AddRange(retryMatches?.Result?.Results?.Select(item => item.MatchId) ?? Enumerable.Empty<Guid>());
                    break; // Break the loop if successful
                }
                else
                {
                    // Log the failure again or handle it appropriately
                    if ((bool)SettingsViewModel.Instance.EnableLogging)
                    {
                        LogEngine.Log($"Error getting match stats through the search endpoint. Retry index: {retryAttempts}. XUID: {retryRequest.xuid}, START: {retryRequest.start}, COUNT: {retryRequest.count}. Response code: {retryMatches.Response.Code}. Response message: {retryMatches.Response.Message}", LogSeverity.Error);
                    }
                    retryAttempts++;
                }
            } while (retryAttempts < 3); // Retry up to 3 times

            if (retryAttempts == 3)
            {
                // Log or handle the failure after 3 attempts
                LogEngine.Log($"Failed to retrieve matches after 3 attempts. XUID: {retryRequest.xuid}, START: {retryRequest.start}, COUNT: {retryRequest.count}", LogSeverity.Error);
            }
        }

        internal static async Task<bool> ReAcquireTokens()
        {
            var authResult = await InitializePublicClientApplication();
            if (authResult != null)
            {
                var result = await InitializeHaloClient(authResult);

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
                    date = MatchesViewModel.Instance.MatchList.Min(a => a.EndTime).ToString("o", CultureInfo.InvariantCulture);
                    matches = DataHandler.GetMatches($"xuid({HomeViewModel.Instance.Xuid})", date, 10);
                }

                if (matches != null)
                {
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        MatchesViewModel.Instance.MatchList.AddRange(matches);
                    });
                }
                else
                {
                    LogEngine.Log("Could not get the list of matches for the specified parameters.", LogSeverity.Error);
                }
            }
        }

        internal static async Task<bool> PopulateSeasonCalendar()
        {
            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
            {
                SeasonCalendarViewModel.Instance.CalendarLoadingState = MetadataLoadingState.Loading;
                SeasonCalendarViewModel.Instance.SeasonDays = [];
            });

            // First, we handle the CSR calendar.
            var csrCalendar = await SafeAPICall(async () => await HaloClient.GameCmsGetCSRCalendar());
            
            // Next, we try to obtain the data for the regular calendar.

            // Using this as a reference point for extra rituals and excluded events.
            var settings = SettingsManager.LoadSettings();

            // First, we get the raw season calendar to get the list of all available events that
            // were registered in the Halo Infinite API.
            var seasonCalendar = await GetSeasonCalendar();
            settings.ExtraRitualEvents?.ForEach(extraRitualEvent =>
            {
                seasonCalendar?.Events.Add(new SeasonCalendarEntry { RewardTrackPath = extraRitualEvent });
            });

            // Once the season calendar is obtained, we want to capture the metadata for every
            // single season entry. Events can be populated directly as part of the battle pass query
            // down the line.
            Dictionary<string, SeasonRewardTrack>? seasonRewardTracks = await GetSeasonRewardTrackMetadata(seasonCalendar);

            // Then, we get the operations that are available for a given player. This is slightly
            // different than the data in the season calendar, so we need both. If no player operations are
            // returned, we can abort.
            var operations = await GetOperations();

            if (operations == null) return false;

            if (settings.ExcludedOperations != null)
            {
                operations.OperationRewardTracks.RemoveAll(operation =>
                    settings.ExcludedOperations.Contains(operation.RewardTrackPath));
            }

            if (csrCalendar == null || csrCalendar.Result == null)
            {
                await HandleCalendarLoadingStateCompleted();
                return false;
            }

            foreach (var season in csrCalendar.Result.Seasons)
            {
                var days = GenerateDateList(season.StartDate.ISO8601Date, season.EndDate.ISO8601Date);
                foreach (var day in days)
                {
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        SeasonCalendarViewDayItem calendarItem = new()
                        {
                            DateTime = day,
                            CSRSeasonText = season.CsrSeasonFilePath.Replace(".json", string.Empty),
                            CSRSeasonMarkerColor = ColorConverter.FromHex(Configuration.SeasonColors[csrCalendar.Result.Seasons.IndexOf(season)])
                        };

                        SeasonCalendarViewModel.Instance.SeasonDays.Add(calendarItem);
                    });
                }
            }

            async Task HandleCalendarLoadingStateCompleted()
            {
                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    SeasonCalendarViewModel.Instance.CalendarLoadingState = MetadataLoadingState.Completed;
                });
            }

            // Complete the parsing of individual seasons
            for (int i = 0; i < seasonRewardTracks.Count; i++)
            {
                // Date ranges for season reward tracks are not structured, so we will need to extract them separately.
                var rewardTrack = seasonRewardTracks.ElementAt(i);

                string? targetBackgroundPath = rewardTrack.Value?.CardBackgroundImage ??
                                               rewardTrack.Value?.Logo ??
                                               rewardTrack.Value?.SummaryBackgroundPath;

                if (!string.IsNullOrEmpty(targetBackgroundPath))
                {
                    if (Path.IsPathRooted(targetBackgroundPath))
                    {
                        targetBackgroundPath = targetBackgroundPath.TrimStart(Path.DirectorySeparatorChar);
                        targetBackgroundPath = targetBackgroundPath.TrimStart(Path.AltDirectorySeparatorChar);
                    }

                    string qualifiedBackgroundImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", targetBackgroundPath);
                    await DownloadAndSetImage(targetBackgroundPath, qualifiedBackgroundImagePath);
                }

                await ProcessRegularSeasonRanges(rewardTrack.Value.DateRange.Value, rewardTrack.Value.Name.Value, i, targetBackgroundPath);
            }

            if (operations != null)
            {
                foreach (var operation in operations.OperationRewardTracks)
                {
                    var compoundOperation = new OperationCompoundModel { RewardTrack = operation, SeasonRewardTrack = seasonRewardTracks.GetValueOrDefault(operation.RewardTrackPath) };

                    var isRewardTrackAvailable = DataHandler.IsOperationRewardTrackAvailable(operation.RewardTrackPath);

                    if (isRewardTrackAvailable)
                    {
                        compoundOperation.RewardTrackMetadata = DataHandler.GetOperationResponseBody(operation.RewardTrackPath);
                        LogEngine.Log($"{operation.RewardTrackPath} (Local) - calendar prep completed");
                    }
                    else
                    {
                        var apiResult = await SafeAPICall(async () => await HaloClient.GameCmsGetEvent(operation.RewardTrackPath, HaloClient.ClearanceToken));
                        if (apiResult?.Result != null)
                            DataHandler.UpdateOperationRewardTracks(apiResult.Response.Message, operation.RewardTrackPath);

                        compoundOperation.RewardTrackMetadata = apiResult.Result;
                        LogEngine.Log($"{operation.RewardTrackPath} - calendar prep completed");
                    }

                    string? targetBackgroundPath = compoundOperation.RewardTrackMetadata?.SummaryImagePath ??
                                                   compoundOperation.RewardTrackMetadata?.BackgroundImagePath ??
                                                   compoundOperation.SeasonRewardTrack?.Logo;

                    if (!string.IsNullOrEmpty(targetBackgroundPath))
                    {
                        if (Path.IsPathRooted(targetBackgroundPath))
                        {
                            targetBackgroundPath = targetBackgroundPath.TrimStart(Path.DirectorySeparatorChar);
                            targetBackgroundPath = targetBackgroundPath.TrimStart(Path.AltDirectorySeparatorChar);
                        }

                        string qualifiedBackgroundImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", targetBackgroundPath);
                        await DownloadAndSetImage(targetBackgroundPath, qualifiedBackgroundImagePath);
                    }

                    await ProcessRegularSeasonRanges(compoundOperation.RewardTrackMetadata.DateRange.Value,
                                                     compoundOperation.RewardTrackMetadata.Name.Value,
                                                     operations.OperationRewardTracks.IndexOf(operation),
                                                     targetBackgroundPath);
                }
            }

            // Extract distinct events based on RewardTrackPath
            var distinctEvents = seasonCalendar.Events
                .Select(x => x.RewardTrackPath)
                .Distinct()
                .ToList();

            foreach (var rewardTrackPath in distinctEvents)
            {
                var compoundEvent = new OperationCompoundModel
                {
                    RewardTrack = new RewardTrack { RewardTrackPath = rewardTrackPath }
                };

                var isRewardTrackAvailable = DataHandler.IsOperationRewardTrackAvailable(rewardTrackPath);

                if (isRewardTrackAvailable)
                {
                    compoundEvent.RewardTrackMetadata = DataHandler.GetOperationResponseBody(rewardTrackPath);
                    var rewardTrack = await GetRewardTrackMetadata("event", compoundEvent.RewardTrackMetadata.TrackId);

                    LogEngine.Log($"{rewardTrackPath} (Local) - calendar prep completed");
                }
                else
                {
                    var apiResult = await SafeAPICall(async () => await HaloClient.GameCmsGetEvent(rewardTrackPath, HaloClient.ClearanceToken));

                    if (apiResult?.Result != null)
                    {
                        DataHandler.UpdateOperationRewardTracks(apiResult.Response.Message, rewardTrackPath);
                    }

                    compoundEvent.RewardTrackMetadata = apiResult?.Result;

                    LogEngine.Log($"{rewardTrackPath} - calendar prep completed");
                }

                // If there is a background image, let's make sure that we attempt to download it.
                string? targetBackgroundPath = compoundEvent?.RewardTrackMetadata?.SummaryImagePath ??
                                               compoundEvent?.RewardTrackMetadata?.BackgroundImagePath ??
                                               compoundEvent?.SeasonRewardTrack?.Logo;

                if (!string.IsNullOrEmpty(targetBackgroundPath))
                {
                    // Normalize the path by trimming separators
                    targetBackgroundPath = targetBackgroundPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    // Construct the qualified path for image caching
                    string qualifiedBackgroundImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", targetBackgroundPath);

                    await DownloadAndSetImage(targetBackgroundPath, qualifiedBackgroundImagePath);
                }

                // Process regular season ranges
                await ProcessRegularSeasonRanges(compoundEvent.RewardTrackMetadata.DateRange.Value,
                                                 compoundEvent.RewardTrackMetadata.Name.Value,
                                                 distinctEvents.IndexOf(rewardTrackPath),
                                                 targetBackgroundPath);
            }

            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
            {
                SeasonCalendarViewModel.Instance.CalendarLoadingState = MetadataLoadingState.Completed;
            });

            return true;
        }

        private async static Task ProcessRegularSeasonRanges(string rangeText, string name, int index, string backgroundPath = "")
        {
            try
            {
                List<Tuple<DateTime, DateTime>> ranges = DateRangeParser.ExtractDateRanges(rangeText);
                foreach (var range in ranges)
                {
                    var days = GenerateDateList(range.Item1, range.Item2);
                    if (days != null)
                    {
                        foreach (var day in days)
                        {
                            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                            {
                                var targetDay = SeasonCalendarViewModel.Instance.SeasonDays
                                    .Where(x => x.DateTime.Date == day.Date)
                                    .FirstOrDefault();

                                if (targetDay != null)
                                {
                                    targetDay.RegularSeasonText = name;
                                    targetDay.RegularSeasonMarkerColor = ColorConverter.FromHex(Configuration.SeasonColors[index]);
                                    targetDay.BackgroundImagePath = backgroundPath;
                                }
                                else
                                {
                                    SeasonCalendarViewDayItem calendarItem = new();
                                    calendarItem.DateTime = day;
                                    calendarItem.CSRSeasonText = string.Empty;
                                    calendarItem.CSRSeasonMarkerColor = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.White);
                                    calendarItem.RegularSeasonText = name;
                                    calendarItem.RegularSeasonMarkerColor = ColorConverter.FromHex(Configuration.SeasonColors[index]);
                                    calendarItem.BackgroundImagePath = backgroundPath;
                                    SeasonCalendarViewModel.Instance.SeasonDays.Add(calendarItem);
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Could not process regular season ranges. {ex.Message}", LogSeverity.Error);
            }
        }

        static List<DateTime> GenerateDateList(DateTime? lowerDate, DateTime? upperDate)
        {
            List<DateTime> dateList = [];

            // Iterate through the dates and add them to the list
            for (DateTime date = (DateTime)lowerDate; date <= upperDate; date = date.AddDays(1))
            {
                dateList.Add(date);
            }

            return dateList;
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
                    date = MedalMatchesViewModel.Instance.MatchList.Min(a => a.EndTime).ToString("o", CultureInfo.InvariantCulture);
                    matches = DataHandler.GetMatchesWithMedal($"xuid({HomeViewModel.Instance.Xuid})", medalNameId, date, 10);
                }

                if (matches != null)
                {
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        MedalMatchesViewModel.Instance.MatchList.AddRange(matches);
                    });
                }
                else
                {
                    LogEngine.Log("Could not get the list of matches for the specified parameters.", LogSeverity.Error);
                }
            }
        }

        internal static List<Medal>? EnrichMedalMetadata(List<Medal> medals, [CallerMemberName] string caller = null)
        {
            try
            {
                LogEngine.Log($"Enriching medal metadata on behalf of {caller}...");

                if (MedalMetadata == null || MedalMetadata.Medals == null || MedalMetadata.Medals.Count == 0)
                    return null;

                var richMedals = medals
                    .Where(medal => MedalMetadata.Medals.Any(metaMedal => metaMedal.NameId == medal.NameId))
                    .Select(medal =>
                    {
                        var metaMedal = MedalMetadata.Medals.First(c => c.NameId == medal.NameId);
                        medal.Name = metaMedal.Name;
                        medal.Description = metaMedal.Description;
                        medal.DifficultyIndex = metaMedal.DifficultyIndex;
                        medal.SpriteIndex = metaMedal.SpriteIndex;
                        medal.TypeIndex = metaMedal.TypeIndex;
                        return medal;
                    })
                    .OrderByDescending(medal => medal.Count)
                    .ToList();

                return richMedals.Count > 0 ? richMedals : null;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Could not enrich medal metadata. Error: {ex.Message}", LogSeverity.Error);

                return null;
            }
        }

        internal static async Task<bool> PopulateMedalData()
        {
            try
            {
                // Log start of operation
                LogEngine.Log("Getting medal metadata...");

                // Retrieve medal metadata asynchronously
                MedalMetadata = await PrepopulateMedalMetadata();

                // Check if metadata or medals list is null or empty
                if (MedalMetadata == null || MedalMetadata.Medals == null || MedalMetadata.Medals.Count == 0)
                {
                    LogEngine.Log("Medal metadata or medals list is empty.", LogSeverity.Warning);
                    return false;
                }

                // Retrieve locally stored medals
                var medals = DataHandler.GetMedals();
                if (medals == null || medals.Count == 0)
                {
                    LogEngine.Log("Locally stored medals not found.", LogSeverity.Warning);

                    if (HomeViewModel.Instance.ServiceRecord != null && HomeViewModel.Instance.ServiceRecord.CoreStats != null
                        && HomeViewModel.Instance.ServiceRecord.CoreStats.Medals != null && HomeViewModel.Instance.ServiceRecord.CoreStats.Medals.Count > 0)
                    {
                        medals = HomeViewModel.Instance.ServiceRecord.CoreStats.Medals;
                        LogEngine.Log("Instead of using medals from the database, using medals from the local service record.", LogSeverity.Info);
                    }
                    else
                    {
                        LogEngine.Log("Re-acquiring service record to get medal data.", LogSeverity.Info);
                        var serviceRecordResult = await PopulateServiceRecordData();
                        if (serviceRecordResult)
                        {
                            if (HomeViewModel.Instance.ServiceRecord != null && HomeViewModel.Instance.ServiceRecord.CoreStats != null && HomeViewModel.Instance.ServiceRecord.CoreStats.Medals != null && HomeViewModel.Instance.ServiceRecord.CoreStats.Medals.Count > 0)
                            {
                                medals = HomeViewModel.Instance.ServiceRecord.CoreStats.Medals;
                                LogEngine.Log("Instead of using medals from the database, using medals from the local service record after re-acquiring.", LogSeverity.Info);
                            }
                            else
                            {
                                LogEngine.Log("Medals could not be populated because the service record contents are empty.", LogSeverity.Warning);
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                // Join locally stored medals with metadata to create compound medals
                var compoundMedals = medals.Join(
                    MedalMetadata.Medals,
                    earned => earned.NameId,
                    references => references.NameId,
                    (earned, references) => new Medal
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
                    })
                    .ToList();

                // Group compound medals by TypeIndex and order by Count
                var groupedMedals = compoundMedals
                    .OrderByDescending(x => x.Count)
                    .GroupBy(x => x.TypeIndex)
                    .ToList();

                // Update MedalsViewModel on UI thread
                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    MedalsViewModel.Instance.Medals = new ObservableCollection<IGrouping<int, Medal>>(groupedMedals);
                });

                // Ensure directory for medal images exists
                string qualifiedMedalPath = Path.Combine(Configuration.AppDataDirectory, "imagecache", "medals");

                // Retrieve sprite content for medals
                var spriteRequestResult = await SafeAPICall(async () =>
                    await HaloClient.GameCmsGetGenericWaypointFile(MedalMetadata.Sprites.ExtraLarge.Path));

                var spriteContent = spriteRequestResult?.Result;
                if (spriteContent != null)
                {
                    // Decode sprite content into SKBitmap
                    using (MemoryStream ms = new(spriteContent))
                    {
                        SkiaSharp.SKBitmap bmp = SkiaSharp.SKBitmap.Decode(ms);
                        using var pixmap = bmp.PeekPixels();
                        // Download and save medal images
                        foreach (var medal in MedalMetadata.Medals)
                        {
                            string medalImagePath = Path.Combine(qualifiedMedalPath, $"{medal.NameId}.png");
                            EnsureDirectoryExists(medalImagePath);

                            // Skip writing if file already exists
                            if (!System.IO.File.Exists(medalImagePath))
                            {
                                // Calculate position and size of medal sprite
                                var row = (int)Math.Floor(medal.SpriteIndex / 16.0);
                                var column = (int)(medal.SpriteIndex % 16.0);
                                SkiaSharp.SKRectI rectI = SkiaSharp.SKRectI.Create(column * 256, row * 256, 256, 256);

                                // Extract subset of pixmap and encode as PNG
                                var subset = pixmap.ExtractSubset(rectI);
                                using (var data = subset.Encode(SkiaSharp.SKPngEncoderOptions.Default))
                                {
                                    await System.IO.File.WriteAllBytesAsync(medalImagePath, data.ToArray());
                                }

                                // Log successful write
                                LogEngine.Log($"Wrote medal to file: {medalImagePath}");
                            }
                        }
                    }

                    // Log completion of medal retrieval
                    LogEngine.Log("Got medals.");
                }
            }
            catch (Exception ex)
            {
                // Log error if any exception occurs
                LogEngine.Log($"Could not obtain medal metadata. Error: {ex.Message}", LogSeverity.Error);
                return false;
            }

            // Return true indicating successful operation
            return true;
        }

        public static async Task<RewardTrack?> GetRewardTrackMetadata(string eventType, string trackId)
        {
            return (await SafeAPICall(async () =>
            {
                return await HaloClient.EconomyGetRewardTrack($"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})", eventType, $"{trackId}");
            })).Result;
        }

        public static async Task<OperationRewardTrackSnapshot?> GetOperations()
        {
            return (await SafeAPICall(async () =>
            {
                return await HaloClient.EconomyPlayerOperations($"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})", HaloClient.ClearanceToken);
            })).Result;
        }

        public static async Task<SeasonCalendar?> GetSeasonCalendar()
        {
            return (await SafeAPICall(async () =>
            {
                return await HaloClient.GameCmsGetSeasonCalendar();
            })).Result;
        }

        public static async Task<CurrencyDefinition?> GetInGameCurrency(string currencyId)
        {
            return (await SafeAPICall(async () =>
            {
                return await HaloClient.GameCmsGetCurrency(currencyId, HaloClient.ClearanceToken);
            })).Result;
        }

        public static async Task<bool> PopulateBattlePassData()
        {
            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() => BattlePassViewModel.Instance.BattlePassLoadingState = MetadataLoadingState.Loading);

            await BattlePassLoadingCancellationTracker.CancelAsync();
            BattlePassLoadingCancellationTracker = new CancellationTokenSource();

            // Using this as a reference point for extra rituals and excluded events.
            var settings = SettingsManager.LoadSettings();

            // First, we get the raw season calendar to get the list of all available events that
            // were registered in the Halo Infinite API.
            var seasonCalendar = await GetSeasonCalendar();
            settings.ExtraRitualEvents?.ForEach(extraRitualEvent =>
            {
                seasonCalendar?.Events.Add(new SeasonCalendarEntry { RewardTrackPath = extraRitualEvent });
            });

            // Once the season calendar is obtained, we want to capture the metadata for every
            // single season entry. Events can be populated directly as part of the battle pass query
            // down the line.
            Dictionary<string, SeasonRewardTrack>? seasonRewardTracks = await GetSeasonRewardTrackMetadata(seasonCalendar);

            // Then, we get the operations that are available for a given player. This is slightly
            // different than the data in the season calendar, so we need both. If no player operations are
            // returned, we can abort.
            var operations = await GetOperations();
            if (operations == null) return false;

            if (settings.ExcludedOperations != null)
            {
                operations.OperationRewardTracks.RemoveAll(operation =>
                    settings.ExcludedOperations.Contains(operation.RewardTrackPath));
            }

            // Let's get the data for each of the operations.
            foreach (var operation in operations.OperationRewardTracks)
            {
                BattlePassLoadingCancellationTracker.Token.ThrowIfCancellationRequested();
                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() => BattlePassViewModel.Instance.BattlePassLoadingParameter = operation.RewardTrackPath);

                var compoundOperation = await ProcessOperation(operation, seasonRewardTracks);
                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() => BattlePassViewModel.Instance.BattlePasses.Add(compoundOperation));
            }

            // Remember that in earlier versions of events they were chunked up - you had to
            // play the same event over many weeks (e.g., they would enable you to play 10 levels per week).
            // For the purposes of this experience, we can just select the distinct events based on reward
            // paths (they are are the same, regardless of which which it happ
            foreach (var eventEntry in seasonCalendar.Events.DistinctBy(x => x.RewardTrackPath))
            {
                // Tell the user that the operations are currently being loaded by changing the
                // loading parameter to the reward track path.
                BattlePassLoadingCancellationTracker.Token.ThrowIfCancellationRequested();
                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() => BattlePassViewModel.Instance.BattlePassLoadingParameter = eventEntry.RewardTrackPath);

                OperationCompoundModel compoundEvent = new()
                {
                    RewardTrack = new RewardTrack() { RewardTrackPath = eventEntry.RewardTrackPath }
                };

                var isRewardTrackAvailable = DataHandler.IsOperationRewardTrackAvailable(eventEntry.RewardTrackPath);

                if (isRewardTrackAvailable)
                {
                    var eventDetails = DataHandler.GetOperationResponseBody(eventEntry.RewardTrackPath);
                    if (eventDetails != null)
                    {
                        compoundEvent.RewardTrackMetadata = eventDetails;
                    }

                    // We want to get the current progress for the evnet.
                    var rewardTrack = await GetRewardTrackMetadata("event", compoundEvent.RewardTrackMetadata.TrackId);

                    // For events, there is no "Current Progress" indicator the same way we have it for operations, so
                    // we're using a dummy value of -1.
                    compoundEvent.Rewards = new(await GetFlattenedRewards(eventDetails.Ranks, (rewardTrack != null ? rewardTrack.CurrentProgress.Rank : -1)));
                    LogEngine.Log($"{eventEntry.RewardTrackPath} (Local) - Completed");
                }
                else
                {
                    var apiResult = await SafeAPICall(async () => await HaloClient.GameCmsGetEvent(eventEntry.RewardTrackPath, HaloClient.ClearanceToken));
                    if (apiResult?.Result != null)
                    {
                        DataHandler.UpdateOperationRewardTracks(apiResult.Response.Message, eventEntry.RewardTrackPath);
                    }
                    compoundEvent.RewardTrackMetadata = apiResult.Result;
                    compoundEvent.Rewards = new(await GetFlattenedRewards(apiResult.Result.Ranks, -1));
                    LogEngine.Log($"{eventEntry.RewardTrackPath} - Completed");
                }

                // Let's make sure that we also download the image for the event, if available.
                if (!string.IsNullOrWhiteSpace(compoundEvent.RewardTrackMetadata.SummaryImagePath))
                {
                    // Some images, like in the example of Noble Intentions event, do not end with an extension. This is not
                    // at all a common occurrence, so I am just making sure that I check it ahead of time in this one special
                    // instance.
                    if (!compoundEvent.RewardTrackMetadata.SummaryImagePath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase)
                        && !compoundEvent.RewardTrackMetadata.SummaryImagePath.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase)
                        && !compoundEvent.RewardTrackMetadata.SummaryImagePath.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase))
                    {
                        compoundEvent.RewardTrackMetadata.SummaryImagePath += ".png";
                    }

                    await DownloadAndSetImage(compoundEvent.RewardTrackMetadata.SummaryImagePath, Path.Combine(Configuration.AppDataDirectory, "imagecache", compoundEvent.RewardTrackMetadata.SummaryImagePath)).ConfigureAwait(false);
                }

                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() => BattlePassViewModel.Instance.Events.Add(compoundEvent));
            }

            return true;
        }

        private static async Task<OperationCompoundModel> ProcessOperation(RewardTrack operation, Dictionary<string, SeasonRewardTrack>? seasonRewardTracks)
        {
            var compoundOperation = new OperationCompoundModel { RewardTrack = operation, SeasonRewardTrack = seasonRewardTracks?.GetValueOrDefault(operation.RewardTrackPath) };

            var isRewardTrackAvailable = DataHandler.IsOperationRewardTrackAvailable(operation.RewardTrackPath);

            if (isRewardTrackAvailable)
            {
                var operationDetails = DataHandler.GetOperationResponseBody(operation.RewardTrackPath);
                if (operationDetails != null)
                {
                    compoundOperation.RewardTrackMetadata = operationDetails;
                }
                compoundOperation.Rewards = new (await GetFlattenedRewards(operationDetails?.Ranks, operation.CurrentProgress.Rank));
                LogEngine.Log($"{operation.RewardTrackPath} (Local) - Completed");
            }
            else
            {
                var apiResult = await SafeAPICall(async () =>
                    await HaloClient.GameCmsGetEvent(operation.RewardTrackPath, HaloClient.ClearanceToken));
                if (apiResult?.Result != null)
                {
                    DataHandler.UpdateOperationRewardTracks(apiResult.Response.Message, operation.RewardTrackPath);
                }
                compoundOperation.RewardTrackMetadata = apiResult?.Result;
                compoundOperation.Rewards =new(await GetFlattenedRewards(apiResult?.Result?.Ranks, operation.CurrentProgress.Rank));
                LogEngine.Log($"{operation.RewardTrackPath} - Completed");
            }

            return compoundOperation;
        }

        private static async Task<Dictionary<string, SeasonRewardTrack>?> GetSeasonRewardTrackMetadata(SeasonCalendar? seasonCalendar)
        {
            if (seasonCalendar == null || seasonCalendar.Seasons == null || seasonCalendar.Seasons.Count == 0)
                return null;

            var seasonRewardTracks = new Dictionary<string, SeasonRewardTrack>();

            var downloadTasks = new List<Task>();

            foreach (var season in seasonCalendar.Seasons)
            {
                if (string.IsNullOrWhiteSpace(season.SeasonMetadata) || string.IsNullOrWhiteSpace(season.OperationTrackPath))
                    continue;

                var result = await SafeAPICall(() => HaloClient.GameCmsGetSeasonRewardTrack(season.SeasonMetadata, HaloClient.ClearanceToken));

                if (result?.Result != null)
                {
                    seasonRewardTracks.Add(season.OperationTrackPath, result.Result);

                    // Queue up image download tasks
                    downloadTasks.Add(Task.Run(async () =>
                    {
                        await DownloadAndSetImage(result.Result.SummaryBackgroundPath, Path.Combine(Configuration.AppDataDirectory, "imagecache", result.Result.SummaryBackgroundPath)).ConfigureAwait(false);
                        await DownloadAndSetImage(result.Result.BattlePassSeasonUpsellBackgroundImage, Path.Combine(Configuration.AppDataDirectory, "imagecache", result.Result.BattlePassSeasonUpsellBackgroundImage)).ConfigureAwait(false);
                        await DownloadAndSetImage(result.Result.ChallengesBackgroundPath, Path.Combine(Configuration.AppDataDirectory, "imagecache", result.Result.ChallengesBackgroundPath)).ConfigureAwait(false);
                        await DownloadAndSetImage(result.Result.BattlePassLogoImage, Path.Combine(Configuration.AppDataDirectory, "imagecache", result.Result.BattlePassLogoImage)).ConfigureAwait(false);
                        await DownloadAndSetImage(result.Result.SeasonLogoImage, Path.Combine(Configuration.AppDataDirectory, "imagecache", result.Result.SeasonLogoImage)).ConfigureAwait(false);
                        await DownloadAndSetImage(result.Result.RitualLogoImage, Path.Combine(Configuration.AppDataDirectory, "imagecache", result.Result.RitualLogoImage)).ConfigureAwait(false);
                        await DownloadAndSetImage(result.Result.StorefrontBackgroundImage, Path.Combine(Configuration.AppDataDirectory, "imagecache", result.Result.StorefrontBackgroundImage)).ConfigureAwait(false);
                        await DownloadAndSetImage(result.Result.CardBackgroundImage, Path.Combine(Configuration.AppDataDirectory, "imagecache", result.Result.CardBackgroundImage)).ConfigureAwait(false);
                        await DownloadAndSetImage(result.Result.ProgressionBackgroundImage, Path.Combine(Configuration.AppDataDirectory, "imagecache", result.Result.ProgressionBackgroundImage)).ConfigureAwait(false);
                    }));
                }
            }

            // Wait for all image download tasks to complete
            await Task.WhenAll(downloadTasks).ConfigureAwait(false);

            return seasonRewardTracks.Count > 0 ? seasonRewardTracks : null;
        }

        internal static async Task<bool> PopulateUserInventory()
        {
            var result = await SafeAPICall(async () =>
            {
                return await HaloClient.EconomyGetInventoryItems($"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})");
            });

            if (result != null && result.Result != null && result.Response.Code == 200)
            {
                var insertionResult = await DataHandler.InsertOwnedInventoryItems(result.Result);
                return insertionResult;
            }
            return false;
        }

        internal static async Task<IEnumerable<IGrouping<int, ItemMetadataContainer>>> GetFlattenedRewards(List<RankSnapshot> rankSnapshots, int currentPlayerRank)
        {
            List<ItemMetadataContainer> rewards = [];

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

                LogEngine.Log($"Rank {rewardBucket.Rank} - Completed");
            }

            return rewards.GroupBy(x => x.Ranks.Item1);
        }

        internal static async Task<List<ItemMetadataContainer>> ExtractCurrencyRewards(int rank, int playerRank, IEnumerable<CurrencyAmount> currencyItems, bool isFree)
        {
            List<ItemMetadataContainer> rewardContainers = [];

            foreach (var currencyReward in currencyItems)
            {
                ItemMetadataContainer container = new()
                {
                    Ranks = new Tuple<int, int>(rank, playerRank),
                    IsFree = isFree,
                    ItemValue = currencyReward.Amount,
                };

                if (!CurrencyDefinitions.TryGetValue(currencyReward.CurrencyPath, out var currencyDetails))
                {
                    currencyDetails = await GetInGameCurrency(currencyReward.CurrencyPath);
                    CurrencyDefinitions.Add(currencyReward.CurrencyPath, currencyDetails);
                }

                container.CurrencyDetails = currencyDetails;

                if (container.CurrencyDetails != null)
                {
                    switch (container.CurrencyDetails.Id.ToLower(CultureInfo.InvariantCulture))
                    {
                        case "rerollcurrency":
                            container.Type = ItemClass.ChallengeReroll;
                            break;
                        case "xpgrant":
                            container.Type = ItemClass.XPGrant;
                            break;
                        case "xb":
                            container.Type = ItemClass.XPBoost;
                            break;
                        case "cr":
                            container.Type = ItemClass.Credits;
                            break;
                        case "softcurrency":
                            container.Type = ItemClass.SpartanPoints;
                            break;
                    }

                    string currencyImageLocation = GetCurrencyImageLocation(container.Type);
                    container.ImagePath = currencyImageLocation;

                    await DownloadAndSetImage(container.ImagePath, Path.Combine(Configuration.AppDataDirectory, "imagecache", currencyImageLocation));
                }

                rewardContainers.Add(container);
            }

            return rewardContainers;
        }

        private static string GetCurrencyImageLocation(ItemClass type)
        {
            return type switch
            {
                ItemClass.ChallengeReroll => "progression/Currencies/1104-000-data-pad-e39bef84-2x2.png", // Challenge swap
                ItemClass.XPGrant => "progression/Currencies/1102-000-xp-grant-c77c6396-2x2.png", // XP grant
                ItemClass.XPBoost => "progression/Currencies/1103-000-xp-boost-5e92621a-2x2.png", // XP boost
                ItemClass.Credits => "progression/Currencies/Credit_Coin-SM.png", // Credit coins
                ItemClass.SpartanPoints => "progression/StoreContent/ToggleTiles/SpartanPoints_Common_2x2.png", // Spartan points
                _ => string.Empty,
            };
        }

        private static async Task WriteImageToFileAsync(string path, byte[] imageData)
        {
            await System.IO.File.WriteAllBytesAsync(path, imageData);
            LogEngine.Log("Stored local image: " + path);
        }

        internal static async Task<List<ItemMetadataContainer>> ExtractInventoryRewards(int rank, int playerRank, IEnumerable<InventoryAmount> inventoryItems, bool isFree)
        {
            List<Task<ItemMetadataContainer>> processingTasks = [];

            foreach (var inventoryReward in inventoryItems)
            {
                processingTasks.Add(ProcessInventoryItem(rank, playerRank, isFree, inventoryReward));
            }

            ItemMetadataContainer[] containers = await Task.WhenAll(processingTasks).ConfigureAwait(false);

            return [.. containers];
        }

        private static async Task<ItemMetadataContainer> ProcessInventoryItem(int rank, int playerRank, bool isFree, InventoryAmount inventoryReward)
        {
            try
            {
                bool inventoryItemLocallyAvailable = DataHandler.IsInventoryItemAvailable(inventoryReward.InventoryItemPath);

                var container = new ItemMetadataContainer
                {
                    Ranks = Tuple.Create(rank, playerRank),
                    IsFree = isFree,
                    ItemValue = inventoryReward.Amount,
                    Type = ItemClass.StandardReward,
                };

                if (inventoryItemLocallyAvailable)
                {
                    container.ItemDetails = DataHandler.GetInventoryItem(inventoryReward.InventoryItemPath);

                    if (container.ItemDetails != null)
                    {
                        LogEngine.Log($"Trying to get local image for {container.ItemDetails.CommonData.Id} (entity: {inventoryReward.InventoryItemPath})");

                        await DownloadAndSetImage(container.ItemDetails.CommonData.DisplayPath.Media.MediaUrl.Path, Path.Combine(Configuration.AppDataDirectory, "imagecache", container.ItemDetails.CommonData.DisplayPath.Media.MediaUrl.Path)).ConfigureAwait(false);
                    }
                    else
                    {
                        LogEngine.Log("Inventory item is null.", LogSeverity.Error);
                    }
                }
                else
                {
                    var item = await SafeAPICall(async () => await HaloClient.GameCmsGetItem(inventoryReward.InventoryItemPath, HaloClient.ClearanceToken).ConfigureAwait(false)).ConfigureAwait(false);

                    if (item?.Result != null)
                    {
                        LogEngine.Log($"Trying to get local image for {item.Result.CommonData.Id} (entity: {inventoryReward.InventoryItemPath})");

                        await DownloadAndSetImage(item.Result.CommonData.DisplayPath.Media.MediaUrl.Path, Path.Combine(Configuration.AppDataDirectory, "imagecache", item.Result.CommonData.DisplayPath.Media.MediaUrl.Path)).ConfigureAwait(false);

                        DataHandler.UpdateInventoryItems(item.Response.Message, inventoryReward.InventoryItemPath);
                        container.ItemDetails = item.Result;
                    }
                }

                container.ImagePath = container.ItemDetails?.CommonData.DisplayPath.Media.MediaUrl.Path;

                return container;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Could not set container item details for {inventoryReward.InventoryItemPath}. {ex.Message}", LogSeverity.Error);
                return null; // or handle error as needed
            }
        }

        internal static async Task<bool> PopulateCsrImages()
        {
            foreach (var rank in Configuration.HaloInfiniteRanks)
            {
                string qualifiedRankImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", "csr", $"{rank}.png");
                EnsureDirectoryExists(qualifiedRankImagePath);

                if (!System.IO.File.Exists(qualifiedRankImagePath))
                {
                    try
                    {
                        LogEngine.Log($"Downloading image for {rank}...");
                        byte[] imageBytes = await WorkshopHttpClient.GetByteArrayAsync(new Uri($"{Configuration.HaloWaypointCsrImageEndpoint}/{rank}.png"));
                        await System.IO.File.WriteAllBytesAsync(qualifiedRankImagePath, imageBytes);
                    }
                    catch (Exception ex)
                    {
                        LogEngine.Log($"Could not download and store rank image for {rank}. {ex.Message}", LogSeverity.Error);
                    }
                }
            }

            return true;
        }

        internal static async Task<bool> PopulateExchangeData()
        {
            try
            {
                await ExchangeCancellationTracker.CancelAsync();
                ExchangeCancellationTracker = new CancellationTokenSource();

                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    ExchangeViewModel.Instance.ExchangeLoadingState = MetadataLoadingState.Loading;
                });

                var exchangeOfferings = await SafeAPICall(async () =>
                {
                    return await HaloClient.EconomyGetSoftCurrencyStore($"xuid({XboxUserContext.DisplayClaims.Xui[0].XUID})");
                });

                if (exchangeOfferings != null && exchangeOfferings.Result != null)
                {
                    // Only clear out exchange items if the previous call to get them from the store succeeded.
                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        ExchangeViewModel.Instance.ExchangeItems = [];
                    });

                    _ = Task.Run(async () =>
                    {
                        await ProcessExchangeItems(exchangeOfferings.Result, ExchangeCancellationTracker.Token);
                    }, ExchangeCancellationTracker.Token);

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogEngine.Log($"Failed to finish updating The Exchange content. Reason: {ex.Message}");

                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    ExchangeViewModel.Instance.ExchangeLoadingState = MetadataLoadingState.Completed;
                });

                return false;
            }
        }

        private static async Task<bool> ProcessExchangeItems(StoreItem exchangeStoreItem, CancellationToken token)
        {
            foreach (var offering in exchangeStoreItem.Offerings)
            {
                token.ThrowIfCancellationRequested();

                // We're only interested in offerings that have items attached to them.
                // Other items are not relevant, and we can skip them (there are no currency
                // or seasonal offers attached to Exchange items.
                if (offering != null && offering.IncludedItems.Any())
                {
                    // Current Exchange offering can contain more items in one (e.g., logos)
                    // but ultimately maps to just one item.
                    var item = offering.IncludedItems.FirstOrDefault();

                    await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                    {
                        ExchangeViewModel.Instance.ExpirationDate = exchangeStoreItem.StorefrontExpirationDate;
                    });

                    if (item != null)
                    {
                        var itemMetadata = await SafeAPICall(async () =>
                        {
                            return await HaloClient.GameCmsGetItem(item.ItemPath, HaloClient.ClearanceToken);
                        });

                        if (itemMetadata != null)
                        {
                            string folderPath = !string.IsNullOrWhiteSpace(itemMetadata.Result.CommonData.DisplayPath.FolderPath) ? itemMetadata.Result.CommonData.DisplayPath.FolderPath : itemMetadata.Result.CommonData.DisplayPath.Media.FolderPath;
                            string fileName = !string.IsNullOrWhiteSpace(itemMetadata.Result.CommonData.DisplayPath.FileName) ? itemMetadata.Result.CommonData.DisplayPath.FileName : itemMetadata.Result.CommonData.DisplayPath.Media.FileName;

                            var metadataContainer = new ItemMetadataContainer
                            {
                                ItemType = item.ItemType,
                                // There is usually just one price, since it's just one offering. There may be
                                // several included items (e.g., shoulder pads) but the price should still be the
                                // same regardless, at least from the current Exchange implementation.
                                // If for some reason there is no price assigned, we will default to -1.
                                ItemValue = (offering.Prices != null && offering.Prices.Any()) ? offering.Prices[0].Cost : -1,
                                ImagePath = (!string.IsNullOrWhiteSpace(folderPath) && !string.IsNullOrWhiteSpace(fileName)) ? Path.Combine(folderPath, fileName).Replace("\\", "/") : itemMetadata.Result.CommonData.DisplayPath.Media.MediaUrl.Path,
                                ItemDetails = new InGameItem()
                                {
                                    CommonData = itemMetadata.Result.CommonData,
                                },
                            };

                            // There is a chance that the image lookup is going to fail. In that case, we want to
                            // fallback to the "dumb" logic, and that is - get the offering and all the related metadata.
                            if (string.IsNullOrWhiteSpace(metadataContainer.ImagePath))
                            {
                                if (offering.OfferingDisplayPath != null)
                                {
                                    var offeringData = await SafeAPICall(async () => await HaloClient.GameCmsGetStoreOffering(offering.OfferingDisplayPath));
                                    if (offeringData != null && offeringData.Result != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(offeringData.Result.ObjectImagePath))
                                        {
                                            metadataContainer.ImagePath = offeringData.Result.ObjectImagePath.Replace("\\", "/");
                                        }
                                    }
                                }
                            }

                            if (Path.IsPathRooted(metadataContainer.ImagePath))
                            {
                                metadataContainer.ImagePath = metadataContainer.ImagePath.TrimStart(Path.DirectorySeparatorChar);
                                metadataContainer.ImagePath = metadataContainer.ImagePath.TrimStart(Path.AltDirectorySeparatorChar);
                            }

                            string qualifiedItemImagePath = Path.Combine(Configuration.AppDataDirectory, "imagecache", metadataContainer.ImagePath);

                            EnsureDirectoryExists(qualifiedItemImagePath);

                            await DownloadAndSetImage(metadataContainer.ImagePath, qualifiedItemImagePath);

                            if (!token.IsCancellationRequested)
                            {
                                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                                {
                                    if (!ExchangeViewModel.Instance.ExchangeItems.Any(x => x.ItemDetails.CommonData.Id == metadataContainer.ItemDetails.CommonData.Id))
                                    {
                                        ExchangeViewModel.Instance.ExchangeItems.Add(metadataContainer);
                                    }
                                });
                            }

                            LogEngine.Log($"Got item for Exchange listing: {item.ItemPath}");
                        }
                    }
                }
            }

            await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
            {
                ExchangeViewModel.Instance.ExchangeLoadingState = MetadataLoadingState.Completed;
            });

            return true;
        }

        internal static async Task<bool> InitializeAllDataOnLaunch()
        {
            try
            {
                var authResult = await InitializePublicClientApplication();
                if (authResult == null)
                    throw new Exception("Authentication with Halo services failed.");

                var haloClientInitialized = await InitializeHaloClient(authResult);
                if (!haloClientInitialized)
                    throw new Exception("Could not initialize Halo client.");

                // Update UI state
                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    SplashScreenViewModel.Instance.IsBlocking = false;
                });

                // Set HomeViewModel properties
                HomeViewModel.Instance.Gamertag = XboxUserContext.DisplayClaims.Xui[0].Gamertag;
                HomeViewModel.Instance.Xuid = XboxUserContext.DisplayClaims.Xui[0].XUID;

                // Bootstrap database and set journaling mode
                var databaseBootstrapResult = DataHandler.BootstrapDatabase();
                var journalingMode = DataHandler.SetWALJournalingMode();

                if (journalingMode.Equals("wal", StringComparison.Ordinal))
                {
                    LogEngine.Log("Successfully set WAL journaling mode.");
                }
                else
                {
                    LogEngine.Log("Could not set WAL journaling mode.", LogSeverity.Warning);
                }

                // Reset collections
                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    BattlePassViewModel.Instance.BattlePasses = BattlePassViewModel.Instance.BattlePasses ?? [];
                    MatchesViewModel.Instance.MatchList = MatchesViewModel.Instance.MatchList ?? [];
                    MedalsViewModel.Instance.Medals = MedalsViewModel.Instance.Medals ?? [];
                });

                // Concurrently populate MatchRecordsData and BattlePassData with other tasks
                Parallel.Invoke(
                    async () => await PopulateMatchRecordsData().ContinueWith(async t =>
                    {
                        if (await t)
                        {
                            LogEngine.Log("Successfully populated the match data from within the app bootstrap sequence.");
                        }
                        else if (t.IsFaulted)
                        {
                            LogEngine.Log("Could not populate the match data from within the app bootstrap sequence.");
                        }

                        // Right now, regardless of result I want to make sure that we reset
                        // the completion state.
                        await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                        {
                            MatchesViewModel.Instance.MatchLoadingState = MetadataLoadingState.Completed;
                            MatchesViewModel.Instance.MatchLoadingParameter = string.Empty;
                        });
                    }, TaskScheduler.Current),
                    async () => await PopulateBattlePassData().ContinueWith(async t =>
                    {
                        if (await t)
                        {
                            LogEngine.Log("Successfully populated the battle pass data from within the app bootstrap sequence.");
                        }
                        else if (t.IsFaulted)
                        {
                            LogEngine.Log("Could not populate the battle pass data from within the app bootstrap sequence.");
                        }

                        await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                        {
                            BattlePassViewModel.Instance.BattlePassLoadingState = MetadataLoadingState.Completed;
                        });
                    }, TaskScheduler.Current),
                    async() => await PopulateCareerData(),
                    async () => await PopulateServiceRecordData(),
                    async () => await PopulateMedalData(),
                    async () => await PopulateExchangeData(),
                    async () => await PopulateCsrImages(),
                    async () => await PopulateSeasonCalendar(),
                    async () => await PopulateUserInventory(),
                    async () => await PopulateCustomizationData(),
                    async () => await PopulateDecorationData()
                );

                return true;
            }
            catch (Exception ex)
            {
                // Handle exceptions
                LogEngine.Log($"Initialization failed: {ex.Message}", LogSeverity.Error);

                await DispatcherWindow.DispatcherQueue.EnqueueAsync(() =>
                {
                    SplashScreenViewModel.Instance.IsErrorMessageDisplayed = true;
                });

                return false;
            }
        }
    }
}
