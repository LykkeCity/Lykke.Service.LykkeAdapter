namespace Lykke.Service.LykkeAdapter.Core.Settings.ServiceSettings
{
    public class RabbitMqPublishToExchangeConfiguration
    {
        public bool Enabled { get; set; }

        public string PublishToExchange { get; set; }

        public string ConnectionString { get; set; }
    }
}
