using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Infrastructure.Interfaces;
using AzureSidekick.Infrastructure.Utilities;

namespace AzureSidekick.Infrastructure.Repository;

/// <summary>
/// Class for subscription operations.
/// </summary>
public class SubscriptionRepository : ISubscriptionRepository
{
    /// <summary>
    /// <see cref="ILogger"/>.
    /// </summary>
    private readonly ILogger _logger;
    
    /// <summary>
    /// Initializes a new instance of <see cref="SubscriptionRepository"/>.
    /// </summary>
    /// <param name="logger">
    /// <see cref="ILogger"/>.
    /// </param>
    public SubscriptionRepository(ILogger logger)
    {
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
    /// <see cref="IEnumerable{SubscriptionResource}"/>.
    /// </returns>
    public async Task<IEnumerable<SubscriptionData>> List(TokenCredential credentials = null, IOperationContext operationContext = null)
    {
        var context = new OperationContext("SubscriptionRepository:List", "List subscriptions",
            operationContext);
        try
        {
            var armClient = new ArmClient(credentials ?? new DefaultAzureCredential());
            var result = armClient.GetSubscriptions().GetAllAsync();
            var subscriptions = new List<SubscriptionData>();
            await foreach (var item in result)
            {
                if (item.Data != null)
                {
                    subscriptions.Add(item.Data);
                }
            }

            return subscriptions;
        }
        catch (Exception exception)
        {
            _logger?.LogException(exception, context);
            throw Helper.GetRequestException(exception,
                "SubscriptionRepository:List - An error occurred while listing subscriptions.");
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }
}