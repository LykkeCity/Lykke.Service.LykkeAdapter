using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autofac;
using Common;
using Common.Log;
using Lykke.Job.OrderBooksCacheProvider.Client;
using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;
using Lykke.Service.LykkeAdapter.Core.Domain.Trading;
using Lykke.Service.LykkeAdapter.Core.Services;

namespace Lykke.Service.LykkeAdapter.Services
{
    public class ProvideDataJob: IStartable, IStopable
    {
        private readonly int _countPerSecond;
        private readonly IOrderBookService _orderBookService;
        private readonly IOrderBookPublisher _bookPublisher;
        private readonly ILog _log;
        private readonly Timer _timerTrigger;
        private readonly Timer _timerStaticstic;
        private readonly TimeSpan _interval;
        private bool _isstarted;
        private readonly Dictionary<string, OrderBookSnapshot> _lastData = new Dictionary<string, OrderBookSnapshot>();
        private readonly Dictionary<string, TickPrice> _lastTicks = new Dictionary<string, TickPrice>();
        private readonly ITickPricePublisher _tickPricePublisher;
        private readonly IOrderBookProviderClient _orderBookProviderClient;

        private int _countSendOrderBook = 0;
        private int _countSendTickPrice = 0;

        public ProvideDataJob(
            int countPerSecond,
            IOrderBookService orderBookService,
            IOrderBookPublisher bookPublisher,
            ITickPricePublisher tickPricePublisher,
            IOrderBookProviderClient orderBookProviderClient,
            ILog log)
        {
            _tickPricePublisher = tickPricePublisher;
            _orderBookProviderClient = orderBookProviderClient;
            _countPerSecond = countPerSecond;
            _orderBookService = orderBookService;
            _bookPublisher = bookPublisher;
            _log = log;

            if (countPerSecond > 0)
            {
                var interval = (int) Math.Round(1000m / countPerSecond, 0);
                _interval = TimeSpan.FromMilliseconds(interval);
            }
            else
            {
                _interval = TimeSpan.FromMilliseconds(100);
            }

            _timerTrigger = new Timer(DoTime, null, Timeout.Infinite, Timeout.Infinite);
            _timerStaticstic = new Timer(WriteStatistic, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private void WriteStatistic(object state)
        {
            _timerStaticstic.Change(Timeout.Infinite, Timeout.Infinite);

            _log.WriteInfoAsync(nameof(ProvideDataJob), nameof(WriteStatistic), $"Lykke Adapter. Sended orderBooks: {_countSendOrderBook}; Sended tickPrice: {_countSendTickPrice}");
            _countSendOrderBook = 0;
            _countSendTickPrice = 0;

            var data = _orderBookService.GetCurrentOrderBooks();
            foreach (var orderBook in data.Where(e => !e.Asks.Any() || !e.Bids.Any()))
            {
                var raws = _orderBookProviderClient.GetOrderBookRawAsync(orderBook.AssetPairId).GetAwaiter().GetResult();
                foreach (var raw in raws)
                {
                    var item = new LykkeOrderBook()
                    {
                        Timestamp = raw.Timestamp,
                        AssetPair = raw.AssetPair,
                        IsBuy = raw.IsBuy,
                        Prices = raw.Prices.Select(e => new PriceVolume((decimal)e.Price, (decimal)e.Volume)).ToList()
                    };
                    _orderBookService.ApplyLykkeOrderBook(item);
                }
            }

            _timerStaticstic.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private readonly Dictionary<string, DateTime> _lastNegativeSpread = new Dictionary<string, DateTime>();
        private DateTime _nextForceData = DateTime.UtcNow.AddMinutes(1);

        private void DoTime(object state)
        {
            _timerTrigger.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            if (!_isstarted)
                return;

            try
            {
                var data = _orderBookService.GetCurrentOrderBooks();
                var force = false;
                if (DateTime.UtcNow >= _nextForceData)
                {
                    force = true;
                    _nextForceData = DateTime.UtcNow.AddMinutes(1);
                }
                foreach (var orderBook in data)
                {
                    //if (orderBook.AssetPairId != "PKTGBP")
                    //    continue;

                    if (orderBook.Bids != null && orderBook.Bids.Any() && orderBook.Asks != null && orderBook.Asks.Any())
                    {
                        var ask = orderBook.Asks.Min(e => e.Price);
                        var bid = orderBook.Bids.Max(e => e.Price);
                        if (ask > bid)
                        {
                            TrySendData(orderBook, force);
                            if (force)
                            {
                                TrySendDataThinned(orderBook);
                            }
                        }
                        else
                        {
                            _lastData.Remove(orderBook.AssetPairId);
                            _lastTicks.Remove(orderBook.AssetPairId);
                            if (_lastNegativeSpread.TryGetValue(orderBook.AssetPairId, out var lastTime))
                            {
                                if ((DateTime.UtcNow - lastTime).TotalSeconds >= 60)
                                {
                                    _log.WriteInfoAsync(nameof(ProvideDataJob), nameof(DoTime), $"orderBook: {orderBook.ToJson()}", "Negative spread detected").GetAwaiter().GetResult();
                                    _lastNegativeSpread[orderBook.AssetPairId] = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                _log.WriteInfoAsync(nameof(ProvideDataJob), nameof(DoTime), $"orderBook: {orderBook.ToJson()}", "Negative spread detected").GetAwaiter().GetResult();
                                _lastNegativeSpread[orderBook.AssetPairId] = DateTime.UtcNow;
                            }
                        }
                    }
                    else
                    {
                        _lastData.Remove(orderBook.AssetPairId);
                        _lastTicks.Remove(orderBook.AssetPairId);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.WriteErrorAsync(nameof(ProvideDataJob), nameof(DoTime), ex);
            }

            if (_isstarted)
                _timerTrigger.Change(_interval, Timeout.InfiniteTimeSpan);
        }

        private void TrySendData(TradingOrderBook orderBook, bool force)
        {
            var snapshot = new OrderBookSnapshot(orderBook);

            if (!force && _lastData.TryGetValue(orderBook.AssetPairId, out var last))
            {
                if (last.Equals(snapshot))
                {
                    _lastData[snapshot.Name] = snapshot;
                    return;
                }
            }

            _lastData[snapshot.Name] = snapshot;
            _bookPublisher.Publish(orderBook).GetAwaiter().GetResult();
            _countSendOrderBook++;

            if (!force && _lastTicks.TryGetValue(orderBook.AssetPairId, out var lastTick))
            {
                if (lastTick.Ask == snapshot.Ask && lastTick.Bid == snapshot.Bid)
                    return;
            }

            var tick = new TickPrice(snapshot.Name, orderBook.Timestamp, snapshot.Ask, snapshot.Bid);
            _tickPricePublisher.Publish(tick).GetAwaiter().GetResult();
            _countSendTickPrice++;
            _lastTicks[tick.Asset] = tick;
        }

        private void TrySendDataThinned(TradingOrderBook orderBook)
        {
            _bookPublisher.PublishThinned(orderBook).GetAwaiter().GetResult();
        }

        public void Start()
        {
            _isstarted = true;
            _timerTrigger.Change(_interval, Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            _timerTrigger?.Dispose();
            _timerStaticstic?.Dispose();
        }

        public void Stop()
        {
            _isstarted = false;
            _timerTrigger?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _timerStaticstic?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public class OrderBookSnapshot
        {
            public OrderBookSnapshot(TradingOrderBook orderBook)
            {
                Name = orderBook.AssetPairId;
                SumBuyVolume = 0;
                SumBuyOppossiteVolume = 0;
                Bid = 0;
                foreach (var bid in orderBook.Bids)
                {
                    SumBuyVolume += bid.Volume;
                    SumBuyOppossiteVolume += bid.Volume * bid.Price;
                    if (bid.Price > Bid) Bid = bid.Price;
                }

                SumSellVolume = 0;
                SumSellOppossiteVolume = 0;
                Ask = 0;
                foreach (var ask in orderBook.Asks)
                {
                    SumSellVolume += ask.Volume;
                    SumSellOppossiteVolume += ask.Volume * ask.Price;
                    if (ask.Price < Ask || Ask <= 0) Ask = ask.Price;
                }
            }

            public string Name { get; set; }
            public decimal SumSellVolume { get; }
            public decimal SumSellOppossiteVolume { get; }
            public decimal SumBuyVolume { get; }
            public decimal SumBuyOppossiteVolume { get; }
            public decimal Ask { get; set; }
            public decimal Bid { get; set; }

            public override string ToString()
            {
                return $"{Name} {SumSellVolume} {SumSellOppossiteVolume} {SumBuyVolume} {SumBuyOppossiteVolume} {Ask} {Bid}";
            }

            protected bool Equals(OrderBookSnapshot other)
            {
                return string.Equals(Name, other.Name) && SumSellVolume == other.SumSellVolume && SumSellOppossiteVolume == other.SumSellOppossiteVolume && SumBuyVolume == other.SumBuyVolume && SumBuyOppossiteVolume == other.SumBuyOppossiteVolume && Ask == other.Ask && Bid == other.Bid;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((OrderBookSnapshot) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Name != null ? Name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ SumSellVolume.GetHashCode();
                    hashCode = (hashCode * 397) ^ SumSellOppossiteVolume.GetHashCode();
                    hashCode = (hashCode * 397) ^ SumBuyVolume.GetHashCode();
                    hashCode = (hashCode * 397) ^ SumBuyOppossiteVolume.GetHashCode();
                    hashCode = (hashCode * 397) ^ Ask.GetHashCode();
                    hashCode = (hashCode * 397) ^ Bid.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}
