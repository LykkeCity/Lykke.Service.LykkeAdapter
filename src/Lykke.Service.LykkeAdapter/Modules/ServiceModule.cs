using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common;
using Common.Log;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Job.OrderBooksCacheProvider.Client;
using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;
using Lykke.Service.LykkeAdapter.Core.Domain.Trading;
using Lykke.Service.LykkeAdapter.Core.Handlers;
using Lykke.Service.LykkeAdapter.Core.Services;
using Lykke.Service.LykkeAdapter.Core.Settings;
using Lykke.Service.LykkeAdapter.Core.Settings.ServiceSettings;
using Lykke.Service.LykkeAdapter.Core.Throttling;
using Lykke.Service.LykkeAdapter.Services;
using Lykke.Service.LykkeAdapter.Services.Exchange;
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
            // TODO: Do not register entire settings in container, pass necessary settings to services which requires them
            // ex:
            //  builder.RegisterType<QuotesPublisher>()
            //      .As<IQuotesPublisher>()
            //      .WithParameter(TypedParameter.From(_settings.CurrentValue.QuotesPublication))

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

            builder.RegisterType<LykkeExchange>().As<ExchangeBase>()
                                                 .As<IStopable>()
                                                 .SingleInstance();

            RegisterRabbitMqHandler<TickPrice>(builder, _settings.CurrentValue.RabbitMq.TickPrices, "tickHandler");
            RegisterRabbitMqHandler<TradingOrderBook>(builder, _settings.CurrentValue.RabbitMq.OrderBooks, "orderBookHandler");

            builder.RegisterType<PreventDuplicatesHandler>()
                .WithParameter((info, context) => info.Name == "rabbitMqHandler",
                    (info, context) => context.ResolveNamed<IHandler<TickPrice>>("tickHandler"))
                .SingleInstance()
                .As<IHandler<TickPrice>>();

            builder.RegisterType<OrderBookHandlerDecorator>()
                .WithParameter((info, context) => info.Name == "rabbitMqHandler",
                    (info, context) => context.ResolveNamed<IHandler<TradingOrderBook>>("orderBookHandler"))
                .SingleInstance()
                .As<IHandler<LykkeOrderBook>>();

            builder.RegisterType<EventsPerSecondPerInstrumentThrottlingManager>()
                .WithParameter("maxEventPerSecondByInstrument", _settings.CurrentValue.MaxEventPerSecondByInstrument)
                .As<IThrottling>().InstancePerDependency();

            builder.RegisterInstance(new OrderBookProviderClient(_orderBooksCacheProviderClientSettings.CurrentValue.ServiceUrl))
                .As<IOrderBookProviderClient>()
                .SingleInstance();

            builder.Populate(_services);
        }

        private static void RegisterRabbitMqHandler<T>(ContainerBuilder container, RabbitMqPublishToExchangeConfiguration exchangeConfiguration, string regKey = "")
        {
            container.RegisterType<RabbitMqHandler<T>>()
                .WithParameter("connectionString", exchangeConfiguration.ConnectionString)
                .WithParameter("exchangeName", exchangeConfiguration.PublishToExchange)
                .WithParameter("enabled", exchangeConfiguration.Enabled)
                .Named<IHandler<T>>(regKey)
                .As<IHandler<T>>();
        }
    }
}
