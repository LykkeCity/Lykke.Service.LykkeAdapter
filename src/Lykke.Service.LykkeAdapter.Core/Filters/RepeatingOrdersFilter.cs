using Lykke.Service.LykkeAdapter.Core.Domain.OrderBooks;
using System.Linq;

namespace Lykke.Service.LykkeAdapter.Core.Filters
{
    public class RepeatingOrdersFilter
    {
        public void FilterOutDuplicatedOrders(LykkeOrderBook orderbook)
        {
            orderbook.Prices = orderbook.Prices.GroupBy(d => d.Price)
                                                .Select(g => new PriceVolume(g.Key, g.Sum(s => s.Volume))).ToList();
        }
    }
}
