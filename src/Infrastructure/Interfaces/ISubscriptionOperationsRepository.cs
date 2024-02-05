using Azure.Core;
using Azure.ResourceManager.Resources;
using AzureSidekick.Core.Interfaces;

namespace AzureSidekick.Infrastructure.Interfaces;

/// <summary>
/// Interface for subscription related operations.
/// </summary>
public interface ISubscriptionOperationsRepository
{
    /// <summary>
    /// List subscriptions signed-in user has access to.
    /// </summary>
    /// <param name="credentials">
    /// <see cref="TokenCredential"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="IEnumerable{SubscriptionResource}"/>.
    /// </returns>
    Task<IEnumerable<SubscriptionData>> List(TokenCredential credentials = default, IOperationContext operationContext = default);
}