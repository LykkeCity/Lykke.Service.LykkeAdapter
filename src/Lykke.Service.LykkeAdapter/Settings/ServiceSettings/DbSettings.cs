using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.LykkeAdapter.Settings.ServiceSettings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
    }
}
