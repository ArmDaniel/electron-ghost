using Microsoft.VisualStudio.TestTools.UnitTesting;
using DestinyGhostAssistant.Services;
using DestinyGhostAssistant.Models;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace DestinyGhostAssistant.Tests
{
    [TestClass]
    public class ChatHistoryServiceTests
    {
        private ChatHistoryService _chatHistoryServiceInstance = null!;

        // Helper to get the actual path ChatHistoryService uses.
        // This must match the path construction logic within ChatHistoryService.
        private static string GetActualChatsFolderPath()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDataFolderName = "DestinyGhostAssistant"; // From ChatHistoryService
            string chatsSubFolderName = "Chats";             // From ChatHistoryService

            string appFolderPath = Path.Combine(localAppDataPath, appDataFolderName);
            return Path.Combine(appFolderPath, chatsSubFolderName);
        }

        private static string GetChatFilePathForTest(string chatName)
        {
             // Simplified for test setup; assumes chatName is already valid for test purposes
            return Path.Combine(GetActualChatsFolderPath(), chatName + ".json");
        }


        [TestInitialize]
        public void TestInitialize()
        {
            _chatHistoryServiceInstance = new ChatHistoryService(); // ChatHistoryService constructor creates the directory
            string actualChatsFolderPath = GetActualChatsFolderPath();

            // Clean the directory before each test
            if (Directory.Exists(actualChatsFolderPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(actualChatsFolderPath);
                foreach (FileInfo file in dirInfo.GetFiles())
                {
                    file.Delete();
                }
                // Do not delete subdirectories if any were created by mistake, only files.
                // Or, if sure, Directory.Delete(actualChatsFolderPath, true); and then Directory.CreateDirectory(actualChatsFolderPath);
            }
            else
            {
                Directory.CreateDirectory(actualChatsFolderPath);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up the entire chats directory after all tests in this class, or after each test.
            // For robust isolation, cleaning after each test is better.
            string actualChatsFolderPath = GetActualChatsFolderPath();
            if (Directory.Exists(actualChatsFolderPath))
            {
                 try
                 {
                    Directory.Delete(actualChatsFolderPath, true); // true for recursive
                 }
                 catch(IOException ex)
                 {
                    // Can happen if a file is locked, e.g. by a pending async operation that didn't finish
                    System.Diagnostics.Debug.WriteLine($"TestCleanup IOException: {ex.Message}. May require manual cleanup of {actualChatsFolderPath}");
                 }
            }
        }

        [TestMethod]
        public async Task SaveAndLoadChatAsync_Success()
        {
            // Arrange
            var messages = new List<ChatMessage>
            {
                new ChatMessage("Hello user", MessageSender.Assistant, DateTime.UtcNow.AddMinutes(-2)),
                new ChatMessage("Hello Ghost", MessageSender.User, DateTime.UtcNow.AddMinutes(-1))
            };
            string chatName = "testChat1";

            // Act
            await _chatHistoryServiceInstance.SaveChatAsync(messages, chatName);
            List<ChatMessage>? loadedMessages = await _chatHistoryServiceInstance.LoadChatAsync(chatName);

            // Assert
            Assert.IsNotNull(loadedMessages, "Loaded messages should not be null.");
            Assert.AreEqual(messages.Count, loadedMessages.Count, "Message counts should match.");

            for (int i = 0; i < messages.Count; i++)
            {
                Assert.AreEqual(messages[i].Text, loadedMessages[i].Text, $"Message {i} text should match.");
                Assert.AreEqual(messages[i].Sender, loadedMessages[i].Sender, $"Message {i} sender should match.");
                // Compare timestamps up to seconds due to potential minor precision differences in serialization/deserialization
                Assert.AreEqual(messages[i].Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), loadedMessages[i].Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), $"Message {i} timestamp should match (to second).");
            }
        }

        [TestMethod]
        public async Task LoadChatAsync_ReturnsNull_WhenChatDoesNotExist()
        {
            // Arrange
            string chatName = "non_existent_chat";

            // Act
            List<ChatMessage>? loadedMessages = await _chatHistoryServiceInstance.LoadChatAsync(chatName);

            // Assert
            Assert.IsNull(loadedMessages, "Loading a non-existent chat should return null.");
        }

        [TestMethod]
        public async Task GetAvailableChatsAsync_ReturnsEmpty_WhenNoChatsSaved()
        {
            // Arrange (TestInitialize ensures directory is clean or just created)

            // Act
            List<string> availableChats = await _chatHistoryServiceInstance.GetAvailableChatsAsync();

            // Assert
            Assert.IsNotNull(availableChats, "Available chats list should not be null.");
            Assert.AreEqual(0, availableChats.Count, "Available chats count should be 0 when no chats are saved.");
        }

        [TestMethod]
        public async Task GetAvailableChatsAsync_ReturnsSavedChatNames()
        {
            // Arrange
            string chatName1 = "sessionOne";
            string chatName2 = "sessionTwo";
            var messages = new List<ChatMessage> { new ChatMessage("Hi", MessageSender.User) };

            await _chatHistoryServiceInstance.SaveChatAsync(messages, chatName1);
            await _chatHistoryServiceInstance.SaveChatAsync(messages, chatName2 + ".with.dots"); // Test name with dots

            // Act
            List<string> availableChats = await _chatHistoryServiceInstance.GetAvailableChatsAsync();

            // Assert
            Assert.IsNotNull(availableChats);
            Assert.AreEqual(2, availableChats.Count, "Should find 2 saved chats.");
            Assert.IsTrue(availableChats.Contains(chatName1), $"Available chats should contain '{chatName1}'.");
            Assert.IsTrue(availableChats.Contains(chatName2 + ".with.dots"), $"Available chats should contain '{chatName2 + ".with.dots"}'.");
        }

        [TestMethod]
        public async Task SaveChatAsync_ThrowsArgumentException_ForInvalidChatName()
        {
            // Arrange
            var messages = new List<ChatMessage>();
            string invalidChatName = "invalid<>name"; // Contains invalid char for filenames

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _chatHistoryServiceInstance.SaveChatAsync(messages, invalidChatName),
                "Saving with an invalid chat name should throw ArgumentException."
            );
        }

        [TestMethod]
        public async Task LoadChatAsync_ReturnsEmptyList_ForEmptyFileContent()
        {
            // Arrange
            string chatName = "empty_chat_file_content";
            string filePath = GetChatFilePathForTest(chatName);
            File.WriteAllText(filePath, "[]"); // Valid empty JSON array

            // Act
            List<ChatMessage>? loadedMessages = await _chatHistoryServiceInstance.LoadChatAsync(chatName);

            // Assert
            Assert.IsNotNull(loadedMessages, "Loaded messages should not be null for an empty JSON array file.");
            Assert.AreEqual(0, loadedMessages.Count, "Loaded messages count should be 0 for an empty JSON array file.");
        }

        [TestMethod]
        public async Task LoadChatAsync_ReturnsEmptyList_ForEmptyStringFile()
        {
            // Arrange
            // ChatHistoryService.LoadChatAsync returns new List<ChatMessage>() if file is empty string or whitespace.
            string chatName = "empty_chat_file_string";
            string filePath = GetChatFilePathForTest(chatName);
            File.WriteAllText(filePath, ""); // Empty string in file

            // Act
            List<ChatMessage>? loadedMessages = await _chatHistoryServiceInstance.LoadChatAsync(chatName);

            // Assert
            Assert.IsNotNull(loadedMessages, "Loaded messages should not be null for an empty string file.");
            Assert.AreEqual(0, loadedMessages.Count, "Loaded messages count should be 0 for an empty string file.");
        }


        [TestMethod]
        public async Task LoadChatAsync_ReturnsNull_ForCorruptJsonFile()
        {
            // Arrange
            string chatName = "corrupt_chat";
            string filePath = GetChatFilePathForTest(chatName);
            File.WriteAllText(filePath, "{this_is_not_valid_json:");

            // Act
            List<ChatMessage>? loadedMessages = await _chatHistoryServiceInstance.LoadChatAsync(chatName);

            // Assert
            // Based on ChatHistoryService, deserialization error should return null
            Assert.IsNull(loadedMessages, "Loading a corrupt JSON file should return null.");
        }

        [TestMethod]
        public async Task LoadChatAsync_ReturnsEmptyList_ForJsonNullLiteral()
        {
            // Arrange
            // ChatHistoryService.LoadChatAsync returns new List<ChatMessage>() if JSON deserializes to null (e.g. "null" literal).
            string chatName = "null_json_literal_chat";
            string filePath = GetChatFilePathForTest(chatName);
            File.WriteAllText(filePath, "null");

            // Act
            List<ChatMessage>? loadedMessages = await _chatHistoryServiceInstance.LoadChatAsync(chatName);

            // Assert
            Assert.IsNotNull(loadedMessages, "Loaded messages should not be null for 'null' literal JSON file.");
            Assert.AreEqual(0, loadedMessages.Count, "Loaded messages count should be 0 for 'null' literal JSON file.");
        }
    }
}
