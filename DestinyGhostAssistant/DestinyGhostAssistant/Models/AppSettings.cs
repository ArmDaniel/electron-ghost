namespace DestinyGhostAssistant.Models
{
    public class AppSettings
    {
        public string? SelectedOpenRouterModel { get; set; }
        public string? CustomSystemPrompt { get; set; }

        // Default values can be applied by SettingsService when a new instance is created
        // or when loading fails. No constructor needed here for defaults.
    }
}
