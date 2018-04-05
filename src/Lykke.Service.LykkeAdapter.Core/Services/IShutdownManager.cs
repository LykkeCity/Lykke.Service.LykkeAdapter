using System.Threading.Tasks;

namespace Lykke.Service.LykkeAdapter.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}
