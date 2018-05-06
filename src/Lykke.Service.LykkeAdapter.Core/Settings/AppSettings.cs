using Lykke.Service.LykkeAdapter.Core.Settings.ServiceSettings;
using Lykke.Service.LykkeAdapter.Core.Settings.SlackNotifications;

namespace Lykke.Service.LykkeAdapter.Core.Settings
{
    public class AppSettings
    {
        public LykkeAdapterSettings LykkeAdapterService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
        public OrderBooksCacheProviderClientSettings OrderBooksCacheProviderClient { get; set; }
    }
}
