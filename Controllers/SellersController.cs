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

        public AppDb Db { get; }
        public SellersController(
            ISellerService sellerService,
            IOptions<AppSettings> appSettings,
            AppDb db
        )
        {
            Db = db;
            _sellerService = sellerService;
            _appSettings = appSettings.Value;
        }



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

            return Ok(new
            {
                Id = seller._Id,
                Username = seller._username,
                FirstName = seller._first_name,
                LastName = seller._last_name,
                Token = tokenString
            });

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] SellerDto sellerDto)
        {
            Seller seller = MapResult(sellerDto);
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
                    var sellerDto = MapResultExport(item);
                    sellerDtos.Add(sellerDto);
                };

            return Ok(sellerDtos);
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
            _sellerService = new SellerService(Db);
            var seller = _sellerService.GetSeller(id).Result;
            var sellerDto = MapResult(seller);
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
            var sellerDto = MapResult(seller);
            return Ok(sellerDto);
            // }
            // catch (NullReferenceException ex)
            // {
            //     return Ok(ex.Message);
            // }

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SellerDto sellerDto)
        {
            var seller = MapResult(sellerDto);
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

        private SellerDto MapResult(Seller seller)
        {
            return new SellerDto
            {
                _Id = seller._Id,
                _username = seller._username,
                _email = seller._email,
                _first_name = seller._first_name,
                _last_name = seller._last_name,
                _address = seller._address,
                _cart = seller._cart,
                _orders = seller._orders,
                _returns = seller._returns
            };
        }

        private Object MapResultExport(Seller seller) {
            return new
            {
                _Id = seller._Id,
                _username = seller._username,
                _email = seller._email,
                _first_name = seller._first_name,
                _last_name = seller._last_name,
                _address = seller._address,
                _cart = seller._cart,
                _orders = seller._orders,
                _returns = seller._returns
            };
        }

        private Seller MapResult(SellerDto seller)
        {
            return new Seller
            {
                _Id = seller._Id,
                _username = seller._username,
                _email = seller._email,
                _first_name = seller._first_name,
                _last_name = seller._last_name,
                _address = seller._address,
                _cart = seller._cart,
                _orders = seller._orders,
                _returns = seller._returns
            };

        }
    }
}