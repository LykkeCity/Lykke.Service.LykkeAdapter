using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common;
using Common.Log;
using Lykke.Service.LykkeAdapter.Core.Services;
using Lykke.Service.LykkeAdapter.Core.Settings.ServiceSettings;
using Lykke.Service.LykkeAdapter.RabbitMq;
using Lykke.Service.LykkeAdapter.Services;
using Lykke.SettingsReader;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.LykkeAdapter.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<LykkeAdapterSettings> _settings;
        private readonly IReloadingManager<OrderBooksCacheProviderClientSettings> _orderBooksCacheProviderClientSettings;

        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public ServiceModule(
            IReloadingManager<LykkeAdapterSettings> settings, 
            IReloadingManager<OrderBooksCacheProviderClientSettings> orderBooksCacheProviderClientSettings, 
            ILog log)
        {
            _settings = settings;
            _orderBooksCacheProviderClientSettings = orderBooksCacheProviderClientSettings;
            _log = log;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();
            
            builder.RegisterInstance(_settings.CurrentValue);

            builder.RegisterType<MeOrderBookSubscriber>()
                .WithParameter(
                    new TypedParameter(
                        typeof(RabbitMqSourceFeedExchangeConfiguration),
                        _settings.CurrentValue.RabbitMq.SourceFeed))
                .As<IStartable>().As<IStopable>()
                .SingleInstance();

            builder.RegisterType<TickPricePublisher>()
                .WithParameter(
                    new TypedParameter(
                        typeof(RabbitMqPublishToExchangeConfiguration),
                        _settings.CurrentValue.RabbitMq.TickPrices))
                .As<ITickPricePublisher>()
                .As<IStartable>().As<IStopable>()
                .SingleInstance();

            builder.RegisterType<OrderBookPublisher>()
                .WithParameter(
                    new TypedParameter(
                        typeof(RabbitMqPublishToExchangeConfiguration),
                        _settings.CurrentValue.RabbitMq.OrderBooks))
                .As<IOrderBookPublisher>()
                .As<IStartable>().As<IStopable>()
                .SingleInstance();

            builder.RegisterType<OrderBookService>()
                .As<ILykkeOrderBookHandler>()
                .As<IOrderBookService>()

                .SingleInstance();

            builder.RegisterType<ProvideDataJob>()
                .WithParameter(
                    new TypedParameter(
                        typeof(int),
                        _settings.CurrentValue.MaxEventPerSecondByInstrument))
                .As<IStartable>().As<IStopable>()
                .SingleInstance();

            builder.Populate(_services);
        }
    }
}
