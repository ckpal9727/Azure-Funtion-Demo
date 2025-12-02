using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
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
        // Skip JWT for Login
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
            string secret = Environment.GetEnvironmentVariable("JwtSecret");
            string issuer = Environment.GetEnvironmentVariable("JwtIssuer");
            string audience = Environment.GetEnvironmentVariable("JwtAudience");

            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secret);

            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            }, out _);

            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError($"JWT failed: {ex.Message}");
            await WriteUnauthorized(context, "Invalid or expired token");
        }
    }

    private static async Task WriteUnauthorized(FunctionContext ctx, string message)
    {
        var req = await ctx.GetHttpRequestDataAsync();
        var res = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
        await res.WriteStringAsync(message);
        ctx.GetInvocationResult().Value = res;
    }
}
