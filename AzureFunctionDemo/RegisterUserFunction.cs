using Azure.Data.Tables;
using AzureFunctionDemo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureFunctionDemo
{
    public class RegisterUserFunction
    {
        private readonly ILogger<RegisterUserFunction> _logger;

        public RegisterUserFunction(ILogger<RegisterUserFunction> logger)
        {
            _logger = logger;
        }

        [Function("RegisterUser")]
        public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "register")] HttpRequest req)
        {
            _logger.LogInformation("Register user request received.");

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<UserRequest>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.Name) || string.IsNullOrEmpty(data.Email))
            {
                return new BadRequestObjectResult(new { error = "Please provide both Name and Email." });
            }

            // Connect to table storage
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var tableClient = new TableClient(connectionString, "UsersTable");

            await tableClient.CreateIfNotExistsAsync();

            // Create entity
            var userEntity = new UserEntity
            {
                RowKey = data.Email,      // Unique identifier (email)
                Name = data.Name,
                Email = data.Email
            };

            // Insert or update
            await tableClient.UpsertEntityAsync(userEntity);

            return new OkObjectResult(new
            {
                message = $"User {data.Name} stored successfully!",
                email = data.Email,
                timestamp = DateTime.UtcNow,
                success = true
            });
        }

    }
}
