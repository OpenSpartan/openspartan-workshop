using System.IO;

namespace OpenSpartan.Workshop.Core
{
    internal static class ImageCachePath
    {
        private static readonly string CacheRoot =
            Path.Combine(Configuration.AppDataDirectory, "imagecache");

        // Resolve a server-supplied relative asset path to a local cache path.
        // Strips any leading separators first: on Windows, Path.Combine(base, "/foo")
        // discards `base` entirely and resolves "/foo" to the current drive's root,
        // dropping cached images at C:\foo or D:\foo instead of inside imagecache.
        internal static string For(string serverRelativePath) =>
            Path.Combine(CacheRoot, (serverRelativePath ?? string.Empty).TrimStart('/', '\\'));
    }
}
