using System.Net;
using System.Text.Json.Nodes;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
using Azure.ResourceManager.Resources;
using AzureSidekick.Core.Exceptions;

namespace AzureSidekick.Infrastructure.Utilities;

/// <summary>
/// Helper methods.
/// </summary>
internal static class Helper
{
    /// <summary>
    /// Execute a resource graph query and returns the result.
    /// </summary>
    /// <param name="subscriptionId">
    /// Subscription id.
    /// </param>
    /// <param name="query">
    /// Resource graph query.
    /// </param>
    /// <param name="credentials">
    /// <see cref="TokenCredential"/>.
    /// </param>
    /// <returns>
    /// <see cref="IEnumerable{JsonObject}"/>.
    /// </returns>
    internal static async Task<IEnumerable<JsonObject>> ExecuteResourceQuery(string subscriptionId, string query, TokenCredential credentials = default)
    {
        var armClient = new ArmClient(credentials ?? new DefaultAzureCredential());
        var subscriptionData = await GetSubscriptionDetails(armClient, subscriptionId);
        var tenantResource = await GetTenantDetails(armClient, subscriptionData.TenantId?.ToString());
        var queryContent = new ResourceQueryContent(query)
        {
            Subscriptions = { subscriptionId }
        };
        var resources = await tenantResource.GetResourcesAsync(queryContent);
        return resources.HasValue ? resources.Value.Data.ToObjectFromJson<List<JsonObject>>() : new List<JsonObject>();
    }
    
    /// <summary>
    /// Get the details about a subscription.
    /// </summary>
    /// <param name="armClient">
    /// <see cref="ArmClient"/>.
    /// </param>
    /// <param name="subscriptionId">
    /// Subscription id.
    /// </param>
    /// <returns>
    /// <see cref="SubscriptionData"/>
    /// </returns>
    private static async Task<SubscriptionData> GetSubscriptionDetails(ArmClient armClient, string subscriptionId)
    {
        var subscriptionResource = await armClient
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync();
        return subscriptionResource.Value?.Data;
    }

    /// <summary>
    /// Get the details about a tenant.
    /// </summary>
    /// <param name="armClient">
    /// <see cref="ArmClient"/>.
    /// </param>
    /// <param name="tenantId">
    /// Tenant id.
    /// </param>
    /// <returns>
    /// <see cref="TenantResource"/>.
    /// </returns>
    private static async Task<TenantResource> GetTenantDetails(ArmClient armClient, string tenantId)
    {
        var result = armClient.GetTenants().GetAllAsync();
        TenantResource tenantResource = null;
        await foreach (var item in result)
        {
            if (!item.HasData || !item.Data.TenantId.HasValue ||
                item.Data.TenantId.Value != Guid.Parse(tenantId)) continue;
            tenantResource = item;
            break;
        }

        return tenantResource;
    }
    
    /// <summary>
    /// Get <see cref="RequestException"/> from an exception.
    /// </summary>
    /// <param name="exception">
    /// <see cref="Exception"/>.
    /// </param>
    /// <param name="message">
    /// Custom error message.
    /// </param>
    /// <returns>
    /// <see cref="RequestException"/>.
    /// </returns>
    internal static RequestException GetRequestException(Exception exception, string message = default)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        if (exception is RequestFailedException requestFailedException)
        {
            statusCode = (HttpStatusCode)requestFailedException.Status;
        }

        return new RequestException(message ?? exception.Message, statusCode, exception);
    }
}