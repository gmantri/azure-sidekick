using System.Net;
using AzureSidekick.Core.EventArgs;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.OperationResults;
using AzureSidekick.Infrastructure.Interfaces;
using AzureSidekick.Services.Interfaces;
using AzureSidekick.Services.Utilities;

namespace AzureSidekick.Services.Management;

/// <summary>
/// Chat operations related to questions that are not specific to supported Azure services (currently Azure Storage).
/// </summary>
public class GeneralChatManagementService : BaseChatManagementService, IGeneralChatManagementService
{
    /// <summary>
    /// <see cref="IGenAIRepository"/>.
    /// </summary>
    private readonly IGenAIRepository _genAIRepository;

    /// <summary>
    /// <see cref="IChatHistoryOperationsRepository"/>.
    /// </summary>
    private readonly IChatHistoryOperationsRepository _chatHistoryOperationsRepository;

    /// <summary>
    /// <see cref="ILogger"/>.
    /// </summary>
    private readonly ILogger _logger;
    
    /// <summary>
    /// <see cref="TaskCompletionSource"/>.
    /// </summary>
    private TaskCompletionSource<bool> _tcs;

    /// <summary>
    /// Event handler for operation result received event.
    /// </summary>
    public event EventHandler OperationResultReceivedEventHandler;

    /// <summary>
    /// Dictionary of pre-defined messages for certain kinds of intents.
    /// </summary>
    private static IDictionary<string, string> _intentPredefinedMessages;

    /// <summary>
    /// Create a new instance of <see cref="GeneralChatManagementService"/>.
    /// </summary>
    /// <param name="genAIRepository">
    /// <see cref="IGenAIRepository"/>.
    /// </param>
    /// <param name="chatHistoryOperationsRepository">
    /// <see cref="IChatHistoryOperationsRepository"/>.
    /// </param>
    /// <param name="logger">
    /// <see cref="ILogger"/>.
    /// </param>
    public GeneralChatManagementService(IGenAIRepository genAIRepository, IChatHistoryOperationsRepository chatHistoryOperationsRepository, ILogger logger)
    {
        _genAIRepository = genAIRepository;
        _chatHistoryOperationsRepository = chatHistoryOperationsRepository;
        _logger = logger;
        _intentPredefinedMessages = new Dictionary<string, string>()
        {
            [Core.Constants.Intent.Ability] = "I can help you with your questions about Azure.",
            [Core.Constants.Intent.MultipleIntents] = "My apologies, but it seems that you are asking too many things in a single question. Can you please ask one question at a time?",
            [Core.Constants.Intent.Unclear] = "My apologies, but I am not sure I understand the question. Can you please provide more details?",
            [Core.Constants.Intent.Other] = "My apologies, but it seems the question is not related to Azure (I may be wrong though). Can you please clarify the question or ask me a question related to Azure."
        };
    }
    
