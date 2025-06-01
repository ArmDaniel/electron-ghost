using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DestinyGhostAssistant.Models; // For ChatMessage
using System.Diagnostics; // For Debug.WriteLine

namespace DestinyGhostAssistant.Services
{
    public class ChatHistoryService
    {
        private static readonly string AppDataFolderName = "DestinyGhostAssistant";
        private static readonly string ChatsSubFolderName = "Chats";

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            // Add any other necessary default options, e.g., converters if ChatMessage becomes more complex
        };

        private static string GetChatsFolderPath()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = Path.Combine(localAppDataPath, AppDataFolderName);
            return Path.Combine(appFolderPath, ChatsSubFolderName);
        }

        private static string GetChatFilePath(string chatName)
        {
            if (string.IsNullOrWhiteSpace(chatName))
            {
                throw new ArgumentException("Chat name cannot be empty or whitespace.", nameof(chatName));
            }
            if (chatName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException($"Chat name '{chatName}' contains invalid characters.", nameof(chatName));
            }
            return Path.Combine(GetChatsFolderPath(), chatName + ".json");
        }

        public ChatHistoryService()
        {
            try
            {
                Directory.CreateDirectory(GetChatsFolderPath());
                Debug.WriteLine($"ChatHistoryService: Ensured chats directory exists at {GetChatsFolderPath()}");
            }
            catch (Exception ex) // Catch broad exception as this is in constructor
            {
                Debug.WriteLine($"ChatHistoryService: Error creating chats directory during construction. Path: {GetChatsFolderPath()}. Error: {ex.Message}");
                // Depending on requirements, might rethrow or handle more gracefully.
                // For now, subsequent operations will likely fail if directory cannot be created.
            }
        }

        public async Task SaveChatAsync(IEnumerable<ChatMessage> messages, string chatName)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            // chatName validation is handled by GetChatFilePath

            string filePath;
            try
            {
                filePath = GetChatFilePath(chatName); // Can throw ArgumentException
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine($"ChatHistoryService: Invalid chat name for saving. Error: {ex.Message}");
                throw; // Rethrow to inform caller of bad chatName
            }

            try
            {
                // Ensure directory exists (might be redundant if constructor succeeded, but good for robustness)
                string? directoryPath = Path.GetDirectoryName(filePath);
                if (directoryPath != null) // Should not be null given GetChatFilePath logic
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string json = JsonSerializer.Serialize(messages, _jsonSerializerOptions);
                await File.WriteAllTextAsync(filePath, json);
                Debug.WriteLine($"ChatHistoryService: Chat '{chatName}' saved to {filePath}");
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"ChatHistoryService: JSON serialization error while saving chat '{chatName}'. Error: {ex.Message}");
                throw new Exception($"Error serializing chat '{chatName}'.", ex); // Wrap in a more generic exception or custom one
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"ChatHistoryService: IOException while saving chat '{chatName}' to {filePath}. Error: {ex.Message}");
                throw new Exception($"IO error saving chat '{chatName}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"ChatHistoryService: UnauthorizedAccessException while saving chat '{chatName}' to {filePath}. Error: {ex.Message}");
                throw new Exception($"Access denied saving chat '{chatName}'.", ex);
            }
            catch (Exception ex) // Catch-all for other potential errors
            {
                Debug.WriteLine($"ChatHistoryService: Unexpected error while saving chat '{chatName}' to {filePath}. Error: {ex.Message}");
                throw new Exception($"Unexpected error saving chat '{chatName}'.", ex);
            }
        }

        public async Task<List<ChatMessage>?> LoadChatAsync(string chatName)
        {
            // chatName validation is handled by GetChatFilePath
            string filePath;
            try
            {
                filePath = GetChatFilePath(chatName); // Can throw ArgumentException
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine($"ChatHistoryService: Invalid chat name for loading. Error: {ex.Message}");
                throw; // Rethrow to inform caller of bad chatName
            }

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"ChatHistoryService: Chat file not found: {filePath}");
                    return null;
                }

                string json = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                     Debug.WriteLine($"ChatHistoryService: Chat file is empty or whitespace: {filePath}");
                     return new List<ChatMessage>(); // Return empty list for empty file
                }

                List<ChatMessage>? messages = JsonSerializer.Deserialize<List<ChatMessage>>(json, _jsonSerializerOptions);
                Debug.WriteLine($"ChatHistoryService: Chat '{chatName}' loaded from {filePath}");
                return messages ?? new List<ChatMessage>(); // Ensure null deserialization returns empty list
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"ChatHistoryService: JSON deserialization error while loading chat '{chatName}' from {filePath}. Error: {ex.Message}");
                return null; // Indicate error by returning null
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"ChatHistoryService: IOException while loading chat '{chatName}' from {filePath}. Error: {ex.Message}");
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"ChatHistoryService: UnauthorizedAccessException while loading chat '{chatName}' from {filePath}. Error: {ex.Message}");
                return null;
            }
            catch (Exception ex) // Catch-all for other potential errors
            {
                Debug.WriteLine($"ChatHistoryService: Unexpected error while loading chat '{chatName}' from {filePath}. Error: {ex.Message}");
                return null;
            }
        }

        public Task<List<string>> GetAvailableChatsAsync()
        {
            string chatsFolderPath = GetChatsFolderPath();
            try
            {
                if (!Directory.Exists(chatsFolderPath))
                {
                    Debug.WriteLine($"ChatHistoryService: Chats directory not found: {chatsFolderPath}");
                    return Task.FromResult(new List<string>());
                }

                string[] files = Directory.GetFiles(chatsFolderPath, "*.json");
                var chatNames = files.Select(filePath => Path.GetFileNameWithoutExtension(filePath))
                                     .Where(name => !string.IsNullOrWhiteSpace(name)) // Filter out any potentially empty names
                                     .ToList();
                Debug.WriteLine($"ChatHistoryService: Found {chatNames.Count} available chats.");
                return Task.FromResult(chatNames);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"ChatHistoryService: IOException while listing available chats in {chatsFolderPath}. Error: {ex.Message}");
                return Task.FromResult(new List<string>());
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"ChatHistoryService: UnauthorizedAccessException while listing available chats in {chatsFolderPath}. Error: {ex.Message}");
                return Task.FromResult(new List<string>());
            }
            catch (Exception ex) // Catch-all for other potential errors
            {
                Debug.WriteLine($"ChatHistoryService: Unexpected error while listing available chats in {chatsFolderPath}. Error: {ex.Message}");
                return Task.FromResult(new List<string>());
            }
        }
    }
}
