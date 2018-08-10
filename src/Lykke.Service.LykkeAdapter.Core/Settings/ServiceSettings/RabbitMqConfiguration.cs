namespace Lykke.Service.LykkeAdapter.Core.Settings.ServiceSettings
{
    public class RabbitMqConfiguration
    {
        public RabbitMqSourceFeedExchangeConfiguration SourceFeed { get; set; }
        
        //order must be the same as in json settings 
        public RabbitMqPublishToExchangeConfiguration TickPrices { get; set; }
        public RabbitMqPublishToExchangeConfiguration OrderBooks { get; set; }

        public RabbitMqPublishToExchangeConfiguration ThinnedOrderBooks { get; set; }
    }
}
