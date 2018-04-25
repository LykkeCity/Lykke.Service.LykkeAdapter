using Newtonsoft.Json;
using System;

namespace Lykke.Service.LykkeAdapter.Core.Domain.Trading
{
    public class TickPrice
    {
        [JsonConstructor]
        public TickPrice(Instrument instrument, DateTime time, decimal ask, decimal bid)
        {
            Asset = instrument.Name;

            Timestamp = time;
            Ask = ask;
            Bid = bid;
        }

        [JsonProperty("source")]
        public readonly string Source = Constants.LykkeExchangeName;

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
