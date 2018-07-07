using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.LykkeAdapter.Core.Domain.Trading;
using Lykke.Service.LykkeAdapter.Core.Services;
using Lykke.Service.LykkeAdapter.Core.Settings.ServiceSettings;
using Lykke.Service.LykkeAdapter.Core.Utils;

namespace Lykke.Service.LykkeAdapter.RabbitMq
{
    public class TickPricePublisher : ITickPricePublisher, IStartable, IStopable
    {
        private readonly RabbitMqPublishToExchangeConfiguration _configuration;
        private readonly ILog _log;
        private RabbitMqPublisher<TickPrice> _rabbitPublisher;


        public TickPricePublisher(RabbitMqPublishToExchangeConfiguration configuration, ILog log)
        {
            _configuration = configuration;
            _log = log;

        }

        public async Task Publish(TickPrice tickPrice)
        {
            try
            {
                await _rabbitPublisher.ProduceAsync(tickPrice);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(OrderBookPublisher), nameof(Publish), $"tickPrice: {tickPrice.ToJson()}", e);
            }
        }

        public void Start()
        {
            var publisherSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _configuration.ConnectionString,
                ExchangeName = _configuration.PublishToExchange,
                IsDurable = true,
                DeadLetterExchangeName = string.Empty
            };

            _rabbitPublisher = new RabbitMqPublisher<TickPrice>(publisherSettings)
                .DisableInMemoryQueuePersistence()
                .SetSerializer(new GenericRabbitModelConverter<TickPrice>())
                .SetLogger(_log)
                .SetPublishStrategy(new DefaultFanoutPublishStrategy(publisherSettings))
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
