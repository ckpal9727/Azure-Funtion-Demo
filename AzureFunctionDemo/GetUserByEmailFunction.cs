using Azure;
using Azure.Data.Tables;
using AzureFunctionDemo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionDemo;

public class GetUserByEmailFunction
{
    private readonly ILogger<GetUserByEmailFunction> _logger;

    public GetUserByEmailFunction(ILogger<GetUserByEmailFunction> logger)
    {
        _logger = logger;
    }

    [Function("GetUserByEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{email}")] HttpRequest req,
        string email)
    {
        _logger.LogInformation($"Fetching user with email: {email}");

        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var tableClient = new TableClient(connectionString, "UsersTable");

        try
        {
            var entity = await tableClient.GetEntityAsync<UserEntity>("Users", email);
            return new OkObjectResult(entity.Value);
        }
        catch (RequestFailedException)
        {
            return new NotFoundObjectResult(new { message = $"User with email {email} not found" });
        }
    }
}