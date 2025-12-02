using AzureFunctionDemo.Models;
using AzureFunctionDemo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionDemo;

public class UpdateUserFunction
{
    private readonly IUserService _userService;
    private readonly ILogger<UpdateUserFunction> _logger;

    public UpdateUserFunction(IUserService userService, ILogger<UpdateUserFunction> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [Function("UpdateUser")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "users/{email}")] HttpRequest req,
        string email)
    {
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<UserRequest>(body);

        if (data == null || string.IsNullOrEmpty(data.Name))
            return new BadRequestObjectResult(new { error = "Name is required" });

        var updated = await _userService.UpdateUser(email, data.Name);

        if (!updated)
            return new NotFoundObjectResult(new { message = "User not found" });

        return new OkObjectResult(new
        {
            message = $"User {email} updated successfully!",
            newName = data.Name
        });
    }
}