using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;
using Lykke.Service.LykkeAdapter.Core.Services;
using Lykke.Service.LykkeAdapter.Core.Settings.ServiceSettings;
using Lykke.Service.LykkeAdapter.Core.Utils;

namespace Lykke.Service.LykkeAdapter.RabbitMq
{
    public class OrderBookPublisher : IOrderBookPublisher, IStartable, IStopable
    {
        private readonly RabbitMqPublishToExchangeConfiguration _configuration;
        private readonly ILog _log;
        private RabbitMqPublisher<TradingOrderBook> _rabbitPublisher;


        public OrderBookPublisher(RabbitMqPublishToExchangeConfiguration configuration, ILog log)
        {
            _configuration = configuration;
            _log = log;
            
        }

        public async Task Publish(TradingOrderBook orderBook)
        {
            try
            {
                await _rabbitPublisher.ProduceAsync(orderBook);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(OrderBookPublisher), nameof(Publish), new { TradingOrderBook = orderBook }.ToJson(), e);
            }
        }

        public void Start()
        {
            var publisherSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _configuration.ConnectionString,
                ExchangeName = _configuration.PublishToExchange,
                IsDurable = false,
                DeadLetterExchangeName = string.Empty
            };

            _rabbitPublisher = new RabbitMqPublisher<TradingOrderBook>(publisherSettings)
                .DisableInMemoryQueuePersistence()
                .SetSerializer(new GenericRabbitModelConverter<TradingOrderBook>())
                .SetLogger(_log)
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(publisherSettings))
                .PublishSynchronously()
                .Start();
        }

        public void Dispose()
        {
            _rabbitPublisher?.Dispose();
        }

        public void Stop()
        {
            _rabbitPublisher?.Stop();
        }
    }
}