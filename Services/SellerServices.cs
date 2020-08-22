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
    public interface ISellerService
    {
        Task<Seller> Create(Seller seller, string password);
        Task<Seller> Authenticate(string seller, string password);
        Task<Seller> GetSeller(int id);
        Task<Seller> GetSeller(string email);
        Task<IEnumerable<Seller>> GetSellers();
        Task AddListing(int sellerId, int listingId);
        Task RemoveListing(int sellerId, int listingId);
        Task AddItem(int id, int itemId);
        Task Update(Seller seller, string password);
        Task Delete(int id);
        Task RemoveItem(int id, int itemId);
        Task AddOrder(int id, int orderId);
    }

    public class SellerService : ISellerService
    {
        internal AppDb Db { get; set; }
        public SellerService()
        {

        }

        // Constructor
        internal SellerService(AppDb db)
        {
            Db = db;
        }

        // used to create a user
        public async Task<Seller> Create(Seller seller, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new AppException("Password is required");

            if (CheckEmailAvalability(seller).Result)
                throw new AppException("Email already registered");

            byte[] password_hash, password_salt;
            CreatePasswordHash(password, out password_hash, out password_salt);

            seller._password_hash = password_hash;
            seller._password_salt = password_salt;

            using var cmd = Db.Connection.CreateCommand();

            cmd.CommandText = @"INSERT INTO `sellers` (`email`, `username`, `first_name`, `last_name`, `password_hash`, `password_salt`, `address`) VALUES (@email, @username, @first_name, @last_name, @password_hash, @password_salt, @address);";
            // seller._Id = (int) cmd.LastInsertedId;
            BindParams(cmd, seller);
            await cmd.ExecuteNonQueryAsync();

            return seller;
        }

        // Authenticates the user when logging in
        public async Task<Seller> Authenticate(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `sellers` WHERE `email` = @email";

            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@email",
                    DbType = DbType.String,
                    Value = email,
                }
            );

            var result = await GetSellersInfo(await cmd.ExecuteReaderAsync());
            var seller = result.SingleOrDefault(x => x._email == email);

            if (seller == null)
                return null;

            if (!VerifyPasswordHash(password, seller._password_hash, seller._password_salt))
                return null;

            return seller;
        }

        // get a user from the database by id
        public async Task<Seller> GetSeller(int id)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `sellers` WHERE `id` = @id";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    DbType = DbType.Int32,
                    Value = id,
                }
            );
            var result = await GetSellersInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }


        public async Task AddListing(int id, int listingId)
        {
            var seller = GetSeller(id).Result;

            if (seller == null)
            {
                throw new AppException("User not found");
            }
            try
            {
                seller._listings.Add(listingId);
            }
            catch (NullReferenceException ex)
            {
                seller._listings = new List<int>();
                seller._listings.Add(listingId);
            }


            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `sellers` SET `listings` = @listings WHERE `id` = @id;";
            BindId(cmd, seller);
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@listings",
                    DbType = DbType.String,
                    Value = System.Text.Json.JsonSerializer.Serialize(seller._listings)
                }
            );
            await cmd.ExecuteNonQueryAsync();
        }


        public async Task AddOrder(int id, int orderId)
        {
            var seller = GetSeller(id).Result;

            if (seller == null)
            {
                throw new AppException("User not found");
            }
            try
            {
                seller._orders.Add(orderId);
            }
            catch (NullReferenceException ex)
            {
                seller._orders = new List<int>();
                seller._orders.Add(orderId);
            }


            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `sellers` SET `orders` = @orders WHERE `id` = @id;";
            BindId(cmd, seller);
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@orders",
                    DbType = DbType.String,
                    Value = System.Text.Json.JsonSerializer.Serialize(seller._orders)
                }
            );
            await cmd.ExecuteNonQueryAsync();
        }


        public async Task RemoveListing(int id, int listingId)
        {
            var seller = GetSeller(id).Result;

            if (seller == null)
            {
                throw new AppException("User not found");
            }
            try
            {
                seller._listings.Remove(listingId);
            }
            catch (NullReferenceException ex)
            {
                
            }


            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `sellers` SET `listings` = @listings WHERE `id` = @id;";
            BindId(cmd, seller);
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@listings",
                    DbType = DbType.String,
                    Value = System.Text.Json.JsonSerializer.Serialize(seller._listings)
                }
            );
            await cmd.ExecuteNonQueryAsync();
        }

        // NON FUNCTIONAL
        public async Task AddItem(int id, int itemId)
        {
            var seller = GetSeller(id).Result;

            if (seller == null)
            {
                throw new AppException("User not found");
            }
            try
            {
                seller._items.Add(itemId);
            }
            catch (NullReferenceException ex)
            {
                seller._items = new List<int>();
                seller._items.Add(itemId);
            }


            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `sellers` SET `items` = @items WHERE `id` = @id;";
            BindId(cmd, seller);
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@items",
                    DbType = DbType.String,
                    Value = System.Text.Json.JsonSerializer.Serialize(seller._items)
                }
            );
            await cmd.ExecuteNonQueryAsync();
        }


        public async Task RemoveItem(int id, int itemId)
        {
            var seller = GetSeller(id).Result;

            if (seller == null)
            {
                throw new AppException("User not found");
            }
            try
            {
                seller._items.Remove(itemId);
            }
            catch (NullReferenceException ex)
            {
                throw new AppException("Item not found");
            }


            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `sellers` SET `items` = @items WHERE `id` = @id;";
            BindId(cmd, seller);
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@items",
                    DbType = DbType.String,
                    Value = System.Text.Json.JsonSerializer.Serialize(seller._items)
                }
            );
            await cmd.ExecuteNonQueryAsync();
        }


        // gets a user from the database by email
        public async Task<Seller> GetSeller(string email)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `id`, `email`, `username`, `first_name`, `last_name`, `address` FROM `sellers` WHERE `email` = @email";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@email",
                    DbType = DbType.String,
                    Value = email,
                }
            );
            var result = await GetSellersInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }

        public async Task<IEnumerable<Seller>> GetSellers()
        {
            var sellers = new List<Seller>();
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `sellers`";

            return await ReadAllAsync(await cmd.ExecuteReaderAsync());
        }



        // reads all sellers from the database for authentication
        private async Task<List<Seller>> GetSellers(DbDataReader reader)
        {
            var sellers = new List<Seller>();
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    byte[] pass_hash = (byte[])reader["password_hash"];
                    byte[] pass_salt = (byte[])reader["password_salt"];

                    // long retval_hash = reader.GetBytes(2, 0, pass_hash, 0, 500);
                    // long retval_salt = reader.GetBytes(11, 0, pass_hash, 0, 500);



                    var seller = new Seller
                    {
                        _Id = reader.GetInt32(0),
                        _email = reader.GetString(1),
                        _password_hash = pass_hash,
                        _password_salt = pass_salt,
                        _username = (string)reader["username"],
                        _first_name = (string)reader["first_name"],
                        _last_name = (string)reader["last_name"],
                        // _password_hash = (byte[])(Convert.FromBase64String(reader.GetByte(2).ToString())),
                        // _password_salt = (byte[])(Convert.FromBase64String(reader.GetByte(11).ToString()))
                    };
                    sellers.Add(seller);
                }
            }
            return sellers;
        }

        private async Task<List<Seller>> GetSellersInfo(DbDataReader reader)
        {
            var sellers = new List<Seller>();
            using (reader)
            {
                while (await reader.ReadAsync())
                {

                    List<int> orders = null;
                    List<int> returns = null;
                    Payment payment = null;
                    // long retval_hash = reader.GetBytes(2, 0, pass_hash, 0, 500);
                    // long retval_salt = reader.GetBytes(11, 0, pass_hash, 0, 500);

                    try
                    {
                        returns = JsonConvert.DeserializeObject<List<int>>((string)reader["returns"]);
                    }
                    catch (InvalidCastException ex)
                    {

                    }

                    try
                    {
                        orders = JsonConvert.DeserializeObject<List<int>>((string)reader["orders"]);
                    }
                    catch (InvalidCastException ex)
                    {

                    }


                    try
                    {
                        payment = JsonConvert.DeserializeObject<Payment>((string)reader["payment"]);
                    }
                    catch (InvalidCastException ex)
                    {

                    }


                    // long retval_hash = reader.GetBytes(2, 0, pass_hash, 0, 500);
                    // long retval_salt = reader.GetBytes(11, 0, pass_hash, 0, 500);



                    var seller = new Seller
                    {
                        _Id = reader.GetInt32(0),
                        _email = reader.GetString(1),
                        _username = (string)reader["username"],
                        _first_name = (string)reader["first_name"],
                        _last_name = (string)reader["last_name"],
                        _orders = orders,
                        _payment = payment,
                        _address = (string)reader["address"]
                        // _password_hash = (byte[])(Convert.FromBase64String(reader.GetByte(2).ToString())),
                        // _password_salt = (byte[])(Convert.FromBase64String(reader.GetByte(11).ToString()))
                    };
                    sellers.Add(seller);
                }
            }
            return sellers;
        }

        // checks if an email is available during signup
        private async Task<bool> CheckEmailAvalability(Seller seller)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `id`, `email`, `password_hash`, `password_salt` FROM `sellers` WHERE `email` = @email";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@email",
                    DbType = DbType.String,
                    Value = seller._email,
                }
            );

            var sellers = new List<Seller>();
            var reader = await cmd.ExecuteReaderAsync();
            await using (reader)
            {
                while (await reader.ReadAsync())
                {
                    byte[] pass_hash = (byte[])reader["password_hash"];
                    byte[] pass_salt = (byte[])reader["password_salt"];

                    // long retval_hash = reader.GetBytes(2, 0, pass_hash, 0, 500);
                    // long retval_salt = reader.GetBytes(11, 0, pass_hash, 0, 500);



                    var new_seller = new Seller
                    {
                        _Id = reader.GetInt32(0),
                        _email = reader.GetString(1),
                        _password_hash = pass_hash,
                        _password_salt = pass_salt,
                        // _password_hash = (byte[])(Convert.FromBase64String(reader.GetByte(2).ToString())),
                        // _password_salt = (byte[])(Convert.FromBase64String(reader.GetByte(11).ToString()))
                    };
                    sellers.Add(new_seller);
                }
            }

            var result = sellers.Count;
            return result > 0;
        }


        // creates a pasword hash that will be stored in the database
        private static void CreatePasswordHash(string password, out byte[] password_hash, out byte[] password_salt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                password_salt = hmac.Key;
                password_hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        // used to verify the password of the user
        private static bool VerifyPasswordHash(string password, byte[] password_hash, byte[] password_salt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (password_hash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (password_salt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(password_salt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != password_hash[i]) return false;
                }
            }

            return true;
        }

        public async Task Update(Seller newSeller, string password)
        {
            var seller = GetSeller(newSeller._Id).Result;

            if (newSeller == null)
            {
                throw new AppException("User not found");
            }

            if (seller._email != newSeller._email)
            {
                if (GetSellers().Result.Any(x => x._email == newSeller._email))
                    throw new AppException("Email " + newSeller._email + " is already registered");
            }

            seller._first_name = newSeller._first_name;
            seller._last_name = newSeller._last_name;
            seller._username = newSeller._username;
            seller._address = newSeller._address;

            if (!string.IsNullOrWhiteSpace(password))
            {
                byte[] password_hash, password_salt;
                CreatePasswordHash(password, out password_hash, out password_salt);

                seller._password_hash = password_hash;
                seller._password_salt = password_salt;
            }

            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `sellers` SET `email` = @email, `username` = @username, `first_name` = @first_name, `last_name` = @last_name, `password_hash` = @password_hash, `password_salt` = @password_salt, `address` = @address WHERE `id` = @id;";
            BindParams(cmd, seller);
            BindId(cmd, seller);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task Delete(int id)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM `sellers` WHERE `id` = @id;";
            BindId(cmd, id);
            await cmd.ExecuteNonQueryAsync();
        }

        private void BindId(MySqlCommand cmd, Seller seller)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = seller._Id,
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
        private void BindParams(MySqlCommand cmd, Seller seller)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@email",
                DbType = DbType.String,
                Value = seller._email,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@username",
                DbType = DbType.String,
                Value = seller._username,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@first_name",
                DbType = DbType.String,
                Value = seller._first_name,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@last_name",
                DbType = DbType.String,
                Value = seller._last_name,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@password_hash",
                DbType = (DbType)MySqlDbType.VarBinary,
                Value = seller._password_hash,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@password_salt",
                DbType = (DbType)MySqlDbType.VarBinary,
                Value = seller._password_salt,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@address",
                DbType = DbType.String,
                Value = seller._address,
            });
        }

        private async Task<List<Seller>> ReadAllAsync(DbDataReader reader)
        {
            var sellers = new List<Seller>();

            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    // var items = JsonConvert.DeserializeObject<Carts>(reader.GetString(8)).Ids.Select(p => p.Id).ToList();
                    // var quantities = JsonConvert.DeserializeObject<Carts>(reader.GetString(8)).Ids.Select(p => p.quantity).ToList();
                    // var orders = JsonConvert.DeserializeObject<OrderIDs>(reader.GetString(9)).Ids.Select(p => p.Id).ToList();

                    var seller = new Seller
                    {
                        _Id = (int)reader["id"],
                        _email = (string)reader["email"],
                        _username = (string)reader["username"],
                        _first_name = (string)reader["first_name"],
                        _last_name = (string)reader["last_name"],
                        _address = (string)reader["address"],
                        // _cart = CombineWith(items, quantities).ToList(),
                        // _orders = orders,
                    };
                    sellers.Add(seller);
                }
            }
            return sellers;
        }





        // public static IEnumerable<Tuple<T, U>> CombineWith<T, U>(this IEnumerable<T> first, IEnumerable<U> second)
        // {
        //     using (var firstEnumerator = first.GetEnumerator())
        //     using (var secondEnumerator = second.GetEnumerator())
        //     {
        //         bool hasFirst = true;
        //         bool hasSecond = true;

        //         while (
        //             // Only call MoveNext if it didn't fail last time.
        //             (hasFirst && (hasFirst = firstEnumerator.MoveNext()))
        //             | // WARNING: Do NOT change to ||.
        //             (hasSecond && (hasSecond = secondEnumerator.MoveNext()))
        //             )
        //         {
        //             yield return Tuple.Create(
        //                     hasFirst ? firstEnumerator.Current : default(T),
        //                     hasSecond ? secondEnumerator.Current : default(U)
        //                 );
        //         }
        //     }
        // }

        // for future might want to add the ability to acall all/multiple users
    }

    // public class Cart
    // {
    //     public string Id { get; set; }
    //     public int quantity { get; set; }
    // }

    // public class Carts
    // {
    //     public List<Cart> Ids { get; set; }
    // }

    // public class OrderId
    // {
    //     public int Id { get; set; }
    // }

    // public class OrderIDs
    // {
    //     public List<OrderId> Ids { get; set; }
    // }
}