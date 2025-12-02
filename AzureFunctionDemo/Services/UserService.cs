using Azure.Data.Tables;
using AzureFunctionDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctionDemo.Services
{
    public class UserService : IUserService
    {
        private readonly TableClient _tableClient;

        public UserService(string storageConnectionString)
        {
            _tableClient = new TableClient(storageConnectionString, "UsersTable");
            _tableClient.CreateIfNotExists();
        }

        public async Task<bool> RegisterUser(string name, string email)
        {
            var entity = new UserEntity
            {
                Name = name,
                Email = email,
                RowKey = email
            };

            await _tableClient.UpsertEntityAsync(entity);
            return true;
        }

        public async Task<UserEntity?> GetUser(string email)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<UserEntity>("Users", email);
                return response.Value;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<UserEntity>> GetUsers()
        {
            var list = new List<UserEntity>();

            await foreach (var entity in _tableClient.QueryAsync<UserEntity>())
            {
                list.Add(entity);
            }

            return list;
        }
        public async Task<bool> UpdateUser(string email, string newName)
        {
            try
            {
                var entity = await _tableClient.GetEntityAsync<UserEntity>("Users", email);
                var user = entity.Value;

                user.Name = newName; // modify field

                await _tableClient.UpdateEntityAsync(user, Azure.ETag.All);

                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> DeleteUser(string email)
        {
            try
            {
                await _tableClient.DeleteEntityAsync("Users", email);
                return true;
            }
            catch
            {
                return false;
            }
        }


    }
}
