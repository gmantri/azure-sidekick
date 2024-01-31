using System.Net;
using System.Text;
using Azure.Core;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.OperationResults;
using AzureSidekick.Core.Utilities;
using AzureSidekick.Infrastructure.Interfaces;
using AzureSidekick.Services.Interfaces;
using AzureSidekick.Services.Utilities;

namespace AzureSidekick.Services.Management;

public class AzureStorageChatManagementService : BaseChatManagementService, IAzureChatManagementService
{
    public string ServiceName => Core.Constants.Intent.Storage;

    private readonly IGenAIRepository _genAIRepository;
    
    private readonly IStorageRepository _storageRepository;

    private readonly ILogger _logger;

    public AzureStorageChatManagementService(IGenAIRepository genAIRepository, IStorageRepository storageRepository, ILogger logger)
    {
        _genAIRepository = genAIRepository;
        _storageRepository = storageRepository;
        _logger = logger;
    }
    
    /// <summary>
    /// Answer a user's question.
    /// </summary>
    /// <param name="subscriptionId">
    /// Azure subscription id.
    /// </param>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="credential">
    /// <see cref="TokenCredential"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{T}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> ProcessQuestion(string subscriptionId, string question, IEnumerable<ChatResponse> chatHistory,
        TokenCredential credential = default, IOperationContext operationContext = default)
    {
        var context = new OperationContext("AzureStorageChatManagementService:ProcessQuestion", $"Process question. Question: {question}", operationContext);
        try
        {
            chatHistory = chatHistory?.ToList();
            int promptTokens = 0, completionTokens = 0;
            // first get the intent of the question.
            var result = await RecognizeIntent(question, chatHistory, context);
            promptTokens += result.PromptTokens;
            completionTokens += result.CompletionTokens;
            var intent = result.Response;
            result = intent switch
            {
                Core.Constants.StorageIntents.GeneralInformation => await ProcessQuestionRelatedToGeneralStorageInformation(question, chatHistory, context),
                Core.Constants.StorageIntents.StorageAccounts => await ProcessQuestionRelatedToStorageAccounts(subscriptionId,
                    question, chatHistory, credential, context),
                Core.Constants.StorageIntents.StorageAccount => await ProcessQuestionRelatedToStorageAccount(subscriptionId, 
                    question, chatHistory, credential, context),
                _ => new ChatResponse()
                {
                    Question = question,
                    Response =
                        "My apologies, but based on the information available to me, I will not able to answer your question. Currently I can only answer questions about properties of storage accounts (e.g. how many storage accounts are without tags?) or that of a single storage account (e.g. where is xyz storage account located?)",
                    Intent = ServiceName,
                    Function = intent
                }
            };

            result.PromptTokens += promptTokens;
            result.CompletionTokens += completionTokens;
            return new SuccessOperationResult<ChatResponse>()
            {
                Item = result,
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

    private async Task<ChatResponse> RecognizeIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default)
    {
        var arguments = new Dictionary<string, object>()
        {
            [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
            [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
        };
        return await _genAIRepository.GenerateResponse(question, ServiceName, "Intent", arguments,
            operationContext);
    }

    private async Task<ChatResponse> ProcessQuestionRelatedToGeneralStorageInformation(string question,
        IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default)
    {
        var arguments = new Dictionary<string, object>()
        {
            [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
            [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
        };
        return await _genAIRepository.GenerateResponse(question, ServiceName,
            Core.Constants.StorageIntents.GeneralInformation, arguments, operationContext);
    }

    private async Task<ChatResponse> ProcessQuestionRelatedToStorageAccounts(string subscriptionId, string question,
        IEnumerable<ChatResponse> chatHistory,
        TokenCredential credential = default, IOperationContext operationContext = default)
    {
        var storageAccounts = (await _storageRepository.List(subscriptionId, credential, operationContext)).ToList();
        if (storageAccounts.Count == 0)
        {
            return new ChatResponse()
            {
                Question = question,
                Response =
                    "My apologies, but I am not able to find any storage accounts that you have access to in the selected subscription. Please choose another subscription or ask another question.",
                Intent = ServiceName,
                Function = Core.Constants.StorageIntents.StorageAccounts
            };
        }
        var context = new StringBuilder();
        foreach (var storageAccount in storageAccounts)
        {
            context.AppendLine(storageAccount.ConvertToYaml());
            context.AppendLine("-------------------");
        }
        var arguments = new Dictionary<string, object>()
        {
            [Constants.ChatArguments.Context] = context.ToString(),
            [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
            [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
        };
        return await _genAIRepository.GenerateResponse(question, ServiceName,
            Core.Constants.StorageIntents.StorageAccounts, arguments, operationContext);
    }

    private async Task<ChatResponse> ProcessQuestionRelatedToStorageAccount(string subscriptionId, string question,
        IEnumerable<ChatResponse> chatHistory,
        TokenCredential credential = default, IOperationContext operationContext = default)
    {
        var result = await GetStorageEntities(question, chatHistory, operationContext);
        var promptTokens = result.PromptTokens;
        var completionTokens = result.CompletionTokens;
        var storageEntities = ExtractStorageEntitiesFromChatResponse(result);
        if (string.IsNullOrWhiteSpace(storageEntities?.StorageAccount))
        {
            return new ChatResponse()
            {
                Question = question,
                Response =
                    "My apologies, but I am not able to figure out the name of the storage account from the question. Please ask the question again and have the name of the storage account explicitly mentioned in the question.",
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }

        var storageAccount =
            await _storageRepository.Get(subscriptionId, storageEntities.StorageAccount, credential, operationContext);
        if (storageAccount == null)
        {
            return new ChatResponse()
            {
                Question = question,
                Response =
                    $"My apologies, but I am not able to get the details about \"{storageEntities.StorageAccount}\" storage account. Please make sure that the storage account exists in the selected subscription and you have permissions to access it.",
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens
            };
        }
        var arguments = new Dictionary<string, object>()
        {
            [Constants.ChatArguments.Context] = storageAccount.ConvertToYaml(),
            [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
            [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
        };
        result = await _genAIRepository.GenerateResponse(question, ServiceName,
            Core.Constants.StorageIntents.StorageAccount, arguments, operationContext);
        result.PromptTokens += promptTokens;
        result.CompletionTokens += completionTokens;
        return result;
    }

    private async Task<ChatResponse> GetStorageEntities(string question,
        IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default)
    {
        var arguments = new Dictionary<string, object>()
        {
            [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
            [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
        };
        return await _genAIRepository.GenerateResponse(question, ServiceName,
            "EntityRecognition", arguments, operationContext);
    }

    private StorageEntities ExtractStorageEntitiesFromChatResponse(ChatResponse chatResponse)
    {
        var response = chatResponse.Response;
        var storageEntities = System.Text.Json.JsonSerializer.Deserialize<StorageEntities>(response);
        return storageEntities;
    }
    
    protected override IEnumerable<ChatResponse> TrimChatHistory(IEnumerable<ChatResponse> chatHistory)
    {
        var chatHistoryItems = chatHistory == null ? new List<ChatResponse>() : chatHistory.ToList();
        chatHistoryItems = chatHistoryItems.Where(c => c.Intent == ServiceName || c.Intent == Core.Constants.Intent.Information).Take(Math.Min(chatHistoryItems.Count, MaxChatHistoryItems)).ToList();
        return chatHistoryItems;
    }
}

internal class StorageEntities
{
    public string StorageAccount { get; set; }
    public string BlobContainer { get; set; }
    public string Queue { get; set; }
    public string Table { get; set; }
    public string FileShare { get; set; }
}