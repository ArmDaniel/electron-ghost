using System.Collections.Generic;
using System.Windows;
using DestinyGhostAssistant.Models;
using DestinyGhostAssistant.Services;
using System.Linq; // Required for Any()

namespace DestinyGhostAssistant.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private AppSettings _currentSettings;

        // Predefined list of common OpenRouter models
        private readonly List<string> _predefinedModels = new List<string>
        {
            "nousresearch/deephermes-3-mistral-24b-preview:free",
            "qwen/qwen3-235b-a22b:free",
            "meta-llama/llama-4-maverick:free",
            "mistralai/mistral-small-3.1-24b-instruct:free",
            "deepseek/deepseek-r1-0528:free"
        };

        public SettingsWindow()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            LoadSettingsData();

            SaveButton.Click += SaveButton_Click;
            CancelButton.Click += CancelButton_Click;
        }

        private void LoadSettingsData()
        {
            _currentSettings = _settingsService.LoadSettings();

            // Populate Model ComboBox
            var modelsToShow = new List<string>(_predefinedModels);
            if (!string.IsNullOrWhiteSpace(_currentSettings.SelectedOpenRouterModel) &&
                !modelsToShow.Contains(_currentSettings.SelectedOpenRouterModel))
            {
                // Add the currently saved model to the list if it's not already there
                // This ensures it's visible and selectable even if it's not in the predefined list.
                modelsToShow.Insert(0, _currentSettings.SelectedOpenRouterModel);
            }

            ModelComboBox.ItemsSource = modelsToShow;

            if (!string.IsNullOrWhiteSpace(_currentSettings.SelectedOpenRouterModel))
            {
                ModelComboBox.SelectedItem = _currentSettings.SelectedOpenRouterModel;
            }
            else if (modelsToShow.Any()) // Default to first in list if no setting and list is not empty
            {
                 ModelComboBox.SelectedIndex = 0;
            }


            // Populate System Prompt TextBox
            SystemPromptTextBox.Text = _currentSettings.CustomSystemPrompt ?? string.Empty;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _currentSettings.SelectedOpenRouterModel = ModelComboBox.SelectedItem as string;

            string customPromptText = SystemPromptTextBox.Text;
            _currentSettings.CustomSystemPrompt = string.IsNullOrWhiteSpace(customPromptText) ? null : customPromptText.Trim();

            _settingsService.SaveSettings(_currentSettings);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
