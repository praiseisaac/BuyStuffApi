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
using Newtonsoft.Json.Linq;

namespace BuyStuffApi.Services
{
    public interface IBuyerService
    {
        Task<Buyer> Create(Buyer buyer, string password);
        Task<Buyer> Authenticate(string buyer, string password);
        Task<Buyer> GetBuyer(int id);
        Task<Buyer> GetBuyer(string email);
        Task<IEnumerable<Buyer>> GetBuyers();

        Task Update(Buyer buyer, string password);
        Task Delete(int id);
        Task AddBuyerOrder(int id, int orderId);
        Task AddBuyerReturn(int id, int orderId);
    }

    public class BuyerService : IBuyerService
    {
        internal AppDb Db { get; set; }
        public BuyerService()
        {

        }

        // Constructor
        internal BuyerService(AppDb db)
        {
            Db = db;
        }

        // used to create a user
        public async Task<Buyer> Create(Buyer buyer, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new AppException("Password is required");

            if (CheckEmailAvalability(buyer).Result)
                throw new AppException("Email already registered");

            byte[] password_hash, password_salt;
            CreatePasswordHash(password, out password_hash, out password_salt);

            buyer._password_hash = password_hash;
            buyer._password_salt = password_salt;

            using var cmd = Db.Connection.CreateCommand();

            cmd.CommandText = @"INSERT INTO `buyers` (`email`, `username`, `first_name`, `last_name`, `password_hash`, `password_salt`, `address`) VALUES (@email, @username, @first_name, @last_name, @password_hash, @password_salt, @address);";
            
            BindParams(cmd, buyer);
            await cmd.ExecuteNonQueryAsync();
            buyer._Id = (int) cmd.LastInsertedId;
            return buyer;
        }

        // Authenticates the user when logging in
        public async Task<Buyer> Authenticate(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `buyers` WHERE `email` = @email";

            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@email",
                    DbType = DbType.String,
                    Value = email,
                }
            );

            var result = await GetBuyers(await cmd.ExecuteReaderAsync());
            var buyer = result.SingleOrDefault(x => x._email == email);

            if (buyer == null)
                return null;

            if (!VerifyPasswordHash(password, buyer._password_hash, buyer._password_salt))
                return null;

