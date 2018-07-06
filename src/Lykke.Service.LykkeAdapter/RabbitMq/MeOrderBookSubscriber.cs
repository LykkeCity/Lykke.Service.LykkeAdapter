using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;
using Lykke.Service.LykkeAdapter.Core.Services;
using Lykke.Service.LykkeAdapter.Core.Settings.ServiceSettings;

namespace Lykke.Service.LykkeAdapter.RabbitMq
{
    public class MeOrderBookSubscriber: IStartable, IStopable
    {
        private readonly ILykkeOrderBookHandler _lykkeOrderBookHandler;
        private readonly ILog _log;
        private readonly RabbitMqSubscriber<LykkeOrderBook> _sourceFeedOrderbooksRabbit;

        public MeOrderBookSubscriber(RabbitMqSourceFeedExchangeConfiguration configuration, ILykkeOrderBookHandler lykkeOrderBookHandler, ILog log)
        {
            _lykkeOrderBookHandler = lykkeOrderBookHandler;
            _log = log;

            var settings = new RabbitMqSubscriptionSettings()
            {
                ConnectionString = configuration.ConnectionString,
                QueueName = configuration.Queue,
                ExchangeName = configuration.Exchange,
                IsDurable = false,
                DeadLetterExchangeName = string.Empty
            };

            _sourceFeedOrderbooksRabbit =
                new RabbitMqSubscriber<LykkeOrderBook>(settings,
                        new ResilientErrorHandlingStrategy(_log, settings, TimeSpan.FromSeconds(10),
                            next: new DefaultErrorHandlingStrategy(_log, settings)))
                    .SetMessageDeserializer(new JsonMessageDeserializer<LykkeOrderBook>())
                    .SetMessageReadStrategy(new MessageReadWithTemporaryQueueStrategy())
                    .Subscribe(HandleOrderBook)
                    .SetLogger(_log);
        }

        private async Task HandleOrderBook(LykkeOrderBook arg)
        {
            try
            {
                await _lykkeOrderBookHandler.Handle(arg);
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(MeOrderBookSubscriber), nameof(HandleOrderBook), new {LykkeOrderBook=arg}.ToJson(), ex);
            }
        }

        public void Start()
        {
            _sourceFeedOrderbooksRabbit.Start();
        }

        public void Dispose()
        {
            _sourceFeedOrderbooksRabbit?.Dispose();
        }

        public void Stop()
        {
            _sourceFeedOrderbooksRabbit?.Stop();
        }
    }
}
