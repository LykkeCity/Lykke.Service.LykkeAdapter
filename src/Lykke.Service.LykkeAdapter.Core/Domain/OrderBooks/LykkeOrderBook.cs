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
    }
}
