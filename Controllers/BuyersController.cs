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
    [Authorize(Policy = "ValidAccessToken")]
    // [EnableCors("ReactPolicy")]
    [Route("api/[controller]")]
    public class BuyersController : Controller
    {
        private IBuyerService _buyerService;
        private readonly AppSettings _appSettings;

        private IOrderService _orderService;
        private IItemService _itemService;

        public AppDb Db { get; }
        public BuyersController(
            IBuyerService buyerService,
            IOptions<AppSettings> appSettings,
            IOrderService orderService,
            IItemService itemService,
            AppDb db
        )
        {
            Db = db;
            _buyerService = buyerService;
            _appSettings = appSettings.Value;
            _orderService = orderService;
            _itemService = itemService;
        }



        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] BuyerDto buyerDto)
        {
            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);

            var buyer = _buyerService.Authenticate(buyerDto._email, buyerDto._password).Result;

            if (buyer == null)
                return Unauthorized();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.Name, buyer._Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)

            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                Id = buyer._Id,
                Username = buyer._username,
                FirstName = buyer._first_name,
                LastName = buyer._last_name,
                Token = tokenString
            });

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] BuyerDto buyerDto)
        {
            Buyer buyer = MapResult(buyerDto);
            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            try
            {
                await _buyerService.Create(buyer, buyerDto._password);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // try {
            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            var buyers = _buyerService.GetBuyers().Result;
            var buyerDtos = new List<Object>();

            // var testBuy = new Buyer{
            //     _first_name = "praise",
            //     _last_name = "daramola"
            // };

            if (buyers == null) return Ok();

            foreach (var item in buyers)
            {
                var buyerDto = MapResultExport(item);
                buyerDtos.Add(buyerDto);
            };

            return Ok(buyerDtos);
            // } catch (NullReferenceException ex) {
            //     return Ok(ex.Message);
            // }

        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // try
            // {
            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            var buyer = _buyerService.GetBuyer(id).Result;
            var buyerDto = MapResult(buyer);
            return Ok(buyerDto);
            // }
            // catch (NullReferenceException ex)
            // {
            //     return Ok(ex.Message);
            // }

        }



        [HttpGet("email={email}")]
        public async Task<IActionResult> GetById(string email)
        {
            // try
            // {
            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            var buyer = _buyerService.GetBuyer(email).Result;
            var buyerDto = MapResult(buyer);
            return Ok(buyerDto);
            // }
            // catch (NullReferenceException ex)
            // {
            //     return Ok(ex.Message);
            // }

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BuyerDto buyerDto)
        {
            var buyer = MapResult(buyerDto);
            buyer._Id = id;
            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            try
            {

                await _buyerService.Update(buyer, buyerDto._password);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpPut("{id}/createorder")]
        public async Task<IActionResult> CreateOrder(int id, [FromBody] Order order)
        {
            // var buyer = MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _orderService = new OrderService(Db);



            try
            {
                var orderId = _orderService.Create(id, order).Result._Id;
                await _buyerService.PlaceOrder(id, orderId);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // [AllowAnonymous]
        // [HttpPut("addtocart")]
        // public async Task<IActionResult> AddToCart([FromBody] Item item)
        // {
        //     // var buyer = MapResult(buyerDto);
        //     // buyer._Id = id;

        //     await Db.Connection.OpenAsync();
        //     _buyerService = new BuyerService(Db);
        //     _itemService = new ItemService(Db);

        //     try
        //     {
        //         var orderId = _orderService.Create(id, order).Result._Id;
        //         await _buyerService.PlaceOrder(id, orderId);
        //         return Ok();
        //     }
        //     catch (AppException ex)
        //     {
        //         return BadRequest(ex.Message);
        //     }
        // }

       
        [HttpPut("{id}/addtocart")]
        public async Task<IActionResult> AddToCart(int id, [FromBody] Item item)
        {
            // var buyer = MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _itemService = new ItemService(Db);
            var buyer = _buyerService.GetBuyer(id).Result;
            try
            {
                await _buyerService.AddItemToCart(id, item);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/removefromcart")]
        public async Task<IActionResult> RemoveFromCart(int id, [FromBody] Item item)
        {
            // var buyer = MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _itemService = new ItemService(Db);
            var buyer = _buyerService.GetBuyer(id).Result;
            try
            {
                await _buyerService.RemoveItemFromCart(id, item);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/cancelorder")]
        public async Task<IActionResult> Checkout(int id, [FromBody] Order order)
        {
            // var buyer = MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _orderService = new OrderService(Db);
            List<int> orderIds = new List<int>();


            try
            {
                var status = _orderService.CancelOrder(order._Id).Result;
                order._status = status;
                return Ok(order);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/createorder")]
        public async Task<IActionResult> Checkout(int id, [FromBody] List<Order> orders)
        {
            // var buyer = MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _orderService = new OrderService(Db);
            List<int> orderIds = new List<int>();


            try
            {
                var order = _orderService.CreateOrders(id, orders).Result;
                for (int i = 0; i < order.Count; i++) {
                    await _buyerService.PlaceOrder(id, order[i]._Id);
                }
                
                return Ok(order);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/getorders")]
        public async Task<IActionResult> GetOrders(int id)
        {
            // var buyer = MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _orderService = new OrderService(Db);



            try
            {
                var buyerId = _buyerService.GetBuyer(id).Result;
                var orders = _orderService.GetOrders(buyerId);
                return Ok(orders.Result);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("return/id={id}/orderId={orderId}")]
        public async Task<IActionResult> AddReturn(int id, int orderId)
        {
            // var buyer = MapResult(buyerDto);
            // buyer._Id = id;
            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            try
            {
                await _buyerService.AddBuyerReturn(id, orderId);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            await _buyerService.Delete(id);
            return Ok();
        }



        private BuyerDto MapResult(Buyer buyer)
        {
            return new BuyerDto
            {
                _Id = buyer._Id,
                _username = buyer._username,
                _email = buyer._email,
                _first_name = buyer._first_name,
                _last_name = buyer._last_name,
                _address = buyer._address,
                _cart = buyer._cart,
                _orders = buyer._orders,
                _returns = buyer._returns
            };
        }

        private Object MapResultExport(Buyer buyer)
        {
            return new
            {
                _Id = buyer._Id,
                _username = buyer._username,
                _email = buyer._email,
                _first_name = buyer._first_name,
                _last_name = buyer._last_name,
                _address = buyer._address,
                _cart = buyer._cart,
                _orders = buyer._orders,
                _returns = buyer._returns
            };
        }

        private Buyer MapResult(BuyerDto buyer)
        {
            return new Buyer
            {
                _Id = buyer._Id,
                _username = buyer._username,
                _email = buyer._email,
                _first_name = buyer._first_name,
                _last_name = buyer._last_name,
                _address = buyer._address,
                _cart = buyer._cart,
                _orders = buyer._orders,
                _returns = buyer._returns
            };

        }
    }
}
