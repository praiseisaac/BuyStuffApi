using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections.Generic;

namespace BuyStuffApi.Entities
{
    public class Seller : User
    {
       public List<int> _listings {get; set;}
       public List<int>_items {get; set;}
    }
}
