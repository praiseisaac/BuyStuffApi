using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using System.Collections.Generic;

namespace BuyStuffApi
{
    public class Buyer
    {
        public int _Id { get; set; }
        public string _email { get; set; }
        public string _username { get; set; }
        public string _first_name { get; set; }
        public string _last_name { get; set; }
        public string _password_hash { get; set; }
        public string _address { get; set; }
        public List<Tuple<string, int>> _cart { get; set; }
        public List<Order> _orders { get; set; }
        public List<Order> _returns { get; set; }
        public List<Tuple<string, string, int, int, int>> _payment { get; set; }
        public byte[] _password_salt { get; set; }

        internal AppDb Db { get; set; }

        public Buyer()
        {

        }

        internal Buyer(AppDb db) {
            Db = db;
        }

        public async Task InsertAsync() {
            using var cmd = Db.Connection.CreateCommand();

            cmd.CommandText = @"INSERT INTO `buyers` (`email`, `username`, `first_name`, `last_name`, `password_hash`, `password_salt`, `address`) VALUES (@email, @username, @first_name, @last_name, @password_hash, @passsword_salt, @adress);";
            BindParams(cmd);
            await cmd.ExecuteNonQueryAsync();
            _Id = (int) cmd.LastInsertedId;
        }

        public async Task UpdateAsync() {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `buyers` SET `email` = @email, `username` = @username, `first_name` = @first_name, `last_name` = @last_name, `password_hash` = @password_hash, `password_salt` = @password_salt, `address` = @adress) WHERE `id` = @id;";
            BindParams(cmd);
            BindId(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync() {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM `buyers` WHERE `id` = @id;";
            BindParams(cmd);
            BindId(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        private void BindId(MySqlCommand cmd)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = Id,
            });
        }
         private void BindParams(MySqlCommand cmd)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@email",
                DbType = DbType.String,
                Value = _email,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@username",
                DbType = DbType.String,
                Value = _username,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@first_name",
                DbType = DbType.String,
                Value = _first_name,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@last_name",
                DbType = DbType.String,
                Value = _last_name,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@password_hash",
                DbType = (DbType) MySqlDbType.VarBinary,
                Value = _password_hash,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@password_salt",
                DbType = (DbType) MySqlDbType.VarBinary,
                Value = _password_salt,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@address",
                DbType = DbType.String,
                Value = _address,
            });
        }
    }
}

