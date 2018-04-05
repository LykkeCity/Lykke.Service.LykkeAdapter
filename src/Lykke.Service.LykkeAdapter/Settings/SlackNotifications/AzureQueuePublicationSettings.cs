using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.LykkeAdapter.Settings.SlackNotifications
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AzureQueuePublicationSettings
    {
        [AzureQueueCheck]
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }
    }
}
