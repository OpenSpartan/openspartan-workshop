using Den.Dev.Orion.Models;
using Den.Dev.Orion.Models.HaloInfinite;
using Microsoft.UI.Xaml;
using OpenSpartan.Shared;
using OpenSpartan.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenSpartan
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

            var authResult = await UserContextManager.InitializePublicClientApplication();
            if (authResult != null)
            {
                var instantiationResult = UserContextManager.InitializeHaloClient(authResult);
                SplashScreenViewModel.Instance.IsBlocking = false;

                if (instantiationResult)
                {
                    ServiceRecordViewModel.Instance.Gamertag = UserContextManager.XboxUserContext.DisplayClaims.Xui[0].Gamertag;
                    ServiceRecordViewModel.Instance.Xuid = UserContextManager.XboxUserContext.DisplayClaims.Xui[0].XUID;
                    HaloApiResultContainer<PlayerServiceRecord, RawResponseContainer> serviceRecordResult = null;
                    HaloApiResultContainer<RewardTrackResultContainer, RawResponseContainer> careerTrackResult = null;
                    HaloApiResultContainer<CareerTrackContainer, RawResponseContainer> careerTrackContainerResult = null;

                    serviceRecordResult = await UserContextManager.HaloClient.StatsGetPlayerServiceRecord(ServiceRecordViewModel.Instance.Gamertag, Den.Dev.Orion.Models.HaloInfinite.LifecycleMode.Matchmade);

                    if (serviceRecordResult != null && serviceRecordResult.Response.Code == 200)
                    {
                        ServiceRecordViewModel.Instance.ServiceRecord = serviceRecordResult.Result;
                    }

                    careerTrackResult = await UserContextManager.HaloClient.EconomyGetPlayerCareerRank(new List<string>() { $"xuid({ServiceRecordViewModel.Instance.Xuid})" }, "careerRank1");

                    if (careerTrackResult != null && careerTrackResult.Response.Code == 200)
                    {
                        ServiceRecordViewModel.Instance.CareerSnapshot = careerTrackResult.Result;
                    }

                    careerTrackContainerResult = await UserContextManager.HaloClient.GameCmsGetCareerRanks("careerRank1");

                    if (careerTrackContainerResult != null && careerTrackContainerResult.Response.Code == 200)
                    {
                        var currentCareerStage = (from c in careerTrackContainerResult.Result.Ranks where c.Rank == ServiceRecordViewModel.Instance.CareerSnapshot.RewardTracks[0].Result.CurrentProgress.Rank select c).FirstOrDefault();
                        if (currentCareerStage != null)
                        {
                            ServiceRecordViewModel.Instance.Title = currentCareerStage.RankTitle.Value;

                            string qualifiedRankImagePath = Path.Combine(Core.Configuration.CacheDirectory, "imagecache", currentCareerStage.RankLargeIcon);
                            string qualifiedAdornmentImagePath = Path.Combine(Core.Configuration.CacheDirectory, "imagecache", currentCareerStage.RankAdornmentIcon);

                            // Let's make sure that we create the directory if it does not exist.
                            System.IO.FileInfo file = new System.IO.FileInfo(qualifiedRankImagePath);
                            file.Directory.Create();

                            file = new System.IO.FileInfo(qualifiedAdornmentImagePath);
                            file.Directory.Create();

                            if (!System.IO.File.Exists(qualifiedRankImagePath))
                            {
                                var rankImage = await UserContextManager.HaloClient.GameCmsGetImage(currentCareerStage.RankLargeIcon);
                                if (rankImage != null && rankImage.Response.Code == 200)
                                {
                                    System.IO.File.WriteAllBytes(qualifiedRankImagePath, rankImage.Result);
                                }
                            }
                            ServiceRecordViewModel.Instance.RankImage = qualifiedRankImagePath;

                            if (!System.IO.File.Exists(qualifiedAdornmentImagePath))
                            {
                                var adornmentImage = await UserContextManager.HaloClient.GameCmsGetImage(currentCareerStage.RankAdornmentIcon);
                                if (adornmentImage != null && adornmentImage.Response.Code == 200)
                                {
                                    System.IO.File.WriteAllBytes(qualifiedAdornmentImagePath, adornmentImage.Result);
                                }
                            }
                            ServiceRecordViewModel.Instance.AdornmentImage = qualifiedAdornmentImagePath;
                        }
                    }
                }
            }
        }        

        private Window m_window;
    }
}
