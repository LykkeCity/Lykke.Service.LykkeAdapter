using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;
using MoreLinq;
using System.Linq;

namespace Lykke.Service.LykkeAdapter.Core.Filters
{
    public class RepeatingOrdersFilter
    {
        public void FilterOutDuplicatedOrders(LykkeOrderBook orderbook)
        {
            orderbook.Prices = orderbook.Prices.DistinctBy(x => new
            {
                x.Price,
                x.Volume
            }).ToList();
        }
    }
}
