using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DestinyGhostAssistant.Models
{
    // --- Models list API response types ---

    public class OpenRouterModelInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("pricing")]
        public OpenRouterModelPricing? Pricing { get; set; }

        [JsonPropertyName("context_length")]
        public int? ContextLength { get; set; }

        /// <summary>Display string for UI: "Name (id)"</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Id : $"{Name}  —  {Id}";
    }

    public class OpenRouterModelPricing
    {
        [JsonPropertyName("prompt")]
        public string? Prompt { get; set; }

        [JsonPropertyName("completion")]
        public string? Completion { get; set; }
    }

    public class OpenRouterModelsResponse
    {
        [JsonPropertyName("data")]
        public List<OpenRouterModelInfo> Data { get; set; } = new();
    }

    // --- Chat completions types ---

    public class OpenRouterMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class OpenRouterRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenRouterMessage> Messages { get; set; } = new List<OpenRouterMessage>();

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; set; }

        // Optional: Add other common parameters like TopP, FrequencyPenalty, PresencePenalty etc.
        // [JsonPropertyName("top_p")]
        // [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        // public double? TopP { get; set; }
    }

    public class OpenRouterChoice
    {
        [JsonPropertyName("message")]
        public OpenRouterMessage Message { get; set; } = new OpenRouterMessage();
    }

    public class OpenRouterResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenRouterChoice> Choices { get; set; } = new List<OpenRouterChoice>();

        // Optional: Include other response fields like Id, Created, Model, Usage etc.
        // [JsonPropertyName("id")]
        // public string? Id { get; set; }
    }
}
