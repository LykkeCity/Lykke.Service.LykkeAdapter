using System;
using Common.Log;

namespace Lykke.Service.LykkeAdapter.Client
{
    public class LykkeAdapterClient : ILykkeAdapterClient, IDisposable
    {
        private readonly ILog _log;

        public LykkeAdapterClient(string serviceUrl, ILog log)
        {
            _log = log;
        }

        public void Dispose()
        {
            //if (_service == null)
            //    return;
            //_service.Dispose();
            //_service = null;
        }
    }
}
