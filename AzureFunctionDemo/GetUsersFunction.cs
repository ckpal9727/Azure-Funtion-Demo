using Azure.Data.Tables;
using AzureFunctionDemo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionDemo;

public class GetUsersFunction
{
    private readonly ILogger<GetUsersFunction> _logger;

    public GetUsersFunction(ILogger<GetUsersFunction> logger)
    {
        _logger = logger;
    }

    [Function("GetUsers")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users")] HttpRequest req)
    {
        _logger.LogInformation("Fetching all users from Azure Table Storage.");

        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var tableClient = new TableClient(connectionString, "UsersTable");

        var users = new List<UserEntity>();

        await foreach (UserEntity entity in tableClient.QueryAsync<UserEntity>())
        {
            users.Add(entity);
        }

        return new OkObjectResult(users);
    }
}