    /// <summary>
    /// Rephrase a question.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> Rephrase(string question, IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:Rephrase", $"Rephrase question. Question: {question}", operationContext);
        try
        {
            var chatHistory = await _chatHistoryOperationsRepository.List(context.UserId, context);
            var arguments = new Dictionary<string, object>()
            {
                [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
                [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
            };
            var chatResponse = await _genAIRepository.GetResponse(question, "General", "Rephrase",
                arguments: arguments, operationContext: context);
            return new SuccessOperationResult<ChatResponse>()
            {
                Item = chatResponse,
                StatusCode = HttpStatusCode.OK
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

    /// <summary>
    /// Get the intent of a question.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> GetIntent(string question, IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:GetIntent", $"Get intent. Question: {question}", operationContext);
        try
        {
            var chatHistory = await _chatHistoryOperationsRepository.List(context.UserId, context);
            var arguments = new Dictionary<string, object>()
            {
                [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
                [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
            };
            var chatResponse = await _genAIRepository.GetResponse(question, "General", "Intent",
                arguments: arguments, operationContext: context);
            return new SuccessOperationResult<ChatResponse>()
            {
                Item = chatResponse,
                StatusCode = HttpStatusCode.OK
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

    /// <summary>
    /// Get the response of a question based on the question's intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="intent">
    /// Question's intent.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> GetResponse(string question, string intent,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:GetResponse", $"Get response. Question: {question}; Intent: {intent}.", operationContext);
        try
        {
            var chatHistory = await _chatHistoryOperationsRepository.List(context.UserId, context);
            switch (intent)
            {
                case Core.Constants.Intent.Azure:
                case Core.Constants.Intent.Information:
                    return await GetResponseFromLlm(question, "General", intent, chatHistory, context);
                default:
                    return GetPredefinedOperationResult(question, intent);
            }
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

    /// <summary>
    /// Get a streaming response for a question based on the question's intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="intent">
    /// Question's intent.
    /// </param>
    /// <param name="state">
    /// <see cref="StreamingResponseState"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    public async Task GetStreamingResponse(string question, string intent,
        StreamingResponseState state = default, IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:GetStreamingResponse", $"Get response. Question: {question}; Intent: {intent}.", operationContext);
        var streamingResponseState = state ?? new StreamingResponseState()
        {
            OperationContext = context
        };
        try
        {
            _tcs = new TaskCompletionSource<bool>();
            var chatHistory = await _chatHistoryOperationsRepository.List(context.UserId, context);
            switch (intent)
            {
                case Core.Constants.Intent.Azure:
                case Core.Constants.Intent.Information:
                    await GetStreamingResponseFromLlm(question, "General", intent, chatHistory, state, context);
                    break;
                default:
                    var result = GetPredefinedOperationResult(question, intent);
                    var eventArgs = new OperationResultReceivedEventArgs()
                    {
                        OperationResult = result,
                        IsLastResponse = false,
                        State = streamingResponseState
                    };
                    RaiseOperationResultReceivedEvent(eventArgs);
                    // raise the same event again but set this as the last response.
                    eventArgs.IsLastResponse = true;
                    RaiseOperationResultReceivedEvent(eventArgs);
                    break;
            }
        }
        catch (Exception exception)
        {
            var result = Helper.GetFailOperationResultFromException(exception, _logger, context);
            var eventArgs = new OperationResultReceivedEventArgs()
            {
                OperationResult = result,
                IsLastResponse = true,
                State = streamingResponseState
            };
            RaiseOperationResultReceivedEvent(eventArgs);
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }

    /// <summary>
    /// Clear chat history.
    /// </summary>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    public async Task<IOperationResult> ClearChatHistory(IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:ClearChatHistory", "Clear chat history.", operationContext);
        try
        {
            await _chatHistoryOperationsRepository.Clear(context.UserId, context);
            return new SuccessOperationResult<ChatResponse>()
            {
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception exception)
        {
            return Helper.GetFailOperationResultFromException(exception, _logger, context);        
        }
        finally
        {
            _logger.LogOperation(context);
        }
    }

    /// <summary>
    /// Get response from LLM in a non-streaming way.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="pluginName">
    /// Plugin name.
    /// </param>
    /// <param name="functionName">
    /// Function name.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    private async Task<IOperationResult> GetResponseFromLlm(string question, string pluginName, string functionName,
        IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext)
    {
        var arguments = GetDefaultChatArguments(chatHistory);
        var chatResponse = await _genAIRepository.GetResponse(question, pluginName, functionName,
            arguments: arguments, operationContext: operationContext);
        await Helper.SaveChatResponse(_chatHistoryOperationsRepository, chatResponse, operationContext);
        return new SuccessOperationResult<ChatResponse>()
        {
            Item = chatResponse,
            StatusCode = HttpStatusCode.OK
        };
    }

    /// <summary>
    /// Get response from LLM in a streaming way.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="pluginName">
    /// Plugin name.
    /// </param>
    /// <param name="functionName">
    /// Function name.
    /// </param>
    /// <param name="state">
    /// <see cref="StreamingResponseState"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    private async Task GetStreamingResponseFromLlm(string question, string pluginName, string functionName,
        IEnumerable<ChatResponse> chatHistory,
        StreamingResponseState state,
        IOperationContext operationContext)
    {
        var arguments = GetDefaultChatArguments(chatHistory);
        var result = _genAIRepository.GetStreamingResponse(question, pluginName, functionName, arguments,
            operationContext);
        await foreach (var chatResponse in result)
        {
            var operationResult = new SuccessOperationResult<ChatResponse>()
            {
                Item = chatResponse,
                StatusCode = HttpStatusCode.OK
            };
            var isLastResponse = (chatResponse.PromptTokens > 0 || chatResponse.CompletionTokens > 0) &&
                                 chatResponse.StoreInChatHistory;
            if (isLastResponse)
            {
                await Helper.SaveChatResponse(_chatHistoryOperationsRepository, chatResponse, operationContext);
            }
            RaiseOperationResultReceivedEvent(new OperationResultReceivedEventArgs()
            {
                OperationResult = operationResult,
                IsLastResponse = isLastResponse,
                State = state
            });
        }
    }
    
    /// <summary>
    /// Raise operation completed received event.
    /// </summary>
    /// <param name="args">
    /// <see cref="OperationResultReceivedEventArgs"/>.
    /// </param>
    private void RaiseOperationResultReceivedEvent(OperationResultReceivedEventArgs args)
    {
        OperationResultReceivedEventHandler?.Invoke(this, args);
        if (args.IsLastResponse)
        {
            _tcs.SetResult(true);
        }
    }

    /// <summary>
    /// Create a pre-defined chat response for certain question intents.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="intent">
    /// Question intent.
    /// </param>
    /// <returns>
    /// Chat result. <see cref="SuccessOperationResult{ChatResponse}"/>.
    /// </returns>
    private IOperationResult GetPredefinedOperationResult(string question, string intent)
    {
        var response = _intentPredefinedMessages[Core.Constants.Intent.Other];
        if (_intentPredefinedMessages.TryGetValue(intent, out var message))
        {
            response = message;
        }
        var chatResponse = new ChatResponse()
        {
            Question = question,
            Response = response,
            Intent = intent
        };
        var result = new SuccessOperationResult<ChatResponse>()
        {
            Item = chatResponse,
            StatusCode = HttpStatusCode.OK
        };
        return result;
    }
}