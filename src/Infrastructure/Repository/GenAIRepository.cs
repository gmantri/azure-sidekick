using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Azure.AI.OpenAI;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Infrastructure.Interfaces;
using AzureSidekick.Infrastructure.Utilities;

namespace AzureSidekick.Infrastructure.Repository;

/// <summary>
/// Class for GenAI operations.
/// </summary>
public class GenAIRepository : IGenAIRepository
{
    private readonly AzureOpenAISettings _azureOpenAISettings;

    private readonly OpenAIClient _azureOpenAIClient;

    private readonly ILogger _logger;

    private readonly string _generalPromptsPath = Path.Combine("Plugins", "Semantic", "General");

    private readonly string _basePromptsPath = Path.Combine("Plugins", "Semantic");
    
    public GenAIRepository(AzureOpenAISettings azureOpenAISettings, OpenAIClient azureOpenAIClient, ILogger logger)
    {
        _azureOpenAISettings = azureOpenAISettings;
        _azureOpenAIClient = azureOpenAIClient;
        _logger = logger;
    }

    /// <summary>
    /// Get response to a user's question.
    /// </summary>
    /// <param name="question">
    /// User question.
    /// </param>
    /// <param name="pluginName">
    /// Name of the plugin.
    /// </param>
    /// <param name="functionName">
    /// Name of the function.
    /// </param>
    /// <param name="arguments">
    /// Arguments for prompt execution. It will contain the data that will be passed to the prompt template.
    /// </param>
    /// <param name="operationContext">
    /// Operation context.
    /// </param>
    /// <returns>
    /// <see cref="ChatResponse"/>.
    /// </returns>
    public async Task<ChatResponse> GetResponse(string question, string pluginName, string functionName, IDictionary<string, object> arguments = default, IOperationContext operationContext = default)
    {
        var context = new OperationContext("GenAIRepository:GenerateResponse", $"Generate response. Question: {question}; Plugin: {pluginName}; Function: {functionName}.", operationContext);
        try
        {
            var kernel = GetKernel();
            var path = Path.Combine(Directory.GetCurrentDirectory(), _basePromptsPath, pluginName, functionName, "index.yaml");
            var function = kernel.CreateFunctionFromPromptYaml(await File.ReadAllTextAsync(path),
                new HandlebarsPromptTemplateFactory());
            var openAIPromptSettings = new OpenAIPromptExecutionSettings()
            {
                ChatSystemPrompt = "You are a truthful AI assistant who is an expert on Azure who answers user's questions about their Azure.",
                Temperature = 0
            };
            var kernelArguments = new KernelArguments(openAIPromptSettings)
            {
                ["question"] = question
            };
            if (arguments != null)
            {
                foreach (var kvp in arguments)
                {
                    kernelArguments.TryAdd(kvp.Key, kvp.Value);
                }
            }
            var result = await kernel.InvokeAsync(function, kernelArguments);
            var tokenUsage = ExtractTokensFromFunctionResult(result);
            var chatResponse = new ChatResponse()
            {
                Question = question,
                Response = result.ToString(),
                PromptTokens = tokenUsage.PromptTokens,
                CompletionTokens = tokenUsage.CompletionTokens,
                Intent = pluginName,
                Function = functionName,
                StoreInChatHistory = true
            };
            return chatResponse;
        }
        catch (Exception exception)
        {
            _logger?.LogException(exception, context);
            throw Helper.GetRequestException(exception,
                "GenAIRepository:GenerateResponse - An error occurred while executing prompt.");
        }
        finally
        {
            _logger?.LogOperation(context);
        }
    }

    private Kernel GetKernel()
    {
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddAzureOpenAIChatCompletion(_azureOpenAISettings.DeploymentId, _azureOpenAIClient);
        var kernel = kernelBuilder.Build();
        return kernel;
    }

    /// <summary>
    /// Extracts the token usage from result of a function.
    /// </summary>
    /// <param name="result">
    /// <see cref="FunctionResult"/>.
    /// </param>
    /// <returns>
    /// A tuple containing consumed input tokens (1st parameter) and output tokens (2nd parameter).
    /// </returns>
    private (int PromptTokens, int CompletionTokens) ExtractTokensFromFunctionResult(FunctionResult result)
    {
        var promptTokens = 0;
        var completionTokens = 0;
        var metadata = result.Metadata;
        if (metadata == null || !metadata.ContainsKey("Usage")) return (promptTokens, completionTokens);
        var usage = (CompletionsUsage)metadata["Usage"];
        if (usage == null) return (promptTokens, completionTokens);
        promptTokens = usage.PromptTokens;
        completionTokens = usage.CompletionTokens;
        return (promptTokens, completionTokens);
    }
}