using AzureSidekick.Core.Interfaces;
using AzureSidekick.Infrastructure.Interfaces;
using AzureSidekick.Services.Factory;
using Microsoft.Extensions.DependencyInjection;
using AzureSidekick.Services.Interfaces;
using AzureSidekick.Services.Management;

namespace AzureSidekick.Services.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// Register various service dependencies in DI container.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection"/>.
    /// </param>
    /// <returns>
    /// <see cref="IServiceCollection"/>.
    /// </returns>
    public static IServiceCollection RegisterServicesDependencies(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger>();
        var genAIRepository = provider.GetRequiredService<IGenAIRepository>();
        var storageRepository = provider.GetRequiredService<IStorageRepository>();

        IAzureChatManagementService storageChatManagementService =
            new AzureStorageChatManagementService(genAIRepository, storageRepository, logger);
        services
            .AddSingleton<ISubscriptionManagementService, SubscriptionManagementService>()
            .AddSingleton<IGeneralChatManagementService, GeneralChatManagementService>()
            .AddSingleton<IAzureChatManagementServiceFactory, AzureChatManagementServiceFactory>()
            .AddSingleton(storageChatManagementService);
        return services;
    }
}