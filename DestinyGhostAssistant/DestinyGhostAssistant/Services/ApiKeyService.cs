using System;
using System.IO;
using System.Diagnostics; // For Debug.WriteLine

namespace DestinyGhostAssistant.Services
{
    public static class ApiKeyService
    {
        private static readonly string AppDataFolderName = "DestinyGhostAssistant";
        private static readonly string ApiKeyFileName = "settings.dat"; // Simple text file

        private static string GetApiKeyFilePath()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = Path.Combine(localAppDataPath, AppDataFolderName);
            return Path.Combine(appFolderPath, ApiKeyFileName);
        }

        public static void SaveApiKey(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) // Do not save empty or whitespace keys
            {
                Debug.WriteLine("ApiKeyService: Attempted to save an empty or whitespace API key. Operation aborted.");
                // Optionally throw an ArgumentException or handle as appropriate
                return;
            }

            string filePath = GetApiKeyFilePath();
            try
            {
                string? directoryPath = Path.GetDirectoryName(filePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                File.WriteAllText(filePath, apiKey); // Overwrites if exists, creates if not
                Debug.WriteLine($"ApiKeyService: API Key saved to {filePath}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"ApiKeyService: IOException while saving API key to {filePath}. Error: {ex.Message}");
                // Optionally rethrow or handle more gracefully
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"ApiKeyService: UnauthorizedAccessException while saving API key to {filePath}. Error: {ex.Message}");
            }
            catch (Exception ex) // Catch-all for other potential errors
            {
                Debug.WriteLine($"ApiKeyService: Unexpected error while saving API key to {filePath}. Error: {ex.Message}");
            }
        }

        public static string? LoadApiKey()
        {
            string filePath = GetApiKeyFilePath();
            try
            {
                if (File.Exists(filePath))
                {
                    string apiKey = File.ReadAllText(filePath);
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        Debug.WriteLine($"ApiKeyService: API Key file found at {filePath}, but it is empty or whitespace.");
                        return null; // Treat empty/whitespace key as if not found
                    }
                    Debug.WriteLine($"ApiKeyService: API Key loaded from {filePath}");
                    return apiKey;
                }
                else
                {
                    Debug.WriteLine($"ApiKeyService: API Key file not found at {filePath}.");
                    return null;
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"ApiKeyService: IOException while loading API key from {filePath}. Error: {ex.Message}");
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"ApiKeyService: UnauthorizedAccessException while loading API key from {filePath}. Error: {ex.Message}");
                return null;
            }
            catch (Exception ex) // Catch-all for other potential errors
            {
                Debug.WriteLine($"ApiKeyService: Unexpected error while loading API key from {filePath}. Error: {ex.Message}");
                return null;
            }
        }

        public static bool HasApiKey()
        {
            string filePath = GetApiKeyFilePath();
            // Additionally check if the loaded key is not just whitespace
            string? key = LoadApiKey(); // This uses the file existence check and whitespace check
            return !string.IsNullOrWhiteSpace(key);
        }

        public static void DeleteApiKey()
        {
            string filePath = GetApiKeyFilePath();
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.WriteLine($"ApiKeyService: API Key file deleted from {filePath}");
                }
                else
                {
                    Debug.WriteLine($"ApiKeyService: API Key file not found at {filePath}. Nothing to delete.");
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"ApiKeyService: IOException while deleting API key file {filePath}. Error: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"ApiKeyService: UnauthorizedAccessException while deleting API key file {filePath}. Error: {ex.Message}");
            }
            catch (Exception ex) // Catch-all for other potential errors
            {
                Debug.WriteLine($"ApiKeyService: Unexpected error while deleting API key file {filePath}. Error: {ex.Message}");
            }
        }
    }
}
