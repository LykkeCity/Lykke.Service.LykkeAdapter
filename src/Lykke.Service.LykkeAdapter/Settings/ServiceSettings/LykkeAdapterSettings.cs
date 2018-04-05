using JetBrains.Annotations;

namespace Lykke.Service.LykkeAdapter.Settings.ServiceSettings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class LykkeAdapterSettings
    {
        public DbSettings Db { get; set; }
    }
}
