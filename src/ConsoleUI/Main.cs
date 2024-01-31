using Azure.ResourceManager.Resources;
using AzureSidekick.Core;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.OperationResults;
using AzureSidekick.Services.Interfaces;

namespace AzureSidekick.ConsoleUI;

public class Main
{
    private readonly ISubscriptionManagementService _subscriptionManagementService;

    private readonly IGeneralChatManagementService _generalChatManagementService;

    private readonly IAzureChatManagementServiceFactory _azureChatManagementServiceFactory;

    private readonly ILogger _logger;

    private IEnumerable<SubscriptionData> _subscriptions;

    private (string subscriptionId, string subscriptionName) _selectedSubscription = ("", "");

    private readonly List<ChatResponse> _chatHistory = new List<ChatResponse>();
    
    public Main(ISubscriptionManagementService subscriptionManagementService, IGeneralChatManagementService generalChatManagementService, IAzureChatManagementServiceFactory azureChatManagementServiceFactory, ILogger logger)
    {
        _subscriptionManagementService = subscriptionManagementService;
        _generalChatManagementService = generalChatManagementService;
        _azureChatManagementServiceFactory = azureChatManagementServiceFactory;
        _logger = logger;
    }
    
    public async Task Run(string[] args)
    {
        Welcome();
        Console.WriteLine("Listing subscriptions. Please wait.");
        _subscriptions = await ListSubscriptions();
        _selectedSubscription = GetSubscriptionIdAndName(SelectSubscription());
        _chatHistory.Clear();
        Console.WriteLine(@$"
Please ask questions about storage accounts in ""{_selectedSubscription.subscriptionName} ({_selectedSubscription.subscriptionId})"" subscription.
Some questions that you can ask are:
- How many storage accounts do I have in my subscription?
- How many storage accounts are in USA?
- What is the type of ""xyz"" storage account?

- If you need to change the subscription, please enter ""change subscription"".
- To clear chat history, please enter ""clear chat history"".
- To exit, please enter ""quit"".
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
            Console.WriteLine("Please ask a question or enter a command.");
            var userInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(userInput)) continue;
            userInput = userInput.Trim();
            switch (userInput.ToLower())
            {
                case "quit":
                {
                    continueLoop = false;
                    break;
                }
                case "change subscription":
                {
                    _selectedSubscription = GetSubscriptionIdAndName(SelectSubscription());
                    _chatHistory.Clear();
                    Console.WriteLine($"Active subscription changed to \"{_selectedSubscription.subscriptionName} ({_selectedSubscription.subscriptionId})\".");
                    break;
                }
                case "clear chat history":
                {
                    _chatHistory.Clear();
                    Console.WriteLine("Chat history cleared.");
                    break;
                }
                default:
                {
                    var context = new OperationContext("Main:Chat", $"Question: {userInput}");
                    try
                    {
                        int promptTokens = 0, completionTokens = 0;
                        // first rephrase the question.
                        var result = await GetRephrasedQuestion(userInput, context);
#if DEBUG
                        Console.WriteLine($"Rephrased question: {result.Response}");
#endif
                        promptTokens += result.PromptTokens;
                        completionTokens += result.CompletionTokens;
                        var question = result.Response;
                        // now let's get the service intent
                        result = await GetIntent(question, context);
                        promptTokens += result.PromptTokens;
                        completionTokens += result.CompletionTokens;
                        var intent = result.Response;
#if DEBUG
                        Console.WriteLine($"Question intent: {intent}");
#endif
                        result = await ProcessQuestion(question, intent, context);
                        promptTokens += result.PromptTokens;
                        completionTokens += result.CompletionTokens;
                        Console.WriteLine($"Answer: {result.Response}");
                        if (result.StoreInChatHistory)
                        {
                            _chatHistory.Add(result);
                        }
                        Console.WriteLine($"Token usage - Prompt tokens: {promptTokens}; Completion tokens: {completionTokens}");
                        result.OriginalQuestion = userInput;
                        _logger?.LogChatResponse(result, context);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                    finally
                    {
                        _logger?.LogOperation(context);
                    }
                    break;
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
    /// Prints welcome message.
    /// </summary>
    private static void Welcome()
    {
        var message = @"
Hello and welcome to Azure Sidekick! 

I am an AI assistant that can answer questions about services in your Azure Subscriptions.

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
    /// Extracts the subscription id and name.
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
        var result = await _generalChatManagementService.Rephrase(question, _chatHistory, context);
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
        var result = await _generalChatManagementService.GetIntent(question, _chatHistory, context);
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
    private async Task<ChatResponse> ProcessQuestion(string question, string intent, IOperationContext context)
    {
        IOperationResult result;
        switch (intent)
        {
            case Constants.Intent.Ability:
                result = await _generalChatManagementService.HandleQuestionWithAbilityIntent(question, _chatHistory,
                    context);
                break;
            case Constants.Intent.MultipleIntents:
                result = await _generalChatManagementService.HandleQuestionWithMultipleIntents(question, _chatHistory,
                    context);
                break;
            case Constants.Intent.Unclear:
                result = await _generalChatManagementService.HandleQuestionWithUnclearIntent(question, _chatHistory,
                    context);
                break;
            case Constants.Intent.Other:
                result = await _generalChatManagementService.HandleQuestionWithOtherIntent(question, _chatHistory,
                    context);
                break;
            case Constants.Intent.Information:
                result = await _generalChatManagementService.HandleQuestionWithInformationIntent(question, _chatHistory,
                    context);
                break;
            case Constants.Intent.Azure:
                result =
                    await _generalChatManagementService.HandleQuestionWithAzureIntent(question, _chatHistory, context);
                break;
            default:
                var azureChatManagementService = _azureChatManagementServiceFactory.GetService(intent);
                result = await azureChatManagementService.ProcessQuestion(_selectedSubscription.subscriptionId,
                    question, _chatHistory, operationContext: context);
                break;
        }
        if (!result.IsOperationSuccessful) throw result.Error;
        var successResult = (SuccessOperationResult<ChatResponse>)result;
        return successResult.Item;
    }
}