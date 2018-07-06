using System.Collections.Generic;
using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;

namespace Lykke.Service.LykkeAdapter.Core.Services
{
    public interface IOrderBookService
    {
        void ApplyLykkeOrderBook(LykkeOrderBook orderBook);
        IReadOnlyList<TradingOrderBook> GetCurrentOrderBooks();
    }
}
