namespace AzureSidekick.Core.Models;

/// <summary>
/// Azure OpenAI settings.
/// </summary>
public class AzureOpenAISettings
{
    /// <summary>
    /// Azure OpenAI service endpoint e.g. https://xyz.openai.azure.com/.
    /// </summary>
    public string Endpoint { get; set; }
    
    /// <summary>
    /// Azure OpenAI service key e.g. 00000000000000000000000
    /// </summary>
    public string Key { get; set; }
    
    /// <summary>
    /// Azure OpenAI deployment id e.g. gpt-4-32k
    /// </summary>
    public string DeploymentId { get; set; }
    
    /// <summary>
    /// Azure OpenAI model type for token calculation. Must be one of the following: gpt-4, gpt-3.5-turbo or gpt-35-turbo.
    /// </summary>
    public string ModelType { get; set; }
}