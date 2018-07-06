using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Service.LykkeAdapter.Core.Handlers;
using Lykke.Service.LykkeAdapter.Core.Services;
using Lykke.Service.LykkeAdapter.Core.Settings;
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

            builder.RegisterGeneric(typeof(RabbitMqHandler<>));

            builder.RegisterInstance(_settings.CurrentValue);

            builder.RegisterType<MeOrderBookSubscriber>()
                .WithParameter(
                    new TypedParameter(
                        typeof(RabbitMqConfiguration),
                        _settings.CurrentValue.RabbitMq.SourceFeed))
                .AutoActivate()
                .SingleInstance();

            builder.RegisterType<TickPricePublisher>()
                .WithParameter(
                    new TypedParameter(
                        typeof(RabbitMqPublishToExchangeConfiguration),
                        _settings.CurrentValue.RabbitMq.TickPrices))
                .As<ITickPricePublisher>()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterType<OrderBookPublisher>()
                .WithParameter(
                    new TypedParameter(
                        typeof(RabbitMqPublishToExchangeConfiguration),
                        _settings.CurrentValue.RabbitMq.OrderBooks))
                .As<IOrderBookPublisher>()
                .AutoActivate()
                .SingleInstance();

            builder.RegisterType<OrderBookService>()
                .As<IOrderBookService>()
                .SingleInstance();

            builder.RegisterType<ProvideDataJob>()
                .AutoActivate()
                .SingleInstance();

            builder.Populate(_services);
        }
    }
}
