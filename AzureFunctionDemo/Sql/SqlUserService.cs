using AzureFunctionDemo.Models;
using AzureFunctionDemo.Services;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctionDemo.Sql
{
    public class SqlUserService : IUserService
    {
        private readonly string _connectionString;

        public SqlUserService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> RegisterUser(string name, string email)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("INSERT INTO Users (Name, Email) VALUES (@Name, @Email)", conn);

            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Email", email);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return true;
        }

        public async Task<UserEntity?> GetUser(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT * FROM Users WHERE Email = @Email", conn);

            cmd.Parameters.AddWithValue("@Email", email);

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.Read()) return null;

            return new UserEntity
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }

        public async Task<List<UserEntity>> GetUsers()
        {
            var list = new List<UserEntity>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT * FROM Users", conn);

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new UserEntity
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }

            return list;
        }

        public async Task<bool> UpdateUser(string email, string newName)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("UPDATE Users SET Name = @Name WHERE Email = @Email", conn);

            cmd.Parameters.AddWithValue("@Name", newName);
            cmd.Parameters.AddWithValue("@Email", email);

            await conn.OpenAsync();
            int rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0;
        }

        public async Task<bool> DeleteUser(string email)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("DELETE FROM Users WHERE Email = @Email", conn);

            cmd.Parameters.AddWithValue("@Email", email);

            await conn.OpenAsync();
            int rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0;
        }
    }
}
