using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;

public class JwtAuthMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<JwtAuthMiddleware> _logger;

    public JwtAuthMiddleware(ILogger<JwtAuthMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Allow Login without token
        if (context.FunctionDefinition.Name == "Login")
        {
            await next(context);
            return;
        }

        var req = await context.GetHttpRequestDataAsync();
        if (req == null)
        {
            await next(context);
            return;
        }

        if (!req.Headers.TryGetValues("Authorization", out var headers))
        {
            await WriteUnauthorized(context, "Missing Authorization header");
            return;
        }

        var header = headers.FirstOrDefault();
        if (header == null || !header.StartsWith("Bearer "))
        {
            await WriteUnauthorized(context, "Invalid Authorization header");
            return;
        }

        string token = header.Substring("Bearer ".Length);

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var secret = Environment.GetEnvironmentVariable("JwtSecret");
            var issuer = Environment.GetEnvironmentVariable("JwtIssuer");
            var audience = Environment.GetEnvironmentVariable("JwtAudience");

            handler.ValidateToken(token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                },
                out _);

            await next(context); // ✔ Token is valid
        }
        catch (Exception ex)
        {
            _logger.LogError($"JWT Error: {ex.Message}");
            await WriteUnauthorized(context, "Invalid or expired token");
        }
    }

    private static async Task WriteUnauthorized(FunctionContext context, string message)
    {
        var req = await context.GetHttpRequestDataAsync();

        var response = req.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteStringAsync(message);

        // ✔ Correct way to return a response from middleware
        context.SetHttpResponseData(response);
    }
}
