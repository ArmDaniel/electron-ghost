using System.Collections.ObjectModel;
using System.Windows.Input;
using DestinyGhostAssistant.Models;
using DestinyGhostAssistant.Utils; // For RelayCommand
using DestinyGhostAssistant.Services; // For OpenRouterService
using System.Threading.Tasks; // For Task
using System.Collections.Generic; // For List
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
                    // Cast to RelayCommand to access RaiseCanExecuteChanged.
                    // Ensure SendMessageCommand is initialized before CurrentMessage is set if it can trigger this.
                    // In this setup, SendMessageCommand is initialized after _currentMessage field but before any UI interaction.
                    if (SendMessageCommand is RelayCommand command) // defensive check
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
                    if (SendMessageCommand is RelayCommand command) // defensive check
                    {
                        command.RaiseCanExecuteChanged();
                    }
                }
            }
        }

        private readonly OpenRouterService _openRouterService;

        public MainViewModel()
        {
            Messages = new ObservableCollection<ChatMessage>();
            // IMPORTANT: This is a placeholder API key.
            // The application will require a valid OpenRouter API key to be configured for actual API calls.
            _openRouterService = new OpenRouterService("YOUR_API_KEY_PLACEHOLDER");

            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), () => !string.IsNullOrWhiteSpace(CurrentMessage) && !IsSendingMessage);

            AddMessage("Welcome to the Ghost Assistant! How can I help you?", MessageSender.Assistant);
        }

        private void AddMessage(string text, MessageSender sender)
        {
            var chatMessage = new ChatMessage(text, sender);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(chatMessage);
            });
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentMessage) || IsSendingMessage)
            {
                return;
            }

            IsSendingMessage = true;
            // No need to call RaiseCanExecuteChanged here for IsSendingMessage because its setter already does.
            // ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged(); // Already handled by IsSendingMessage setter

            string userMessageText = CurrentMessage; // Store before clearing
            AddMessage(userMessageText, MessageSender.User);
            CurrentMessage = string.Empty; // Clear input field immediately after capturing text

            try
            {
                // Create a list of messages for the API call (currently just the user's message)
                var apiMessages = new List<OpenRouterMessage>
                {
                    new OpenRouterMessage { Role = "user", Content = userMessageText }
                };

                string assistantResponseText = await _openRouterService.GetChatCompletionAsync(apiMessages);
                AddMessage(assistantResponseText, MessageSender.Assistant);
            }
            catch (System.Exception ex) // Catch potential exceptions from the service call
            {
                // Log the exception (e.g., using a logging framework)
                // System.Diagnostics.Debug.WriteLine($"Error calling OpenRouterService: {ex.Message}");
                AddMessage($"Error: Could not get response from Ghost. {ex.Message}", MessageSender.System);
            }
            finally
            {
                IsSendingMessage = false;
                // ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged(); // Already handled by IsSendingMessage setter
            }
        }
    }
}
