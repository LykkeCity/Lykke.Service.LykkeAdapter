using Autofac;
using Common;
using Common.Log;
using Lykke.Service.LykkeAdapter.Core;
using Lykke.Service.LykkeAdapter.Core.Domain.Exchange;
using Lykke.Service.LykkeAdapter.Core.Domain.Trading;
using Lykke.Service.LykkeAdapter.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Service.LykkeAdapter.Services.Exchange
{
    public abstract class ExchangeBase : IStartable, IStopable
    {
        protected readonly ILog LykkeLog;

        internal LykkeAdapterSettings Config { get; }

        public ExchangeState State { get; private set; }

        public IReadOnlyList<Instrument> Instruments { get; }

        protected ExchangeBase(LykkeAdapterSettings config, ILog log)
        {
            Config = config;
            State = ExchangeState.Initializing;
            LykkeLog = log;

            Instruments = config.SupportedCurrencySymbols?.Select(x => new Instrument(x.LykkeSymbol)).ToList() ?? new List<Instrument>();

            if (!Instruments.Any() && config.UseSupportedCurrencySymbolsAsFilter != false)
            {
                throw new ArgumentException($"There is no instruments in the settings for {Constants.LykkeExchangeName} exchange");
            }
        }

        public void Start()
        {
            LykkeLog.WriteInfoAsync(nameof(ExchangeBase), nameof(Start), Constants.LykkeExchangeName, $"Starting exchange {Constants.LykkeExchangeName}, current state is {State}").Wait();

            if (State != ExchangeState.ErrorState && State != ExchangeState.Stopped && State != ExchangeState.Initializing)
                return;

            State = ExchangeState.Connecting;
            StartImpl();
        }

        protected abstract void StartImpl();
        public event Action Connected;
        protected void OnConnected()
        {
            State = ExchangeState.Connected;
            Connected?.Invoke();
        }

        public void Stop()
        {
            if (State == ExchangeState.Stopped)
                return;

            State = ExchangeState.Stopping;
            StopImpl();
            LykkeLog.WriteInfoAsync(GetType().Name, "Cleanup", "Stopped");
        }

        protected abstract void StopImpl();
        public event Action Stopped;
        protected void OnStopped()
        {
            State = ExchangeState.Stopped;
            Stopped?.Invoke();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
