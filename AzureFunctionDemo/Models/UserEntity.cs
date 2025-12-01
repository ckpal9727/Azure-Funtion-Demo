using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureFunctionDemo.Models
{
    public class UserEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Users";
        public string RowKey { get; set; }  // Unique ID (email or guid)

        public string Name { get; set; }
        public string Email { get; set; }

        // Required system properties
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
