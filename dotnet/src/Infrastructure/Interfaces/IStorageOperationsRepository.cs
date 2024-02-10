using System.Text.Json.Nodes;
using Azure.Core;
using AzureSidekick.Core.Interfaces;

namespace AzureSidekick.Infrastructure.Interfaces;

/// <summary>
/// Interface for storage related operations.
/// </summary>
public interface IStorageOperationsRepository
{
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
    /// <see cref="IEnumerable{JsonObject}"/>. JSON representation of storage accounts.
    /// </returns>
    Task<IEnumerable<JsonObject>> List(string subscriptionId, TokenCredential credentials = default,
        IOperationContext operationContext = default);
    
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
    Task<JsonObject> Get(string subscriptionId, string accountName, TokenCredential credentials = default,
        IOperationContext operationContext = default);
}