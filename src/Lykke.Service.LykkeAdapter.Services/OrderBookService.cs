using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;
using Lykke.Service.LykkeAdapter.Core.Domain.Trading;
using Lykke.Service.LykkeAdapter.Core.Services;

namespace Lykke.Service.LykkeAdapter.Services
{
    public class OrderBookService : IOrderBookService, ILykkeOrderBookHandler
    {
        public const string LykkeName = "lykke";

        private readonly object _gate = new object();
        private Dictionary<string, TradingOrderBook> _data = new Dictionary<string, TradingOrderBook>();

        public OrderBookService()
        {
            _data["EURUSD"] = new TradingOrderBook(LykkeName, "EURUSD", new List<PriceVolume>(), new List<PriceVolume>(), DateTime.UtcNow);
            _data["GBPUSD"] = new TradingOrderBook(LykkeName, "GBPUSD", new List<PriceVolume>(), new List<PriceVolume>(), DateTime.UtcNow);
            _data["USDCHF"] = new TradingOrderBook(LykkeName, "USDCHF", new List<PriceVolume>(), new List<PriceVolume>(), DateTime.UtcNow);
            _data["USDJPY"] = new TradingOrderBook(LykkeName, "USDJPY", new List<PriceVolume>(), new List<PriceVolume>(), DateTime.UtcNow);
        }

        public void ApplyLykkeOrderBook(LykkeOrderBook orderBook)
        {
            lock (_gate)
            {
                var data = new TradingOrderBook(LykkeName, orderBook.AssetPair, new List<PriceVolume>(),
                    new List<PriceVolume>(), orderBook.Timestamp);
                if (_data.TryGetValue(orderBook.AssetPair, out var item))
                {
                    data.Asks = item.Asks;
                    data.Bids = item.Bids;
                }

                if (orderBook.IsBuy)
                {
                    data.Bids = orderBook.Prices ?? new List<PriceVolume>();
                }
                else
                {
                    data.Asks = orderBook.Prices ?? new List<PriceVolume>();
                }

                data.Timestamp = orderBook.Timestamp;

                _data[data.AssetPairId] = data;
            }
        }

        public IReadOnlyList<TradingOrderBook> GetCurrentOrderBooks()
        {
            List<TradingOrderBook> data;
            lock (_gate) data = _data.Values.ToList();
            return data;

        }

        public Task Handle(LykkeOrderBook orderBook)
        {
            ApplyLykkeOrderBook(orderBook);
            return Task.CompletedTask;
        }
    }
}
