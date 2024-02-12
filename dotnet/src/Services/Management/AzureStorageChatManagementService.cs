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

/// <summary>
/// Chat operations related to Azure storage.
/// </summary>
public class AzureStorageChatManagementService : BaseChatManagementService, IAzureChatManagementService
{
    /// <summary>
    /// Azure service name.
    /// </summary>
    public string ServiceName => Core.Constants.Intent.Storage;

    /// <summary>
    /// <see cref="IGenAIRepository"/>.
    /// </summary>
    private readonly IGenAIRepository _genAIRepository;
    
    /// <summary>
    /// <see cref="IStorageOperationsRepository"/>.
    /// </summary>
    private readonly IStorageOperationsRepository _storageOperationsRepository;

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
    private static IDictionary<string, string> _predefinedMessages;

    /// <summary>
    /// Create a new instance of <see cref="AzureStorageChatManagementService"/>.
    /// </summary>
    /// <param name="genAIRepository">
    /// <see cref="IGenAIRepository"/>.
    /// </param>
    /// <param name="storageOperationsRepository">
    /// <see cref="IStorageOperationsRepository"/>.
    /// </param>
    /// <param name="chatHistoryOperationsRepository">
    /// <see cref="IChatHistoryOperationsRepository"/>.
    /// </param>
    /// <param name="logger">
    /// <see cref="ILogger"/>.
    /// </param>
    public AzureStorageChatManagementService(IGenAIRepository genAIRepository, IStorageOperationsRepository storageOperationsRepository, IChatHistoryOperationsRepository chatHistoryOperationsRepository, ILogger logger)
    {
        _genAIRepository = genAIRepository;
        _storageOperationsRepository = storageOperationsRepository;
        _chatHistoryOperationsRepository = chatHistoryOperationsRepository;
        _logger = logger;
        _predefinedMessages = new Dictionary<string, string>()
        {
            ["UnableToAnswer"] =
                "My apologies, but based on the information available to me, I am unable to answer your question. Currently I can only answer questions about properties of storage accounts (e.g. how many storage accounts are without tags?) or that of a single storage account (e.g. where is xyz storage account located?)",
            ["NoStorageAccounts"] =
                "My apologies, but I am not able to find any storage accounts that you have access to in the selected subscription. Please choose another subscription or ask another question.",
            ["UnableToExtractStorageAccountName"] =
                "My apologies, but I am not able to figure out the name of the storage account from the question. Please ask the question again and have the name of the storage account explicitly mentioned in the question.",
            ["UnableToFindStorageAccountDetails"] =
                "My apologies, but I am not able to get the details about \"{0}\" storage account. Please make sure that the storage account exists in the selected subscription and you have permissions to access it."
        };
    }
    
