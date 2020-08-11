using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections.Generic;

namespace BuyStuffApi.Entities
{
    public class Order {
        public int Id { get; set; }
        public List<Item> items { get; set; }
        public double total_cost { get; set; }
        public DateTime date_created { get; set; }
        public DateTime date_shipped { get; set; }
        public string tracking_number { get; set; }
        public string delivery_address { get; set; }
        public double shipping_cost { get; set; }
        public Status status { get; set; }
        public DateTime date_delivered { get; set; }
    }
}