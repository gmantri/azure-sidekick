using System.Text;
using AzureSidekick.Core.Interfaces;
using AzureSidekick.Core.Models;
using AzureSidekick.Core.Utilities;

namespace AzureSidekick.Core.Loggers;

/// <summary>
/// ILogger implementation for storing logs and exceptions on local file system.
/// The logs are stored in the "temp" directory of the logged in user under "AzureStorageCopilot"
/// folder. For each day, a new directory will be created in "yyyy-MM-dd" pattern and that will
/// contain the data files (logs.csv for logs and errors.csv for errors).
/// </summary>
public class FileSystemLogger : ILogger
{
    /// <summary>
    /// Log file full path.
    /// </summary>
    private string _logFile = "";
    
    /// <summary>
    /// Error file full path.
    /// </summary>
    private string _errorFile = "";
    
    /// <summary>
    /// Prompt log file full path.
    /// </summary>
    private string _chatResponseLogFile = "";
    
    /// <summary>
    /// Log file header
    /// </summary>
    private static readonly string LogFileHeader = $"\"Operation Id\",\"Operation Name\",\"Parent Operation Id\",\"Start\",\"End\",\"Total (ms)\"{Environment.NewLine}";
    
    /// <summary>
    /// Error file header
    /// </summary>
    private static readonly string ErrorFileHeader = $"\"Operation Id\",\"Operation Name\",\"Parent Operation Id\",\"Start\",\"End\",\"Total (ms)\",\"Error Message\",\"Stack Trace\"{Environment.NewLine}";
    
    /// <summary>
    /// Log file header
    /// </summary>
    private static readonly string ChatResponseLogFileHeader = $"\"Operation Id\",\"Question (Original)\",\"Question (Revised)\",\"Response\",\"Intent\",\"Function\",\"Prompt Tokens\",\"Completion Tokens\"{Environment.NewLine}";

    /// <summary>
    /// Creates a new instance of <see cref="FileSystemLogger"/>.
    /// </summary>
    public FileSystemLogger()
    {
        Initialize();
    }

    /// <summary>
    /// Logs an operation and saves it to a local file. Creates the file if it does not exist.
    /// </summary>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    public void LogOperation(IOperationContext operationContext)
    {
        if (!File.Exists(_logFile))
        {
            using var fs = File.Create(_logFile);
            fs.Write(Encoding.UTF8.GetBytes(LogFileHeader));
        }
        operationContext.EndTime = DateTime.UtcNow;
        using var sw = File.AppendText(_logFile);
        sw.Write($"\"{operationContext.OperationId}\",\"{operationContext.OperationName.EscapeDoubleQuotes()}\",\"{operationContext.ParentOperationId}\",\"{operationContext.StartTime:o}\",\"{operationContext.EndTime:o}\",\"{operationContext.ElapsedTime}\"");
        sw.Write(Environment.NewLine);
    }

    /// <summary>
    /// Logs an exception and saves it to a local file. Creates the file if it does not exist.
    /// </summary>
    /// <param name="exception">
    /// <see cref="Exception"/>.
    /// </param>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    public void LogException(Exception exception, IOperationContext operationContext)
    {
        if (!File.Exists(_errorFile))
        {
            using var fs = File.Create(_errorFile);
            fs.Write(Encoding.UTF8.GetBytes(ErrorFileHeader));
        }
        operationContext.EndTime = DateTime.UtcNow;
        using var sw = File.AppendText(_errorFile);
        sw.Write($"\"{operationContext.OperationId}\",\"{operationContext.OperationName.EscapeDoubleQuotes()}\",\"{operationContext.ParentOperationId}\",\"{operationContext.StartTime:o}\",\"{operationContext.EndTime:o}\",\"{operationContext.ElapsedTime}\",\"{exception.Message.EscapeDoubleQuotes()}\",\"{exception.StackTrace.EscapeDoubleQuotes()}\"");
        sw.Write(Environment.NewLine);
    }


    /// <summary>
    /// Logs the chat response.
    /// </summary>
    /// <param name="response">
    /// <see cref="ChatResponse"/>.
    /// </param>
    /// <param name="operationContext">
    /// <see cref="IOperationContext"/>.
    /// </param>
    public void LogChatResponse(ChatResponse response, IOperationContext operationContext)
    {
        if (!File.Exists(_chatResponseLogFile))
        {
            using var fs = File.Create(_chatResponseLogFile);
            fs.Write(Encoding.UTF8.GetBytes(ChatResponseLogFileHeader));
        }
        operationContext.EndTime = DateTime.UtcNow;
        using var sw = File.AppendText(_logFile);
        sw.Write($"\"{operationContext.OperationId}\",\"{response.OriginalQuestion.EscapeDoubleQuotes()}\",\"{response.Question.EscapeDoubleQuotes()}\",\"{response.Response.EscapeDoubleQuotes()}\",\"{response.Intent.EscapeDoubleQuotes()}\",\"{response.Function.EscapeDoubleQuotes()}\",\"{response.PromptTokens}\",\"{response.CompletionTokens}\"");
        sw.Write(Environment.NewLine);
    }

    /// <summary>
    /// Creates the directories for storing logs if it does not exist.
    /// </summary>
    private void Initialize()
    {
        var path = Path.GetTempPath();
        var loggerDirectory = Path.Combine(path, "AzureSidekick");
        if (!Directory.Exists(loggerDirectory))
        {
            Directory.CreateDirectory(loggerDirectory);
        }

        var logDirectoryPath = Path.Combine(loggerDirectory, DateTime.Now.ToString("yyyy-MM-dd"));
        if (!Directory.Exists(logDirectoryPath))
        {
            Directory.CreateDirectory(logDirectoryPath);
        }

        _logFile = Path.Combine(logDirectoryPath, "logs.csv");
        _errorFile = Path.Combine(logDirectoryPath, "errors.csv");
        _chatResponseLogFile = Path.Combine(logDirectoryPath, "chat-responses.csv");
    }
}