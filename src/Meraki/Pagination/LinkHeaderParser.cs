using System.Text.RegularExpressions;

namespace QRStickers.Meraki.Pagination;

/// <summary>
/// Parses RFC 5988 Link headers for pagination
/// </summary>
public static class LinkHeaderParser
{
    // Regex pattern to parse Link header entries
    // Format: <url>; rel="relation"
    private static readonly Regex LinkPattern = new(
        @"<(?<url>[^>]+)>\s*;\s*rel\s*=\s*""?(?<rel>[^""]+)""?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Parses a Link header string into PageInfo
    /// </summary>
    /// <param name="linkHeader">The Link header value (comma-separated list of links)</param>
    /// <returns>PageInfo containing parsed pagination URLs</returns>
    public static PageInfo Parse(string? linkHeader)
    {
        var pageInfo = new PageInfo();

        if (string.IsNullOrWhiteSpace(linkHeader))
        {
            return pageInfo;
        }

        // Split by comma to handle multiple links
        var links = linkHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var link in links)
        {
            var match = LinkPattern.Match(link);
            if (!match.Success)
            {
                continue;
            }

            var url = match.Groups["url"].Value;
            var rel = match.Groups["rel"].Value.ToLowerInvariant();

            switch (rel)
            {
                case "first":
                    pageInfo.First = url;
                    break;
                case "last":
                    pageInfo.Last = url;
                    break;
                case "prev":
                    pageInfo.Prev = url;
                    break;
                case "next":
                    pageInfo.Next = url;
                    break;
            }
        }

        return pageInfo;
    }

    /// <summary>
    /// Extracts the startingAfter parameter from a pagination URL
    /// </summary>
    /// <param name="url">The pagination URL containing startingAfter parameter</param>
    /// <returns>The startingAfter token, or null if not found</returns>
    public static string? ExtractStartingAfter(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // Parse query string to extract startingAfter parameter
        var uri = new Uri(url);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return queryParams["startingAfter"];
    }

    /// <summary>
    /// Extracts the endingBefore parameter from a pagination URL
    /// </summary>
    /// <param name="url">The pagination URL containing endingBefore parameter</param>
    /// <returns>The endingBefore token, or null if not found</returns>
    public static string? ExtractEndingBefore(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // Parse query string to extract endingBefore parameter
        var uri = new Uri(url);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return queryParams["endingBefore"];
    }
}
