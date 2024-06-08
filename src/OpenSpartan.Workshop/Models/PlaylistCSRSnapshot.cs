using Den.Dev.Orion.Models.HaloInfinite;
using System;

namespace OpenSpartan.Workshop.Models
{
    internal class PlaylistCSRSnapshot
    {
        public string Name { get; set; }

        public Guid Id { get; set; }

        public Guid Version { get; set; }

        public PlaylistCsrResults Snapshot { get; set; }
    }
}
