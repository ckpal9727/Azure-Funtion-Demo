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
            // Allow Login to skip JWT validation
            if (context.FunctionDefinition.Name.Equals("Login", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var request = await context.GetHttpRequestDataAsync();
            if (request == null)
            {
                await WriteUnauthorized(context, null, "Invalid HTTP request (no request data)");
                return;
            }

            if (!request.Headers.TryGetValues("Authorization", out var headerValues))
            {
                await WriteUnauthorized(context, request, "Missing Authorization header");
                return;
            }

            var header = headerValues.FirstOrDefault();
            if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer "))
            {
                await WriteUnauthorized(context, request, "Invalid Authorization header");
                return;
            }

            var token = header.Substring("Bearer ".Length).Trim();

            try
            {
                string secret = Environment.GetEnvironmentVariable("JwtSecret");
                string issuer = Environment.GetEnvironmentVariable("JwtIssuer");
                string audience = Environment.GetEnvironmentVariable("JwtAudience");

                if (string.IsNullOrEmpty(secret) ||
                    string.IsNullOrEmpty(issuer) ||
                    string.IsNullOrEmpty(audience))
                {
                    await WriteUnauthorized(context, request, "JWT configuration missing");
                    return;
                }

                var handler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(secret);

                handler.ValidateToken(
                    token,
                    new TokenValidationParameters
                    {
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidateAudience = true,
                        ValidateIssuer = true
                    },
                    out _
                );

                // Token is valid
                await next(context);
            }
            catch (SecurityTokenExpiredException)
            {
                await WriteUnauthorized(context, request, "Token expired");
            }
            catch (Exception ex)
            {
                _logger.LogError($"JWT validation error: {ex.Message}");
                await WriteUnauthorized(context, request, "Invalid or expired token");
            }
        }

        private static async Task WriteUnauthorized(
            FunctionContext context,
            HttpRequestData request,
            string message)
        {
            HttpResponseData response;

            if (request != null)
            {
                response = request.CreateResponse(HttpStatusCode.Unauthorized);
            }
            else
            {
                // Fallback when HttpRequestData is completely unavailable
                var inv = context.GetInvocationResult();
                var fakeRequest = (HttpRequestData)inv.Value;
                response = fakeRequest.CreateResponse(HttpStatusCode.Unauthorized);
            }

            await response.WriteStringAsync(message);
            context.GetInvocationResult().Value = response;
        }
    }
}
