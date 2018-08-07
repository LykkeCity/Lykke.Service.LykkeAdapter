using System.Threading.Tasks;
using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;

namespace Lykke.Service.LykkeAdapter.Core.Services
{
    public interface IOrderBookPublisher
    {
        Task Publish(TradingOrderBook orderBook);

        /// <summary>
        /// push message to special chanel with thinned stream
        /// </summary>
        Task PublishThinned(TradingOrderBook orderBook);
    }
}
