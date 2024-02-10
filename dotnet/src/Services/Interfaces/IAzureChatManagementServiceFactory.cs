namespace AzureSidekick.Services.Interfaces;

/// <summary>
/// Factory interface to work with Azure service-specific chat. Responsible for returning
/// appropriate Azure related chat management service.
/// </summary>
public interface IAzureChatManagementServiceFactory
{
    /// <summary>
    /// Get a service-specific implementation of <see cref="IAzureChatManagementService"/>.
    /// </summary>
    /// <param name="serviceName">
    /// Name of Azure service e.g. Storage, CosmosDB etc.
    /// </param>
    /// <returns>
    /// <see cref="IAzureChatManagementService"/>.
    /// </returns>
    IAzureChatManagementService GetService(string serviceName);
}