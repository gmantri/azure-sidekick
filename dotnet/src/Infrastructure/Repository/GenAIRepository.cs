using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Azure.AI.OpenAI;
using AzureSidekick.Core.EventArgs;
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
    /// <summary>
    /// <see cref="AzureOpenAISettings"/>.
    /// </summary>
    private readonly AzureOpenAISettings _azureOpenAISettings;

    /// <summary>
    /// <see cref="OpenAIClient"/>.
    /// </summary>
    private readonly OpenAIClient _azureOpenAIClient;

    /// <summary>
    /// <see cref="ILogger"/>.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Base directory paths for semantic prompts.
    /// </summary>
    private readonly string _basePromptsPath = Path.Combine("Plugins", "Semantic");

    /// <summary>
    /// Create an instance of <see cref="GenAIRepository"/>.
    /// </summary>
    /// <param name="azureOpenAISettings">
    /// <see cref="AzureOpenAISettings"/>.
    /// </param>
    /// <param name="azureOpenAIClient">
    /// <see cref="OpenAIClient"/>.
    /// </param>
    /// <param name="logger">
    /// <see cref="ILogger"/>.
    /// </param>
    public GenAIRepository(AzureOpenAISettings azureOpenAISettings, OpenAIClient azureOpenAIClient, ILogger logger)
    {
        _azureOpenAISettings = azureOpenAISettings;
        _azureOpenAIClient = azureOpenAIClient;
        _logger = logger;
    }

    /// <summary>
    /// Get a response to a user's question.
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
        var context = new OperationContext("GenAIRepository:GetResponse", $"Get response. Question: {question}; Plugin: {pluginName}; Function: {functionName}.", operationContext);
        try
        {
            var kernel = GetKernel();
            var path = Path.Combine(Directory.GetCurrentDirectory(), _basePromptsPath, pluginName, functionName, "index.yaml");
            var function = kernel.CreateFunctionFromPromptYaml(await File.ReadAllTextAsync(path),
                new HandlebarsPromptTemplateFactory());
            var openAIPromptSettings = new OpenAIPromptExecutionSettings()
            {
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
                "GenAIRepository:GetStreamingResponse - An error occurred while getting response.");
        }
        finally
        {
            _logger?.LogOperation(context);
        }
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
    /// <see cref="IAsyncEnumerable{ChatResponse}"/>.
    /// </returns>
    public async IAsyncEnumerable<ChatResponse> GetStreamingResponse(string question, string pluginName, string functionName, IDictionary<string, object> arguments = default,
        IOperationContext operationContext = default)
    {
        var context = new OperationContext("GenAIRepository:GetStreamingResponse", $"Get streaming response. Question: {question}; Plugin: {pluginName}; Function: {functionName}.", operationContext);
        IAsyncEnumerable<StreamingKernelContent> result;
        string prompt;
        try
        {
            var kernel = GetKernel();
            var path = Path.Combine(Directory.GetCurrentDirectory(), _basePromptsPath, pluginName, functionName, "index.yaml");
            var function = kernel.CreateFunctionFromPromptYaml(await File.ReadAllTextAsync(path),
                new HandlebarsPromptTemplateFactory());
            var openAIPromptSettings = new OpenAIPromptExecutionSettings()
            {
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
            result = kernel.InvokeStreamingAsync(function, kernelArguments);
            var getPromptResult = await GetPrompt(kernel, path, kernelArguments, context);
            prompt = getPromptResult.prompt;
        }
        catch (Exception exception)
        {
            _logger?.LogException(exception, context);
            throw Helper.GetRequestException(exception,
                "GenAIRepository:GetStreamingResponse - An error occurred while getting response.");
        }
        finally
        {
            _logger?.LogOperation(context);
        }

        var responseStringBuilder = new StringBuilder();
        await foreach (var item in result)
        {
            var response = item.ToString();
            responseStringBuilder.Append(response);
            var streamingChatResponse = new ChatResponse()
            {
                Question = question,
                Intent = pluginName,
                Function = functionName,
                Response = response
            };
            yield return streamingChatResponse;
        }
        //send a dummy chat response to indicate end of response
        var answer = responseStringBuilder.ToString();
        var tokenUsage = CalculateTokensForPromptAndResponse(prompt ?? question, answer, context);
        var finalChatResponse = new ChatResponse()
        {
            Question = question,
            Response = answer,
            PromptTokens = tokenUsage.PromptTokens,
            CompletionTokens = tokenUsage.CompletionTokens,
            Intent = pluginName,
            Function = functionName,
            StoreInChatHistory = true
        };
        yield return finalChatResponse;
    }

    /// <summary>
    /// Create <see cref="Kernel"/>.
    /// </summary>
    /// <returns>
    /// <see cref="Kernel"/>;
    /// </returns>
    private Kernel GetKernel()
    {
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddAzureOpenAIChatCompletion(_azureOpenAISettings.DeploymentId, _azureOpenAIClient);
        var kernel = kernelBuilder.Build();
        return kernel;
    }

    /// <summary>
    /// Extract token usage from result of a function.
    /// </summary>
    /// <param name="result">
    /// <see cref="FunctionResult"/>.
    /// </param>
    /// <returns>
    /// A tuple containing consumed prompt tokens (1st parameter) and completion tokens (2nd parameter).
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
        return (PromptTokens: promptTokens, CompletionTokens: completionTokens);
    }

    /// <summary>
    /// Calculate tokens for the question and answer. 
    /// </summary>
    /// <param name="prompt">
    /// Prompt that is sent to LLM.
    /// </param>
    /// <param name="response">
    /// LLM's response.
    /// </param>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    /// <returns>
    /// A tuple containing consumed prompt tokens (1st parameter) and completion tokens (2nd parameter).
    /// </returns>
    private (int PromptTokens, int CompletionTokens) CalculateTokensForPromptAndResponse(string prompt, string response, IOperationContext operationContext)
    {
        try
        {
            var promptTokens = 0;
            var completionTokens = 0;
            var encodingForModel = Tiktoken.Encoding.TryForModel(_azureOpenAISettings.ModelType);
            if (encodingForModel == null) return (PromptTokens: promptTokens, CompletionTokens: completionTokens);
            promptTokens = encodingForModel.CountTokens(prompt);
            completionTokens = encodingForModel.CountTokens(response);
            return (PromptTokens: promptTokens, CompletionTokens: completionTokens);
        }
        catch (Exception exception)
        {
            _logger?.LogException(exception, operationContext);
            return (PromptTokens: 0, CompletionTokens: 0);
        }
    }

    /// <summary>
    /// Get the prompt from the prompt template.
    /// </summary>
    /// <param name="kernel">
    /// <see cref="Kernel"/>.
    /// </param>
    /// <param name="promptFilePath">
    /// Prompt file path.
    /// </param>
    /// <param name="kernelArguments">
    /// <see cref="KernelArguments"/>.
    /// </param>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    /// <returns>
    /// A tuple containing prompt (1st parameter) and any exception (2nd parameter).
    /// </returns>
    private async Task<(string prompt, Exception exception)> GetPrompt(Kernel kernel, string promptFilePath, KernelArguments kernelArguments,
        IOperationContext operationContext)
    {
        try
        {
            var promptFileContents = await File.ReadAllTextAsync(promptFilePath);
            var promptTemplateConfig = KernelFunctionYaml.ToPromptTemplateConfig(promptFileContents);
            // var promptTemplateConfig = new PromptTemplateConfig(promptFileContents);
            var factory = new HandlebarsPromptTemplateFactory();
            if (!factory.TryCreate(promptTemplateConfig, out var promptTemplate)) return (string.Empty, new InvalidOperationException("Unable to create prompt template."));

            var prompt = await promptTemplate.RenderAsync(kernel, kernelArguments);
            return (prompt, null);
        }
        catch (Exception exception)
        {
            _logger.LogException(exception, operationContext);
            return (string.Empty, exception);
        }
    }
}