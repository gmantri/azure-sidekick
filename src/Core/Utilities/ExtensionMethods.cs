using System.Text.Json.Nodes;
using YamlDotNet.System.Text.Json;

namespace AzureSidekick.Core.Utilities;

/// <summary>
/// Collection of extension methods that will be used throughout the application.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Extension method to escape double quotes (") in a string.
    /// E.g. Replaces ""Test"" with "\"Test\"".
    /// </summary>
    /// <param name="input">
    /// Input string.
    /// </param>
    /// <returns>
    /// String with escaped double quotes.
    /// </returns>
    public static string EscapeDoubleQuotes(this string input)
    {
        return string.IsNullOrWhiteSpace(input) ? input : input.Replace("\"", "\\\"");
    }

    /// <summary>
    /// Extension method to escape single quotes (') in a string.
    /// E.g. Replaces "you're" with "you\'re".
    /// </summary>
    /// <param name="input">
    /// Input string.
    /// </param>
    /// <returns>
    /// String with escaped single quotes.
    /// </returns>
    public static string EscapeSingleQuotes(this string input)
    {
        return string.IsNullOrWhiteSpace(input) ? input : input.Replace("\'", "\\\'");
    }

    public static string ConvertToYaml(this JsonObject input)
    {
        var yaml = YamlConverter.Serialize(input);
        return yaml;
    }
}