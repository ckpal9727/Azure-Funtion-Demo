using AzureFunctionDemo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionDemo;

public class DeleteUserFunction
{
    private readonly IUserService _userService;
    private readonly ILogger<DeleteUserFunction> _logger;

    public DeleteUserFunction(IUserService userService, ILogger<DeleteUserFunction> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [Function("DeleteUser")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "users/{email}")] HttpRequest req,
        string email)
    {
        _logger.LogInformation($"Deleting user with email: {email}");

        var deleted = await _userService.DeleteUser(email);

        if (!deleted)
            return new NotFoundObjectResult(new { message = $"User {email} not found" });

        return new OkObjectResult(new
        {
            message = $"User {email} deleted successfully",
            success = true
        });
    }
}