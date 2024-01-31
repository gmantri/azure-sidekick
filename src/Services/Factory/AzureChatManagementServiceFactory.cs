using AzureSidekick.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AzureSidekick.Services.Factory;

/// <summary>
/// Factory interface to work with Azure service-specific chat. Responsible for returning
/// appropriate Azure related chat management service.
/// </summary>
public class AzureChatManagementServiceFactory : IAzureChatManagementServiceFactory
{
    /// <summary>
    /// <see cref="IServiceProvider"/>.
    /// </summary>
    private readonly IServiceProvider _provider;
    
    /// <summary>
    /// Initializes a new instance of AzureChatManagementServiceFactory.
    /// </summary>
    /// <param name="provider">
    /// <see cref="IServiceProvider"/>.
    /// </param>
    public AzureChatManagementServiceFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Gets a service-specific implementation of <see cref="IAzureChatManagementService"/>.
    /// </summary>
    /// <param name="serviceName">
    /// Name of Azure service e.g. Storage, CosmosDB etc.
    /// </param>
    /// <returns>
    /// <see cref="IAzureChatManagementService"/>.
    /// </returns>
    public IAzureChatManagementService GetService(string serviceName)
    {
        return _provider.GetServices<IAzureChatManagementService>()
            .FirstOrDefault(s => s.ServiceName == serviceName);
    }
}