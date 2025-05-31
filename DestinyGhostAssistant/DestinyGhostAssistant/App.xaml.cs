using System.Configuration;
using System.Data;
using System.Windows;
using DestinyGhostAssistant.Services; // For ApiKeyService
using DestinyGhostAssistant.Views;   // For ApiKeyInputDialog

namespace DestinyGhostAssistant
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string OpenRouterApiKey { get; private set; } = string.Empty;

        protected override void OnStartup(StartupEventArgs e) // Made void, not async void for now. Dialog interaction is modal.
        {
            base.OnStartup(e);

            if (!ApiKeyService.HasApiKey())
            {
                PromptAndSaveApiKey();
            }
            else
            {
                OpenRouterApiKey = ApiKeyService.LoadApiKey() ?? string.Empty; // Ensure not null
                if (string.IsNullOrWhiteSpace(OpenRouterApiKey)) // Check if loaded key is actually empty/whitespace
                {
                    MessageBox.Show(
                        "The cached API Key is empty or invalid. Please re-enter your API key.",
                        "API Key Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    ApiKeyService.DeleteApiKey(); // Delete the invalid key
                    PromptAndSaveApiKey(); // Prompt again
                }
            }

            // Final check before starting main window
            if (string.IsNullOrWhiteSpace(OpenRouterApiKey))
            {
                // If still no valid key (e.g., user cancelled again), shutdown.
                // MessageBox was already shown in PromptAndSaveApiKey if user cancelled.
                // Or if PromptAndSaveApiKey failed to set it for some reason.
                if (Current.MainWindow == null || !Current.MainWindow.IsVisible) // Avoid showing if already handled by dialog
                {
                     MessageBox.Show("A valid OpenRouter API Key is required to continue. Application will now exit.", "API Key Required - Critical", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Current.Shutdown(-1); // Indicate an error
                return;
            }

            // If we have a key, proceed to show the main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void PromptAndSaveApiKey()
        {
            var inputDialog = new ApiKeyInputDialog();
            // Setting owner for a modal dialog shown at startup before MainWindow is tricky.
            // If MainWindow is not yet created/shown, Owner should be null or it might not behave as expected.
            // inputDialog.Owner = Application.Current.MainWindow; // This might be null or not visible yet

            if (inputDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(inputDialog.ApiKey))
            {
                ApiKeyService.SaveApiKey(inputDialog.ApiKey);
                OpenRouterApiKey = inputDialog.ApiKey;
            }
            else
            {
                MessageBox.Show(
                    "OpenRouter API Key is required for the assistant to function. The application will now exit.",
                    "API Key Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Current.Shutdown(-1); // Indicate an error
            }
        }
    }
}
