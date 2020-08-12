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
        Task<Order> Create(Order order);
        Task<Order> GetOrder(int id);
        Task<Order> GetOrders(List<int> ids);

        Task CancelOrder(int id);
        Task Delete(int id);
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
        public async Task<Order> Create(Order order)
        {
            using var cmd = Db.Connection.CreateCommand();

            cmd.CommandText = @"INSERT INTO `orders` (`items`, `total_cost`, `date_created`, `tracking_number`, `delivery_address`, `shipping_cost`, `status`) VALUES (@items, @total_cost, @date_created, @tracking_number, @delivery_address, @shipping_cost, @status);";
            // order._Id = (int) cmd.LastInsertedId;
            BindParams(cmd, order);
            await cmd.ExecuteNonQueryAsync();

            return order;
        }

        // get a user from the database by id
        public async Task<Order> GetOrders(List<int> ids)
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
            return result.Count > 0 ? result[0] : null;
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
            try
            {
                items = JsonConvert.DeserializeObject<List<Item>>((string)reader["items"]);
            }
            catch (InvalidCastException ex)
            {

            }
            var orders = new List<Order>();
            using (reader)
            {
                while (await reader.ReadAsync())
                {

                    var order = new Order
                    {
                        _Id = (int)reader["id"],
                        _date_created = (DateTime)reader["date_created"],
                        _date_shipped = (DateTime)reader["date_shipped"],
                        _total_cost = (Double)reader["total_cost"],
                        _items = items,
                        _tracking_number = (string)reader["tracking_number"],
                        _delivery_address = (string)reader["address"],
                        _shipping_cost = (Double)reader["shipping_cost"],
                        _status = (Status)Enum.Parse(typeof(Status), (string)reader["status"]),
                        _date_delivered = (DateTime)reader["date_delivered"]
                    };
                    orders.Add(order);
                }
            }
            return orders;
        }


        public async Task CancelOrder(int id)
        {
            var order = GetOrder(id).Result;

            if (order == null)
            {
                throw new AppException("Order not found");
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
                Value = order._date_created,
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
        }



    }
}