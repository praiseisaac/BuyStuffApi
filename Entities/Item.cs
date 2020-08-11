using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections.Generic;

namespace BuyStuffApi.Entities
{
    public class Item {
        public int Id { get; set; }
        public string name { get; set; }
        public int quantity{ get; set; }
    }
}