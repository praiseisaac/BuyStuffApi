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
    public interface IListingService
    {
        Task<Listing> Create(int buyer_id, string seller_name, Listing listing);
        Task<Listing> GetListing(int id);
        // Task<List<Listing>> GetListings(List<int> ids);
        Task<List<Listing>> GetListings(Seller seller);
        Task<IEnumerable<Listing>> GetListings();
        Task<Listing> Update(Listing listing);

        Task Delete(int id);
        Task<List<Listing>> GetListings(string name);
        Task<List<Listing>> GetListings(int id);
        Task<Seller> GetSeller(int id);
        Task<List<Listing>> GetListings(List<int> id);
    }

    public class ListingService : IListingService
    {
        internal AppDb Db { get; set; }
        private ISellerService _sellerService;
        public ListingService()
        {

        }

        // Constructor
        internal ListingService(AppDb db)
        {
            Db = db;
        }

        // used to create a listing
        public async Task<Listing> Create(int seller_id, string seller_name, Listing listing)
        {
            using var cmd = Db.Connection.CreateCommand();
            // listing._date_created = DateTime.Now;
            listing._status = ListingStatus.SCHEDULED;
            listing._seller_Id = seller_id;
            listing._seller_name = seller_name;

            cmd.CommandText = @"INSERT INTO `listings` (`item`, `date_created`, `title`, `description`, `price`, `date_modified`, `shipping_cost`, `seller_id`, `seller_name`, `status`) VALUES (@item, @date_created, @title, @description, @price, @date_modified, @shipping_cost, @seller_id, @seller_name`, @status);";
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@date_created",
                DbType = DbType.String,
                Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            });

            BindParams(cmd, listing);
            await cmd.ExecuteNonQueryAsync();
            listing._Id = (int)cmd.LastInsertedId;
            return listing;
        }

        public async Task<Listing> Update(Listing listing)
        {
            using var cmd = Db.Connection.CreateCommand();
            // listing._date_created = DateTime.Now;

            cmd.CommandText = @"UPDATE `listings` SET `item` = @item, `title` = @title, `description` = @description, `price` = @price, `date_modified` = @date_modified, `shipping_cost` = @shipping_cost, `seller_id` = @seller_id, `status` = @status WHERE `id` = @id;";


            BindParams(cmd, listing);
            await cmd.ExecuteNonQueryAsync();
            return listing;
        }

        // get a user from the database by id
        public async Task<List<Listing>> GetListings(int id)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `listings` WHERE `id` = @id";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    Value = id
                }
            );
            var result = await GetListingsInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result : null;
        }

        public async Task<List<Listing>> GetListings(List<int> id)
        {
            var listings = new List<Listing>();
            var temp = new Listing();;
            for (int i = 0; i < id.Count; i++) {
                temp = await GetListing(id[i]);
                listings.Add(temp);
            }
            return listings.Count > 0 ? listings : null;
        }

        // get a user from the database by id
        public async Task<List<Listing>> GetListings(string name)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `listings` WHERE `title` LIKE @name";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@name",
                    Value = "%" + name + "%"
                }
            );
            var result = await GetListingsInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result : null;
        }

        public async Task<List<Listing>> GetListings(Seller seller)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `listings` WHERE `seller_id` = @id";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    Value = seller._Id
                }
            );
            var result = await GetListingsInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result : null;
        }

        public async Task<Listing> GetListing(int id)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `listings` WHERE `id` = @id";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    DbType = DbType.Int32,
                    Value = id
                }
            );
            var result = await GetListingsInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }

        public async Task<Seller> GetSeller(int id)
        {
            Listing listing = await GetListing(id);
            _sellerService = new SellerService(Db);
            Seller seller = await _sellerService.GetSeller(listing._seller_Id);
            return seller;
        }

        public async Task<IEnumerable<Listing>> GetListings()
        {
            var listings = new List<Listing>();
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `listings`";

            return await GetListingsInfo(await cmd.ExecuteReaderAsync());
        }

        private async Task<List<Listing>> GetListingsInfo(DbDataReader reader)
        {
            Item item = null;
            Listing listing = null;
            var listings = new List<Listing>();
            List<string> images = null;
            using (reader)
            {

                while (await reader.ReadAsync())
                {
                    try
                    {
                        item = JsonConvert.DeserializeObject<Item>((string)reader["item"]);
                    }
                    catch (InvalidCastException ex)
                    {

                    }

                    try {
                        images = JsonConvert.DeserializeObject<List<string>>((string)reader["images"]);
                    }
                    catch (InvalidCastException ex)
                    {

                    }
                    listing = new Listing
                    {
                        _Id = (int)reader["id"],
                        _date_created = reader.GetDateTime(0),
                        _price = reader.GetDouble(6),
                        _item = item,
                        _title = (string)reader["title"],
                        _description = (string)reader["description"],
                        _shipping_cost = reader.GetDouble(7),
                        _status = (ListingStatus)Enum.Parse(typeof(ListingStatus), (string)reader["status"]),
                        _date_modified = reader.GetDateTime(1),
                        _seller_Id = (int)reader["seller_id"],
                        _images = images,
                        _rating = (int) reader["rating"],
                        _seller_name = (string)reader["seller_name"]
                    };

                    listings.Add(listing);
                }
            }
            return listings;
        }



        public async Task Delete(int id)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM `listings` WHERE `id` = @id;";
            BindId(cmd, id);
            await cmd.ExecuteNonQueryAsync();
        }

        private void BindId(MySqlCommand cmd, Listing listing)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = listing._Id,
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
        private void BindParams(MySqlCommand cmd, Listing listing)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@item",
                DbType = DbType.String,
                Value = System.Text.Json.JsonSerializer.Serialize(listing._item)
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@price",
                DbType = DbType.String,
                Value = listing._price,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@title",
                DbType = DbType.String,
                Value = listing._title,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@description",
                DbType = DbType.String,
                Value = listing._description,
            });

            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@date_modified",
                DbType = DbType.String,
                Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@shipping_cost",
                DbType = (DbType)MySqlDbType.VarBinary,
                Value = listing._shipping_cost,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@status",
                DbType = DbType.String,
                Value = listing._status.ToString(),
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@seller_id",
                DbType = DbType.Int32,
                Value = listing._seller_Id,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@seller_name",
                DbType = DbType.String,
                Value = listing._seller_name,
            });
        }



    }
}
