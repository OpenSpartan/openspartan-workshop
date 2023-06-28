using Den.Dev.Orion.Authentication;
using Den.Dev.Orion.Core;
using Den.Dev.Orion.Models;
using Den.Dev.Orion.Util;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using OpenSpartan.Authentication;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace OpenSpartan.Shared
{
    internal static class UserContextManager
    {
        static HaloInfiniteClient HaloClient { get; set; }

        internal static async Task<AuthenticationResult> InitializePublicClientApplication()
        {
            var storageProperties = new StorageCreationPropertiesBuilder(Authentication.Configuration.CacheFileName, Authentication.Configuration.CacheDirectory).Build();

            var pca = PublicClientApplicationBuilder.Create(Authentication.Configuration.ClientID).WithAuthority(AadAuthorityAudience.PersonalMicrosoftAccount).Build();

            // This hooks up the cross-platform cache into MSAL
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(pca.UserTokenCache);

            IAccount accountToLogin = (await pca.GetAccountsAsync()).FirstOrDefault();

            AuthenticationResult authResult = null;

            try
            {
                authResult = await pca.AcquireTokenSilent(Authentication.Configuration.Scopes, accountToLogin)
                                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                authResult = await pca.AcquireTokenInteractive(Authentication.Configuration.Scopes)
                                            .WithAccount(accountToLogin)
                                            .ExecuteAsync();
            }

            return authResult;
        }

        internal static void InitializeHaloClient(AuthenticationResult authResult)
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

            HaloClient = new(haloToken.Token, extendedTicket.DisplayClaims.Xui[0].XUID);
        }
    }
}
