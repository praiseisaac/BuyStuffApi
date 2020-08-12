using System;
using System.Collections.Generic;

namespace BuyStuffApi.Entities
{
    public class Payment
    {
        public int _name { get; set; }
        public string _code { get; set; }
        public byte[] _card_number { get; set; }
        public int _month { get; set; }
        public int _year { get; set; }
    }
}