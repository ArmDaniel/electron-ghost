using System.Collections.Generic;
using System.Text.Json;

namespace DestinyGhostAssistant.Utils
{
    /// <summary>
    /// Helper for extracting typed values from tool parameter dictionaries.
    /// System.Text.Json deserializes Dictionary&lt;string, object&gt; values as JsonElement,
    /// not as native CLR types, so direct "is string" checks fail.
    /// </summary>
    public static class ToolParameterHelper
    {
        /// <summary>
        /// Extracts a string value from the parameters dictionary, handling both
        /// native string and JsonElement representations.
        /// </summary>
        public static string? GetString(Dictionary<string, object> parameters, string key)
        {
            if (!parameters.TryGetValue(key, out object? value) || value == null)
                return null;

            if (value is string s)
                return s;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString();

                // For non-string JSON types, return the raw text representation
                if (jsonElement.ValueKind != JsonValueKind.Null &&
                    jsonElement.ValueKind != JsonValueKind.Undefined)
                    return jsonElement.GetRawText();
            }

            return value.ToString();
        }
    }
}
