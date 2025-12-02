using Azure.Data.Tables;
using AzureFunctionDemo.Models;
using AzureFunctionDemo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionDemo;

public class GetUsersFunction
{
    private readonly IUserService _userService;

    public GetUsersFunction(IUserService userService)
    {
        _userService = userService;
    }

    [Function("GetUsers")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users")] HttpRequest req)
    {
        var users = await _userService.GetUsers();
        return new OkObjectResult(users);
    }
}