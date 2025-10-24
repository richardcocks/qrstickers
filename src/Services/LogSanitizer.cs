using System.Text.RegularExpressions;

namespace QRStickers.Services;

/// <summary>
/// Utility class for sanitizing user input and external data before logging
/// Prevents log injection attacks where attackers inject fake log entries via newlines
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes a string for safe logging by removing newlines and control characters
    /// </summary>
    /// <param name="input">Raw user input or external data</param>
    /// <returns>Sanitized string safe for logging, or empty string if input is null/empty</returns>
    /// <example>
    /// <code>
    /// _logger.LogInformation("User created template '{Name}'", LogSanitizer.Sanitize(templateName));
    /// </code>
    /// </example>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove newlines, carriage returns, tabs, and control characters
        // Keep printable ASCII and common Unicode characters
        // Pattern: \x00 (null), \r (CR), \n (LF), \t (tab), \x01-\x1F (control chars), \x7F-\x9F (extended control chars)
        // Note: \x00 is explicit to avoid regex range edge case issues
        var sanitized = Regex.Replace(input, @"[\x00\r\n\t\x01-\x1F\x7F-\x9F]", "");

        // Truncate to reasonable length for logs (prevent log flooding attacks)
        const int maxLogLength = 200;
        if (sanitized.Length > maxLogLength)
        {
            return sanitized.Substring(0, maxLogLength) + "...";
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes multiple strings in one call (convenience method)
    /// </summary>
    /// <param name="inputs">Array of strings to sanitize</param>
    /// <returns>Array of sanitized strings</returns>
    public static string[] Sanitize(params string?[] inputs)
    {
        return inputs.Select(Sanitize).ToArray();
    }
}
