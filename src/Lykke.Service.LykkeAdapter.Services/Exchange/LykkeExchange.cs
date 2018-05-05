using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.LykkeAdapter.Core;
using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;
using Lykke.Service.LykkeAdapter.Core.Domain.Trading;
using Lykke.Service.LykkeAdapter.Core.Filters;
using Lykke.Service.LykkeAdapter.Core.Handlers;
using Lykke.Service.LykkeAdapter.Core.Settings;
using Lykke.Service.LykkeAdapter.Core.Throttling;
using Lykke.Service.LykkeAdapter.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Service.LykkeAdapter.Services.Exchange
{
    public class LykkeExchange : ExchangeBase
    {
        private readonly IHandler<TickPrice> _tickPriceHandler;
        private readonly IHandler<LykkeOrderBook> _orderBookHandler;
        private readonly IThrottling _orderBooksThrottler;
        private readonly IThrottling _tickPricesThrottler;
        private new LykkeAdapterSettings Config => (LykkeAdapterSettings)base.Config;
        private CancellationTokenSource ctSource;
        private RabbitMqSubscriber<LykkeOrderBook> sourceFeedOrderbooksRabbit;
        private readonly RepeatingOrdersFilter ordersFilter;

        private readonly Dictionary<string, decimal> _lastBids;
        private readonly Dictionary<string, decimal> _lastAsks;

        public LykkeExchange(LykkeAdapterSettings config, 
            IHandler<TickPrice> tickPriceHandler, 
            IHandler<LykkeOrderBook> orderBookHandler,
            IThrottling orderBooksThrottler,
            IThrottling tickPriceThrottler,
            ILog log)
            : base(config, log)
        {
            _tickPriceHandler = tickPriceHandler;
            _orderBookHandler = orderBookHandler;

            _orderBooksThrottler = orderBooksThrottler;
            _tickPricesThrottler = tickPriceThrottler;

            _lastBids = Instruments.ToDictionary(x => x.Name, x => 0m);
            _lastAsks = Instruments.ToDictionary(x => x.Name, x => 0m);

            ordersFilter = new RepeatingOrdersFilter();
        }

        protected override void StartImpl()
        {
            LykkeLog.WriteInfoAsync(nameof(LykkeExchange), nameof(StartImpl), string.Empty, $"Starting {Constants.LykkeExchangeName} exchange").Wait();

            ctSource = new CancellationTokenSource();

            StartRabbitMqTickPriceSubscription();

            OnConnected();
        }

        private void StartRabbitMqTickPriceSubscription()
        {
            var rabbitSettings = new RabbitMqSubscriptionSettings()
            {
                ConnectionString = Config.RabbitMq.SourceFeed.ConnectionString,
                ExchangeName = Config.RabbitMq.SourceFeed.Exchange,
                QueueName = Config.RabbitMq.SourceFeed.Queue
            };
            var errorStrategy = new DefaultErrorHandlingStrategy(LykkeLog, rabbitSettings);
            sourceFeedOrderbooksRabbit = new RabbitMqSubscriber<LykkeOrderBook>(rabbitSettings, errorStrategy)
                .SetMessageDeserializer(new GenericRabbitModelConverter<LykkeOrderBook>())
                .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                .SetConsole(new LogToConsole())
                .SetLogger(LykkeLog)
                .Subscribe(HandleOrderBook)
                .Start();
        }


        private async Task HandleOrderBook(LykkeOrderBook lykkeOrderBook)
        {
            var instrument = Instruments.FirstOrDefault(x => string.Compare(x.Name, lykkeOrderBook.AssetPair, StringComparison.InvariantCultureIgnoreCase) == 0);

            if (instrument == null && Config.UseSupportedCurrencySymbolsAsFilter == false)
            {
                instrument = new Instrument(lykkeOrderBook.AssetPair);
            }

            if (instrument != null)
            {
                if (lykkeOrderBook.Prices.Any())
                {
                    decimal bestBid = 0;
                    decimal bestAsk = 0;

                    ordersFilter.FilterOutDuplicatedOrders(lykkeOrderBook);

                    if (lykkeOrderBook.IsBuy)
                    {
                        _lastBids[instrument.Name] = bestBid = lykkeOrderBook.Prices.Select(x => x.Price).OrderByDescending(x => x).First();
                        bestAsk = _lastAsks.ContainsKey(instrument.Name) ? _lastAsks[instrument.Name] : 0;
                    }
                    else
                    {
                        _lastAsks[instrument.Name] = bestAsk = lykkeOrderBook.Prices.Select(x => x.Price).OrderBy(x => x).First();
                        bestBid = _lastBids.ContainsKey(instrument.Name) ? _lastBids[instrument.Name] : 0;
                    }

                    if (bestBid > 0 && bestAsk > 0 && bestAsk > bestBid)
                    {
                        if (!_tickPricesThrottler.NeedThrottle(instrument.Name))
                        {
                            var tickPrice = new TickPrice(instrument, lykkeOrderBook.Timestamp, bestAsk, bestBid);
                            await _tickPriceHandler.Handle(tickPrice);
                        }
                    }

                    if (!_orderBooksThrottler.NeedThrottle(instrument.Name))
                    {
                        await _orderBookHandler.Handle(lykkeOrderBook);
                    }
                }
            }
        }

        protected override void StopImpl()
        {
            ctSource?.Cancel();
            OnStopped();
        }
    }
}
