using Den.Dev.Orion.Models.HaloInfinite;

namespace OpenSpartan.Models
{
    internal class RewardMetaContainer
    {
        public bool IsFree { get; set; }

        public int Rank { get; set; }  

        public InGameItem ItemDetails { get; set; }

        public CurrencyDefinition CurrencyDetails {  get; set; }

        public string ImagePath { get; set; }

        public int Amount { get; set; }
    }
}
