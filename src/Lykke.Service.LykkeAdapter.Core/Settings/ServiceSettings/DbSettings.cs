using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.LykkeAdapter.Core.Settings.ServiceSettings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
    }
}
