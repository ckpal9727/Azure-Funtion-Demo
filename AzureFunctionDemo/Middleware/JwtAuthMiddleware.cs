using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            // Skip JWT check on Login function
            if (context.FunctionDefinition.Name == "Login")
            {
                await next(context);
                return;
            }

            // Read Authorization header
            var httpReq = await context.GetHttpRequestDataAsync();
            if (httpReq == null)
            {
                await next(context);
                return;
            }

            if (!httpReq.Headers.TryGetValues("Authorization", out var authHeaders))
            {
                _logger.LogWarning("Missing Authorization header.");
                throw new UnauthorizedAccessException("Missing Authorization Header");
            }

            var authHeader = authHeaders.FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                throw new UnauthorizedAccessException("Invalid Authorization Header");
            }

            string token = authHeader.Substring("Bearer ".Length);

            try
            {
                string? secret = Environment.GetEnvironmentVariable("JwtSecret");
                string? issuer = Environment.GetEnvironmentVariable("JwtIssuer");
                string? audience = Environment.GetEnvironmentVariable("JwtAudience");

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
                }, out SecurityToken validatedToken);

                // Token is valid → proceed
                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError($"JWT validation failed: {ex.Message}");
                throw new UnauthorizedAccessException("Invalid or expired token");
            }
        }
    }
}
