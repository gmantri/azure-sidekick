using System.Net;
using System.Text;
using Azure.Core;
using AzureSidekick.Core.EventArgs;
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
    /// <param name="logger">
    /// <see cref="ILogger"/>.
    /// </param>
    public AzureStorageChatManagementService(IGenAIRepository genAIRepository, IStorageOperationsRepository storageOperationsRepository, ILogger logger)
    {
        _genAIRepository = genAIRepository;
        _storageOperationsRepository = storageOperationsRepository;
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
        _genAIRepository.ChatResponseReceivedEventHandler += HandleChatResponseReceivedEvent;
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
    public async Task<IOperationResult> GetResponse(string subscriptionId, string question, IEnumerable<ChatResponse> chatHistory,
        TokenCredential credential = default, IOperationContext operationContext = default)
    {
        var context = new OperationContext("AzureStorageChatManagementService:GetResponse", $"Get response. Question: {question}", operationContext);
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

    /// <summary>
    /// Get a streaming response to user's question.
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
    /// <param name="state">
    /// <see cref="StreamingResponseState"/>.
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
    public async Task GetStreamingResponse(string subscriptionId, string question,
        IEnumerable<ChatResponse> chatHistory, StreamingResponseState state = default, TokenCredential credential = default,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("AzureStorageChatManagementService:GetStreamingResponse", $"Get response. Question: {question}", operationContext);
        _tcs = new TaskCompletionSource<bool>();
        var streamingResponseState = state ?? new StreamingResponseState()
        {
            OperationContext = context
        };
        try
        {
            chatHistory = chatHistory?.ToList();
            // first get the intent of the question.
            var result = await RecognizeIntent(question, chatHistory, context);
            streamingResponseState.PromptTokens += result.PromptTokens;
            streamingResponseState.CompletionTokens += result.CompletionTokens;
            var intent = result.Response;
            switch (intent)
            {
                case Core.Constants.StorageIntents.GeneralInformation:
                {
                    await GetStreamingResponseForGeneralStorageInformationQuestion(question, chatHistory,
                        streamingResponseState, context);
                    break;
                }
                case Core.Constants.StorageIntents.StorageAccounts:
                {
                    await GetStreamingResponseForStorageAccountsQuestion(subscriptionId, question, chatHistory, streamingResponseState,
                        credential, context);
                    break;
                }
                case Core.Constants.StorageIntents.StorageAccount:
                {
                    await GetStreamingResponseForStorageAccountQuestion(subscriptionId, question, chatHistory,
                        streamingResponseState,
                        credential, context);
                    break;
                }
                default:
                {
                    GenerateEventArgsAndRaiseOperationResultReceivedEvent(question,
                        _predefinedMessages["UnableToAnswer"], intent, streamingResponseState);
                    break;
                }
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
    /// <param name="state">
    /// <see cref="StreamingResponseState"/>.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    private async Task GetStreamingResponseForGeneralStorageInformationQuestion(string question,
        IEnumerable<ChatResponse> chatHistory, StreamingResponseState state = default, IOperationContext operationContext = default)
    {
        var arguments = GetDefaultChatArguments(chatHistory);
        await _genAIRepository.GetStreamingResponse(question, ServiceName,
            Core.Constants.StorageIntents.GeneralInformation, arguments, state, operationContext);
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
                Function = Core.Constants.StorageIntents.StorageAccounts
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
    /// <param name="state">
    /// <see cref="StreamingResponseState"/>.
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
    private async Task GetStreamingResponseForStorageAccountsQuestion(string subscriptionId, string question,
        IEnumerable<ChatResponse> chatHistory, StreamingResponseState state,
        TokenCredential credential = default, IOperationContext operationContext = default)
    {
        var storageAccounts = (await _storageOperationsRepository.List(subscriptionId, credential, operationContext)).ToList();
        if (storageAccounts.Count == 0)
        {
            GenerateEventArgsAndRaiseOperationResultReceivedEvent(question, _predefinedMessages["NoStorageAccounts"],
                Core.Constants.StorageIntents.StorageAccounts, state);
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
            await _genAIRepository.GetStreamingResponse(question, ServiceName,
                Core.Constants.StorageIntents.StorageAccounts, arguments, state, operationContext);
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
                CompletionTokens = completionTokens
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
                CompletionTokens = completionTokens
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
    /// <param name="state">
    /// <see cref="StreamingResponseState"/>.
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
    private async Task GetStreamingResponseForStorageAccountQuestion(string subscriptionId, string question,
        IEnumerable<ChatResponse> chatHistory, StreamingResponseState state,
        TokenCredential credential = default, IOperationContext operationContext = default)
    {
        var result = await GetStorageEntities(question, chatHistory, operationContext);
        state.PromptTokens += result.PromptTokens;
        state.CompletionTokens += result.CompletionTokens;
        var storageEntities = ExtractStorageEntitiesFromChatResponse(result);
        if (string.IsNullOrWhiteSpace(storageEntities?.StorageAccount))
        {
            GenerateEventArgsAndRaiseOperationResultReceivedEvent(question, _predefinedMessages["UnableToExtractStorageAccountName"],
                Core.Constants.StorageIntents.StorageAccount, state);
        }
        else
        {
            var storageAccount =
                await _storageOperationsRepository.Get(subscriptionId, storageEntities.StorageAccount, credential, operationContext);
            if (storageAccount == null)
            {
                GenerateEventArgsAndRaiseOperationResultReceivedEvent(question, 
                    string.Format(_predefinedMessages["UnableToFindStorageAccountDetails"], storageEntities.StorageAccount),
                    Core.Constants.StorageIntents.StorageAccount, state);
            }
            else
            {
                var arguments = GetDefaultChatArguments(chatHistory);
                arguments[Constants.ChatArguments.Context] = storageAccount.ConvertToYaml();
                await _genAIRepository.GetStreamingResponse(question, ServiceName,
                    Core.Constants.StorageIntents.StorageAccount, arguments, state, operationContext);
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
    /// Chat response received event handler.
    /// </summary>
    /// <param name="sender">
    /// Event sender.
    /// </param>
    /// <param name="args">
    /// Event arguments. <see cref="ChatResponseReceivedEventArgs"/>.
    /// </param>
    private void HandleChatResponseReceivedEvent(object sender, EventArgs args)
    {
        var eventArgs = (ChatResponseReceivedEventArgs)args;
        var operationResult = new SuccessOperationResult<ChatResponse>()
        {
            Item = eventArgs.ChatResponse,
            StatusCode = HttpStatusCode.OK
        };
        RaiseOperationResultReceivedEvent(new OperationResultReceivedEventArgs()
        {
            OperationResult = operationResult,
            IsLastResponse = eventArgs.IsLastResponse,
            State = eventArgs.State
        });
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
    /// Generate event args and raise operation result received event.
    /// </summary>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="message">
    /// Message to be included in response.
    /// </param>
    /// <param name="function">
    /// Question sub intent.
    /// </param>
    /// <param name="state">
    /// <see cref="StreamingResponseState"/>.
    /// </param>
    private void GenerateEventArgsAndRaiseOperationResultReceivedEvent(string question, string message, string function, StreamingResponseState state)
    {
        var eventArgs = new OperationResultReceivedEventArgs()
        {
            OperationResult = new SuccessOperationResult<ChatResponse>()
            {
                StatusCode = HttpStatusCode.OK,
                Item = new ChatResponse()
                {
                    Question = question,
                    Response = message,
                    Intent = ServiceName,
                    Function = function,
                    StoreInChatHistory = true
                }
            },
            IsLastResponse = false,
            State = state
        };
        RaiseOperationResultReceivedEvent(eventArgs);
        // raise the same event again but set this as the last response.
        eventArgs.IsLastResponse = true;
        RaiseOperationResultReceivedEvent(eventArgs);
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