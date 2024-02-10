using Azure.ResourceManager.Resources;
using AzureSidekick.Core;
using AzureSidekick.Core.EventArgs;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.OperationResults;
using AzureSidekick.Services.Interfaces;

namespace AzureSidekick.ConsoleUI;

public class Main
{
    /// <summary>
    /// <see cref="ISubscriptionManagementService"/>.
    /// </summary>
    private readonly ISubscriptionManagementService _subscriptionManagementService;

    /// <summary>
    /// <see cref="IGeneralChatManagementService"/>.
    /// </summary>
    private readonly IGeneralChatManagementService _generalChatManagementService;

    /// <summary>
    /// <see cref="IAzureChatManagementServiceFactory"/>.
    /// </summary>
    private readonly IAzureChatManagementServiceFactory _azureChatManagementServiceFactory;

    /// <summary>
    /// <see cref="ILogger"/>.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// List of subscriptions logged-in user has access to.
    /// </summary>
    private IEnumerable<SubscriptionData> _subscriptions;

    /// <summary>
    /// Currently selected subscription.
    /// </summary>
    private (string subscriptionId, string subscriptionName) _selectedSubscription = ("", "");

    /// <summary>
    /// Indicates if the response should be streamed or not (default is true).
    /// </summary>
    private bool _getStreamingResponse = true;

    /// <summary>
    /// Dummy user id.
    /// </summary>
    private readonly string _dummyUserId = Guid.Empty.ToString();
    
    /// <summary>
    /// Initialize an instance of <see cref="Main"/>.
    /// </summary>
    /// <param name="subscriptionManagementService">
    /// <see cref="ISubscriptionManagementService"/>.
    /// </param>
    /// <param name="generalChatManagementService">
    /// <see cref="IGeneralChatManagementService"/>.
    /// </param>
    /// <param name="azureChatManagementServiceFactory">
    /// <see cref="IAzureChatManagementServiceFactory"/>.
    /// </param>
    /// <param name="logger">
    /// <see cref="ILogger"/>.
    /// </param>
    public Main(ISubscriptionManagementService subscriptionManagementService, IGeneralChatManagementService generalChatManagementService, IAzureChatManagementServiceFactory azureChatManagementServiceFactory, ILogger logger)
    {
        _subscriptionManagementService = subscriptionManagementService;
        _generalChatManagementService = generalChatManagementService;
        _azureChatManagementServiceFactory = azureChatManagementServiceFactory;
        _logger = logger;
    }
    
