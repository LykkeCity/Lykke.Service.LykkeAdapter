using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autofac;
using Common;
using Common.Log;
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
        private readonly TimeSpan _interval;
        private bool _isstarted;
        private readonly Dictionary<string, OrderBookSnapshot> _lastData = new Dictionary<string, OrderBookSnapshot>();
        private readonly Dictionary<string, TickPrice> _lastTicks = new Dictionary<string, TickPrice>();
        private readonly ITickPricePublisher _tickPricePublisher;

        public ProvideDataJob(
            int countPerSecond,
            IOrderBookService orderBookService,
            IOrderBookPublisher bookPublisher,
            ITickPricePublisher tickPricePublisher,
            ILog log)
        {
            _tickPricePublisher = tickPricePublisher;
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
        }

        private void DoTime(object state)
        {
            _timerTrigger.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            if (!_isstarted)
                return;

            try
            {
                var data = _orderBookService.GetCurrentOrderBooks();
                foreach (var orderBook in data)
                {
                    if (orderBook.Bids != null && orderBook.Bids.Any() && orderBook.Asks != null && orderBook.Asks.Any())
                    {
                        var ask = orderBook.Asks.Min(e => e.Price);
                        var bid = orderBook.Bids.Max(e => e.Price);
                        if (ask > bid)
                        {
                            TrySendData(orderBook);
                        }
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

        private void TrySendData(TradingOrderBook orderBook)
        {
            var snapshot = new OrderBookSnapshot(orderBook);

            if (_lastData.TryGetValue(orderBook.AssetPairId, out var last))
            {
                if (last.Equals(snapshot))
                {
                    _lastData[snapshot.Name] = snapshot;
                    return;
                }
            }

            _lastData[snapshot.Name] = snapshot;
            _bookPublisher.Publish(orderBook).GetAwaiter().GetResult();

            if (_lastTicks.TryGetValue(orderBook.AssetPairId, out var lastTick))
            {
                if (lastTick.Ask == snapshot.Ask && lastTick.Bid == snapshot.Bid)
                    return;
            }

            var tick = new TickPrice(snapshot.Name, orderBook.Timestamp, snapshot.Ask, snapshot.Bid);
            _tickPricePublisher.Publish(tick).GetAwaiter().GetResult();
            _lastTicks[tick.Asset] = tick;
        }

        public void Start()
        {
            _isstarted = true;
            _timerTrigger.Change(_interval, Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            _timerTrigger?.Dispose();
        }

        public void Stop()
        {
            _isstarted = false;
            _timerTrigger?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
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
                    if (ask.Price > Bid) Bid = ask.Price;
                }
            }

            public string Name { get; set; }
            public decimal SumSellVolume { get; }
            public decimal SumSellOppossiteVolume { get; }
            public decimal SumBuyVolume { get; }
            public decimal SumBuyOppossiteVolume { get; }
            public decimal Ask { get; set; }
            public decimal Bid { get; set; }

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
