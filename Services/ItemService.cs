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
    public interface IItemService
    {
        Task<Item> Create(int item_id, Item item);
        Task<Item> GetItem(int id);
        Task<Item> GetItems(List<int> ids);
        // Task<List<Item>> GetSellerItems(int id);
        Task<IEnumerable<Item>> GetItems();
        Task UpdateQuantity(Item item);
        Task<IEnumerable<Item>> GetItems(Seller seller);
        Task Delete(int id);
        Task<Item> Update(Item newItem);
    }

    public class ItemService : IItemService
    {
        internal AppDb Db { get; set; }
        public ItemService()
        {

        }

        // Constructor
        internal ItemService(AppDb db)
        {
            Db = db;
        }

        // used to create a item item
        public async Task<Item> Create(int sellerId, Item item)
        {
            using var cmd = Db.Connection.CreateCommand();
            item._seller_id = sellerId;

            cmd.CommandText = @"INSERT INTO `items` (`name`, `quantity`, `seller_id`) VALUES (@name, @quantity, @seller_id);";

            BindParams(cmd, item);
            await cmd.ExecuteNonQueryAsync();
            item._Id = (int)cmd.LastInsertedId;
            return item;
        }

        // get a user from the database by id
        public async Task<Item> GetItems(List<int> ids)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `items` WHERE `id` IN {@id}";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    Value = string.Join(", ", ids)
                }
            );
            var result = await GetItemsInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }

        // public async Task<List<Item>> GetSellerItems(List<int> ids)
        // {
        //     using var cmd = Db?.Connection.CreateCommand();
        //     cmd.CommandText = @"SELECT * FROM `items` WHERE `seller_id` IN {@id}";
        //     cmd.Parameters.Add(
        //         new MySqlParameter
        //         {
        //             ParameterName = "@id",
        //             DbType = DbType.Int32,
        //             Value = string.Join(", ", ids)
        //         }
        //     );
        //     var result = await GetItemsInfo(await cmd.ExecuteReaderAsync());
        //     return result.Count > 0 ? result : null;
        // }

        public async Task<Item> GetItem(int id)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `items` WHERE `id` IN {@id}";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    DbType = DbType.Int32,
                    Value = id
                }
            );
            var result = await GetItemsInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }

        public async Task<IEnumerable<Item>> GetItems()
        {
            var items = new List<Item>();
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `items`";

            return await GetItemsInfo(await cmd.ExecuteReaderAsync());
        }

        public async Task<IEnumerable<Item>> GetItems(Seller seller)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `items` WHERE `seller_id` = @id";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    DbType = DbType.Int32,
                    Value = seller._Id
                }
            );

            return await GetItemsInfo(await cmd.ExecuteReaderAsync());
        }

        private async Task<List<Item>> GetItemsInfo(DbDataReader reader)
        {
            var items = new List<Item>();
            using (reader)
            {
                while (await reader.ReadAsync())
                {

                    var item = new Item
                    {
                        _Id = (int)reader["id"],
                        _name = (string) reader["name"],
                        _quantity = (int) reader["quantity"],
                        _seller_id = (int) reader["seller_id"]
                    };
                    items.Add(item);
                }
            }
            return items;
        }

        public async Task<Item> Update(Item newItem)
        {
            var item = GetItem(newItem._Id).Result;

            if (newItem == null) {
                throw new AppException("Item not found");
            }
            item._name = newItem._name;
            item._quantity = newItem._quantity;

            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `name` = @name, `quantity` = @quantity WHERE `id` = @id;";
            BindParams(cmd, item);
            BindId(cmd, item);
            await cmd.ExecuteNonQueryAsync();
            return item;
        }

        public async Task UpdateQuantity(Item newItem)
        {
            var item = GetItem(newItem._Id).Result;

            if (newItem == null) {
                throw new AppException("Item not found");
            }
            item._quantity = newItem._quantity;

            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `quantity` = @quantity WHERE `id` = @id;";
            BindParams(cmd, item);
            BindId(cmd, item);
            await cmd.ExecuteNonQueryAsync();
        }


        public async Task Delete(int id)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM `items` WHERE `id` = @id;";
            BindId(cmd, id);
            await cmd.ExecuteNonQueryAsync();
        }

        private void BindId(MySqlCommand cmd, Item item)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = item._Id,
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
        private void BindParams(MySqlCommand cmd, Item item)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@name",
                DbType = DbType.String,
                Value = item._name
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@quantity",
                DbType = DbType.Int32,
                Value = item._quantity,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@seller_id",
                DbType = DbType.Int32,
                Value = item._seller_id,
            });
        }



    }
}