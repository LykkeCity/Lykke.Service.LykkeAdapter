using System.Collections.Generic;
using System.Linq;
using System.Net;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.LykkeAdapter.Core.Domain.Trading;
using Lykke.Service.LykkeAdapter.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.LykkeAdapter.Controllers
{
    [Route("api/[controller]")]
    public class OrderBookController : Controller
    {
        private readonly IOrderBookService _orderBookService;

        public OrderBookController(IOrderBookService orderBookService)
        {
            _orderBookService = orderBookService;
        }

        [HttpGet("GetAllInstruments")]
        [SwaggerOperation("GetAllInstruments")]
        [ProducesResponseType(typeof(List<string>), (int) HttpStatusCode.OK)]
        public IActionResult GetAllInstruments()
        {
            var data = _orderBookService.GetCurrentOrderBooks();
            return Ok(data.Select(e => e.AssetPairId).ToList());
        }

        [HttpGet("GetAllTickPrices")]
        [SwaggerOperation("GetAllTickPrices")]
        [ProducesResponseType(typeof(List<TickPrice>), (int) HttpStatusCode.OK)]
        public IActionResult GetAllTickPrices()
        {
            var data = _orderBookService.GetCurrentOrderBooks();
            return Ok(data.Select(e => new TickPrice(e.AssetPairId, e.Timestamp,
                e.Asks.Select(i => i.Price).DefaultIfEmpty(0).Min(),
                e.Bids.Select(i => i.Price).DefaultIfEmpty(0).Max())).ToList());
        }

        [HttpGet("GetOrderBook")]
        [SwaggerOperation("GetOrderBook")]
        [ProducesResponseType(typeof(List<TickPrice>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult GetOrderBook(string assetPair)
        {
            var data = _orderBookService.GetCurrentOrderBooks().FirstOrDefault(e => e.AssetPairId == assetPair);
            if (data != null)
                return Ok(data);

            return NotFound();
        }
    }
}
