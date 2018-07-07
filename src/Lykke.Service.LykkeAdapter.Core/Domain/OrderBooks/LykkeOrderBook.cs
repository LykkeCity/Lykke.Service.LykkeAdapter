using System;
using System.Collections.Generic;

namespace Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks
{
    public class LykkeOrderBook
    {
        public string AssetPair { get; set; }

        public bool IsBuy { get; set; }

        public DateTime Timestamp { get; set; }

        public List<PriceVolume> Prices { get; set; }

        public override string ToString()
        {
            var str = $"{AssetPair} {IsBuy}";
            foreach (var price in Prices)
            {
                str += $"\n{price.Price}  {price.Volume}";
            }

            return str;
        }
    }
}
