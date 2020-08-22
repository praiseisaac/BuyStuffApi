using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections.Generic;

namespace BuyStuffApi.Entities
{
    public class Item {
        public int _Id { get; set; }
        public string _name { get; set; }
        public int _quantity{ get; set; }
        public int _seller_id { get; set; }
        public double _price {get; set;}
    }
}