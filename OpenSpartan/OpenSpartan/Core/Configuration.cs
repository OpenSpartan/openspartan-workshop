using System;
using System.IO;

namespace OpenSpartan.Workshop.Core
{
    internal class Configuration
    {
        // Endpoints
        internal static readonly string SettingsEndpoint = "https://wokrshop.api.openspartan.com/clientsettings";
        internal static readonly string HaloWaypointPlayerEndpoint = "https://www.halowaypoint.com/halo-infinite/players";

        // Build-related metadata.
        internal static readonly string Version = "1.0.0";
        internal static readonly string BuildId = "AKELUS-02082024";
        internal static readonly string PackageName = "OpenSpartan.Workshop";

        internal static readonly string[] Scopes = new string[] { "Xboxlive.signin", "Xboxlive.offline_access" };
        internal static readonly string ClientID = "1079e683-7752-435a-aa4a-3cfdd700de82";
        internal static readonly string CacheFileName = "authcache.bin";
        internal static readonly string SettingsFileName = "settings.json";
        internal static readonly string AppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PackageName);

        // API-related default metadata.
        internal static readonly string DefaultRelease = "1.6";
        internal static readonly string DefaultAPIVersion = "1";
        internal static readonly string DefaultHeaderImage = "progression/Switcher/Season_Switcher_S6_SOF.png";
    }
}
