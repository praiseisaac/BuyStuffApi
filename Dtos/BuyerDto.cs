using System;
using System.Collections.Generic;

namespace BuyStuffApi.Dtos
{
    public class BuyerDto
    {
        public int _Id { get; set; }
        public string _email { get; set; }
        public string _username { get; set; }
        public string _first_name { get; set; }
        public string _last_name { get; set; }
        public string _address { get; set; }
        public List<Tuple<string, int>> _cart { get; set; }
        public List<int> _orders { get; set; }
        public List<int> _returns { get; set; }
        public List<Tuple<string, string, int, int, int>> _payment { get; set; }
        public string _password { get; set; }
    }
}