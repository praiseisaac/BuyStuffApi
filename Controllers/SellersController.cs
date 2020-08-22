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
    // [Authorize]
    // [EnableCors("ReactPolicy")]
    [Route("api/[controller]")]
    public class SellersController : Controller
    {
        private ISellerService _sellerService;
        private readonly AppSettings _appSettings;
        private IOrderService _orderService;
        private IListingService _listingService;
        private IItemService _itemService;
        private Mapper mapper = new Mapper();

        public AppDb Db { get; }
        public SellersController(
            ISellerService sellerService,
            IOrderService orderService,
            IListingService listingService,
            IOptions<AppSettings> appSettings,
            IItemService itemService,
            AppDb db
        )
        {
            Db = db;
            _sellerService = sellerService;
            _orderService = orderService;
            _listingService = listingService;
            _appSettings = appSettings.Value;
            _itemService = itemService;
        }


        [Authorize(Policy = "ValidAccessToken")]
        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] SellerDto sellerDto)
        {
            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);

            var seller = _sellerService.Authenticate(sellerDto._email, sellerDto._password).Result;

            if (seller == null)
                return Unauthorized();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.Name, seller._Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)

            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            var new_seller = mapper.MapResult(seller);
            new_seller.Token = tokenString;
            return Ok(new_seller);

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] SellerDto sellerDto)
        {
            Seller seller = mapper.MapResult(sellerDto);
            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            try
            {
                await _sellerService.Create(seller, sellerDto._password);
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
            _sellerService = new SellerService(Db);
            var sellers = _sellerService.GetSellers().Result;
            var sellerDtos = new List<Object>();

            // var testBuy = new Seller{
            //     _first_name = "praise",
            //     _last_name = "daramola"
            // };

            if (sellers == null) return Ok();

            foreach (var item in sellers)
            {
                var sellerDto = mapper.MapResultExport(item);
                sellerDtos.Add(sellerDto);
            };

            return Ok(sellerDtos);
            // } catch (NullReferenceException ex) {
            //     return Ok(ex.Message);
            // }

        }

        [AllowAnonymous]
        [HttpGet("{id}/sellerinfo")]
        public async Task<IActionResult> GetSellerInfo(int id)
        {
            // try
            // {
            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            var seller = _sellerService.GetSeller(id).Result;
            var sellerDto = mapper.MapResultInfo(seller);
            return Ok(sellerDto);
            // }
            // catch (NullReferenceException ex)
            // {
            //     return Ok(ex.Message);
            // }

        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            // try
            // {
            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            var seller = _sellerService.GetSeller(id).Result;
            var sellerDto = mapper.MapResultExport(seller);
            return Ok(sellerDto);
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
            _sellerService = new SellerService(Db);
            var seller = _sellerService.GetSeller(email).Result;
            var sellerDto = mapper.MapResult(seller);
            return Ok(sellerDto);

        }

        [HttpPut("{id}/createlisting")]
        public async Task<IActionResult> CreateListing(int id, [FromBody] Listing listing)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            _listingService = new ListingService(Db);
            Seller seller = await _sellerService.GetSeller(id);

            try
            {
                var listingId = _listingService.Create(id, seller._username, listing).Result._Id;
                await _sellerService.AddListing(id, listingId);
                return Ok();
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
            _sellerService = new SellerService(Db);
            _orderService = new OrderService(_sellerService, Db);

            if (id != order._seller_Id)
            {
                return BadRequest("Invalid request");
            }
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

        [HttpPut("{id}/updatelisting")]
        public async Task<IActionResult> UpdateListing(int id, [FromBody] Listing listing)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            _listingService = new ListingService(Db);


            if (id != listing._seller_Id)
            {
                return BadRequest("Invalid request");
            }

            try
            {
                var listingId = _listingService.Update(listing).Result._Id;
                await _sellerService.AddListing(id, listingId);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPut("{id}/deletelisting")]
        public async Task<IActionResult> DeleteListing(int id, [FromBody] Listing listing)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            _listingService = new ListingService(Db);


            if (id != listing._seller_Id)
            {
                return BadRequest("Invalid request");
            }

            try
            {
                var listingId = _listingService.Delete(listing._Id);
                await _sellerService.RemoveListing(id, listing._Id);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/createitem")]
        public async Task<IActionResult> CreateItem(int id, [FromBody] Item item)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            _itemService = new ItemService(Db);
            
            try
            {
                var itemId = _itemService.Create(id, item).Result._Id;
                await _sellerService.AddItem(id, itemId);
                return Ok(itemId);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/updateitem")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] Item item)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            _itemService = new ItemService(Db);
            if (id != item._seller_id)
            {
                return BadRequest("Invalid request");
            }

            try
            {
                var newItem = _itemService.Update(item).Result;
                return Ok(newItem);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPut("{id}/deleteitem")]
        public async Task<IActionResult> DeleteItem(int id, [FromBody] Item item)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            _itemService = new ItemService(Db);

            try
            {
                var itemId = _itemService.Delete(id);
                await _sellerService.RemoveItem(id, item._Id);
                return Ok(itemId);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpGet("{id}/getlistings")]
        public async Task<IActionResult> GetListings(int id)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            _listingService = new ListingService(Db);



            try
            {
                var seller = _sellerService.GetSeller(id).Result;
                var listings = _listingService.GetListings(seller);
                return Ok(listings.Result);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("{id}/getitems")]
        public async Task<IActionResult> GetItems(int id)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            _itemService = new ItemService(Db);



            try
            {
                var seller = _sellerService.GetSeller(id).Result;
                var items = _itemService.GetItems(seller);
                return Ok(items.Result);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{sellerId}/getorders")]
        public async Task<IActionResult> GetOrders(int sellerId)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            _orderService = new OrderService(_sellerService, Db);



            try
            {
                var seller = _sellerService.GetSeller(sellerId).Result;
                var orders = _orderService.GetOrders(seller);
                return Ok(orders.Result);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{sellerId}/fulfillorder/{orderId}")]
        public async Task<IActionResult> FulfillOrder(int sellerId, int orderId)
        {
            // var buyer = mapper.MapResult(buyerDto);
            // buyer._Id = id;

            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            _orderService = new OrderService(_sellerService, Db);

            try
            {
                var seller = _sellerService.GetSeller(sellerId).Result;
                var order = _orderService.FulfillOrder(sellerId, orderId);
                return Ok(order.Result);
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SellerDto sellerDto)
        {
            var seller = mapper.MapResult(sellerDto);
            seller._Id = id;
            await Db.Connection.OpenAsync();
            _sellerService = new SellerService(Db);
            try
            {
                await _sellerService.Update(seller, sellerDto._password);
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
            _sellerService = new SellerService(Db);
            await _sellerService.Delete(id);
            return Ok();
        }

        
    }
}
