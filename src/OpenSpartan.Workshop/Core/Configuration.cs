using System;
using System.IO;

namespace OpenSpartan.Workshop.Core
{
    internal sealed class Configuration
    {
        // Endpoint metadata.
        internal const string SettingsEndpoint = "https://wokrshop.api.openspartan.com/clientsettings";
        internal const string HaloWaypointPlayerEndpoint = "https://www.halowaypoint.com/halo-infinite/players";
        internal const string HaloWaypointCsrImageEndpoint = "https://www.halowaypoint.com/images/halo-infinite/csr/";

        // Build-related metadata.
        internal const string Version = "1.0.4";
        internal const string BuildId = "URDIDACT-03112024";
        internal const string PackageName = "OpenSpartan.Workshop";

        // Authentication and setting-related metadata.
        internal static readonly string[] Scopes = ["Xboxlive.signin", "Xboxlive.offline_access"];
        internal const string ClientID = "1079e683-7752-435a-aa4a-3cfdd700de82";
        internal const string CacheFileName = "authcache.bin";
        internal const string SettingsFileName = "settings.json";
        internal static readonly string AppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PackageName);

        // API-related default metadata.
        internal const string DefaultRelease = "1.7";
        internal const string DefaultAPIVersion = "1";
        internal const string DefaultHeaderImage = "progression/Switcher/Season_Switcher_S7_BH.png";
        internal const string DefaultSandbox = "UNUSED";
        internal const string DefaultBuild = "257116.24.04.18.1334-2";

        // Rank markers used to download the rank images.
        internal static readonly string[] HaloInfiniteRanks =
        [
            "unranked_0",
            "unranked_1",
            "unranked_2",
            "unranked_3",
            "unranked_4",
            "unranked_5",
            "unranked_6",
            "unranked_7",
            "unranked_8",
            "unranked_9",
            "bronze_1",
            "bronze_2",
            "bronze_3",
            "bronze_4",
            "bronze_5",
            "bronze_6",
            "silver_1",
            "silver_2",
            "silver_3",
            "silver_4",
            "silver_5",
            "silver_6",
            "gold_1",
            "gold_2",
            "gold_3",
            "gold_4",
            "gold_5",
            "gold_6",
            "platinum_1",
            "platinum_2",
            "platinum_3",
            "platinum_4",
            "platinum_5",
            "platinum_6",
            "diamond_1",
            "diamond_2",
            "diamond_3",
            "diamond_4",
            "diamond_5",
            "diamond_6",
            "onyx_1",
        ];

        internal static readonly string[] SeasonColors =
        [
            "#08B2E3",
            "#EE6352",
            "#57A773",
            "#AB2346",
            "#D5A021",
            "#550527",
            "#688E26",
            "#F44708",
            "#A10702",
            "#38405F",
            "#FF0035",
            "#820263",
            "#D90368",
            "#170F11",
            "#36C9C6",
            "#D8A7CA",
            "#7E3F8F",
            "#402039",
            "#313715",
            "#390099",
            "#1E555C",
            "#941C2F",
        ];
    }
}
