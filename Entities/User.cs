using System;
using System.Collections.Generic;

namespace BuyStuffApi.Entities
{
    public class User
    {
        public int _Id { get; set; }
        public string _email { get; set; }
        public string _username { get; set; }
        public string _first_name { get; set; }
        public string _last_name { get; set; }
        public byte[] _password_hash { get; set; }
        public string _address { get; set; }
        public List<Tuple<int, int>> _cart { get; set; }
        public List<int> _orders { get; set; }
        public List<int> _returns { get; set; }
        public Payment _payment { get; set; }
        public byte[] _password_salt { get; set; }
    }
}