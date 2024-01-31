using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AzureSidekick.ConsoleUI;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Loggers;
using AzureSidekick.Infrastructure.Extensions;
using AzureSidekick.Services.Extensions;

using var host = CreateHostBuilder(args).Build();
using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

IConfiguration LoadConfiguration()
{
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

    return builder.Build();
}

IHostBuilder CreateHostBuilder(string[] strings)
{
    var configuration = LoadConfiguration();
    return Host.CreateDefaultBuilder()
        .ConfigureServices((_, serviceCollection) =>
        {
            serviceCollection.AddSingleton<ILogger, FileSystemLogger>();
            serviceCollection.RegisterInfrastructureDependencies(configuration);
            serviceCollection.RegisterServicesDependencies();
            serviceCollection.AddSingleton<Main>();
        });
}

try
{
    await services.GetRequiredService<Main>().Run(args);
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}