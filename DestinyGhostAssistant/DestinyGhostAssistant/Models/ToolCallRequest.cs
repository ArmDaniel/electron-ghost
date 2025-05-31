using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DestinyGhostAssistant.Models
{
    public class ToolCallRequest
    {
        [JsonPropertyName("tool_name")]
        public string ToolName { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
