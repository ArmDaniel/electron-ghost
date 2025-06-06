using Microsoft.VisualStudio.TestTools.UnitTesting;
using DestinyGhostAssistant.ViewModels;
using DestinyGhostAssistant.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows; // For Application, though it will be null in tests.
using System.Threading; // For ApartmentState
using System.Windows.Threading; // For Dispatcher

namespace DestinyGhostAssistant.Tests
{
    [TestClass]
    public class MainViewModelTests
    {
        private MainViewModel _viewModel = null!;

        // It's tricky to set up a fully functional WPF Application context in unit tests.
        // MainViewModel has been modified to check Application.Current for null.
        // For tests involving UI-dependent features (like dialogs), we'd need UI testing or more advanced mocking.
        // These tests focus on ViewModel logic that can be isolated.

        [TestInitialize]
        public void TestInitialize()
        {
            // ApiKeyService will use its actual path, ensure it's clean for OpenRouterService init if it matters.
            // SettingsService will also use its actual path, MainViewModel loads it.
            // ChatHistoryService also uses actual paths, MainViewModel instantiates it.
            // For these tests, we assume these services construct okay and focus on VM logic.
            // The Dispatcher checks in MainViewModel should prevent crashes.

            // If tests need a specific API key to avoid OpenRouterService constructor error
            if (Application.Current == null) // A simple way to ensure App static properties are set if not in WPF context
            {
                // This is a hack for tests. In a real app, App.xaml.cs handles this.
                // We need to provide a dummy key because OpenRouterService constructor expects one.
                // This won't create a real Application object but allows static properties to be set.
                // The actual API key logic via ApiKeyService is tested in ApiKeyServiceTests.
                // Here, we just need OpenRouterService to not throw.
                typeof(App).GetProperty("OpenRouterApiKey")?.SetValue(null, "dummy_test_key_for_vm_tests", null);
            }

            _viewModel = new MainViewModel();
        }

        [TestMethod]
        public void StartNewChatSession_ClearsMessagesAndHistory_AddsSystemPromptAndWelcome()
        {
            // Arrange
            // Add some dummy messages to ensure they are cleared
            _viewModel.Messages.Add(new ChatMessage("Old user message", MessageSender.User));
            // To modify _conversationHistory for arrange, we'd need more internal access or a more complex setup.
            // For now, we'll verify its state by checking the count and first item after StartNewChatSession.
            // And by checking what's sent to OpenRouterService if we were mocking it.

            // Act
            _viewModel.StartNewChatSession(isUserAction: false);

            // Assert
            Assert.AreEqual(1, _viewModel.Messages.Count, "Messages collection should only contain the welcome message.");
            Assert.AreEqual("Welcome, Guardian! Ghost at your service. How can I assist you today?", _viewModel.Messages[0].Text);
            Assert.AreEqual(MessageSender.Assistant, _viewModel.Messages[0].Sender, "Welcome message sender should be Assistant.");

            var conversationHistory = _viewModel.GetConversationHistoryForTest();
            Assert.AreEqual(1, conversationHistory.Count, "Conversation history should only contain the system prompt.");
            Assert.AreEqual("system", conversationHistory[0].Role, "First message in history should be system role.");
            Assert.IsTrue(conversationHistory[0].Content.Contains("You are a helpful Ghost assistant"), "System prompt content is incorrect.");
        }

