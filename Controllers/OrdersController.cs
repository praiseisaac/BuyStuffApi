using System.Collections.Generic;
using System;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuyStuffApi.Services;
using BuyStuffApi.Helpers;
using BuyStuffApi.Dtos;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using BuyStuffApi.Entities;
using Microsoft.AspNetCore.Cors;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace BuyStuffApi.Controllers
{
    // [EnableCors("ReactPolicy")]
    [Route("api/[controller]")]
    public class OrdersController : Controller
    {
        private IOrderService _orderService;
        private IBuyerService _buyerService;
        private ISellerService _sellerService;
        private readonly AppSettings _appSettings;

        public AppDb Db { get; }
        public OrdersController(
            IOrderService orderService,
            IOptions<AppSettings> appSettings,
            IBuyerService buyerService,
            ISellerService sellerService,
            AppDb db
        )
        {
            Db = db;
            _orderService = orderService;
            _appSettings = appSettings.Value;
            _orderService = orderService;
            _buyerService = buyerService;
            _sellerService = sellerService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // try {
            await Db.Connection.OpenAsync();
            _orderService = new OrderService(_buyerService, Db);
            var orders = _orderService.GetOrders().Result;
            var orderDtos = new List<Object>();

            // var testBuy = new Order{
            //     _first_name = "praise",
            //     _last_name = "daramola"
            // };

            if (orders == null) return Ok();
            return Ok(orders);
            // } catch (NullReferenceException ex) {
            //     return Ok(ex.Message);
            // }

        }

        [HttpGet("id={id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // try
            // {
            await Db.Connection.OpenAsync();
            _orderService = new OrderService(_buyerService, Db);
            var order = _orderService.GetOrder(id).Result;
            return Ok(order);
            // }
            // catch (NullReferenceException ex)
            // {
            //     return Ok(ex.Message);
            // }

        }
    }
}
