using Microsoft.Extensions.DependencyInjection;
using AzureSidekick.Services.Factory;
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
        services
            .AddSingleton<ISubscriptionManagementService, SubscriptionManagementService>()
            .AddSingleton<IGeneralChatManagementService, GeneralChatManagementService>()
            .AddSingleton<IAzureChatManagementServiceFactory, AzureChatManagementServiceFactory>()
            .AddKeyedSingleton<IAzureChatManagementService, AzureStorageChatManagementService>("Storage");
        return services;
    }
}