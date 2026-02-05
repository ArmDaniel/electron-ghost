using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DestinyGhostAssistant.Utils;

namespace DestinyGhostAssistant.Services.Tools
{
    public class WebSearchTool : ITool
    {
        private readonly Func<string?> _getApiKey;
        private static readonly HttpClient _httpClient = new();

        public string Name => "web_search";

        public string Description =>
            "Searches the web using Google via Serper API and returns the top 10 results. " +
            "Parameters: 'query' (string, required - the search query). " +
            "Returns a list of results with title, link, and snippet for each.";

        /// <summary>
        /// Creates a WebSearchTool. The apiKeyProvider func is called at execution time
        /// so that key changes in settings take effect immediately.
        /// </summary>
        public WebSearchTool(Func<string?> apiKeyProvider)
        {
            _getApiKey = apiKeyProvider ?? throw new ArgumentNullException(nameof(apiKeyProvider));
        }

        public async Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            // Validate query parameter
            string? query = ToolParameterHelper.GetString(parameters, "query");
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Error: 'query' parameter is required and must be a non-empty string.";
            }

            string? apiKey = _getApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "Error: Serper API key is not configured. Please set it in Settings.";
            }

            try
            {
                Debug.WriteLine($"WebSearchTool: Searching for '{query}'");

                var requestBody = JsonSerializer.Serialize(new { q = query, num = 10 });
                var request = new HttpRequestMessage(HttpMethod.Post, "https://google.serper.dev/search")
                {
                    Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-API-KEY", apiKey);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"WebSearchTool: Serper API returned {response.StatusCode}: {errorBody}");
                    return $"Error: Serper API returned status {response.StatusCode}. Details: {errorBody}";
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var results = new List<string>();

                // Organic results (Serper uses "organic" not "organic_results")
                if (root.TryGetProperty("organic", out JsonElement organicResults))
                {
                    int index = 1;
                    foreach (var result in organicResults.EnumerateArray())
                    {
                        string title = result.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                        string link = result.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "";
                        string snippet = result.TryGetProperty("snippet", out var s) ? s.GetString() ?? "" : "";

                        results.Add($"**{index}. {title}**\n[{link}]({link})\n{snippet}");
                        index++;

                        if (index > 10) break;
                    }
                }

                // Answer box / featured snippet if available
                string answerBox = "";
                if (root.TryGetProperty("answerBox", out JsonElement ab))
                {
                    string abTitle = ab.TryGetProperty("title", out var abt) ? abt.GetString() ?? "" : "";
                    string abSnippet = ab.TryGetProperty("snippet", out var abs2) ? abs2.GetString() ?? "" : "";
                    string abAnswer = ab.TryGetProperty("answer", out var aba) ? aba.GetString() ?? "" : "";

                    string featured = abAnswer != "" ? abAnswer : abSnippet;
                    if (!string.IsNullOrWhiteSpace(featured))
                    {
                        answerBox = $"**Featured Answer: {abTitle}**\n{featured}\n\n";
                    }
                }

                // Knowledge graph if available
                string knowledgeGraph = "";
                if (root.TryGetProperty("knowledgeGraph", out JsonElement kg))
                {
                    string kgTitle = kg.TryGetProperty("title", out var kgt) ? kgt.GetString() ?? "" : "";
                    string kgDesc = kg.TryGetProperty("description", out var kgd) ? kgd.GetString() ?? "" : "";

                    if (!string.IsNullOrWhiteSpace(kgDesc))
                    {
                        knowledgeGraph = $"**Knowledge Graph: {kgTitle}**\n{kgDesc}\n\n";
                    }
                }

                if (results.Count == 0 && string.IsNullOrEmpty(answerBox) && string.IsNullOrEmpty(knowledgeGraph))
                {
                    return $"No results found for query: '{query}'.";
                }

                string header = $"Web search results for: \"{query}\"\n\n";
                string body = answerBox + knowledgeGraph + string.Join("\n\n", results);

                Debug.WriteLine($"WebSearchTool: Found {results.Count} organic results for '{query}'.");
                return header + body;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"WebSearchTool: Network error: {ex.Message}");
                return $"Error: Network error during web search: {ex.Message}";
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"WebSearchTool: JSON parse error: {ex.Message}");
                return $"Error: Failed to parse search results: {ex.Message}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebSearchTool: Unexpected error: {ex.Message}");
                return $"Error: Unexpected error during web search: {ex.Message}";
            }
        }
    }
}
