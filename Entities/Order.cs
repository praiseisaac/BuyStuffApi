using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections.Generic;

namespace BuyStuffApi.Entities
{
    public class Order {
        public int _Id { get; set; }
        public List<Item> _items { get; set; }
        public double _total_cost { get; set; }
        public DateTime _date_created { get; set; }
        public DateTime  _date_shipped { get; set; }
        public string _tracking_number { get; set; }
        public string _delivery_address { get; set; }
        public double _shipping_cost { get; set; }
        public Status _status { get; set; }
        public DateTime _date_delivered { get; set; }
    }
}