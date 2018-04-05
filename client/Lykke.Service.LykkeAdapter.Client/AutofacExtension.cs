using System;
using Autofac;
using Common.Log;

namespace Lykke.Service.LykkeAdapter.Client
{
    public static class AutofacExtension
    {
        public static void RegisterLykkeAdapterClient(this ContainerBuilder builder, string serviceUrl, ILog log)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (serviceUrl == null) throw new ArgumentNullException(nameof(serviceUrl));
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            builder.RegisterType<LykkeAdapterClient>()
                .WithParameter("serviceUrl", serviceUrl)
                .As<ILykkeAdapterClient>()
                .SingleInstance();
        }

        public static void RegisterLykkeAdapterClient(this ContainerBuilder builder, LykkeAdapterServiceClientSettings settings, ILog log)
        {
            builder.RegisterLykkeAdapterClient(settings?.ServiceUrl, log);
        }
    }
}
