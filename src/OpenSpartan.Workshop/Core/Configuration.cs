using System;
using System.IO;

namespace OpenSpartan.Workshop.Core
{
    internal class Configuration
    {
        // Endpoints
        internal const string SettingsEndpoint = "https://wokrshop.api.openspartan.com/clientsettings";
        internal const string HaloWaypointPlayerEndpoint = "https://www.halowaypoint.com/halo-infinite/players";

        // Build-related metadata.
        internal const string Version = "1.0.1";
        internal const string BuildId = "MANTLE-02292024";
        internal const string PackageName = "OpenSpartan.Workshop";

        internal static readonly string[] Scopes = ["Xboxlive.signin", "Xboxlive.offline_access"];
        internal const string ClientID = "1079e683-7752-435a-aa4a-3cfdd700de82";
        internal const string CacheFileName = "authcache.bin";
        internal const string SettingsFileName = "settings.json";
        internal static readonly string AppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PackageName);

        // API-related default metadata.
        internal const string DefaultRelease = "1.6";
        internal const string DefaultAPIVersion = "1";
        internal const string DefaultHeaderImage = "progression/Switcher/Season_Switcher_S6_SOF.png";
    }
}
