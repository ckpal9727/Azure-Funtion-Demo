using AzureFunctionDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctionDemo.Services
{
    public interface IUserService
    {
        Task<UserEntity?> GetUser(string email);
        Task<List<UserEntity>> GetUsers();
        Task<bool> RegisterUser(string name, string email);
    }
}
