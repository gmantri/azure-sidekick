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
    /// Gets the response of a question based on the question's intent.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="intent">
    /// Question's intent.
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
    public async Task<IOperationResult> GetResponse(string question, string intent, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("GeneralChatManagementService:GetResponse", $"Get intent. Question: {question}; Intent: {intent}", operationContext);
        try
        {
            IOperationResult result;
            switch (intent)
            {
                case Core.Constants.Intent.Ability:
                    result = GetPredefinedOperationResult(question,
                        "I can help you with your questions about Azure.",
                        Core.Constants.Intent.Ability);;
                    break;
                case Core.Constants.Intent.MultipleIntents:
                    result = GetPredefinedOperationResult(question,
                        "My apologies, but it seems that you are asking too many things in a single question. Can you please ask one question at a time?",
                        Core.Constants.Intent.MultipleIntents);
                    break;
                case Core.Constants.Intent.Unclear:
                    result = GetPredefinedOperationResult(question,
                        "My apologies, but I am not sure I understand the question. Can you please provide more details?",
                        Core.Constants.Intent.Unclear);
                    break;
                case Core.Constants.Intent.Information:
                    result = await HandleQuestionWithInformationIntent(question, chatHistory,
                        context);
                    break;
                case Core.Constants.Intent.Azure:
                    result =
                        await HandleQuestionWithAzureIntent(question, chatHistory, context);
                    break;
                default:
                    result = GetPredefinedOperationResult(question,
                        "My apologies, but it seems the question is not related to Azure (I may be wrong though). Can you please clarify the question or ask me a question related to Azure.",
                        Core.Constants.Intent.Other);
                    break;
            }
            return result;
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
    private async Task<IOperationResult> HandleQuestionWithAzureIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default)
    {
        try
        {
            var arguments = new Dictionary<string, object>()
            {
                [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
                [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
            };
            var chatResponse = await _genAIRepository.GetResponse(question, "General", "Azure",
                arguments: arguments, operationContext: operationContext);
            return new SuccessOperationResult<ChatResponse>()
            {
                Item = chatResponse,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception exception)
        {
            return Helper.GetFailOperationResultFromException(exception, _logger, operationContext);
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
        try
        {
            var arguments = new Dictionary<string, object>()
            {
                [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
                [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
            };
            var chatResponse = await _genAIRepository.GetResponse(question, "General", "Information",
                arguments: arguments, operationContext: operationContext);
            return new SuccessOperationResult<ChatResponse>()
            {
                Item = chatResponse,
                StatusCode = HttpStatusCode.OK
            };
        }
        catch (Exception exception)
        {
            return Helper.GetFailOperationResultFromException(exception, _logger, operationContext);
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
    private static IOperationResult GetPredefinedOperationResult(string question, string response, string intent)
    {
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