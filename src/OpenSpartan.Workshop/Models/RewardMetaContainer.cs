using Den.Dev.Orion.Models.HaloInfinite;
using System;

namespace OpenSpartan.Workshop.Models
{
    internal sealed class RewardMetaContainer
    {
        public bool IsFree { get; set; }

        public Tuple<int, int> Ranks { get; set; }  

        public InGameItem ItemDetails { get; set; }

        public CurrencyDefinition CurrencyDetails {  get; set; }

        public string ImagePath { get; set; }

        public int Amount { get; set; }
    }
}
