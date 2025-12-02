using AzureFunctionDemo.Helpers;
using AzureFunctionDemo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

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
     [HttpTrigger(AuthorizationLevel.Function, "post", Route = "login")] HttpRequestData req,
     FunctionContext context)
    {
        var logger = context.GetLogger("Login");

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<LoginRequest>(body);

        if (data == null || string.IsNullOrEmpty(data.Email))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "Please provide email" });
            return bad;
        }

        var user = await _userService.GetUser(data.Email);
        if (user == null)
        {
            var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
            await unauthorized.WriteAsJsonAsync(new { error = "Invalid email or user not found" });
            return unauthorized;
        }

        string secret = Environment.GetEnvironmentVariable("JwtSecret");
        string issuer = Environment.GetEnvironmentVariable("JwtIssuer");
        string audience = Environment.GetEnvironmentVariable("JwtAudience");

        string token = JwtHelper.GenerateToken(user.Email, user.Name, secret, issuer, audience);

        var response = req.CreateResponse(HttpStatusCode.OK);
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
