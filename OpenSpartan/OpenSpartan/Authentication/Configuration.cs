using System;
using System.IO;

namespace OpenSpartan.Authentication
{
    internal class Configuration
    {
        internal static readonly string[] Scopes = new string[] { "Xboxlive.signin", "Xboxlive.offline_access" };

        internal static readonly string ClientID = "1079e683-7752-435a-aa4a-3cfdd700de82";

        internal static readonly string CacheFileName = "authcache.bin";

        internal static readonly string CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenSpartan");
    }
}
