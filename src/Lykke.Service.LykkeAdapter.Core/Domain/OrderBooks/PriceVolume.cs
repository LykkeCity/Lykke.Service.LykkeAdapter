using Newtonsoft.Json;
using System;

namespace Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks
{
    public sealed class PriceVolume
    {
        public PriceVolume(decimal price, decimal volume)
        {
            Price = price;
            Volume = Math.Abs(volume);
        }

        [JsonProperty("price")]
        public decimal Price { get; }

        [JsonProperty("volume")]
        public decimal Volume { get; }

    }
}
