using System.Threading.Tasks;
using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;

namespace Lykke.Service.LykkeAdapter.Core.Services
{
    public interface ILykkeOrderBookHandler
    {
        Task Handle(LykkeOrderBook orderBook);
    }
}
