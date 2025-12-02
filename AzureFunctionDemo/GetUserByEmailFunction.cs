using Azure;
using Azure.Data.Tables;
using AzureFunctionDemo.Models;
using AzureFunctionDemo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionDemo;

public class GetUserByEmailFunction
{
    private readonly IUserService _userService;

    public GetUserByEmailFunction(IUserService userService)
    {
        _userService = userService;
    }

    [Function("GetUserByEmail")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{email}")] HttpRequest req,
        string email)
    {
        var result = await _userService.GetUser(email);

        if (result == null)
            return new NotFoundObjectResult(new { message = "Not found" });

        return new OkObjectResult(result);
    }
}