    /// <summary>
    /// Run the application.
    /// </summary>
    /// <param name="args">
    /// Application arguments.
    /// </param>
    public async Task Run(string[] args)
    {
        Welcome();
        Console.WriteLine("Listing subscriptions. Please wait.");
        _subscriptions = await ListSubscriptions();
        _selectedSubscription = GetSubscriptionIdAndName(SelectSubscription());
        Console.WriteLine(@$"
You have selected ""{_selectedSubscription.subscriptionName} ({_selectedSubscription.subscriptionId})"" subscription.
        ");
        await Chat();
    }

    /// <summary>
    /// Chat.
    /// </summary>
    private async Task Chat()
    {
        var continueLoop = true;
        do
        {
            Console.WriteLine("");
            Console.WriteLine("[Azure Sidekick] Please ask a question or enter a command.");
            var userInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(userInput)) continue;
            var context = new OperationContext("Main:Chat", $"Question: {userInput}")
            {
                UserId = _dummyUserId
            };
            var logOperation = false;
            try
            {
                switch (userInput.ToLower())
                {
                    case "cls":
                    case "clear":
                    {
                        Console.Clear();
                        break;
                    }
                    case "exit":
                    case "quit":
                    {
                        continueLoop = false;
                        break;
                    }
                    case "change subscription":
                    {
                        _selectedSubscription = GetSubscriptionIdAndName(SelectSubscription());
                        await _generalChatManagementService.ClearChatHistory(context);
                        Console.WriteLine($"Active subscription changed to \"{_selectedSubscription.subscriptionName} ({_selectedSubscription.subscriptionId})\".");
                        break;
                    }
                    case "clear chat history":
                    {
                        await _generalChatManagementService.ClearChatHistory(context);
                        Console.WriteLine("Chat history cleared.");
                        break;
                    }
                    case "toggle response mode":
                    {
                        _getStreamingResponse = !_getStreamingResponse;
                        Console.WriteLine(_getStreamingResponse ? "Response will be streamed." : "Response will not be streamed.");
                        break;
                    }
                    case "help":
                    {
                        Help();
                        break;
                    }
                    default:
                    {
                        logOperation = true;
                        var state = new StreamingResponseState()
                        {
                            UserInput = userInput
                        };
                        int promptTokens = 0, completionTokens = 0;
                        // first rephrase the question.
                        var result = await GetRephrasedQuestion(userInput, context);
#if DEBUG
                        Console.WriteLine($"Rephrased question: {result.Response}");
#endif
                        state.PromptTokens += result.PromptTokens;
                        state.CompletionTokens += result.CompletionTokens;
                        promptTokens += result.PromptTokens;
                        completionTokens += result.CompletionTokens;
                        var question = result.Response;
                        // now let's get the service intent
                        result = await GetIntent(question, context);
                        state.PromptTokens += result.PromptTokens;
                        state.CompletionTokens += result.CompletionTokens;
                        promptTokens += result.PromptTokens;
                        completionTokens += result.CompletionTokens;
                        var intent = result.Response;
#if DEBUG
                        Console.WriteLine($"Question intent: {intent}");
#endif
                        if (_getStreamingResponse)
                        {
                            await GetStreamingResponse(question, intent, state, context);
                        }
                        else
                        {
                            result = await GetResponse(question, intent, context);
                            Console.WriteLine(result.Response);
                            promptTokens += result.PromptTokens;
                            completionTokens += result.CompletionTokens;
#if DEBUG
                            Console.WriteLine($"Token usage - Prompt tokens: {promptTokens}; Completion tokens: {completionTokens}");
#endif
                            result.OriginalQuestion = userInput;
                            _logger?.LogChatResponse(result, context);
                        }
                        break;
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("An error occurred while processing request. Please see error log for more details.");
            }
            finally
            {
                if (logOperation)
                {
                    _logger?.LogOperation(context);
                }
            }
        } while (continueLoop);
        Console.WriteLine("Thank you for using Azure Sidekick!");
    }

    /// <summary>
    /// List subscriptions a user has access to.
    /// </summary>
    /// <returns>
    /// <see cref="IEnumerable{SubscriptionData}"/>.
    /// </returns>
    /// <exception cref="Exception"></exception>
    private async Task<IEnumerable<SubscriptionData>> ListSubscriptions()
    {
        var context = new OperationContext("Main:ListSubscriptions", "List subscriptions");
        try
        {
            var result = await _subscriptionManagementService.List(operationContext: context);
            if (!result.IsOperationSuccessful)
            {
                throw result.Error;
            }

            var successResult = (SuccessOperationResult<SubscriptionData>)result;
            return successResult.Items;
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }

    /// <summary>
    /// Select a subscription from the list of subscriptions a user has access to.
    /// </summary>
    /// <returns>
    /// <see cref="SubscriptionData"/>.
    /// </returns>
    private SubscriptionData SelectSubscription()
    {
        var subscriptionsList = (List<SubscriptionData>)_subscriptions;
        var numberOfSubscriptions = subscriptionsList.Count;
        if (numberOfSubscriptions == 0)
        {
            Console.WriteLine("We are not able to find any subscriptions that you have access to.");
            Console.WriteLine("Please make sure that the signed-in account has access to at least one subscription.");
            Console.WriteLine("Good bye!");
            Environment.Exit(0);
        }
        else
        {
            Console.Write("#".PadRight(5));
            Console.Write("Subscription Id".PadRight(41));
            Console.Write("Subscription Name" + Environment.NewLine);
            Console.WriteLine(string.Concat(Enumerable.Repeat("=", 96)));
            for (var i = 0; i < numberOfSubscriptions; i++)
            {
                var subscription = subscriptionsList[i];
                var subscriptionInfo = GetSubscriptionIdAndName(subscription);
                var subscriptionId = subscriptionInfo.subscriptionId;
                var subscriptionName = subscriptionInfo.subscriptionName;
                Console.Write((i+1).ToString().PadRight(5));
                Console.Write(subscriptionId);
                Console.Write("     ");
                Console.Write(subscriptionName.Substring(0, Math.Min(subscriptionName.Length, 50)) + Environment.NewLine);
            }

            do
            {
                Console.WriteLine($"Please select a subscription by entering a number between 1 and {numberOfSubscriptions}. To select the first subscription from the list, just press enter.");
                var response = Console.ReadLine()?.Trim();
                if (response == "") return subscriptionsList[0];
                if (int.TryParse(response, out var index) && (index > 0 && index <= numberOfSubscriptions))
                {
                    return subscriptionsList[index - 1];
                }
            } while (true);
        }

        return null;
    }

    /// <summary>
    /// Print welcome message.
    /// </summary>
    private static void Welcome()
    {
        var message = @"
Hello and welcome to Azure Sidekick! 

I am an AI assistant that can answer questions about Azure services and resources in your Azure Subscriptions.

I am still being developed so please do not get frustrated if I am not able to answer all of your questions.

Currently, I can provide answers to:
- your general questions about Azure.
- your general questions about Azure Storage.
- your questions about storage accounts in your Azure subscriptions.
- your questions about a specific storage account in your Azure subscription.

Here are the commands that you can use:
- ""change subscription"": Use this command if you need to change the subscription.
- ""clear chat history"": Use this command to clear chat history.
- ""toggle response mode"": Use this command to toggle the response mode between streaming (default, recommended) and non-streaming.
- ""cls"" or ""clear"": Use either of these commands to clear the console.
- ""help"": Use this command to see help.
- ""exit"" or ""quit"": Use either of these commands to exit the application.

Before you begin:
=================
Please ensure that you have signed-in into your Azure account using Azure CLI, Azure PowerShell,
Visual Studio or Visual Studio Code. Your Azure credentials are used to fetch information about
storage accounts in your Azure Subscriptions.

Press any key to continue.
        ";
        Console.WriteLine(message);
        Console.ReadKey();
    }

    /// <summary>
    /// Print help message.
    /// </summary>
    private static void Help()
    {
        var message = @"
Hi, I am Azure Sidekick! I am here to answer questions about Azure resources and services in your Azure Subscriptions.

I am still being developed so please do not get frustrated if I am not able to answer all of your questions.

Currently, I can provide answers to:
- your general questions about Azure.
- your general questions about Azure Storage.
- your questions about storage accounts in your Azure subscriptions.
- your questions about a specific storage account in your Azure subscription.

Here are the commands that you can use:
- ""change subscription"": Use this command if you need to change the subscription.
- ""clear chat history"": Use this command to clear chat history.
- ""toggle response mode"": Use this command to toggle the response mode between streaming (default, recommended) and non-streaming.
- ""cls"" or ""clear"": Use either of these commands to clear the console.
- ""help"": Use this command to see help.
- ""exit"" or ""quit"": Use either of these commands to exit the application.
    ";
        Console.WriteLine(message);
    }

    /// <summary>
    /// Extract the subscription id and name.
    /// </summary>
    /// <param name="subscription">
    /// <see cref="SubscriptionData"/>.
    /// </param>
    /// <returns>
    /// A tuple with 2 elements, first being subscription id and second being subscription name.
    /// </returns>
    private static (string subscriptionId, string subscriptionName) GetSubscriptionIdAndName(
        SubscriptionData subscription)
    {
        var subscriptionId = subscription.SubscriptionId;
        var subscriptionName = subscription.DisplayName;
        return (subscriptionId, subscriptionName);
    }

    /// <summary>
    /// Rephrase the user's question so that it is clear.
    /// </summary>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="context">
    /// <see cref="IOperationContext"/>.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    /// <exception cref="Exception"></exception>
    private async Task<ChatResponse> GetRephrasedQuestion(string question, IOperationContext context)
    {
        var result = await _generalChatManagementService.Rephrase(question, context);
        if (!result.IsOperationSuccessful) throw result.Error;
        var successResult = (SuccessOperationResult<ChatResponse>)result;
        return successResult.Item;
    }

    /// <summary>
    /// Find out the intent of the question.
    /// </summary>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="context">
    /// <see cref="IOperationContext"/>.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    /// <exception cref="Exception"></exception>
    private async Task<ChatResponse> GetIntent(string question, IOperationContext context)
    {
        var result = await _generalChatManagementService.GetIntent(question, context);
        if (!result.IsOperationSuccessful) throw result.Error;
        var successResult = (SuccessOperationResult<ChatResponse>)result;
        return successResult.Item;
    }

    /// <summary>
    /// Answer the question based on the intent.
    /// </summary>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="intent">
    /// Intent of the question.
    /// </param>
    /// <param name="context">
    /// <see cref="IOperationContext"/>.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    /// <exception cref="Exception"></exception>
    private async Task<ChatResponse> GetResponse(string question, string intent, IOperationContext context)
    {
        IOperationResult result;
        switch (intent)
        {
            case Constants.Intent.Ability:
            case Constants.Intent.MultipleIntents:
            case Constants.Intent.Unclear:
            case Constants.Intent.Other:
            case Constants.Intent.Information:
            case Constants.Intent.Azure:
                result = await _generalChatManagementService.GetResponse(question, intent, context);
                break;
            default:
                var azureChatManagementService = _azureChatManagementServiceFactory.GetService(intent);
                result = await azureChatManagementService.GetResponse(_selectedSubscription.subscriptionId,
                    question, operationContext: context);
                break;
        }
        if (!result.IsOperationSuccessful) throw result.Error;
        var successResult = (SuccessOperationResult<ChatResponse>)result;
        return successResult.Item;
    }

    /// <summary>
    /// Answer the question based on the intent in a streaming manner.
    /// </summary>
    /// <param name="question">
    /// User's question.
    /// </param>
    /// <param name="intent">
    /// Intent of the question.
    /// </param>
    /// <param name="state">
    /// <see cref="StreamingResponseState"/>.
    /// </param>
    /// <param name="context">
    /// <see cref="IOperationContext"/>.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    /// <exception cref="Exception"></exception>
    private async Task GetStreamingResponse(string question, string intent, StreamingResponseState state, IOperationContext context)
    {
        switch (intent)
        {
            case Constants.Intent.Ability:
            case Constants.Intent.MultipleIntents:
            case Constants.Intent.Unclear:
            case Constants.Intent.Other:
            case Constants.Intent.Information:
            case Constants.Intent.Azure:
            {
                _generalChatManagementService.OperationResultReceivedEventHandler -= HandleOperationResultReceivedEvent;
                _generalChatManagementService.OperationResultReceivedEventHandler += HandleOperationResultReceivedEvent;
                await _generalChatManagementService.GetStreamingResponse(
                    question: question, 
                    intent: intent, 
                    state: state, 
                    operationContext: context);
                break;
            }
            default:
                var azureChatManagementService = _azureChatManagementServiceFactory.GetService(intent);
                azureChatManagementService.OperationResultReceivedEventHandler -= HandleOperationResultReceivedEvent;
                azureChatManagementService.OperationResultReceivedEventHandler += HandleOperationResultReceivedEvent;
                await azureChatManagementService.GetStreamingResponse(
                    subscriptionId: _selectedSubscription.subscriptionId, 
                    question: question, 
                    state: state, 
                    operationContext: context);
                break;
        }
    }

    /// <summary>
    /// Operation result received event handler.
    /// </summary>
    /// <param name="sender">
    /// Event sender.
    /// </param>
    /// <param name="args">
    /// Event arguments.
    /// </param>
    private void HandleOperationResultReceivedEvent(object sender, EventArgs args)
    {
        var eventArgs = (OperationResultReceivedEventArgs)args;
        var operationResult = eventArgs.OperationResult;
        if (!operationResult.IsOperationSuccessful)
        {
            Console.WriteLine("An error occurred while processing request. Please see error log for more details.");
        }
        else
        {
            var successResult = (SuccessOperationResult<ChatResponse>)operationResult;
            var chatResponse = successResult.Item;
            if (!eventArgs.IsLastResponse)
            {
                Console.Write(chatResponse.Response);
            }
            else
            {
                Console.WriteLine();
                var state = eventArgs.State;
#if DEBUG
                Console.WriteLine($"Token usage - Prompt tokens: {chatResponse.PromptTokens + state.PromptTokens}; Completion tokens: {chatResponse.CompletionTokens + state.CompletionTokens}");
#endif
                chatResponse.OriginalQuestion = state.UserInput;
                _logger?.LogChatResponse(chatResponse, eventArgs.State.OperationContext);
                Console.WriteLine("");
            }
        }
    }
}