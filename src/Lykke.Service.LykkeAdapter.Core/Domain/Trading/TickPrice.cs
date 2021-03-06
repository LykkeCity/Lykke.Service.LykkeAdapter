﻿using Newtonsoft.Json;
using System;

namespace Lykke.Service.LykkeAdapter.Core.Domain.Trading
{
    public class TickPrice
    {
        [JsonConstructor]
        public TickPrice(string assetPair, DateTime time, decimal ask, decimal bid)
        {
            Asset = assetPair;

            Timestamp = time;
            Ask = ask;
            Bid = bid;
            Source = Constants.LykkeExchangeName;
        }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("asset")]
        public string Asset { get; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; }

        [JsonProperty("ask")]
        public decimal Ask { get; }

        [JsonProperty("bid")]
        public decimal Bid { get; }

        public override string ToString()
        {
            return $"TickPrice for {Asset}: Time={Timestamp}, Ask={Ask}, Bid={Bid}";
        }
    }
}
