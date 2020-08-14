using System.Reflection.Metadata;
using System.Linq;
using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Win32;
using BuyStuffApi.Helpers;
using BuyStuffApi.Entities;
using Newtonsoft.Json;

namespace BuyStuffApi.Services
{
    public interface IOrderService
    {
        Task<Order> Create(int buyer_id, Order order);
        Task<Order> GetOrder(int id);
        Task<List<Order>> GetOrders(List<int> ids);
        Task<List<Order>> GetOrders(Seller seller);
        Task<List<Order>> GetOrders(Buyer buyer);
        Task<OrderStatus> CancelOrder(int id);
        Task Delete(int id);
        Task<Order> FulfillOrder(int sellerId, int orderId);
        Task<List<Order>> CreateOrders(int buyer_id, List<Order> orders);
    }

    public class OrderService : IOrderService
    {
        internal AppDb Db { get; set; }
        public OrderService()
        {

        }

        // Constructor
        internal OrderService(AppDb db)
        {
            Db = db;
        }

        // used to create a user
        public async Task<Order> Create(int buyer_id, Order order)
        {
            using var cmd = Db.Connection.CreateCommand();
            // order._date_created = DateTime.Now;
            order._status = OrderStatus.ORDERED;
            order._buyer_Id = buyer_id;

            // -- get seller info --
            // order._seller_Id = ;

            // -- shipping --
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            order._tracking_number = new string(Enumerable.Repeat(chars, 20).Select(s => s[random.Next(s.Length)]).ToArray());
            // -- shiping end --

            cmd.CommandText = @"INSERT INTO `orders` (`items`, `total_cost`, `date_created`, `tracking_number`, `delivery_address`, `shipping_cost`, `status`, `buyer_id`, `seller_id`) VALUES (@items, @total_cost, @date_created, @tracking_number, @delivery_address, @shipping_cost, @status, @buyer_id, @seller_id);";

            BindParams(cmd, order);
            await cmd.ExecuteNonQueryAsync();
            order._Id = (int)cmd.LastInsertedId;
            return order;
        }

        public async Task<List<Order>> CreateOrders(int buyer_id, List<Order> orders)
        {
            List<Order> newOrders = new List<Order>();
            for (int i = 0; i < orders.Count; i++) {
                newOrders.Add(await Create(buyer_id, orders[i]));
            }

            return newOrders;
        }

