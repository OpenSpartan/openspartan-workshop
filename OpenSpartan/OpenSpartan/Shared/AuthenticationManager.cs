using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Diagnostics;
using System.Linq;

namespace OpenSpartan.Shared
{
    internal static class AuthenticationManager
    {
        internal static async void InitializePublicClientApplication()
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

            if (authResult != null)
            {
                // Authentication succeeded.
                Debug.WriteLine("Authenticated.");
            }
        }
    }
}
