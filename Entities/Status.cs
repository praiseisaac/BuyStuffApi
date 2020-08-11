using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections.Generic;

namespace BuyStuffApi
{
    public enum Status {
        ACTIVE, INACTIVE, DELIVERED, SHIPPED, SCHEDULED, CANCELLED
    }
}