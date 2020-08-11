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

namespace BuyStuffApi.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class BuyersController : Controller
    {
        private IBuyerService _buyerService;
        private readonly AppSettings _appSettings;

        public BuyersController(
            IBuyerService buyerService,
            AppSettings appSettings
        )
        {
            _buyerService = buyerService;
            _appSettings = appSettings;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] BuyerDto buyerDto)
        {
            var buyer = _buyerService.Authenticate(buyerDto._username, buyerDto._password).Result;

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
        public IActionResult Register([FromBody] BuyerDto buyerDto)
        {
            Buyer buyer = MapResult(buyerDto);

            try
            {
                _buyerService.Create(buyer, buyerDto._password);
                return Ok();
            }
            catch (AppException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var buyers = _buyerService.GetBuyers().Result;
            var buyerDtos = new List<BuyerDto>();
            foreach (var item in buyers)
            {
                var buyerDto = MapResult(item);
                buyerDtos.Add(buyerDto);
            };

            return Ok(buyerDtos);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id) {
            var buyer = _buyerService.GetBuyer(id).Result;
            var buyerDto = MapResult(buyer);
            return Ok(buyerDto);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] BuyerDto buyerDto) {
            var buyer = MapResult(buyerDto);
            buyer._Id = id;

            try {
                _buyerService.Update(buyer, buyerDto._password);
                return Ok();
            } catch (AppException ex) {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id) {
            _buyerService.Delete(id);
            return Ok();
        }

        private BuyerDto MapResult(Buyer buyer) {
            return new BuyerDto
                {
                    _username = buyer._username,
                    _email = buyer._email,
                    _first_name = buyer._first_name,
                    _last_name = buyer._last_name,
                    _address = buyer._address,
                };
        }

        private Buyer MapResult(BuyerDto buyer) {
            return new Buyer
                {
                    _username = buyer._username,
                    _email = buyer._email,
                    _first_name = buyer._first_name,
                    _last_name = buyer._last_name,
                    _address = buyer._address,
                };

        }
    }
}