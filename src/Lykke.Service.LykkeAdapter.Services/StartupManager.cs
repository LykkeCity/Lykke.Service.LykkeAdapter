using System.Collections.Generic;
using Common.Log;
using Lykke.Service.LykkeAdapter.Core.Services;
using System.Threading.Tasks;
using Autofac;
using Common;

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
        private readonly IEnumerable<IStartable> _items;

        public StartupManager(ILog log, IEnumerable<IStartable> items)
        {
            _log = log;
            _items = items;
        }

        public async Task StartAsync()
        {
            foreach (var item in _items)
            {
                item.Start();
            }

            await Task.CompletedTask;
        }
    }
}
