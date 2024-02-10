namespace AzureSidekick.Core;

public static class Constants
{
    /// <summary>
    /// Describe the primary intent of the question.
    /// </summary>
    public struct Intent
    {
        /// <summary>
        /// Indicates that the question is a general Azure question and not specific to any Azure service currently supported by this tool.
        /// </summary>
        public const string Azure = "Azure";
        
        /// <summary>
        /// Prompt/user question contains multiple unrelated questions. This requires the
        /// need to break up the question into sub questions.
        /// </summary>
        public const string MultipleIntents = "MultipleIntents";

        /// <summary>
        /// Indicates that the question is about Azure Storage.
        /// </summary>
        public const string Storage = "Storage";

        /// <summary>
        /// Indicates that the question is not really a question but rather user
        /// is providing some information or making a statement about the question
        /// they are going to ask.
        /// </summary>
        public const string Information = "Information";

        /// <summary>
        /// Indicates that the question is about the abilities of the tool.
        /// </summary>
        public const string Ability = "Ability";

        /// <summary>
        /// Indicates that the question is not clear.
        /// </summary>
        public const string Unclear = "Unclear";

        /// <summary>
        /// Indicates that the question is about a service still not supported by this service.
        /// </summary>
        public const string Other = "Other";
    }
    
    /// <summary>
    /// Possible intents of a prompt/user question related to Azure Storage.
    /// </summary>
    public struct StorageIntents
    {
        /// <summary>
        /// Prompt/user question is about general information (rules, restrictions etc.)
        /// about Azure storage.
        /// </summary>
        public const string GeneralInformation = "GeneralInformation";
        
        /// <summary>
        /// Prompt/user question contains multiple unrelated questions. This requires the
        /// need to break up the question into sub questions.
        /// </summary>
        public const string MultipleIntents = "MultipleIntents";
        
        /// <summary>
        /// Prompt/user question is not clear. This requires asking the user to further
        /// clarify their question.
        /// </summary>
        public const string Unclear = "Unclear";
        
        /// <summary>
        /// Prompt/user question is about properties of multiple storage accounts
        /// in a subscription.
        /// </summary>
        public const string StorageAccounts = "StorageAccounts";
        
        /// <summary>
        /// Prompt/user question is about properties of a storage account
        /// in a subscription.
        /// </summary>
        public const string StorageAccount = "StorageAccount";
        
        /// <summary>
        /// Prompt/user question not related to Azure Storage.
        /// </summary>
        public const string Other = "Other";
    }
}