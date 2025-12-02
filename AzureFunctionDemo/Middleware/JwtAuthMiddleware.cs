using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Text;

public class JwtAuthMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<JwtAuthMiddleware> _logger;

    public JwtAuthMiddleware(ILogger<JwtAuthMiddleware> logger)
        => _logger = logger;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Allow Login (or any function you whitelist) to bypass JWT
        if (string.Equals(context.FunctionDefinition.Name, "Login", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Try get HttpRequestData (middleware runs for non-HTTP triggers too)
        var req = await context.GetHttpRequestDataAsync();
        if (req == null)
        {
            // Not an HTTP invocation → let it pass
            await next(context);
            return;
        }

        // Check header
        if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            await WriteUnauthorized(context, req, "Missing Authorization header");
            return;
        }

        var header = authHeaders.FirstOrDefault();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await WriteUnauthorized(context, req, "Invalid Authorization header");
            return;
        }

        var token = header.Substring("Bearer ".Length).Trim();

        try
        {
            var secret = Environment.GetEnvironmentVariable("JwtSecret");
            var issuer = Environment.GetEnvironmentVariable("JwtIssuer");
            var audience = Environment.GetEnvironmentVariable("JwtAudience");

            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("JWT configuration missing.");
                await WriteUnauthorized(context, req, "JWT configuration missing");
                return;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            }, out _);

            // token validated → continue to function
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT validation failed.");
            await WriteUnauthorized(context, req, "Invalid or expired token");
            return;
        }
    }

    private static async Task WriteUnauthorized(FunctionContext context, HttpRequestData req, string message)
    {
        // Create a response from the request (correct pattern)
        var response = req.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteStringAsync(message);

        // **Correct**: set the invocation result to short-circuit execution
        // This is supported and used widely in docs & samples.
        context.GetInvocationResult().Value = response;
    }
}
