using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Lykke.Service.LykkeAdapter.Core.Domain.Trading;
using System.Threading.Tasks;
using Lykke.Common.ExchangeAdapter.Contracts;
using Newtonsoft.Json;

namespace Lykke.Service.LykkeAdapter.Core.Handlers
{
    public sealed class PreventDuplicatesHandler : IHandler<TickPrice>
    {
        private readonly Dictionary<string, TickPrice> _lastTicks
            = new Dictionary<string, TickPrice>(StringComparer.InvariantCultureIgnoreCase);

        private readonly object _syncRoot = new object();

        private readonly IHandler<TickPrice> _rabbitMqHandler;

        public PreventDuplicatesHandler(IHandler<TickPrice> rabbitMqHandler)
        {
            _rabbitMqHandler = rabbitMqHandler;
        }

        public async Task Handle(TickPrice tickPrice)
        {
            if (UpdateCacheAndCheckChanged(tickPrice))
            {
                await _rabbitMqHandler.Handle(tickPrice);
            }
        }

        private bool UpdateCacheAndCheckChanged(TickPrice tickPrice)
        {
            // Not sure if it possible to use ConcurrentDictionary without global lock
            lock (_syncRoot)
            {
                if (!_lastTicks.TryGetValue(tickPrice.Asset, out var prev))
                {
                    _lastTicks[tickPrice.Asset] = tickPrice;
                    return true;
                }
                else
                {
                    if (prev.Equals(tickPrice))
                    {
                        return false;
                    }
                    else
                    {
                        _lastTicks[tickPrice.Asset] = tickPrice;
                        return true;

                    }
                }
            }
        }
    }
}
