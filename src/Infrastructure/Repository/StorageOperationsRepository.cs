using System.Net;
using System.Text.Json.Nodes;
using Azure.Core;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Infrastructure.Interfaces;
using AzureSidekick.Infrastructure.Utilities;

namespace AzureSidekick.Infrastructure.Repository;

/// <summary>
/// Class for storage operations.
/// </summary>
public class StorageOperationsRepository : IStorageOperationsRepository
{
    /// <summary>
    /// <see cref="ILogger"/>.
    /// </summary>
    private readonly ILogger _logger;
    
    /// <summary>
    /// Initialize a new instance of <see cref="StorageOperationsRepository"/>.
    /// </summary>
    /// <param name="logger">
    /// <see cref="ILogger"/>.
    /// </param>
    public StorageOperationsRepository(ILogger logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// List storage accounts in a subscription a user has access to.
    /// </summary>
    /// <param name="subscriptionId">
    /// Subscription id.
    /// </param>
    /// <param name="credentials">
    /// <see cref="TokenCredential"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="IEnumerable{SubscriptionResource}"/>.
    /// </returns>
    public async Task<IEnumerable<JsonObject>> List(string subscriptionId,
        TokenCredential credentials = default,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("StorageRepository:List", $"List storage accounts. Subscription id: {subscriptionId}",
            operationContext);
        try
        {
            var resourceQuery =
                $"Resources | where type =~ 'Microsoft.Storage/storageAccounts'";
            var storageAccounts = await Helper.ExecuteResourceQuery(subscriptionId, resourceQuery, credentials);
            return storageAccounts;
        }
        catch (Exception exception)
        {
            _logger?.LogException(exception, context);
            throw Helper.GetRequestException(exception,
                $"StorageRepository:List - An error occurred while listing storage accounts in \"{subscriptionId}\" subscription.");
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }
    
    /// <summary>
    /// Get details about a storage account.
    /// </summary>
    /// <param name="subscriptionId">
    /// Subscription id.
    /// </param>
    /// <param name="accountName">
    /// Storage account name.
    /// </param>
    /// <param name="credentials">
    /// <see cref="TokenCredential"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="JsonObject"/> representing storage account if found, otherwise null.
    /// </returns>
    public async Task<JsonObject> Get(string subscriptionId, string accountName, TokenCredential credentials = default,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("StorageRepository:Get", $"Get storage account details. Subscription id: {subscriptionId}. Storage account: {accountName}.",
            operationContext);
        try
        {
            var resourceQuery =
                $"Resources | where type =~ 'Microsoft.Storage/storageAccounts' and name =~ '{accountName}'";
            var storageAccounts = await Helper.ExecuteResourceQuery(subscriptionId, resourceQuery, credentials);
            return storageAccounts.FirstOrDefault();
        }
        catch (Exception exception)
        {
            _logger?.LogException(exception, context);
            var requestException = Helper.GetRequestException(exception,
                $"StorageRepository:Get - An error occurred while getting details for \"{accountName}\" storage account in \"{subscriptionId}\" subscription.");
            if (requestException.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw requestException;
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }
}