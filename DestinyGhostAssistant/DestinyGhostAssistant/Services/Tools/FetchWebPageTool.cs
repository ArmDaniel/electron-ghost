using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DestinyGhostAssistant.Utils;

namespace DestinyGhostAssistant.Services.Tools
{
    public class FetchWebPageTool : ITool
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public string Name => "fetch_webpage";

        public string Description =>
            "Fetches the text content of a web page URL and returns it. " +
            "Use this to go in-depth on a specific search result. " +
            "Parameters: 'url' (string, required - the full URL to fetch, e.g. https://example.com/page).";

        static FetchWebPageTool()
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) DestinyGhostAssistant/1.0");
            }
        }

        public async Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            string? url = ToolParameterHelper.GetString(parameters, "url");
            if (string.IsNullOrWhiteSpace(url))
            {
                return "Error: 'url' parameter is required and must be a non-empty string.";
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                return $"Error: '{url}' is not a valid HTTP/HTTPS URL.";
            }

            try
            {
                Debug.WriteLine($"FetchWebPageTool: Fetching '{url}'");

                string html = await _httpClient.GetStringAsync(uri);

                // Strip HTML to plain text
                string text = StripHtmlToText(html);

                // Truncate to avoid sending too much to the LLM
                const int maxChars = 12000;
                if (text.Length > maxChars)
                {
                    text = text.Substring(0, maxChars) + "\n\n[Content truncated — showing first ~12,000 characters]";
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    return $"The page at '{url}' returned no readable text content.";
                }

                Debug.WriteLine($"FetchWebPageTool: Extracted {text.Length} characters from '{url}'.");
                return $"Content from {url}:\n\n{text}";
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"FetchWebPageTool: HTTP error fetching '{url}': {ex.Message}");
                return $"Error fetching '{url}': {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                return $"Error: Request to '{url}' timed out.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FetchWebPageTool: Unexpected error: {ex.Message}");
                return $"Error fetching '{url}': {ex.Message}";
            }
        }

        private static string StripHtmlToText(string html)
        {
            // Remove script and style blocks
            html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);
            // Remove all HTML comments
            html = Regex.Replace(html, @"<!--[\s\S]*?-->", " ");
            // Replace block-level tags with newlines
            html = Regex.Replace(html, @"<(br|p|div|h[1-6]|li|tr|blockquote)[^>]*>", "\n", RegexOptions.IgnoreCase);
            // Remove remaining tags
            html = Regex.Replace(html, @"<[^>]+>", " ");
            // Decode common HTML entities
            html = System.Net.WebUtility.HtmlDecode(html);
            // Collapse whitespace
            html = Regex.Replace(html, @"[ \t]+", " ");
            html = Regex.Replace(html, @"\n{3,}", "\n\n");

            return html.Trim();
        }
    }
}
