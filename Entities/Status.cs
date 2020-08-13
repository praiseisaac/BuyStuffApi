using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections.Generic;

namespace BuyStuffApi.Entities
{
    public enum OrderStatus {
         ORDERED, SHIPPED, DELIVERED, CANCELLED
    }

    public enum ListingStatus {
        SCHEDULED, ACTIVE, INACTIVE
    }
}