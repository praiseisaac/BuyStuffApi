using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections.Generic;



namespace BuyStuffApi.Entities
{
    public class Listing {
        public int _Id { get; set; }
        public int _seller_Id {get; set;}
        public string _title {get; set;}
        public string _description {get; set;}
        public double _price { get; set; }
        public ListingStatus _status { get; set; }
        public DateTime _date_created { get; set; }
        public DateTime  _date_modified { get; set; }
        public double _shipping_cost { get; set; }
        public Item _item {get; set;}
        public List<string> _images {get;set;}
        public int _rating {get; set;}
        public String _seller_name {get; set;}
        
    }
}