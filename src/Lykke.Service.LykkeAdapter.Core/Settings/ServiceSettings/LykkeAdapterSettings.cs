namespace Lykke.Service.LykkeAdapter.Core.Settings.ServiceSettings
{
    public class LykkeAdapterSettings
    {
        public DbSettings Db { get; set; }
        public RabbitMqConfiguration RabbitMq { get; set; }
        public int MaxEventPerSecondByInstrument { get; set; }
    }
}