        [TestMethod]
        public void PopulateChat_CorrectlyPopulatesUiAndApiHistory()
        {
            // Arrange
            var testTimestampUser = DateTime.UtcNow.AddMinutes(-5);
            var testTimestampAssistant = DateTime.UtcNow.AddMinutes(-4);
            var testTimestampSystemUi = DateTime.UtcNow.AddMinutes(-3); // This system message is for UI only

            var messagesToLoad = new List<ChatMessage>
            {
                new ChatMessage("User message from loaded chat", MessageSender.User, testTimestampUser),
                new ChatMessage("Assistant response from loaded chat", MessageSender.Assistant, testTimestampAssistant),
                new ChatMessage("System info from loaded chat", MessageSender.System, testTimestampSystemUi)
            };
            string chatName = "test_populate_chat";

            // Act
            _viewModel.PopulateChatForTest(messagesToLoad, chatName); // Using the internal test helper

            // Assert - UI Messages
            Assert.AreEqual(3, _viewModel.Messages.Count, "UI Messages count should match loaded messages.");

            Assert.AreEqual("User message from loaded chat", _viewModel.Messages[0].Text);
            Assert.AreEqual(MessageSender.User, _viewModel.Messages[0].Sender);
            Assert.AreEqual(testTimestampUser.ToString("o"), _viewModel.Messages[0].Timestamp.ToString("o"));

            Assert.AreEqual("Assistant response from loaded chat", _viewModel.Messages[1].Text);
            Assert.AreEqual(MessageSender.Assistant, _viewModel.Messages[1].Sender);
            Assert.AreEqual(testTimestampAssistant.ToString("o"), _viewModel.Messages[1].Timestamp.ToString("o"));

            Assert.AreEqual("System info from loaded chat", _viewModel.Messages[2].Text);
            Assert.AreEqual(MessageSender.System, _viewModel.Messages[2].Sender);
            Assert.AreEqual(testTimestampSystemUi.ToString("o"), _viewModel.Messages[2].Timestamp.ToString("o"));

            // Assert - API Conversation History (_conversationHistory)
            var conversationHistory = _viewModel.GetConversationHistoryForTest();
            // Expecting: System Prompt, User Message, Assistant Message (System UI message is excluded from API history)
            Assert.AreEqual(3, conversationHistory.Count, "API _conversationHistory count is incorrect.");

            Assert.AreEqual("system", conversationHistory[0].Role, "First API history item should be system prompt.");
            Assert.IsTrue(conversationHistory[0].Content.Contains("You are a helpful Ghost assistant"), "System prompt content is incorrect in API history.");

            Assert.AreEqual("user", conversationHistory[1].Role, "Second API history item should be user.");
            Assert.AreEqual("User message from loaded chat", conversationHistory[1].Content);

            Assert.AreEqual("assistant", conversationHistory[2].Role, "Third API history item should be assistant.");
            Assert.AreEqual("Assistant response from loaded chat", conversationHistory[2].Content);
        }

        [TestMethod]
        public void SendMessageCommand_CanExecute_ChangesWithCurrentMessageAndIsSendingMessage()
        {
            // Arrange
            // Initial state (CurrentMessage is empty, IsSendingMessage is false)
            Assert.IsFalse(_viewModel.SendMessageCommand.CanExecute(null), "SendMessageCommand should initially be disabled (empty message).");

            // Act: Set CurrentMessage to non-empty
            _viewModel.CurrentMessage = "Hello";
            // Assert
            Assert.IsTrue(_viewModel.SendMessageCommand.CanExecute(null), "SendMessageCommand should be enabled when CurrentMessage is not empty.");

            // Act: Set IsSendingMessage to true
            _viewModel.IsSendingMessage = true;
            // Assert
            Assert.IsFalse(_viewModel.SendMessageCommand.CanExecute(null), "SendMessageCommand should be disabled when IsSendingMessage is true.");

            // Act: Set IsSendingMessage back to false
            _viewModel.IsSendingMessage = false;
            // Assert
            Assert.IsTrue(_viewModel.SendMessageCommand.CanExecute(null), "SendMessageCommand should be re-enabled when IsSendingMessage is false.");

            // Act: Set CurrentMessage to whitespace
            _viewModel.CurrentMessage = "   ";
            // Assert
            Assert.IsFalse(_viewModel.SendMessageCommand.CanExecute(null), "SendMessageCommand should be disabled when CurrentMessage is only whitespace.");

            // Act: Set CurrentMessage to empty
            _viewModel.CurrentMessage = "";
            // Assert
            Assert.IsFalse(_viewModel.SendMessageCommand.CanExecute(null), "SendMessageCommand should be disabled when CurrentMessage is empty.");
        }
    }
}
