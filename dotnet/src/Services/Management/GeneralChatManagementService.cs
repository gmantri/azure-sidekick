using System.Net;
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
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Streaming chat result. See <see cref="IAsyncEnumerable{IOperationResult}"/>.
    /// </returns>
    public async IAsyncEnumerable<IOperationResult> GetStreamingResponse(string question, string intent,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:GetStreamingResponse", $"Get response. Question: {question}; Intent: {intent}.", operationContext);
        IOperationResult failureResult = null;
        IOperationResult predefinedOperationResult = null;
        IAsyncEnumerable<IOperationResult> result = null;
        try
        {
            var chatHistory = await _chatHistoryOperationsRepository.List(context.UserId, context);
            switch (intent)
            {
                case Core.Constants.Intent.Azure:
                case Core.Constants.Intent.Information:
                    result = GetStreamingResponseFromLlm(question, "General", intent, chatHistory, context);
                    break;
                default:
                    predefinedOperationResult = GetPredefinedOperationResult(question, intent);
                    break;
            }
        }
        catch (Exception exception)
        {
            failureResult = Helper.GetFailOperationResultFromException(exception, _logger, context);
        }
        finally
        {
            _logger?.LogOperation(context);
        }

        if (failureResult != null) yield return failureResult;
        if (predefinedOperationResult != null) yield return predefinedOperationResult;
        if (result == null) yield break;
        await foreach (var item in result)
        {
            yield return item;
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
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    private async IAsyncEnumerable<IOperationResult> GetStreamingResponseFromLlm(string question, string pluginName, string functionName,
        IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext)
    {
        var arguments = GetDefaultChatArguments(chatHistory);
        var result = _genAIRepository.GetStreamingResponse(question, pluginName, functionName, arguments,
            operationContext);
        await foreach (var chatResponse in result)
        {
            var isLastResponse = (chatResponse.PromptTokens > 0 || chatResponse.CompletionTokens > 0) &&
                                 chatResponse.StoreInChatHistory;
            var operationResult = new SuccessOperationResult<ChatResponse>()
            {
                Item = chatResponse,
                StatusCode = isLastResponse ? HttpStatusCode.NoContent : HttpStatusCode.OK // use HttpStatusCode.NoContent to indicate that the consumer should ignore the response.
            };
            if (isLastResponse)
            {
                await Helper.SaveChatResponse(_chatHistoryOperationsRepository, chatResponse, operationContext);
            }
            yield return operationResult;
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