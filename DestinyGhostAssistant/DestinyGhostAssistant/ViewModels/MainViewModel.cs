using System.Collections.ObjectModel;
using System.Windows.Input;
using DestinyGhostAssistant.Models;
using DestinyGhostAssistant.Utils; // For RelayCommand
using DestinyGhostAssistant.Services; // For OpenRouterService and ToolExecutorService
using DestinyGhostAssistant.Services.Tools; // For ITool
using System.Threading.Tasks; // For Task
using System.Collections.Generic; // For List
using System.Linq; // For ToList to create a copy
using System.Text; // For StringBuilder
using System.Windows; // Required for Application.Current.Dispatcher

namespace DestinyGhostAssistant.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public ObservableCollection<ChatMessage> Messages { get; }

        private string _currentMessage = string.Empty;
        public string CurrentMessage
        {
            get => _currentMessage;
            set
            {
                if (SetProperty(ref _currentMessage, value))
                {
                    if (SendMessageCommand is RelayCommand command)
                    {
                        command.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        public ICommand SendMessageCommand { get; }

        private bool _isSendingMessage;
        public bool IsSendingMessage
        {
            get => _isSendingMessage;
            private set
            {
                if (SetProperty(ref _isSendingMessage, value))
                {
                    if (SendMessageCommand is RelayCommand command)
                    {
                        command.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        private readonly OpenRouterService _openRouterService;
        private readonly ToolExecutorService _toolExecutorService;
        private readonly List<OpenRouterMessage> _conversationHistory;
        private const int MaxHistoryMessages = 20;
        // SystemPrompt is no longer a const, it's built in the constructor
        // private const string SystemPrompt = "...";

        public MainViewModel()
        {
            Messages = new ObservableCollection<ChatMessage>();
            _conversationHistory = new List<OpenRouterMessage>();

            // API Key is now loaded at App startup and passed from there via App.OpenRouterApiKey.
            _openRouterService = new OpenRouterService(App.OpenRouterApiKey);
            _toolExecutorService = new ToolExecutorService(this);

            // Construct the dynamic system prompt
            var systemPromptBuilder = new StringBuilder();
            systemPromptBuilder.AppendLine("You are a helpful Ghost assistant from the Destiny game universe. Embody the character of Ghost. Be concise and helpful, but with Ghost's personality quirks and way of speaking. Keep responses relatively short.");
            systemPromptBuilder.AppendLine();
            systemPromptBuilder.AppendLine("You have access to the following tools. To use a tool, respond ONLY with a JSON object in the following format (do not add any other text before or after the JSON if you are calling a tool):");
            systemPromptBuilder.AppendLine("{");
            systemPromptBuilder.AppendLine("  \"tool_name\": \"name_of_the_tool\",");
            systemPromptBuilder.AppendLine("  \"parameters\": {");
            systemPromptBuilder.AppendLine("    \"param_name1\": \"value1\",");
            systemPromptBuilder.AppendLine("    \"param_name2\": \"value2\"");
            systemPromptBuilder.AppendLine("  }");
            systemPromptBuilder.AppendLine("}");
            systemPromptBuilder.AppendLine();
            systemPromptBuilder.AppendLine("Available tools:");

            var availableTools = _toolExecutorService.GetAvailableTools();
            if (availableTools.Any())
            {
                foreach (var tool in availableTools)
                {
                    systemPromptBuilder.AppendLine($"- Tool: {tool.Name}");
                    systemPromptBuilder.AppendLine($"  Description: {tool.Description}");
                }
            }
            else
            {
                systemPromptBuilder.AppendLine("No tools are currently available.");
            }

            string systemPrompt = systemPromptBuilder.ToString();
            _conversationHistory.Add(new OpenRouterMessage { Role = "system", Content = systemPrompt });

            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), () => !string.IsNullOrWhiteSpace(CurrentMessage) && !IsSendingMessage);

            AddMessage("Welcome, Guardian! Ghost at your service. How can I assist you today?", MessageSender.Assistant);
        }

        private void AddMessage(string text, MessageSender sender)
        {
            var chatMessage = new ChatMessage(text, sender);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(chatMessage);
            });
        }

        public void AddSystemMessage(string message)
        {
            AddMessage(message, MessageSender.System);
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentMessage) || IsSendingMessage)
            {
                return;
            }

            IsSendingMessage = true;

            string userMessageText = CurrentMessage;
            AddMessage(userMessageText, MessageSender.User);
            _conversationHistory.Add(new OpenRouterMessage { Role = "user", Content = userMessageText });
            CurrentMessage = string.Empty;

            try
            {
                string assistantResponseText = await _openRouterService.GetChatCompletionAsync(new List<OpenRouterMessage>(_conversationHistory));

                ToolCallRequest? potentialToolCall = _toolExecutorService.TryParseToolCall(assistantResponseText);
                string processingResult = await _toolExecutorService.ProcessAssistantResponseForToolsAsync(assistantResponseText);

                if (potentialToolCall != null)
                {
                    _conversationHistory.Add(new OpenRouterMessage { Role = "assistant", Content = assistantResponseText });
                    _conversationHistory.Add(new OpenRouterMessage { Role = "user", Content = $"Tool output: {processingResult}" });
                }
                else
                {
                    AddMessage(processingResult, MessageSender.Assistant);
                    _conversationHistory.Add(new OpenRouterMessage { Role = "assistant", Content = processingResult });
                }

                TruncateConversationHistory();
            }
            catch (System.Exception ex)
            {
                AddSystemMessage($"Error during message processing or API call: {ex.Message}");
            }
            finally
            {
                IsSendingMessage = false;
            }
        }

        private void TruncateConversationHistory()
        {
            if (_conversationHistory.Count > MaxHistoryMessages)
            {
                int itemsToRemove = _conversationHistory.Count - MaxHistoryMessages;
                if (_conversationHistory.Count > 0 && _conversationHistory[0].Role == "system")
                {
                    if (itemsToRemove > 0 && _conversationHistory.Count > 1 + itemsToRemove -1)
                    {
                         _conversationHistory.RemoveRange(1, itemsToRemove);
                    }
                    else if (itemsToRemove > 0)
                    {
                        _conversationHistory.RemoveRange(1, _conversationHistory.Count - 1);
                    }
                }
                else
                {
                    _conversationHistory.RemoveRange(0, itemsToRemove);
                }
            }
        }
    }
}
