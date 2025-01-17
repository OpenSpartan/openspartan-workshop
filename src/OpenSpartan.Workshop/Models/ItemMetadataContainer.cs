using Den.Dev.Grunt.Models.HaloInfinite;
using System;

namespace OpenSpartan.Workshop.Models
{
    internal sealed class ItemMetadataContainer
    {
        public bool IsFree { get; set; }

        public Tuple<int, int> Ranks { get; set; }  

        public InGameItem ItemDetails { get; set; }

        public CurrencyDefinition CurrencyDetails {  get; set; }

        public string ImagePath { get; set; }

        /// <summary>
        /// Gets or sets the numeric value associated with the currency
        /// or item cost.
        /// </summary>
        public int ItemValue { get; set; }

        public ItemClass Type { get; set; }

        public string ItemType { get; set; }

        public string ItemPath { get; set; }
    }
}
