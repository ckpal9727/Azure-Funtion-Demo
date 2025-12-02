using Azure.Data.Tables;
using AzureFunctionDemo.Models;
using AzureFunctionDemo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureFunctionDemo
{
    public class RegisterUserFunction
    {
        private readonly IUserService _userService;
        private readonly ILogger<RegisterUserFunction> _logger;

        public RegisterUserFunction(IUserService userService, ILogger<RegisterUserFunction> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [Function("RegisterUser")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "register")] HttpRequest req)
        {
            _logger.LogInformation("Register user request received.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<UserRequest>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.Name) || string.IsNullOrEmpty(data.Email))
            {
                return new BadRequestObjectResult(new { error = "Please provide both Name and Email." });
            }

            await _userService.RegisterUser(data.Name, data.Email);

            return new OkObjectResult(new
            {
                message = $"User {data.Name} stored successfully!",
                email = data.Email,
                success = true,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
