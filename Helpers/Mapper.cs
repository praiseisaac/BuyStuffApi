using System;
using BuyStuffApi.Dtos;
using BuyStuffApi.Entities;

namespace BuyStuffApi
{
    class Mapper {
        public SellerDto MapResult(Seller seller)
        {
            return new SellerDto
            {
                _Id = seller._Id,
                _username = seller._username,
                _email = seller._email,
                _first_name = seller._first_name,
                _last_name = seller._last_name,
                _address = seller._address,
                _orders = seller._orders,
                _returns = seller._returns
            };
        }

        public Object MapResultInfo(Seller seller)
        {
            return new
            {
                _Id = seller._Id,
                _username = seller._username,
                _address = seller._address
            };
        }

        public Object MapResultExport(Seller seller)
        {
            return new
            {
                _Id = seller._Id,
                _username = seller._username,
                _email = seller._email,
                _first_name = seller._first_name,
                _last_name = seller._last_name,
                _address = seller._address,
                _orders = seller._orders,
                _returns = seller._returns,
                _payment = seller._payment
            };
        }

        public Seller MapResult(SellerDto seller)
        {
            return new Seller
            {
                _Id = seller._Id,
                _username = seller._username,
                _email = seller._email,
                _first_name = seller._first_name,
                _last_name = seller._last_name,
                _address = seller._address,
                _orders = seller._orders,
                _returns = seller._returns
            };

        }
        
        public BuyerDto MapResult(Buyer buyer)
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

        public Object MapResultExport(Buyer buyer)
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

        public Buyer MapResult(BuyerDto buyer)
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

