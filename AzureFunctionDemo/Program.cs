using AzureFunctionDemo.Middleware;
using AzureFunctionDemo.Services;
using AzureFunctionDemo.Sql;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.UseMiddleware<JwtAuthMiddleware>();
builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton<IUserService>(sp =>
{
    var conn = Environment.GetEnvironmentVariable("SqlConnectionString");
    return new SqlUserService(conn);
});



// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