            return buyer;
        }

        // get a user from the database by id
        public async Task<Buyer> GetBuyer(int id)
        {
            using var cmd = Db?.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `buyers` WHERE `id` = @id";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@id",
                    DbType = DbType.Int32,
                    Value = id,
                }
            );
            var result = await GetBuyersInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }

        // gets a user from the database by email
        public async Task<Buyer> GetBuyer(string email)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `buyers` WHERE `email` = @email";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@email",
                    DbType = DbType.String,
                    Value = email,
                }
            );
            var result = await GetBuyersInfo(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }

        public async Task<IEnumerable<Buyer>> GetBuyers()
        {
            var buyers = new List<Buyer>();
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT * FROM `buyers`";

            return await ReadAllAsync(await cmd.ExecuteReaderAsync());
        }

        // reads all buyers from the database for authentication
        private async Task<List<Buyer>> GetBuyers(DbDataReader reader)
        {
            var buyers = new List<Buyer>();
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    byte[] pass_hash = (byte[])reader["password_hash"];
                    byte[] pass_salt = (byte[])reader["password_salt"];

                    // long retval_hash = reader.GetBytes(2, 0, pass_hash, 0, 500);
                    // long retval_salt = reader.GetBytes(11, 0, pass_hash, 0, 500);



                    var buyer = new Buyer
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
                    buyers.Add(buyer);
                }
            }
            return buyers;
        }
        // -- NEW --








        public async Task AddBuyerOrder(int id, int orderId)
        {
            var buyer = GetBuyer(id).Result;

            if (buyer == null)
            {
                throw new AppException("User not found");
            }
            try {
                buyer._orders.Add(orderId);
            } catch (NullReferenceException ex) {
                buyer._orders = new List<int>();
                buyer._orders.Add(orderId);
            }
            

            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `buyers` SET `orders` = @orders WHERE `id` = @id;";
            BindId(cmd, buyer);
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@orders",
                    DbType = DbType.String,
                    Value = System.Text.Json.JsonSerializer.Serialize(buyer._orders)
                }
            );
            await cmd.ExecuteNonQueryAsync();
        }


        public async Task AddBuyerReturn(int id, int orderId)
        {
            var buyer = GetBuyer(id).Result;

            if (buyer == null)
            {
                throw new AppException("User not found");
            }
            try {
                buyer._returns.Add(orderId);
            } catch (NullReferenceException ex) {
                buyer._returns = new List<int>();
                buyer._returns.Add(orderId);
            }
            

            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `buyers` SET `returns` = @returns WHERE `id` = @id;";
            BindId(cmd, buyer);
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@returns",
                    DbType = DbType.String,
                    Value = System.Text.Json.JsonSerializer.Serialize(buyer._returns)
                }
            );
            await cmd.ExecuteNonQueryAsync();
        }









        // -- NEW END --
        private async Task<List<Buyer>> GetBuyersInfo(DbDataReader reader)
        {
            var buyers = new List<Buyer>();
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    List<Tuple<int, int>> cart = null;
                    List<int> orders = null;
                    List<int> returns = null;
                    Payment payment = null;
                    // long retval_hash = reader.GetBytes(2, 0, pass_hash, 0, 500);
                    // long retval_salt = reader.GetBytes(11, 0, pass_hash, 0, 500);
                    try {
                        var items = JsonConvert.DeserializeObject<Carts>((string)reader["cart"]).Ids.Select(p => p.Id).ToList();
                        var quantities = JsonConvert.DeserializeObject<Carts>((string)reader["cart"]).Ids.Select(p => p.quantity).ToList();
                        cart = Help.CombineWith(items, quantities).ToList();
                    } catch (InvalidCastException ex) {

                    }

                    try {
                        returns = JsonConvert.DeserializeObject<List<int>>((string)reader["returns"]);
                    } catch (InvalidCastException ex) {

                    }

                    try {
                        orders = JsonConvert.DeserializeObject<List<int>>((string)reader["orders"]);
                    } catch (InvalidCastException ex) {

                    }
                    
                    
                    try {
                        payment = JsonConvert.DeserializeObject<Payment>((string)reader["payment"]);
                    } catch (InvalidCastException ex) {

                    }
                    


                    var buyer = new Buyer
                    {
                        _Id = reader.GetInt32(0),
                        _email = reader.GetString(1),
                        _username = (string)reader["username"],
                        _first_name = (string)reader["first_name"],
                        _last_name = (string)reader["last_name"],
                        _orders = orders,
                        _cart = cart,
                        _returns = returns,
                        _payment = payment,
                        // _password_hash = (byte[])(Convert.FromBase64String(reader.GetByte(2).ToString())),
                        // _password_salt = (byte[])(Convert.FromBase64String(reader.GetByte(11).ToString()))
                    };
                    buyers.Add(buyer);
                }
            }
            return buyers;
        }

        // checks if an email is available during signup
        private async Task<bool> CheckEmailAvalability(Buyer buyer)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `id`, `email`, `password_hash`, `password_salt` FROM `buyers` WHERE `email` = @email";
            cmd.Parameters.Add(
                new MySqlParameter
                {
                    ParameterName = "@email",
                    DbType = DbType.String,
                    Value = buyer._email,
                }
            );
            var result = await GetBuyers(await cmd.ExecuteReaderAsync());
            return result.Count > 0;
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

        public async Task Update(Buyer newBuyer, string password)
        {
            var buyer = GetBuyer(newBuyer._Id).Result;

            if (newBuyer == null)
            {
                throw new AppException("User not found");
            }

            if (buyer._email != newBuyer._email)
            {
                if (GetBuyers().Result.Any(x => x._email == newBuyer._email))
                    throw new AppException("Email " + newBuyer._email + " is already registered");
            }

            buyer._first_name = newBuyer._first_name;
            buyer._last_name = newBuyer._last_name;
            buyer._username = newBuyer._username;
            buyer._address = newBuyer._address;

            if (!string.IsNullOrWhiteSpace(password))
            {
                byte[] password_hash, password_salt;
                CreatePasswordHash(password, out password_hash, out password_salt);

                buyer._password_hash = password_hash;
                buyer._password_salt = password_salt;
            }

            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `buyers` SET `email` = @email, `username` = @username, `first_name` = @first_name, `last_name` = @last_name, `password_hash` = @password_hash, `password_salt` = @password_salt, `address` = @address WHERE `id` = @id;";
            BindParams(cmd, buyer);
            BindId(cmd, buyer);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task Delete(int id)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM `buyers` WHERE `id` = @id;";
            BindId(cmd, id);
            await cmd.ExecuteNonQueryAsync();
        }

        private void BindId(MySqlCommand cmd, Buyer buyer)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = buyer._Id,
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
        private void BindParams(MySqlCommand cmd, Buyer buyer)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@email",
                DbType = DbType.String,
                Value = buyer._email,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@username",
                DbType = DbType.String,
                Value = buyer._username,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@first_name",
                DbType = DbType.String,
                Value = buyer._first_name,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@last_name",
                DbType = DbType.String,
                Value = buyer._last_name,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@password_hash",
                DbType = (DbType)MySqlDbType.VarBinary,
                Value = buyer._password_hash,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@password_salt",
                DbType = (DbType)MySqlDbType.VarBinary,
                Value = buyer._password_salt,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@address",
                DbType = DbType.String,
                Value = buyer._address,
            });
        }

        private async Task<List<Buyer>> ReadAllAsync(DbDataReader reader)
        {
            var buyers = new List<Buyer>();

            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    // var items = JsonConvert.DeserializeObject<Carts>(reader.GetString(8)).Ids.Select(p => p.Id).ToList();
                    // var quantities = JsonConvert.DeserializeObject<Carts>(reader.GetString(8)).Ids.Select(p => p.quantity).ToList();
                    // var orders = JsonConvert.DeserializeObject<OrderIDs>(reader.GetString(9)).Ids.Select(p => p.Id).ToList();

                    var buyer = new Buyer
                    {
                        _Id = reader.GetInt32(0),
                        _email = reader.GetString(1),
                        _username = reader.GetString(3),
                        _first_name = reader.GetString(4),
                        _last_name = reader.GetString(5),
                        _address = reader.GetString(6),
                        // _cart = CombineWith(items, quantities).ToList(),
                        // _orders = orders,
                    };
                    buyers.Add(buyer);
                }
            }
            return buyers;
        }



        // for future might want to add the ability to acall all/multiple users
    }

    public static class Help
    {
        public static IEnumerable<Tuple<T, U>> CombineWith<T, U>(this IEnumerable<T> first, IEnumerable<U> second)
        {
            using (var firstEnumerator = first.GetEnumerator())
            using (var secondEnumerator = second.GetEnumerator())
            {
                bool hasFirst = true;
                bool hasSecond = true;

                while (
                    // Only call MoveNext if it didn't fail last time.
                    (hasFirst && (hasFirst = firstEnumerator.MoveNext()))
                    | // WARNING: Do NOT change to ||.
                    (hasSecond && (hasSecond = secondEnumerator.MoveNext()))
                    )
                {
                    yield return Tuple.Create(
                            hasFirst ? firstEnumerator.Current : default(T),
                            hasSecond ? secondEnumerator.Current : default(U)
                        );
                }
            }
        }
    }


    public class Cart
    {
        public int Id { get; set; }
        public int quantity { get; set; }
    }

    public class Carts
    {
        public List<Cart> Ids { get; set; }
    }

    public class OrderId
    {
        public int Id { get; set; }
    }

    public class OrderIDs
    {
        public List<OrderId> Ids { get; set; }
    }
}