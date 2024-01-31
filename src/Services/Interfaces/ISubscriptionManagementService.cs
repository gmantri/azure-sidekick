using Azure.Core;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.OperationResults;

namespace AzureSidekick.Services.Interfaces;

/// <summary>
/// Interface for Azure Subscription management operations.
/// </summary>
public interface ISubscriptionManagementService
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
    /// Result of list subscriptions. Could be <see cref="SuccessOperationResult{T}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    Task<IOperationResult> List(TokenCredential credentials = default, IOperationContext operationContext = default);
}