    /// <summary>
    /// Get a response to user's question.
    /// </summary>
    /// <param name="subscriptionId">
    /// Azure subscription id.
    /// </param>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="credential">
    /// <see cref="TokenCredential"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async Task<IOperationResult> GetResponse(string subscriptionId, string question,
        TokenCredential credential = default, IOperationContext operationContext = default)
    {
        var context = new OperationContext("AzureStorageChatManagementService:GetResponse", $"Get response. Question: {question}", operationContext);
        try
        {
            var chatHistory = await _chatHistoryOperationsRepository.List(context.UserId, context);
            int promptTokens = 0, completionTokens = 0;
            // first get the intent of the question.
            var result = await RecognizeIntent(question, chatHistory, context);
            promptTokens += result.PromptTokens;
            completionTokens += result.CompletionTokens;
            var intent = result.Response;
            result = intent switch
            {
                Core.Constants.StorageIntents.GeneralInformation => await GetResponseForGeneralStorageInformationQuestion(question, chatHistory, context),
                Core.Constants.StorageIntents.StorageAccounts => await GetResponseForStorageAccountsQuestion(subscriptionId,
                    question, chatHistory, credential, context),
                Core.Constants.StorageIntents.StorageAccount => await GetResponseForStorageAccountQuestion(subscriptionId, 
                    question, chatHistory, credential, context),
                _ => new ChatResponse()
                {
                    Question = question,
                    Response = _predefinedMessages["UnableToAnswer"],
                    Intent = ServiceName,
                    Function = intent,
                    StoreInChatHistory = true
                }
            };

            result.PromptTokens += promptTokens;
            result.CompletionTokens += completionTokens;
            await Helper.SaveChatResponse(_chatHistoryOperationsRepository, result, context);
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

    /// <summary>
    /// Get a streaming response to user's question.
    /// </summary>
    /// <param name="subscriptionId">
    /// Azure subscription id.
    /// </param>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="credential">
    /// <see cref="TokenCredential"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// Chat result. Could be <see cref="SuccessOperationResult{ChatResponse}"/> or <see cref="FailOperationResult"/>.
    /// </returns>
    public async IAsyncEnumerable<IOperationResult> GetStreamingResponse(string subscriptionId, string question,
        TokenCredential credential = default,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("AzureStorageChatManagementService:GetStreamingResponse", $"Get response. Question: {question}", operationContext);
        IOperationResult failureResult = null;
        IOperationResult unableToAnswerResult = null;
        IAsyncEnumerable<IOperationResult> successResult = null;
        var promptTokens = 0;
        var completionTokens = 0;
        try
        {
            var chatHistory = await _chatHistoryOperationsRepository.List(context.UserId, context);
            // first get the intent of the question.
            var result = await RecognizeIntent(question, chatHistory, context);
            promptTokens += result.PromptTokens;
            completionTokens += result.CompletionTokens;
            var intent = result.Response;
            switch (intent)
            {
                case Core.Constants.StorageIntents.GeneralInformation:
                {
                    successResult = GetStreamingResponseForGeneralStorageInformationQuestion(question, chatHistory,
                        context);
                    break;
                }
                case Core.Constants.StorageIntents.StorageAccounts:
                {
                    successResult = GetStreamingResponseForStorageAccountsQuestion(subscriptionId, question, chatHistory,
                        credential, context);
                    break;
                }
                case Core.Constants.StorageIntents.StorageAccount:
                {
                    successResult = GetStreamingResponseForStorageAccountQuestion(subscriptionId, question, chatHistory,
                        credential, context);
                    break;
                }
                default:
                {
                    unableToAnswerResult = GetPredefinedOperationResult(question, _predefinedMessages["UnableToAnswer"],
                        intent, promptTokens, completionTokens);
                    break;
                }
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
        if (unableToAnswerResult != null) yield return unableToAnswerResult;
        if (successResult == null) yield break;
        await foreach (var item in successResult)
        {
            if (item is SuccessOperationResult<ChatResponse> successResultItem)
            {
                // add prompt & completion tokens for get intent LLM call to the last chat response.
                if (successResultItem.StatusCode == HttpStatusCode.NoContent)
                {
                    successResultItem.Item.PromptTokens += promptTokens;
                    successResultItem.Item.CompletionTokens += completionTokens;
                }
                yield return successResultItem;
            }
            else
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Recognize intent of the question.
    /// </summary>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    private async Task<ChatResponse> RecognizeIntent(string question, IEnumerable<ChatResponse> chatHistory,
        IOperationContext operationContext = default)
    {
        var arguments = GetDefaultChatArguments(chatHistory);
        return await _genAIRepository.GetResponse(question, ServiceName, "Intent", arguments,
            operationContext);
    }

    /// <summary>
    /// Get response for a general storage information related question.
    /// </summary>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    private async Task<ChatResponse> GetResponseForGeneralStorageInformationQuestion(string question,
        IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default)
    {
        var arguments = GetDefaultChatArguments(chatHistory);
        return await _genAIRepository.GetResponse(question, ServiceName,
            Core.Constants.StorageIntents.GeneralInformation, arguments, operationContext);
    }

    /// <summary>
    /// Get streaming response for a general storage information related question.
    /// </summary>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    private async IAsyncEnumerable<IOperationResult> GetStreamingResponseForGeneralStorageInformationQuestion(string question,
        IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default)
    {
        var arguments = GetDefaultChatArguments(chatHistory);
        var successResult = GetStreamingResponseFromLlm(question, Core.Constants.StorageIntents.GeneralInformation, arguments,
            operationContext);
        await foreach (var item in successResult)
        {
            yield return item;
        }
    }

    /// <summary>
    /// Get response for a question related to storage accounts in a subscription.
    /// </summary>
    /// <param name="subscriptionId">
    /// Subscription id.
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
    /// <see cref="ChatResponse"/>.
    /// </returns>
    private async Task<ChatResponse> GetResponseForStorageAccountsQuestion(string subscriptionId, string question,
        IEnumerable<ChatResponse> chatHistory,
        TokenCredential credential = default, IOperationContext operationContext = default)
    {
        var storageAccounts = (await _storageOperationsRepository.List(subscriptionId, credential, operationContext)).ToList();
        if (storageAccounts.Count == 0)
        {
            return new ChatResponse()
            {
                Question = question,
                Response = _predefinedMessages["NoStorageAccounts"],
                Intent = ServiceName,
                Function = Core.Constants.StorageIntents.StorageAccounts,
                StoreInChatHistory = true
            };
        }
        var context = new StringBuilder();
        foreach (var storageAccount in storageAccounts)
        {
            context.AppendLine(storageAccount.ConvertToYaml());
            context.AppendLine("-------------------");
        }
        var arguments = GetDefaultChatArguments(chatHistory);
        arguments[Constants.ChatArguments.Context] = context.ToString();
        return await _genAIRepository.GetResponse(question, ServiceName,
            Core.Constants.StorageIntents.StorageAccounts, arguments, operationContext);
    }

    /// <summary>
    /// Get streaming response for a question related to storage accounts in a subscription.
    /// </summary>
    /// <param name="subscriptionId">
    /// Subscription id.
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
    /// <see cref="ChatResponse"/>.
    /// </returns>
    private async IAsyncEnumerable<IOperationResult> GetStreamingResponseForStorageAccountsQuestion(string subscriptionId, string question,
        IEnumerable<ChatResponse> chatHistory, 
        TokenCredential credential = default, IOperationContext operationContext = default)
    {
        var storageAccounts = (await _storageOperationsRepository.List(subscriptionId, credential, operationContext)).ToList();
        if (storageAccounts.Count == 0)
        {
            var noStorageAccountsOperationResult = GetPredefinedOperationResult(question,
                _predefinedMessages["NoStorageAccounts"], Core.Constants.StorageIntents.StorageAccounts, 0, 0);
            yield return noStorageAccountsOperationResult;
        }
        else
        {
            var context = new StringBuilder();
            foreach (var storageAccount in storageAccounts)
            {
                context.AppendLine(storageAccount.ConvertToYaml());
                context.AppendLine("-------------------");
            }
            var arguments = GetDefaultChatArguments(chatHistory);
            arguments[Constants.ChatArguments.Context] = context.ToString();
            var successResult = GetStreamingResponseFromLlm(question, Core.Constants.StorageIntents.StorageAccounts, arguments,
                operationContext);

            await foreach (var item in successResult)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Get response for a question related to a specific storage account in a subscription.
    /// </summary>
    /// <param name="subscriptionId">
    /// Subscription id.
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
    /// <see cref="ChatResponse"/>.
    /// </returns>
    private async Task<ChatResponse> GetResponseForStorageAccountQuestion(string subscriptionId, string question,
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
                Response = _predefinedMessages["UnableToExtractStorageAccountName"],
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                StoreInChatHistory = true
            };
        }

        var storageAccount =
            await _storageOperationsRepository.Get(subscriptionId, storageEntities.StorageAccount, credential, operationContext);
        if (storageAccount == null)
        {
            return new ChatResponse()
            {
                Question = question,
                Response = string.Format(_predefinedMessages["UnableToFindStorageAccountDetails"], storageEntities.StorageAccount),
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                StoreInChatHistory = true
            };
        }
        var arguments = GetDefaultChatArguments(chatHistory);
        arguments[Constants.ChatArguments.Context] = storageAccount.ConvertToYaml();
        result = await _genAIRepository.GetResponse(question, ServiceName,
            Core.Constants.StorageIntents.StorageAccount, arguments, operationContext);
        result.PromptTokens += promptTokens;
        result.CompletionTokens += completionTokens;
        return result;
    }

    /// <summary>
    /// Get streaming response for a question related to a specific storage account in a subscription.
    /// </summary>
    /// <param name="subscriptionId">
    /// Subscription id.
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
    /// <see cref="ChatResponse"/>.
    /// </returns>
    private async IAsyncEnumerable<IOperationResult> GetStreamingResponseForStorageAccountQuestion(string subscriptionId, string question,
        IEnumerable<ChatResponse> chatHistory,
        TokenCredential credential = default, IOperationContext operationContext = default)
    {
        var promptTokens = 0;
        var completionTokens = 0;
        var result = await GetStorageEntities(question, chatHistory, operationContext);
        promptTokens += result.PromptTokens;
        completionTokens += result.CompletionTokens;
        var storageEntities = ExtractStorageEntitiesFromChatResponse(result);
        if (string.IsNullOrWhiteSpace(storageEntities?.StorageAccount))
        {
            var failedToExtractStorageAccountNameOperationResult = GetPredefinedOperationResult(question,
                _predefinedMessages["UnableToExtractStorageAccountName"], Core.Constants.StorageIntents.StorageAccount, promptTokens, completionTokens);
            yield return failedToExtractStorageAccountNameOperationResult;
        }
        else
        {
            var storageAccount =
                await _storageOperationsRepository.Get(subscriptionId, storageEntities.StorageAccount, credential, operationContext);
            if (storageAccount == null)
            {
                var unableToFindStorageAccountOperationResult = GetPredefinedOperationResult(question,
                    string.Format(_predefinedMessages["UnableToFindStorageAccountDetails"],
                        storageEntities.StorageAccount), Core.Constants.StorageIntents.StorageAccount, promptTokens,
                    completionTokens);
                yield return unableToFindStorageAccountOperationResult;
            }
            else
            {
                var arguments = GetDefaultChatArguments(chatHistory);
                arguments[Constants.ChatArguments.Context] = storageAccount.ConvertToYaml();
                var successResult = GetStreamingResponseFromLlm(question, Core.Constants.StorageIntents.StorageAccount, arguments,
                    operationContext, promptTokens, completionTokens);

                await foreach (var item in successResult)
                {
                    yield return item;
                }
            }
        }
    }

    /// <summary>
    /// Extract the name of storage entities mentioned in the user's question.
    /// </summary>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    private async Task<ChatResponse> GetStorageEntities(string question,
        IEnumerable<ChatResponse> chatHistory, IOperationContext operationContext = default)
    {
        var arguments = GetDefaultChatArguments(chatHistory);
        return await _genAIRepository.GetResponse(question, ServiceName,
            "EntityRecognition", arguments, operationContext);
    }

    /// <summary>
    /// Get and process streaming response from LLM.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="functionName">
    /// Semantic function to invoke.
    /// </param>
    /// <param name="arguments">
    /// Arguments for prompt execution.
    /// </param>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    /// <param name="previousPromptTokens">
    /// Prompt tokens from any previous LLM calls before calling this method.
    /// </param>
    /// <param name="previousCompletionTokens">
    /// Completion tokens from any previous LLM calls before calling this method.
    /// </param>
    private async IAsyncEnumerable<IOperationResult> GetStreamingResponseFromLlm(string question, string functionName,
        IDictionary<string, object> arguments, IOperationContext operationContext, int previousPromptTokens = 0, int previousCompletionTokens = 0)
    {
        var streamingResponseResult = _genAIRepository.GetStreamingResponse(question, ServiceName,
            functionName, arguments, operationContext);
        await foreach (var chatResponse in streamingResponseResult)
        {
            var isLastResponse = (chatResponse.PromptTokens > 0 || chatResponse.CompletionTokens > 0) &&
                                 chatResponse.StoreInChatHistory;
            if (isLastResponse)
            {
                chatResponse.PromptTokens += previousPromptTokens;
                chatResponse.CompletionTokens += previousCompletionTokens;
            }
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
    /// Extract storage entities from a chat response.
    /// </summary>
    /// <param name="chatResponse">
    /// <see cref="ChatResponse"/>.
    /// </param>
    /// <returns>
    /// <see cref="StorageEntities"/>.
    /// </returns>
    private StorageEntities ExtractStorageEntitiesFromChatResponse(ChatResponse chatResponse)
    {
        var response = chatResponse.Response;
        var storageEntities = System.Text.Json.JsonSerializer.Deserialize<StorageEntities>(response);
        return storageEntities;
    }
    
    /// <summary>
    /// Create a pre-defined chat response for certain question intents.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="message">
    /// Message to be included in response.
    /// </param>
    /// <param name="function">
    /// Question sub intent.
    /// </param>
    /// <param name="promptTokens">
    /// Prompt tokens.
    /// </param>
    /// <param name="completionTokens">
    /// Completion tokens.
    /// </param>
    /// <returns>
    /// Chat result. <see cref="SuccessOperationResult{ChatResponse}"/>.
    /// </returns>
    private IOperationResult GetPredefinedOperationResult(string question, string message, string function, int promptTokens, int completionTokens)
    {
        var chatResponse = new ChatResponse()
        {
            Question = question,
            Response = message,
            Intent = ServiceName,
            Function = function,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens
        };
        var result = new SuccessOperationResult<ChatResponse>()
        {
            Item = chatResponse,
            StatusCode = HttpStatusCode.OK
        };
        return result;
    }

    /// <summary>
    /// Trim chat history by removing items more than needed.
    /// </summary>
    /// <param name="chatHistory"></param>
    /// <returns>
    /// Trimmed chat history.
    /// </returns>
    protected override IEnumerable<ChatResponse> TrimChatHistory(IEnumerable<ChatResponse> chatHistory)
    {
        var chatHistoryItems = chatHistory == null ? new List<ChatResponse>() : chatHistory.ToList();
        chatHistoryItems = chatHistoryItems.Where(c => c.Intent == ServiceName || c.Intent == Core.Constants.Intent.Information).ToList();
        return base.TrimChatHistory(chatHistoryItems);
    }
}

/// <summary>
/// Class representing names of the storage entities mentioned in a question.
/// </summary>
internal class StorageEntities
{
    /// <summary>
    /// Storage account name.
    /// </summary>
    public string StorageAccount { get; set; }
    
    /// <summary>
    /// Blob container name.
    /// </summary>
    public string BlobContainer { get; set; }
    
    /// <summary>
    /// Queue name.
    /// </summary>
    public string Queue { get; set; }
    
    /// <summary>
    /// Table name.
    /// </summary>
    public string Table { get; set; }
    
    /// <summary>
    /// File share name.
    /// </summary>
    public string FileShare { get; set; }
}