        // get a user from the database by id
        public async Task<List<Order>> GetOrders(List<int> ids)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `orders` WHERE `id` IN {@id}";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    Value = string.Join(", ", ids)
                }
            );
            var result = await GetOrdersInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result : null;
        }

        // get a user from the database by id
        public async Task<List<Order>> GetOrders(Buyer buyer)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `orders` WHERE `buyer_id` = @id";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    Value = buyer._Id
                }
            );
            var result = await GetOrdersInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result : null;
        }

        public async Task<List<Order>> GetOrders(Seller seller)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `orders` WHERE `seller_id` = @id";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    Value = seller._Id
                }
            );
            var result = await GetOrdersInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result : null;
        }

        public async Task<Order> GetOrder(int id)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `orders` WHERE `id` IN {@id}";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    DbType = DbType.Int32,
                    Value = id
                }
            );
            var result = await GetOrdersInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }

        public async Task<IEnumerable<Order>> GetOrders()
        {
            var orders = new List<Order>();
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `orders`";

            return await GetOrdersInfo(await cmd.ExecuteReaderAsync());
        }

        private async Task<List<Order>> GetOrdersInfo(DbDataReader reader)
        {
            List<Item> items = null;
            Order order = null;
            var orders = new List<Order>();
            using (reader)
            {

                while (await reader.ReadAsync())
                {
                    try
                    {
                        items = JsonConvert.DeserializeObject<List<Item>>((string)reader["items"]);
                    }
                    catch (InvalidCastException ex)
                    {

                    }
                    try
                    {
                        items = JsonConvert.DeserializeObject<List<Item>>((string)reader["items"]);
                    }
                    catch (InvalidCastException ex)
                    {

                    }
                    switch ((OrderStatus)Enum.Parse(typeof(OrderStatus), (string)reader["status"]))
                    {
                        case (OrderStatus.DELIVERED):
                            order = new Order
                            {
                                _Id = (int)reader["id"],
                                _date_created = DateTime.Parse((string)reader["date_created"]),
                                _date_shipped = DateTime.Parse((string)reader["date_shipped"]),
                                _total_cost = (Double)reader["total_cost"],
                                _items = items,
                                _tracking_number = (string)reader["tracking_number"],
                                _delivery_address = (string)reader["delivery_address"],
                                _shipping_cost = (Double)reader["shipping_cost"],
                                _status = (OrderStatus)Enum.Parse(typeof(OrderStatus), (string)reader["status"]),
                                _date_delivered = DateTime.Parse((string)reader["date_delivered"]),
                            };
                            break;
                        case (OrderStatus.SHIPPED):
                            order = new Order
                            {
                                _Id = (int)reader["id"],
                                _date_created = DateTime.Parse((string)reader["date_created"]),
                                _date_shipped = DateTime.Parse((string)reader["date_shipped"]),
                                _total_cost = (Double)reader["total_cost"],
                                _items = items,
                                _tracking_number = (string)reader["tracking_number"],
                                _delivery_address = (string)reader["delivery_address"],
                                _shipping_cost = (Double)reader["shipping_cost"],
                                _status = (OrderStatus)Enum.Parse(typeof(OrderStatus), (string)reader["status"])
                            };
                        break;
                        default:
                            order = new Order
                            {
                                _Id = (int)reader["id"],
                                _date_created = reader.GetDateTime(2),
                                _total_cost = reader.GetDouble(3),
                                _items = items,
                                _tracking_number = (string)reader["tracking_number"],
                                _delivery_address = (string)reader["delivery_address"],
                                _shipping_cost = reader.GetDouble(4),
                                _status = (OrderStatus)Enum.Parse(typeof(OrderStatus), (string)reader["status"])
                            };
                        break;

                    }

                    orders.Add(order);
                }
            }
            return orders;
        }

        public async Task<Order> FulfillOrder(int sellerId, int orderId) {
            var order = GetOrder(orderId).Result;

            if (order == null)
            {
                throw new AppException("Order not found");
            }
            order._status = OrderStatus.SHIPPED;
            if (order._seller_Id != sellerId) {
                throw new AppException("Invalid! Not your order");
            }

            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `orders` SET `status` = @status WHERE `id` = @id;";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    DbType = DbType.Int32,
                    Value = orderId
                }
            );
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@status",
                    Value = "SHIPPED"
                }
            );
            BindId(cmd, order);
            await cmd.ExecuteNonQueryAsync();
            return order;
        }

        public async Task<OrderStatus> CancelOrder(int id)
        {
            var order = GetOrder(id).Result;

            if (order == null)
            {
                throw new AppException("Order not found");
            }
            switch (order._status)
            {
                case OrderStatus.SHIPPED:
                    throw new AppException("Unable to cancel order! Item has been shipped.");
                case OrderStatus.DELIVERED:
                    throw new AppException("Unable to cancel order! Item has been delivered.");
            }

            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `orders` SET `status` = @status WHERE `id` = @id;";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    DbType = DbType.Int32,
                    Value = id
                }
            );
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@status",
                    Value = "CANCELLED"
                }
            );
            BindId(cmd, order);
            await cmd.ExecuteNonQueryAsync();
            return order._status;
        }

        public async Task Delete(int id)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM `orders` WHERE `id` = @id;";
            BindId(cmd, id);
            await cmd.ExecuteNonQueryAsync();
        }

        private void BindId(MySqlCommand cmd, Order order)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = order._Id,
            });
        }

        private void BindId(MySqlCommand cmd, int id)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = id,
            });
        }
        private void BindParams(MySqlCommand cmd, Order order)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@items",
                DbType = DbType.String,
                Value = System.Text.Json.JsonSerializer.Serialize(order._items)
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@total_cost",
                DbType = DbType.String,
                Value = order._total_cost,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@date_created",
                DbType = DbType.String,
                Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@tracking_number",
                DbType = DbType.String,
                Value = order._tracking_number,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@delivery_address",
                DbType = (DbType)MySqlDbType.VarBinary,
                Value = order._delivery_address,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@shipping_cost",
                DbType = (DbType)MySqlDbType.VarBinary,
                Value = order._shipping_cost,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@status",
                DbType = DbType.String,
                Value = order._status.ToString(),
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@buyer_id",
                DbType = DbType.Int32,
                Value = order._buyer_Id,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@seller_id",
                DbType = DbType.Int32,
                Value = order._seller_Id,
            });
        }



    }
}