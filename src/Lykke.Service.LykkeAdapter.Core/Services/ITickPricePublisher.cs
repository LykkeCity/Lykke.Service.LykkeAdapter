using System.Threading.Tasks;
using Lykke.Service.LykkeAdapter.Core.Domain.Trading;

namespace Lykke.Service.LykkeAdapter.Core.Services
{
    public interface ITickPricePublisher
    {
        Task Publish(TickPrice tickPrice);
    }
}