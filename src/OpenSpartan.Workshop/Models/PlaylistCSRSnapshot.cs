using Den.Dev.Grunt.Models.HaloInfinite;
using System;

namespace OpenSpartan.Workshop.Models
{
    internal sealed class PlaylistCSRSnapshot
    {
        public string Name { get; set; } = string.Empty;

        public Guid Id { get; set; }

        public Guid Version { get; set; }

        public PlaylistCsrResults? Snapshot { get; set; }
    }
}
