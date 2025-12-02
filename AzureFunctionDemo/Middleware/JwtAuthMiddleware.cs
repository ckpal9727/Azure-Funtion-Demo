using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;

namespace AzureFunctionDemo.Middleware
{
    public class JwtAuthMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<JwtAuthMiddleware> _logger;

        public JwtAuthMiddleware(ILogger<JwtAuthMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            // Skip JWT check on Login
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

            if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
            {
                await WriteUnauthorized(context, "Missing Authorization header");
                return;
            }

            var header = authHeaders.FirstOrDefault();
            if (header == null || !header.StartsWith("Bearer "))
            {
                await WriteUnauthorized(context, "Invalid Authorization header");
                return;
            }

            var token = header.Substring("Bearer ".Length);

            try
            {
                string secret = Environment.GetEnvironmentVariable("JwtSecret")!;
                string issuer = Environment.GetEnvironmentVariable("JwtIssuer")!;
                string audience = Environment.GetEnvironmentVariable("JwtAudience")!;

                var handler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                }, out _);

                // Valid → go next
                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT validation failed");
                await WriteUnauthorized(context, "Invalid or expired token");
            }
        }

        private async Task WriteUnauthorized(FunctionContext context, string message)
        {
            var req = await context.GetHttpRequestDataAsync();
            var res = req!.CreateResponse(HttpStatusCode.Unauthorized);
            await res.WriteStringAsync(message);

            context.GetInvocationResult().Value = res;
        }
    }
}
