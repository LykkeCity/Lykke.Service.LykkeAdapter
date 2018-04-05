using JetBrains.Annotations;
using Lykke.Service.LykkeAdapter.Settings.ServiceSettings;
using Lykke.Service.LykkeAdapter.Settings.SlackNotifications;

namespace Lykke.Service.LykkeAdapter.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings
    {
        public LykkeAdapterSettings LykkeAdapterService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
