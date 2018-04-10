using Common.Log;
using Lykke.Service.LykkeAdapter.Core.Services;
using Lykke.Service.LykkeAdapter.Services.Exchange;
using System.Threading.Tasks;

namespace Lykke.Service.LykkeAdapter.Services
{
    // NOTE: Sometimes, startup process which is expressed explicitly is not just better, 
    // but the only way. If this is your case, use this class to manage startup.
    // For example, sometimes some state should be restored before any periodical handler will be started, 
    // or any incoming message will be processed and so on.
    // Do not forget to remove As<IStartable>() and AutoActivate() from DI registartions of services, 
    // which you want to startup explicitly.

    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;
        private readonly ExchangeBase _exchange;

        public StartupManager(ILog log, ExchangeBase exchange)
        {
            _log = log;
            _exchange = exchange;
        }

        public async Task StartAsync()
        {
            _exchange.Start();

            await Task.CompletedTask;
        }
    }
}
