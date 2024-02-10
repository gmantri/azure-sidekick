using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;

namespace AzureSidekick.Core.Loggers;

/// <summary>
/// Application Insights logger.
/// </summary>
public class AppInsightsLogger : ILogger
{
    /// <summary>
    /// Telemetry client.
    /// </summary>
    private readonly TelemetryClient _telemetryClient;
    
    /// <summary>
    /// Initialize a new instance of <see cref="AppInsightsLogger"/>.
    /// </summary>
    /// <param name="telemetryClient">
    /// <see cref="TelemetryClient"/>.
    /// </param>
    public AppInsightsLogger(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    /// <summary>
    /// Log exception and send it to Application Insights.
    /// </summary>
    /// <param name="exception">
    /// <see cref="Exception"/>.
    /// </param>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    public void LogException(Exception exception, IOperationContext operationContext)
    {
        ExceptionTelemetry exceptionTelemetry = new(exception);
        if (operationContext != null)
        {
            exceptionTelemetry.Message = operationContext.Message;
            exceptionTelemetry.Context.Operation.Id = operationContext.OperationId;
            operationContext.EndTime = DateTime.UtcNow;
            exceptionTelemetry.Context.Operation.Id = operationContext.OperationId;
            exceptionTelemetry.Context.Operation.ParentId = operationContext.ParentOperationId;
            exceptionTelemetry.Context.Operation.Name = operationContext.OperationName;
            exceptionTelemetry.Context.User.Id = operationContext.UserId;
            operationContext.Metadata.TryAdd(Constants.StartDateTime, operationContext.StartTime.ToString("O"));
            operationContext.Metadata.TryAdd(Constants.EndDateTime, operationContext.EndTime.ToString("O"));
            operationContext.Metadata.TryAdd(Constants.ElapsedTime, operationContext.ElapsedTime);
            foreach ((string key, object value) in operationContext.Metadata)
            {
                if (!exceptionTelemetry.Properties.ContainsKey(key))
                {
                    exceptionTelemetry.Properties.Add(key, value.ToString());
                }
            }
        }
        _telemetryClient.TrackException(exceptionTelemetry);
    }

    /// <summary>
    /// Log chat response and send it to Application Insights.
    /// </summary>
    /// <param name="response">
    /// <see cref="ChatResponse"/>.
    /// </param>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    public void LogChatResponse(ChatResponse response, IOperationContext operationContext = default)
    {
        var traceTelemetry = new TraceTelemetry()
        {
            Message = $"Question: {response.OriginalQuestion}{Environment.NewLine}Revised Question: {response.Question}{Environment.NewLine}Response: {response.Response}"
        };
        traceTelemetry.Context.Operation.Id = operationContext?.OperationId;
        traceTelemetry.Context.Operation.ParentId = operationContext?.ParentOperationId;
        traceTelemetry.Context.Operation.Name = operationContext?.OperationName;
        traceTelemetry.Context.User.Id = operationContext?.UserId;
        traceTelemetry.Properties.Add("Question", response.OriginalQuestion);
        traceTelemetry.Properties.Add("RevisedQuestion", response.Question);
        traceTelemetry.Properties.Add("Response", response.Response);
        traceTelemetry.Properties.Add("Intent", response.Intent);
        traceTelemetry.Properties.Add("Function", response.Function);
        traceTelemetry.Properties.Add("PromptTokens", response.PromptTokens.ToString());
        traceTelemetry.Properties.Add("CompletionTokens", response.CompletionTokens.ToString());
    }

    /// <summary>
    /// Log operation and send it to Application Insights.
    /// </summary>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    public void LogOperation(IOperationContext operationContext)
    {
        var traceTelemetry = new TraceTelemetry()
        {
            Message = operationContext.Message
        };
        operationContext.EndTime = DateTime.UtcNow;
        traceTelemetry.Context.Operation.Id = operationContext.OperationId;
        traceTelemetry.Context.Operation.ParentId = operationContext.ParentOperationId;
        traceTelemetry.Context.Operation.Name = operationContext.OperationName;
        traceTelemetry.Context.User.Id = operationContext.UserId;
        operationContext.Metadata.TryAdd(Constants.StartDateTime, operationContext.StartTime.ToString("O"));
        operationContext.Metadata.TryAdd(Constants.EndDateTime, operationContext.EndTime.ToString("O"));
        operationContext.Metadata.TryAdd(Constants.ElapsedTime, operationContext.ElapsedTime);
        foreach ((string key, object value) in operationContext.Metadata)
        {
            if (!traceTelemetry.Properties.ContainsKey(key))
            {
                traceTelemetry.Properties.Add(key, value.ToString());
            }
        }
        _telemetryClient.TrackTrace(traceTelemetry);
    }
}