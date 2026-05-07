using System;
using System.Collections.Concurrent;
using System.IO;

namespace OpenSpartan.Workshop.Core
{
    internal static class ImageCachePath
    {
        private static readonly string CacheRoot =
            Path.Combine(Configuration.AppDataDirectory, "imagecache");

        // Cache of paths we've confirmed exist on disk. Positive only — a path
        // that doesn't exist yet might be downloaded later in the session, so
        // cached "miss" results would go stale. The set never shrinks for the
        // process lifetime, which is fine because images are immutable once
        // written.
        private static readonly ConcurrentDictionary<string, bool> _existsCache =
            new(StringComparer.OrdinalIgnoreCase);

        // Resolve a server-supplied relative asset path to a local cache path.
        // Strips any leading separators first: on Windows, Path.Combine(base, "/foo")
        // discards `base` entirely and resolves "/foo" to the current drive's root,
        // dropping cached images at C:\foo or D:\foo instead of inside imagecache.
        internal static string For(string serverRelativePath) =>
            Path.Combine(CacheRoot, (serverRelativePath ?? string.Empty).TrimStart('/', '\\'));

        // Returns the absolute local cache path if the file is present on disk,
        // otherwise null. The disk stat is performed at most once per process per
        // path; subsequent calls hit a static cache. Used by IValueConverters that
        // run on the UI thread per binding evaluation, where File.Exists per scroll
        // is otherwise a measurable hitch on slow storage.
        internal static string? ResolveIfExists(string serverRelativePath)
        {
            if (string.IsNullOrEmpty(serverRelativePath))
            {
                return null;
            }

            var localPath = For(serverRelativePath);

            if (_existsCache.ContainsKey(localPath))
            {
                return localPath;
            }

            if (File.Exists(localPath))
            {
                _existsCache.TryAdd(localPath, true);
                return localPath;
            }

            return null;
        }
    }
}
