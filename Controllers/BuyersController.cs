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
        private IListingService _listingService;
        private Mapper mapper = new Mapper();
        public AppDb Db { get; }
        public BuyersController(
            IBuyerService buyerService,
            IOptions<AppSettings> appSettings,
            IOrderService orderService,
            IItemService itemService,
            IListingService listingService,
            AppDb db
        )
        {
            Db = db;
            _buyerService = buyerService;
            _appSettings = appSettings.Value;
            _orderService = orderService;
            _itemService = itemService;
            _listingService = listingService;
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
            var new_buyer = mapper.MapResult(buyer);
            new_buyer.Token = tokenString;
            return Ok(new_buyer);

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] BuyerDto buyerDto)
        {
            Buyer buyer = mapper.MapResult(buyerDto);
            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            try
            {
                await _buyerService.Create(buyer, buyerDto._password);
                return Ok(buyer);
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
                var buyerDto = mapper.MapResultExport(item);
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
            var buyerDto = mapper.MapResult(buyer);
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
            var buyerDto = mapper.MapResult(buyer);
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
            var buyer = mapper.MapResult(buyerDto);
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

        // [AllowAnonymous]
        // [HttpPut("addtocart")]
        // public async Task<IActionResult> AddToCart([FromBody] Item item)
        // {
        //     // var buyer = mapper.MapResult(buyerDto);
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
        public async Task<IActionResult> AddToCart(int id, [FromBody] Listing listing)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _listingService = new ListingService(Db);
            var dbListing = _listingService.GetListing(listing._Id).Result;

            var buyer = _buyerService.GetBuyer(id).Result;
            try
            {
                await _buyerService.AddToCart(id, dbListing);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/addtemptocart")]
        public async Task<IActionResult> AddToCart(int id, [FromBody] List<int> listing)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _listingService = new ListingService(Db);
            var dbListing = _listingService.GetListings(listing).Result;

            var buyer = _buyerService.GetBuyer(id).Result;
            try
            {
                await _buyerService.AddToCart(id, dbListing);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/removefromcart")]
        public async Task<IActionResult> RemoveFromCart(int id, [FromBody] Listing listing)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _itemService = new ItemService(Db);
            var buyer = _buyerService.GetBuyer(id).Result;
            try
            {
                buyer = _buyerService.RemoveFromCart(id, listing).Result;
                return buyer._cart.Count > 0 ? Ok(buyer._cart) : Ok(new List<int>());
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/cancelorder")]
        public async Task<IActionResult> CancelOrder(int id, [FromBody] Order order)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _orderService = new OrderService(_buyerService, Db);
            List<int> orderIds = new List<int>();


            try
            {
                var status = _orderService.CancelOrder(order._Id);
                order._status = status.Result;
                return Ok(order);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpPut("{id}/checkout")]
        public async Task<IActionResult> Checkout(int id, [FromBody] List<Order> orders)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _orderService = new OrderService(_buyerService, Db);
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

        [HttpGet("{id}/cart")]
        public async Task<IActionResult> GetCart(int id)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _orderService = new OrderService(_buyerService, Db);
            _listingService = new ListingService(Db);
            var tempCart = new Listing();

            try
            {
                var buyer = _buyerService.GetBuyer(id).Result;
                for (int i = 0; i < buyer._cart.Count; i++) {
                    tempCart = await _listingService.GetListing(buyer._cart[i]._Id);
                    
                    buyer._cart[i]._price = tempCart._price * buyer._cart[i]._item._quantity;
                    
                }
                return Ok(buyer._cart);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/getorders")]
        public async Task<IActionResult> GetOrders(int id)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _buyerService = new BuyerService(Db);
            _orderService = new OrderService(_buyerService, Db);

            


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
            // var buyer = mapper.MapResult(buyerDto);
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



        
    }
}
