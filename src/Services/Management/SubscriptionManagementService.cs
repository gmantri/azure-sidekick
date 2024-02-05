using System.Net;
using Azure.Core;
using Azure.ResourceManager.Resources;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.OperationResults;
using AzureSidekick.Infrastructure.Interfaces;
using AzureSidekick.Services.Interfaces;
using AzureSidekick.Services.Utilities;

namespace AzureSidekick.Services.Management;

/// <summary>
/// Class for Azure Subscription management operations.
/// </summary>
public class SubscriptionManagementService : ISubscriptionManagementService
{
    /// <summary>
    /// <see cref="ISubscriptionOperationsRepository"/>.
    /// </summary>
    private readonly ISubscriptionOperationsRepository _subscriptionOperationsRepository;

    /// <summary>
    /// <see cref="ILogger"/>.
    /// </summary>
    private readonly ILogger _logger;
    
    /// <summary>
    /// Initialize a new instance of <see cref="SubscriptionManagementService"/>.
    /// </summary>
    /// <param name="subscriptionOperationsRepository">
    /// <see cref="ISubscriptionOperationsRepository"/>.
    /// </param>
    /// <param name="logger">
    /// <see cref="ILogger"/>.
    /// </param>
    public SubscriptionManagementService(ISubscriptionOperationsRepository subscriptionOperationsRepository, ILogger logger)
    {
        _subscriptionOperationsRepository = subscriptionOperationsRepository;
        _logger = logger;
    }
    
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
    public async Task<IOperationResult> List(TokenCredential credentials = default, IOperationContext operationContext = default)
    {
        var context =
            new OperationContext("SubscriptionManagementService:ListSubscriptions", "List subscriptions", operationContext);
        try
        {
            var subscriptions = await _subscriptionOperationsRepository.List(credentials, context);
            return new SuccessOperationResult<SubscriptionData>()
            {
                OperationId = context.OperationId,
                StatusCode = HttpStatusCode.OK,
                Items = subscriptions
            };
        }
        catch (Exception exception)
        {
            return Helper.GetFailOperationResultFromException(exception, _logger, context);
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }
}