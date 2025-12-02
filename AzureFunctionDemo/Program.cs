using AzureFunctionDemo.Services;
using AzureFunctionDemo.Sql;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

// Register middleware (host-level)
//builder.UseMiddleware<JwtAuthMiddleware>();
builder.ConfigureFunctionsWebApplication();


// Register services
builder.Services.AddSingleton<IUserService>(sp =>
{
    var conn = Environment.GetEnvironmentVariable("SqlConnectionString");
    return new SqlUserService(conn);
});

builder.Build().Run();
