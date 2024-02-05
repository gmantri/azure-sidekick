namespace AzureSidekick.Services;

/// <summary>
/// Various constants.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// Chat arguments.
    /// </summary>
    internal struct ChatArguments
    {
        /// <summary>
        /// Grounding rules.
        /// </summary>
        internal const string GroundingRules = "grounding_rules";

        /// <summary>
        /// Chat history.
        /// </summary>
        internal const string ChatHistory = "chat_history";

        /// <summary>
        /// Context.
        /// </summary>
        internal const string Context = "context";
    }
}