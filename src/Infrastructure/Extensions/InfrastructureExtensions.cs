using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using AzureSidekick.Core.Models;
using AzureSidekick.Infrastructure.Interfaces;
using AzureSidekick.Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzureSidekick.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    /// <summary>
    /// Register various infrastructure dependencies in DI container.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection"/>.
    /// </param>
    /// <returns>
    /// <see cref="IServiceCollection"/>.
    /// </returns>
    public static IServiceCollection RegisterInfrastructureDependencies(this IServiceCollection services,
        IConfiguration configuration)
    {
        var azureOpenAISettings = new AzureOpenAISettings();
        configuration.GetSection("AzureOpenAISettings").Bind(azureOpenAISettings);
        var azureOpenAIClient =  string.IsNullOrWhiteSpace(azureOpenAISettings.Key) ? 
            new OpenAIClient(new Uri(azureOpenAISettings.Endpoint), new DefaultAzureCredential()) :
            new OpenAIClient(new Uri(azureOpenAISettings.Endpoint), new AzureKeyCredential(azureOpenAISettings.Key));
        services
            .AddSingleton(azureOpenAISettings)
            .AddSingleton(azureOpenAIClient)
            .AddSingleton<ISubscriptionRepository, SubscriptionRepository>()
            .AddSingleton<IStorageRepository, StorageRepository>()
            .AddSingleton<IGenAIRepository, GenAIRepository>();
        return services;
    }
}