using Den.Dev.Orion.Authentication;
using Den.Dev.Orion.Core;
using Den.Dev.Orion.Models;
using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using OpenSpartan.Data;
using OpenSpartan.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;

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
                    ServiceRecordViewModel.Instance.CareerSnapshot = careerTrackResult.Result;
                }

                var careerTrackContainerResult = await HaloClient.GameCmsGetCareerRanks("careerRank1");

                if (careerTrackContainerResult.Result != null && careerTrackContainerResult.Response.Code == 200)
                {
                    ServiceRecordViewModel.Instance.MaxRank = careerTrackContainerResult.Result.Ranks.Count;

                    // The rank here is incremented by one because of off-by-one counting when ranks are established. The introductory rank apparently is counted differently in the index
                    // compared to the full set of ranks in the reward track.
                    var currentCareerStage = (from c in careerTrackContainerResult.Result.Ranks where c.Rank == ServiceRecordViewModel.Instance.CareerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank + 1 select c).FirstOrDefault();
                    if (currentCareerStage != null)
                    {
                        ServiceRecordViewModel.Instance.Title = currentCareerStage.RankTitle.Value;
                        ServiceRecordViewModel.Instance.CurrentRankExperience = careerTrackResult.Result.RewardTracks[0].Result.CurrentProgress.PartialProgress;
                        ServiceRecordViewModel.Instance.RequiredRankExperience = currentCareerStage.XpRequiredForRank;

                        // Let's also compute secondary values that can tell us how far the user is from the Hero title.
                        ServiceRecordViewModel.Instance.ExperienceTotalRequired = careerTrackContainerResult.Result.Ranks.Sum(item => item.XpRequiredForRank);

                        var relevantRanks = (from c in careerTrackContainerResult.Result.Ranks where c.Rank <= ServiceRecordViewModel.Instance.CareerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank select c);
                        ServiceRecordViewModel.Instance.ExperienceEarnedToDate = relevantRanks.Sum(rank => rank.XpRequiredForRank) + careerTrackResult.Result.RewardTracks[0].Result.CurrentProgress.PartialProgress;

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
                        ServiceRecordViewModel.Instance.RankImage = qualifiedRankImagePath;

                        if (!System.IO.File.Exists(qualifiedAdornmentImagePath))
                        {
                            var adornmentImage = await HaloClient.GameCmsGetImage(currentCareerStage.RankAdornmentIcon);
                            if (adornmentImage.Result != null && adornmentImage.Response.Code == 200)
                            {
                                System.IO.File.WriteAllBytes(qualifiedAdornmentImagePath, adornmentImage.Result);
                            }
                        }
                        ServiceRecordViewModel.Instance.AdornmentImage = qualifiedAdornmentImagePath;
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
                ServiceRecordViewModel.Instance.Gamertag = XboxUserContext.DisplayClaims.Xui[0].Gamertag;
                ServiceRecordViewModel.Instance.Xuid = XboxUserContext.DisplayClaims.Xui[0].XUID;

                // Get initial service record details
                var serviceRecordResult = await HaloClient.StatsGetPlayerServiceRecord(ServiceRecordViewModel.Instance.Gamertag, Den.Dev.Orion.Models.HaloInfinite.LifecycleMode.Matchmade);

                if (serviceRecordResult.Result != null && serviceRecordResult.Response.Code == 200)
                {
                    ServiceRecordViewModel.Instance.ServiceRecord = serviceRecordResult.Result;

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

                ServiceRecordViewModel.Instance.SeasonalBackground = qualifiedBackgroundImagePath;

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
                    ServiceRecordViewModel.Instance.ServiceTag = customizationResult.Result.Appearance.ServiceTag;

                    var emblemMapping = await HaloClient.GameCmsGetEmblemMapping();

                    if (emblemMapping.Result != null && emblemMapping.Response.Code == 200)
                    {
                        var emblem = await HaloClient.GameCmsGetItem(customizationResult.Result.Appearance.Emblem.EmblemPath, HaloClient.ClearanceToken);
                        var backdrop = await HaloClient.GameCmsGetItem(customizationResult.Result.Appearance.BackdropImagePath, HaloClient.ClearanceToken);

                        var nameplate = (from n in emblemMapping.Result where n.Key == emblem.Result.CommonData.Id select n).FirstOrDefault();
                        var configuration = (from c in nameplate.Value where c.Key.ToString() == customizationResult.Result.Appearance.Emblem.ConfigurationId.ToString() select c).FirstOrDefault();

                        if (!configuration.Equals(default))
                        {
                            ServiceRecordViewModel.Instance.IDBadgeTextColor = configuration.Value.TextColor;

                            string qualifiedNameplateImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", configuration.Value.NameplateCmsPath);
                            string qualifiedEmblemImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", configuration.Value.EmblemCmsPath);
                            string qualifiedBackdropImagePath = Path.Combine(Core.Configuration.AppDataDirectory, "imagecache", backdrop.Result.ImagePath.Media.MediaUrl.Path);

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
                            ServiceRecordViewModel.Instance.Nameplate = qualifiedNameplateImagePath;

                            if (!System.IO.File.Exists(qualifiedEmblemImagePath))
                            {
                                var emblemData = await HaloClient.GameCmsGetGenericWaypointFile(configuration.Value.EmblemCmsPath);

                                if (emblemData.Result != null && emblemData.Response.Code == 200)
                                {
                                    System.IO.File.WriteAllBytes(qualifiedEmblemImagePath, emblemData.Result);
                                }
                            }
                            ServiceRecordViewModel.Instance.Emblem = qualifiedEmblemImagePath;

                            if (!System.IO.File.Exists(qualifiedBackdropImagePath))
                            {
                                var backdropData = await HaloClient.GameCmsGetImage(backdrop.Result.ImagePath.Media.MediaUrl.Path);

                                if (backdropData.Result != null && backdropData.Response.Code == 200)
                                {
                                    System.IO.File.WriteAllBytes(qualifiedBackdropImagePath, backdropData.Result);
                                }
                            }
                            ServiceRecordViewModel.Instance.Backdrop = qualifiedBackdropImagePath;
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
    }
}
