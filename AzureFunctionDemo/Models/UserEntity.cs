using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctionDemo.Models
{
    public class UserEntity 
    {
        public int Id { get; set; } 
       

        public string Name { get; set; }
        public string Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  
    }
}
