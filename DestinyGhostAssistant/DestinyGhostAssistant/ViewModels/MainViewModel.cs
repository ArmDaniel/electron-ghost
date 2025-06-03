using System.Collections.ObjectModel;
using System.Windows.Input;
using DestinyGhostAssistant.Models;
using DestinyGhostAssistant.Utils; // For RelayCommand
using DestinyGhostAssistant.Services; // For OpenRouterService, ToolExecutorService, ChatHistoryService, SettingsService
using DestinyGhostAssistant.Services.Tools; // For ITool
using System; // For DateTime
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
        public string CurrentMessage { get => _currentMessage; set => SetProperty(ref _currentMessage, value); }

        public ICommand SendMessageCommand { get; }
        public ICommand NewChatCommand { get; }
        public ICommand SaveChatCommand { get; }
        public ICommand LoadChatCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand CopyToClipboardCommand { get; } // Added command

        public event EventHandler? RequestFocusOnInput;

        private bool _isSendingMessage;
        public bool IsSendingMessage
        {
            get => _isSendingMessage;
            private set
            {
                if (SetProperty(ref _isSendingMessage, value))
                {
                    if (SendMessageCommand is RelayCommand sendCmd) sendCmd.RaiseCanExecuteChanged();
                    if (SaveChatCommand is RelayCommand saveCmd) saveCmd.RaiseCanExecuteChanged();
                    if (LoadChatCommand is RelayCommand loadCmd) loadCmd.RaiseCanExecuteChanged();
                    if (NewChatCommand is RelayCommand newCmd) newCmd.RaiseCanExecuteChanged();
                    if (OpenSettingsCommand is RelayCommand openSetCmd) openSetCmd.RaiseCanExecuteChanged();

                    if (!value) // If IsSendingMessage is being set to false
                    {
                        RequestFocusOnInput?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        private readonly OpenRouterService _openRouterService;
        private readonly ToolExecutorService _toolExecutorService;
        private readonly ChatHistoryService _chatHistoryService;
        private readonly SettingsService _settingsService;
        private AppSettings _appSettings;
        private readonly List<OpenRouterMessage> _conversationHistory;
        private const int MaxHistoryMessages = 20;
        private string _systemPromptString;
        private ChatMessage? _thinkingMessage; // Field for the "Thinking..." message instance

        public MainViewModel()
        {
            Messages = new ObservableCollection<ChatMessage>();
            _conversationHistory = new List<OpenRouterMessage>();

            _settingsService = new SettingsService();
            _appSettings = _settingsService.LoadSettings();

            _toolExecutorService = new ToolExecutorService(this);
            _systemPromptString = BuildInitialSystemPrompt();

            _openRouterService = new OpenRouterService(App.OpenRouterApiKey);
            _chatHistoryService = new ChatHistoryService();

            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), () => !string.IsNullOrWhiteSpace(CurrentMessage) && !IsSendingMessage);
            NewChatCommand = new RelayCommand(StartNewChatUserAction, () => !IsSendingMessage);
            SaveChatCommand = new RelayCommand(async () => await SaveChatAsync(), () => Messages.Any() && !IsSendingMessage);
            LoadChatCommand = new RelayCommand(async () => await LoadChatListAndPromptAsync(), () => !IsSendingMessage);
            OpenSettingsCommand = new RelayCommand(OpenSettingsWindow, () => !IsSendingMessage);
            CopyToClipboardCommand = new RelayCommand<string?>(CopyTextToClipboard, CanCopyTextToClipboard); // Initialize command

            Messages.CollectionChanged += (s, e) =>
            {
                if (SaveChatCommand is RelayCommand saveCmd) saveCmd.RaiseCanExecuteChanged();
            };

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CurrentMessage))
                {
                    if (SendMessageCommand is RelayCommand sendCmd) sendCmd.RaiseCanExecuteChanged();
                }
            };

            StartNewChatSession(isUserAction: false);
        }

        private string BuildInitialSystemPrompt()
        {
            if (!string.IsNullOrWhiteSpace(_appSettings.CustomSystemPrompt))
            {
                return _appSettings.CustomSystemPrompt;
            }
            else
            {
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
                return systemPromptBuilder.ToString();
            }
        }

        private void OpenSettingsWindow()
        {
            IsSendingMessage = true;
            var settingsWindow = new DestinyGhostAssistant.Views.SettingsWindow();
            if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                settingsWindow.Owner = Application.Current.MainWindow;
            }

            bool? result = settingsWindow.ShowDialog();

            if (result == true)
            {
                _appSettings = _settingsService.LoadSettings();
                _systemPromptString = BuildInitialSystemPrompt();
                AddSystemMessage("Settings saved. System prompt changes will apply to new chats. Model changes apply immediately to next request.");
            }
            IsSendingMessage = false;
        }

        private async Task SaveChatAsync()
        {
            var saveDialog = new DestinyGhostAssistant.Views.SaveChatDialog();
             if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
             {
                 saveDialog.Owner = Application.Current.MainWindow;
             }

            if (saveDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(saveDialog.ChatName))
            {
                string chatName = saveDialog.ChatName;
                IsSendingMessage = true;
                AddSystemMessage($"Saving chat as '{chatName}'...");
                try
                {
                    var messagesToSave = new List<ChatMessage>(Messages);
                    await _chatHistoryService.SaveChatAsync(messagesToSave, chatName);
                    AddSystemMessage($"Chat '{chatName}' saved successfully.");
                }
                catch (ArgumentException argEx)
                {
                    AddSystemMessage($"Error saving chat: Invalid chat name. {argEx.Message}");
                }
                catch (Exception ex)
                {
                    AddSystemMessage($"Error saving chat '{chatName}': {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"SaveChatAsync Exception: {ex}");
                }
                finally
                {
                    IsSendingMessage = false;
                }
            }
            else
            {
                AddSystemMessage("Save chat cancelled by user or no name provided.");
            }
        }

        private void CopyTextToClipboard(string? textToCopy)
        {
            if (!string.IsNullOrEmpty(textToCopy))
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() => Clipboard.SetText(textToCopy));
                    AddSystemMessage("Assistant's message copied to clipboard!");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error copying to clipboard: {ex.Message}");
                    AddSystemMessage("Error: Could not copy message to clipboard.");
                }
            }
        }

        private bool CanCopyTextToClipboard(string? textToCopy)
        {
            return !string.IsNullOrEmpty(textToCopy);
        }

        private async Task LoadChatListAndPromptAsync()
        {
            IsSendingMessage = true;
            AddSystemMessage("Fetching list of saved chats...");
            System.Diagnostics.Debug.WriteLine("[MainViewModel] LoadChatListAndPromptAsync: Fetching chat list.");
            try
            {
                List<string> availableChats = await _chatHistoryService.GetAvailableChatsAsync();
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] LoadChatListAndPromptAsync: Found {availableChats?.Count ?? 0} available chats. Chats: {string.Join(", ", availableChats ?? new List<string>())}");

                if (availableChats == null || !availableChats.Any())
                {
                    AddSystemMessage("No saved chats found.");
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] LoadChatListAndPromptAsync: No saved chats found or list is null.");
                    return; // Return here so IsSendingMessage is set to false in finally
                }

                var loadDialog = new DestinyGhostAssistant.Views.LoadChatDialog(availableChats);
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    loadDialog.Owner = Application.Current.MainWindow;
                }

                if (loadDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(loadDialog.SelectedChatName))
                {
                    string chatToLoad = loadDialog.SelectedChatName;
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] LoadChatListAndPromptAsync: User selected chat '{chatToLoad}'. Calling LoadChatAsync.");
                    await LoadChatAsync(chatToLoad);
                }
                else
                {
                    AddSystemMessage("Load chat cancelled by user or no chat selected.");
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] LoadChatListAndPromptAsync: Load chat dialog cancelled or no selection.");
                }
            }
            catch (Exception ex)
            {
                AddSystemMessage($"Error fetching chat list: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] LoadChatListAndPromptAsync Exception: {ex}");
            }
            finally
            {
                IsSendingMessage = false;
            }
        }

        private void StartNewChatUserAction()
        {
            StartNewChatSession(isUserAction: true);
        }

        private void AddMessage(string text, MessageSender sender, DateTime? timestamp = null)
        {
            var chatMessage = new ChatMessage(text, sender, timestamp);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(chatMessage);
            });
        }

        // Helper to add the exact ChatMessage object instance to the UI
        private void AddChatMessageObject(ChatMessage message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(message);
            });
        }

        // Helper to remove the exact ChatMessage object instance from the UI
        private void RemoveChatMessageObject(ChatMessage? message)
        {
            if (message != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Messages.Contains(message)) // Check if it's still there before removing
                    {
                        Messages.Remove(message);
                    }
                });
            }
        }


        public void AddSystemMessage(string message)
        {
            AddMessage(message, MessageSender.System);
        }

        public void StartNewChatSession(string newChatName = "New Chat", bool isUserAction = true)
        {
            if (isUserAction)
            {
                AddSystemMessage("Starting new chat session...");
            }
            Messages.Clear();
            System.Diagnostics.Debug.WriteLine("[MainViewModel] StartNewChatSession: Cleared Messages collection.");
            _conversationHistory.Clear();
            System.Diagnostics.Debug.WriteLine("[MainViewModel] StartNewChatSession: Cleared _conversationHistory.");
            if(_systemPromptString != null)
            {
                 _conversationHistory.Add(new OpenRouterMessage { Role = "system", Content = _systemPromptString });
                 System.Diagnostics.Debug.WriteLine("[MainViewModel] StartNewChatSession: Added system prompt to _conversationHistory.");
            }
            else
            {
                _conversationHistory.Add(new OpenRouterMessage { Role = "system", Content = "You are a helpful assistant." });
                AddSystemMessage("Error: System prompt not initialized. Using a very basic default.");
                System.Diagnostics.Debug.WriteLine("[MainViewModel] StartNewChatSession: ERROR - _systemPromptString was null, added basic default.");
            }
            AddMessage("Welcome, Guardian! Ghost at your service. How can I assist you today?", MessageSender.Assistant);
        }

        private void PopulateChat(IEnumerable<ChatMessage> messagesToLoad, string loadedChatName)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] PopulateChat: Loading chat '{loadedChatName}'. Message count to load: {messagesToLoad.Count()}");
            Messages.Clear();
            System.Diagnostics.Debug.WriteLine("[MainViewModel] PopulateChat: Cleared Messages collection.");
            _conversationHistory.Clear();
            System.Diagnostics.Debug.WriteLine("[MainViewModel] PopulateChat: Cleared _conversationHistory.");

            if(_systemPromptString != null)
            {
                _conversationHistory.Add(new OpenRouterMessage { Role = "system", Content = _systemPromptString });
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] PopulateChat: Added system prompt to _conversationHistory. Length: {_systemPromptString.Length}");
            }
            else
            {
                 _conversationHistory.Add(new OpenRouterMessage { Role = "system", Content = "You are a helpful assistant." });
                 AddSystemMessage("Error: System prompt not initialized for populating chat. Using a very basic default.");
                 System.Diagnostics.Debug.WriteLine("[MainViewModel] PopulateChat: ERROR - _systemPromptString was null, added basic default for _conversationHistory.");
            }

            foreach (var loadedMsg in messagesToLoad)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] PopulateChat: Adding to UI: [{loadedMsg.Timestamp}] {loadedMsg.Sender}: {loadedMsg.Text.Substring(0, Math.Min(loadedMsg.Text.Length, 50))}");
                AddMessage(loadedMsg.Text, loadedMsg.Sender, loadedMsg.Timestamp);

                if (loadedMsg.Sender != MessageSender.System)
                {
                    string role = (loadedMsg.Sender == MessageSender.Assistant) ? "assistant" : "user";
                    _conversationHistory.Add(new OpenRouterMessage { Role = role, Content = loadedMsg.Text });
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] PopulateChat: Added to API history: Role={role}, Content Snippet: {loadedMsg.Text.Substring(0, Math.Min(loadedMsg.Text.Length, 50))}");
                }
            }
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] PopulateChat: UI Messages count: {Messages.Count}, API History count: {_conversationHistory.Count}");
            TruncateConversationHistory();
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] PopulateChat: After truncation, API History count: {_conversationHistory.Count}");
        }

        public async Task LoadChatAsync(string chatName)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] LoadChatAsync: Attempting to load chat '{chatName}'.");
            if (string.IsNullOrWhiteSpace(chatName))
            {
                AddSystemMessage("Chat name cannot be empty to load.");
                System.Diagnostics.Debug.WriteLine("[MainViewModel] LoadChatAsync: Chat name is null or whitespace.");
                return;
            }

            IsSendingMessage = true;
            AddSystemMessage($"Loading chat: {chatName}...");
            try
            {
                List<ChatMessage>? loadedMessages = await _chatHistoryService.LoadChatAsync(chatName);
                if (loadedMessages != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] LoadChatAsync: Successfully loaded {loadedMessages.Count} messages for chat '{chatName}' from service.");
                    PopulateChat(loadedMessages, chatName);
                    AddSystemMessage($"Chat '{chatName}' loaded successfully.");
                }
                else
                {
                    AddSystemMessage($"Failed to load chat: {chatName}. Chat not found or empty. Starting a new chat session.");
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] LoadChatAsync: Failed to load chat '{chatName}' (service returned null or empty list). Starting new chat.");
                    StartNewChatSession(isUserAction: false);
                }
            }
            catch (Exception ex)
            {
                AddSystemMessage($"An error occurred while loading chat '{chatName}': {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] LoadChatAsync: Exception while loading chat '{chatName}'. Error: {ex.Message}");
                StartNewChatSession(isUserAction: false);
            }
            finally
            {
                IsSendingMessage = false;
            }
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

            // Add "Thinking..." message
            _thinkingMessage = new ChatMessage("Ghost is thinking...", MessageSender.System);
            AddChatMessageObject(_thinkingMessage); // Use helper to add the exact instance

            try
            {
                string modelToUse = _appSettings.SelectedOpenRouterModel ?? "gryphe/mythomax-l2-13b";
                string assistantResponseText = await _openRouterService.GetChatCompletionAsync(new List<OpenRouterMessage>(_conversationHistory), modelToUse);

                // Remove "Thinking..." message BEFORE adding the actual response or processing tools
                RemoveChatMessageObject(_thinkingMessage);
                _thinkingMessage = null;

                ToolCallRequest? potentialToolCall = _toolExecutorService.TryParseToolCall(assistantResponseText);
                string processingResult = await _toolExecutorService.ProcessAssistantResponseForToolsAsync(assistantResponseText);

                if (potentialToolCall != null)
                {
                    _conversationHistory.Add(new OpenRouterMessage { Role = "assistant", Content = assistantResponseText });
                    _conversationHistory.Add(new OpenRouterMessage { Role = "user", Content = $"Tool output: {processingResult}" });
                    // UI messages for tool execution are handled by ToolExecutorService via AddSystemMessage
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
                RemoveChatMessageObject(_thinkingMessage); // Ensure removal on error too
                _thinkingMessage = null;
                AddSystemMessage($"Error during message processing or API call: {ex.Message}");
            }
            finally
            {
                // Safeguard: Ensure thinking message is removed if it somehow persisted (e.g., due to an unhandled exception path before finally)
                RemoveChatMessageObject(_thinkingMessage);
                _thinkingMessage = null;
                IsSendingMessage = false; // This will trigger CanExecuteChanged for commands
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
