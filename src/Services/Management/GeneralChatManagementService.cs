using System.Net;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.OperationResults;
using AzureSidekick.Infrastructure.Interfaces;
using AzureSidekick.Services.Interfaces;
using AzureSidekick.Services.Utilities;

namespace AzureSidekick.Services.Management;

public class GeneralChatManagementService : BaseChatManagementService, IGeneralChatManagementService
{
    private readonly IGenAIRepository _genAIRepository;

    private readonly ILogger _logger;

    public GeneralChatManagementService(IGenAIRepository genAIRepository, ILogger logger)
    {
        _genAIRepository = genAIRepository;
        _logger = logger;
    }
    
    /// <summary>
    /// Rephrases a question.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> Rephrase(string question, IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:Rephrase", $"Rephrase question. Question: {question}", operationContext);
        try
        {
            var arguments = new Dictionary<string, object>()
            {
                [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
                [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
            };
            var chatResponse = await _genAIRepository.GenerateResponse(question, "General", "Rephrase",
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
    /// Gets the intent of a question.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> GetIntent(string question, IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:GetIntent", $"Get intent. Question: {question}", operationContext);
        try
        {
            var arguments = new Dictionary<string, object>()
            {
                [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
                [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
            };
            var chatResponse = await _genAIRepository.GenerateResponse(question, "General", "Intent",
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
    /// Handles question with Azure intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> HandleQuestionWithAzureIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:HandleQuestionWithAzureIntent", $"Handle question with Azure intent. Question: {question}", operationContext);
        try
        {
            var arguments = new Dictionary<string, object>()
            {
                [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
                [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
            };
            var chatResponse = await _genAIRepository.GenerateResponse(question, "General", "Azure",
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
    /// Handles question with multiple intents.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> HandleQuestionWithMultipleIntents(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:HandleQuestionWithMultipleIntents", $"Handle question with multiple intents. Question: {question}", operationContext);
        try
        {
            return await GetPredefinedOperationResult(question,
                "My apologies, but it seems that you are asking too many things in a single question. Can you please ask one question at a time?",
                Core.Constants.Intent.MultipleIntents);
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }

    /// <summary>
    /// Handles question with information intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> HandleQuestionWithInformationIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:HandleQuestionWithInformationIntent", $"Handle question with information intent. Question: {question}", operationContext);
        try
        {
            var arguments = new Dictionary<string, object>()
            {
                [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
                [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
            };
            var chatResponse = await _genAIRepository.GenerateResponse(question, "General", "Information",
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
    /// Handles question with ability intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> HandleQuestionWithAbilityIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:HandleQuestionWithAbilityIntent", $"Handle question with ability intent. Question: {question}", operationContext);
        try
        {
            return await GetPredefinedOperationResult(question,
                "I can help you with your questions about Azure.",
                Core.Constants.Intent.Ability);
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }

    /// <summary>
    /// Handles question with unclear intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> HandleQuestionWithUnclearIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:HandleQuestionWithUnclearIntent", $"Handle question with unclear intent. Question: {question}", operationContext);
        try
        {
            return await GetPredefinedOperationResult(question,
                "My apologies, but I am not sure I understand the question. Can you please provide more details?",
                Core.Constants.Intent.Unclear);
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }

    /// <summary>
    /// Handles question with other intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> HandleQuestionWithOtherIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:HandleQuestionWithOtherIntent", $"Handle question with other intent. Question: {question}", operationContext);
        try
        {
            return await GetPredefinedOperationResult(question,
                "My apologies, but it seems the question is not related to Azure (I may be wrong though). Can you please clarify the question or ask me a question related to Azure.",
                Core.Constants.Intent.Other);
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }

    /// <summary>
    /// Creates a pre-defined chat response for certain question intents.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="response">
    /// Pre-defined response.
    /// </param>
    /// <param name="intent">
    /// Question intent.
    /// </param>
    /// <returns>
    /// Chat result. <see cref="SuccessOperationResult{ChatResponse}"/>.
    /// </returns>
    private static async Task<IOperationResult> GetPredefinedOperationResult(string question, string response, string intent)
    {
        var chatResponse = new ChatResponse()
        {
            Question = question,
            Response = response,
            Intent = Core.Constants.Intent.MultipleIntents
        };
        var result = await Task.FromResult(new SuccessOperationResult<ChatResponse>()
        {
            Item = chatResponse,
            StatusCode = HttpStatusCode.OK
        });
        return result;
    }
}