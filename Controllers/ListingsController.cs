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
    public class ListingsController : Controller
    {
        private IListingService _listingService;
        private readonly AppSettings _appSettings;

        private IOrderService _orderService;

        public AppDb Db { get; }
        public ListingsController(
            IListingService listingService,
            IOptions<AppSettings> appSettings,
            IOrderService orderService,
            AppDb db
        )
        {
            Db = db;
            _listingService = listingService;
            _appSettings = appSettings.Value;
            _orderService = orderService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // try {
            await Db.Connection.OpenAsync();
            _listingService = new ListingService(Db);
            var listings = _listingService.GetListings().Result;
            var listingDtos = new List<Object>();

            // var testBuy = new Listing{
            //     _first_name = "praise",
            //     _last_name = "daramola"
            // };

            if (listings == null) return Ok();
            return Ok(listings);
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
            _listingService = new ListingService(Db);
            var listing = _listingService.GetListing(id).Result;
            return Ok(listing);
            // }
            // catch (NullReferenceException ex)
            // {
            //     return Ok(ex.Message);
            // }

        }

        [HttpGet("name={name}")]
        public async Task<IActionResult> GetByName(string name)
        {
            // try
            // {
            await Db.Connection.OpenAsync();
            _listingService = new ListingService(Db);
            var listing = _listingService.GetListings(name).Result;
            return Ok(listing);

        }
    }
}
