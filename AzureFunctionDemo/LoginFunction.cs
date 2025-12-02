using AzureFunctionDemo.Helpers;
using AzureFunctionDemo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFunctionDemo;

public class LoginFunction
{
    private readonly IUserService _userService;
    private readonly ILogger<LoginFunction> _logger;

    public LoginFunction(IUserService userService, ILogger<LoginFunction> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [Function("Login")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "login")] HttpRequest req)
    {
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<LoginRequest>(body);

        if (data == null || string.IsNullOrEmpty(data.Email))
            return new BadRequestObjectResult(new { error = "Please provide email" });

        // Check if user exists in SQL
        var user = await _userService.GetUser(data.Email);
        if (user == null)
            return new UnauthorizedObjectResult(new { error = "Invalid email or user not found" });

        // Load JWT config
        string? secret = Environment.GetEnvironmentVariable("JwtSecret");
        string? issuer = Environment.GetEnvironmentVariable("JwtIssuer");
        string? audience = Environment.GetEnvironmentVariable("JwtAudience");

        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            return new ObjectResult(new { error = "JWT configuration missing" }) { StatusCode = 500 };

        // Generate Token
        string token = JwtHelper.GenerateToken(user.Email, user.Name, secret, issuer, audience);

        return new OkObjectResult(new
        {
            token = token,
            expiresIn = 7200,
            email = user.Email,
            name = user.Name
        });
    }
}

public class LoginRequest
{
    public string Email { get; set; }
}