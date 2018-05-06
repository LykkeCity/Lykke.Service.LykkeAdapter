using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Job.OrderBooksCacheProvider.Client;

namespace Lykke.Service.LykkeAdapter.Core.Handlers
{
    public class OrderBookHandlerDecorator : IHandler<LykkeOrderBook>
    {
        private const string Name = "lykke";
        private readonly IHandler<TradingOrderBook> _rabbitMqHandler;
        private readonly IOrderBookProviderClient _bookProviderClient;
        private readonly ILog _log;
        private readonly Dictionary<string, LykkeOrderBook> _halfOrderBooks = new Dictionary<string, LykkeOrderBook>();

        public OrderBookHandlerDecorator(IHandler<TradingOrderBook> rabbitMqHandler, IOrderBookProviderClient bookProviderClient, ILog log)
        {
            _rabbitMqHandler = rabbitMqHandler;
            _bookProviderClient = bookProviderClient;
            _log = log;
        }

        public async Task Handle(LykkeOrderBook message)
        {
            var currentKey = message.AssetPair + message.IsBuy;
            var wantedKey = message.AssetPair + !message.IsBuy;

            await CheckInited(message, currentKey, wantedKey);

            _halfOrderBooks[currentKey] = message;
            if (!_halfOrderBooks.TryGetValue(wantedKey, out var otherHalf))
            {
                otherHalf = new LykkeOrderBook()
                {
                    AssetPair = message.AssetPair,
                    IsBuy = !message.IsBuy,
                    Timestamp = DateTime.MinValue,
                    Prices = new List<PriceVolume>()
                };
            }

            var fullOrderBook = CreateOrderBook(message, otherHalf);

            // If bestAsk < bestBid then ignore the order book as outdated
            var isOutdated = fullOrderBook.Asks.Any() && fullOrderBook.Bids.Any() &&
                             fullOrderBook.Asks.Min(x => x.Price) < fullOrderBook.Bids.Max(x => x.Price);

            if (!isOutdated)
            {
                await _rabbitMqHandler.Handle(fullOrderBook);
            }
        }

        private async Task CheckInited(LykkeOrderBook message, string currentKey, string wantedKey)
        {
            try
            {
                if (!_halfOrderBooks.ContainsKey(currentKey) && !_halfOrderBooks.ContainsKey(wantedKey))
                {
                    var data = await _bookProviderClient.GetOrderBookRawAsync(message.AssetPair);
                    var buy = data?.FirstOrDefault(e => e.IsBuy);
                    var sell = data?.FirstOrDefault(e => !e.IsBuy);
                    _halfOrderBooks[message.AssetPair + true] = new LykkeOrderBook()
                    {
                        AssetPair = message.AssetPair,
                        IsBuy = true,
                        Timestamp = buy?.Timestamp ?? DateTime.MinValue,
                        Prices = buy?.Prices.Select(e => new PriceVolume((decimal) e.Price, (decimal) e.Volume))
                                     .ToList() ??
                                 new List<PriceVolume>()
                    };
                    _halfOrderBooks[message.AssetPair + false] = new LykkeOrderBook()
                    {
                        AssetPair = message.AssetPair,
                        IsBuy = false,
                        Timestamp = sell?.Timestamp ?? DateTime.MinValue,
                        Prices = sell?.Prices.Select(e => new PriceVolume((decimal) e.Price, (decimal) e.Volume))
                                     .ToList() ??
                                 new List<PriceVolume>()
                    };
                }
            }
            catch (Exception ex)
            {
                await _log.WriteInfoAsync(nameof(OrderBookHandlerDecorator), nameof(CheckInited), message.AssetPair, ex.ToString());

                _halfOrderBooks[message.AssetPair + true] = new LykkeOrderBook()
                {
                    AssetPair = message.AssetPair,
                    IsBuy = !message.IsBuy,
                    Timestamp = DateTime.MinValue,
                    Prices = new List<PriceVolume>()
                };

                _halfOrderBooks[message.AssetPair + false] = new LykkeOrderBook()
                {
                    AssetPair = message.AssetPair,
                    IsBuy = !message.IsBuy,
                    Timestamp = DateTime.MinValue,
                    Prices = new List<PriceVolume>()
                };
            }
        }

        private TradingOrderBook CreateOrderBook(LykkeOrderBook one, LykkeOrderBook another)
        {
            if (one.AssetPair != another.AssetPair)
                throw new ArgumentException($"{nameof(one)}.{nameof(one.AssetPair)} != {nameof(another)}.{nameof(another.AssetPair)}");

            if (one.IsBuy == another.IsBuy)
                throw new ArgumentException($"{nameof(one)}.{nameof(one.IsBuy)} == {nameof(another)}.{nameof(another.IsBuy)}");

            var assetPair = one.AssetPair;
            var timestamp = one.Timestamp > another.Timestamp ? one.Timestamp : another.Timestamp;

            var onePrices = one.Prices.Select(x => new PriceVolume(x.Price, x.Volume)).ToList();
            var anotherPrices = another.Prices.Select(x => new PriceVolume(x.Price, x.Volume)).ToList();

            var bids = one.IsBuy ? onePrices : anotherPrices;
            var asks = !one.IsBuy ? onePrices : anotherPrices;
            
            var result = new TradingOrderBook(Name, assetPair, asks, bids, timestamp);

            return result;
        }
    }
}
