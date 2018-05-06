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
using Common;

namespace Lykke.Service.LykkeAdapter.Services.Exchange
{
    public class LykkeExchange : ExchangeBase
    {
        private readonly IHandler<TickPrice> _tickPriceHandler;
        private readonly IHandler<LykkeOrderBook> _orderBookHandler;
        private readonly IThrottling _orderBooksThrottler;
        private readonly IThrottling _tickPricesThrottler;
        private readonly ILog _log;
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
            _log = log;

            _lastBids = Instruments.ToDictionary(x => x.Name, x => 0m);
            _lastAsks = Instruments.ToDictionary(x => x.Name, x => 0m);

            ordersFilter = new RepeatingOrdersFilter();


            var settings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = Config.RabbitMq.SourceFeed.ConnectionString,
                QueueName = Config.RabbitMq.SourceFeed.Queue,
                ExchangeName = Config.RabbitMq.SourceFeed.Exchange
            };

            sourceFeedOrderbooksRabbit =
                new RabbitMqSubscriber<LykkeOrderBook>(settings,
                        new ResilientErrorHandlingStrategy(log, settings, TimeSpan.FromSeconds(10),
                            next: new DefaultErrorHandlingStrategy(log, settings)))
                    .SetMessageDeserializer(new JsonMessageDeserializer<LykkeOrderBook>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .Subscribe(HandleOrderBook)
                    .SetLogger(log);
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
            sourceFeedOrderbooksRabbit.Start();
        }


        private async Task HandleOrderBook(LykkeOrderBook lykkeOrderBook)
        {
            try
            {
                var instrument = Instruments.FirstOrDefault(x =>
                    string.Compare(x.Name, lykkeOrderBook.AssetPair, StringComparison.InvariantCultureIgnoreCase) == 0);

                if (instrument == null && Config.UseSupportedCurrencySymbolsAsFilter == false)
                {
                    instrument = new Instrument(lykkeOrderBook.AssetPair);
                }

                if (instrument != null)
                {
                    decimal bestBid = 0;
                    decimal bestAsk = 0;

                    ordersFilter.FilterOutDuplicatedOrders(lykkeOrderBook);

                    if (lykkeOrderBook.IsBuy)
                    {
                        _lastBids[instrument.Name] = bestBid = lykkeOrderBook.Prices.Select(x => x.Price)
                            .OrderByDescending(x => x).FirstOrDefault();
                        bestAsk = _lastAsks.ContainsKey(instrument.Name) ? _lastAsks[instrument.Name] : 0;
                    }
                    else
                    {
                        _lastAsks[instrument.Name] = bestAsk =
                            lykkeOrderBook.Prices.Select(x => x.Price).OrderBy(x => x).FirstOrDefault();
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
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(LykkeExchange), nameof(HandleOrderBook),
                    $"message: {lykkeOrderBook.ToJson()}", ex);
            }
        }

        protected override void StopImpl()
        {
            ctSource?.Cancel();
            OnStopped();
        }
    }
}
