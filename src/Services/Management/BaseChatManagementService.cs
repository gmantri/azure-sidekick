using AzureSidekick.Core.Models;

namespace AzureSidekick.Services.Management;

public abstract class BaseChatManagementService
{
    /// <summary>
    /// Maximum number of items that can be sent to LLM as chat history. 
    /// </summary>
    protected readonly int MaxChatHistoryItems = 5;

    /// <summary>
    /// Trim chat history by removing items more than <see cref="MaxChatHistoryItems"/>.
    /// </summary>
    /// <param name="chatHistory"></param>
    /// <returns>
    /// Trimmed chat history.
    /// </returns>
    protected virtual IEnumerable<ChatResponse> TrimChatHistory(IEnumerable<ChatResponse> chatHistory)
    {
        var chatHistoryItems = chatHistory == null ? new List<ChatResponse>() : chatHistory.ToList();
        chatHistoryItems.Reverse();
        var partialChatHistoryItems = chatHistoryItems.Take(Math.Min(chatHistoryItems.Count, MaxChatHistoryItems)).ToList();
        partialChatHistoryItems.Reverse();
        return partialChatHistoryItems;
    }
    
    /// <summary>
    /// Get grounding rules for LLM. These will be the part of system prompt.
    /// </summary>
    /// <returns>
    /// Grounding rules for LLM.
    /// </returns>
    protected virtual List<string> GetGroundingRules()
    {
        return new List<string>()
        {
            "You are Azure Sidekick, an AI assistant specializing in Azure, tasked with providing accurate and knowledgeable responses to user inquiries about Azure.",  
            "Maintain honesty. If uncertain of an answer, respond with, \"I apologize, but I currently lack sufficient information to accurately answer your question.",  
            "Uphold user privacy. Do not ask for, store, or share personal data without explicit permission.",  
            "Promote inclusivity and respect. Do not engage in or tolerate hate speech, discrimination, or bigotry of any form. Treat all users equally, irrespective of race, ethnicity, religion, gender, age, nationality, or disability.",  
            "Respect copyright laws and intellectual property rights. Do not share, reproduce, or distribute copyrighted material without the appropriate authorization.",  
            "Provide precise and concise responses. Maintain a respectful and professional tone in all interactions.",  
            "Wait for the user's question before providing information. Stay within your domain of expertise - Azure and related services.",  
            "Ensure responses are up-to-date and accessible. Avoid unnecessary jargon and technical language when possible."          
        };
    }
    
    /// <summary>
    /// Get default chat arguments. Includes grounding rules and trimmed chat history.
    /// </summary>
    /// <param name="chatHistory">
    /// Chat history.
    /// </param>
    /// <returns>
    /// Dictionary of chat arguments.
    /// </returns>
    protected virtual IDictionary<string, object> GetDefaultChatArguments(IEnumerable<ChatResponse> chatHistory)
    {
        return new Dictionary<string, object>()
        {
            [Constants.ChatArguments.GroundingRules] = GetGroundingRules(),
            [Constants.ChatArguments.ChatHistory] = TrimChatHistory(chatHistory)
        };
    }
}