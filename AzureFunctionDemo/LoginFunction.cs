using AzureFunctionDemo.Helpers;
using AzureFunctionDemo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "login")] HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body);
        string body = await reader.ReadToEndAsync();

        var data = JsonConvert.DeserializeObject<LoginRequest>(body);

        var response = req.CreateResponse();

        if (data == null || string.IsNullOrEmpty(data.Email))
        {
            response.StatusCode = System.Net.HttpStatusCode.BadRequest;
            await response.WriteAsJsonAsync(new { error = "Please provide email" });
            return response;
        }

        var user = await _userService.GetUser(data.Email);
        if (user == null)
        {
            response.StatusCode = System.Net.HttpStatusCode.Unauthorized;
            await response.WriteAsJsonAsync(new { error = "Invalid email or user not found" });
            return response;
        }

        string secret = Environment.GetEnvironmentVariable("JwtSecret");
        string issuer = Environment.GetEnvironmentVariable("JwtIssuer");
        string audience = Environment.GetEnvironmentVariable("JwtAudience");

        string token = JwtHelper.GenerateToken(user.Email, user.Name, secret, issuer, audience);

        response.StatusCode = System.Net.HttpStatusCode.OK;
        await response.WriteAsJsonAsync(new
        {
            token,
            expiresIn = 7200,
            email = user.Email,
            name = user.Name
        });

        return response;
    }
}

public class LoginRequest
{
    public string Email { get; set; }
}
