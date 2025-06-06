using System;
using System.IO;
using System.Text.Json;
using DestinyGhostAssistant.Models;
using System.Diagnostics;

namespace DestinyGhostAssistant.Services
{
    public class SettingsService
    {
        private static readonly string AppDataFolderName = "DestinyGhostAssistant";
        private static readonly string SettingsFileName = "app_settings.json";

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = true, // Be more lenient on load
            PropertyNameCaseInsensitive = true // For flexibility if user edits file
        };

        private static string GetSettingsFilePath()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = Path.Combine(localAppDataPath, AppDataFolderName);
            return Path.Combine(appFolderPath, SettingsFileName);
        }

        public SettingsService()
        {
            try
            {
                // Ensure the base directory for app settings exists
                string filePath = GetSettingsFilePath();
                string? directoryPath = Path.GetDirectoryName(filePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Debug.WriteLine($"SettingsService: Created settings directory at {directoryPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsService: Error creating settings directory during construction. Error: {ex.Message}");
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            string filePath = GetSettingsFilePath();
            try
            {
                string json = JsonSerializer.Serialize(settings, _jsonSerializerOptions);
                File.WriteAllText(filePath, json);
                Debug.WriteLine($"SettingsService: Settings saved to {filePath}");
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"SettingsService: JSON serialization error while saving settings. Error: {ex.Message}");
                // Optionally rethrow or handle more gracefully
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"SettingsService: IOException while saving settings to {filePath}. Error: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"SettingsService: UnauthorizedAccessException while saving settings to {filePath}. Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsService: Unexpected error while saving settings to {filePath}. Error: {ex.Message}");
            }
        }

        public AppSettings LoadSettings()
        {
            string filePath = GetSettingsFilePath();
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Debug.WriteLine($"SettingsService: Settings file found at {filePath}, but it is empty or whitespace. Returning default settings.");
                        return GetDefaultAppSettings();
                    }

                    AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonSerializerOptions);
                    if (settings != null)
                    {
                        Debug.WriteLine($"SettingsService: Settings loaded from {filePath}");
                        // Ensure defaults are applied if specific settings are missing from the loaded file
                        settings.SelectedOpenRouterModel ??= GetDefaultAppSettings().SelectedOpenRouterModel;
                        // CustomSystemPrompt can remain null if not set, MainViewModel will handle its own default.
                        return settings;
                    }
                    else
                    {
                        Debug.WriteLine($"SettingsService: Failed to deserialize settings from {filePath}. Returning default settings.");
                        return GetDefaultAppSettings();
                    }
                }
                else
                {
                    Debug.WriteLine($"SettingsService: Settings file not found at {filePath}. Returning default settings.");
                    return GetDefaultAppSettings();
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"SettingsService: JSON deserialization error while loading settings from {filePath}. Error: {ex.Message}. Returning default settings.");
                return GetDefaultAppSettings();
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"SettingsService: IOException while loading settings from {filePath}. Error: {ex.Message}. Returning default settings.");
                return GetDefaultAppSettings();
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"SettingsService: UnauthorizedAccessException while loading settings from {filePath}. Error: {ex.Message}. Returning default settings.");
                return GetDefaultAppSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsService: Unexpected error while loading settings from {filePath}. Error: {ex.Message}. Returning default settings.");
                return GetDefaultAppSettings();
            }
        }

        private AppSettings GetDefaultAppSettings()
        {
            return new AppSettings
            {
                SelectedOpenRouterModel = "gryphe/mythomax-l2-13b",
                CustomSystemPrompt = null // MainViewModel will use its dynamic prompt if this is null
            };
        }
    }
}
