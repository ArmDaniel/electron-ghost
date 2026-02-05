using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers; // Required for AuthenticationHeaderValue
using System.Text; // Required for System.Text.Encoding
using System.Text.Json;
using System.Text.Json.Serialization; // Required for JsonSerializerOptions & JsonIgnoreCondition
using System.Threading.Tasks;
using DestinyGhostAssistant.Models; // For OpenRouterMessage etc.
using System.Diagnostics; // Required for Debug.WriteLine
using System.Linq; // Required for OrderBy

namespace DestinyGhostAssistant.Services
{
    public class OpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string OpenRouterApiUrl = "https://openrouter.ai/api/v1/chat/completions";
        private const string OpenRouterModelsUrl = "https://openrouter.ai/api/v1/models";

        private static readonly ProductInfoHeaderValue AppUserAgent = new ProductInfoHeaderValue("DestinyGhostAssistant", "1.0.0");
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Though attributes override this, good for consistency
        };


        public OpenRouterService(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "OpenRouter API key cannot be null or empty.");
            }
            _apiKey = apiKey;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "Destiny Ghost Assistant");
            _httpClient.DefaultRequestHeaders.UserAgent.Add(AppUserAgent);
        }

        /// <summary>
        /// Fetches the list of all available models from OpenRouter.
        /// This is a static call that doesn't require an API key.
        /// </summary>
        public static async Task<List<OpenRouterModelInfo>> FetchAvailableModelsAsync()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(AppUserAgent);

            try
            {
                var response = await client.GetAsync(OpenRouterModelsUrl);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                var modelsResponse = JsonSerializer.Deserialize<OpenRouterModelsResponse>(json);

                if (modelsResponse?.Data != null)
                {
                    return modelsResponse.Data
                        .OrderBy(m => m.Name)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching models from OpenRouter: {ex.Message}");
            }

            return new List<OpenRouterModelInfo>();
        }

        public async Task<string> GetChatCompletionAsync(List<OpenRouterMessage> messages, string modelName = "gryphe/mythomax-l2-13b")
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_API_KEY_PLACEHOLDER") // Defensive check
            {
                Debug.WriteLine("Error: OpenRouter API Key not configured.");
                return "API Key not configured. Please check application settings.";
            }

            var requestPayload = new OpenRouterRequest
            {
                Model = modelName,
                Messages = messages,
                // Temperature = 0.7, // Example: can be set here or passed as parameter
                // MaxTokens = 150    // Example: can be set here or passed as parameter
            };

            string jsonRequest;
            try
            {
                jsonRequest = JsonSerializer.Serialize(requestPayload, _jsonSerializerOptions);
            }
            catch (JsonException e)
            {
                Debug.WriteLine($"JSON serialization error: {e.Message}");
                return $"Error serializing request: {e.Message}";
            }

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(OpenRouterApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    OpenRouterResponse? openRouterResponse = null;
                    try
                    {
                        openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(jsonResponse);
                    }
                    catch (JsonException e)
                    {
                        Debug.WriteLine($"JSON parsing error from OpenRouter response: {e.Message}\nResponse: {jsonResponse}");
                        return $"Error parsing OpenRouter response: {e.Message}";
                    }


                    if (openRouterResponse?.Choices != null && openRouterResponse.Choices.Count > 0 && openRouterResponse.Choices[0].Message != null)
                    {
                        return openRouterResponse.Choices[0].Message.Content?.Trim() ?? "No content in assistant's message.";
                    }
                    Debug.WriteLine($"OpenRouter response was empty or malformed. Response: {jsonResponse}");
                    return "Assistant response was empty or malformed.";
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"OpenRouter API Error: {response.StatusCode}\n{errorContent}");
                    return $"Error from OpenRouter API ({response.StatusCode}). Details: {errorContent}";
                }
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine($"Network error connecting to OpenRouter: {e.Message}");
                return $"Network error: {e.Message}";
            }
            // Removed the more specific JsonException catch here as deserialization is now in its own try-catch.
            catch (Exception e) // Catch-all for other unexpected errors during HTTP call or initial processing
            {
                Debug.WriteLine($"Unexpected error in OpenRouterService: {e.Message}");
                return $"An unexpected error occurred: {e.Message}";
            }
        }
    }
}
