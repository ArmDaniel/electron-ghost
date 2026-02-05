using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DestinyGhostAssistant.Models;
using DestinyGhostAssistant.Services;

namespace DestinyGhostAssistant.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsService _settingsService;
        private AppSettings _currentSettings;
        private List<OpenRouterModelInfo> _allModels = new();
        private OpenRouterModelInfo? _selectedModel;
        private bool _suppressSelectionEvent;

        public SettingsWindow()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            _currentSettings = _settingsService.LoadSettings();

            SystemPromptTextBox.Text = _currentSettings.CustomSystemPrompt ?? string.Empty;
            SerpApiKeyBox.Password = _currentSettings.SerpApiKey ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(_currentSettings.SelectedOpenRouterModel))
            {
                SelectedModelText.Text = _currentSettings.SelectedOpenRouterModel;
            }

            SaveButton.Click += SaveButton_Click;
            CancelButton.Click += CancelButton_Click;

            Loaded += async (_, _) => await LoadModelsAsync();
        }

        private async System.Threading.Tasks.Task LoadModelsAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                _allModels = await OpenRouterService.FetchAvailableModelsAsync();

                if (_allModels.Count == 0)
                {
                    // Fallback: if the API returned nothing, show a message
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    MessageBox.Show(
                        "Could not retrieve models from OpenRouter. Check your internet connection.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ApplyFilter(string.Empty);

                // Pre-select the currently saved model
                if (!string.IsNullOrWhiteSpace(_currentSettings.SelectedOpenRouterModel))
                {
                    var match = _allModels.FirstOrDefault(m =>
                        m.Id.Equals(_currentSettings.SelectedOpenRouterModel, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        _selectedModel = match;
                        _suppressSelectionEvent = true;
                        ModelListBox.SelectedItem = match;
                        ModelListBox.ScrollIntoView(match);
                        _suppressSelectionEvent = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading models: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchTextBox.Text;
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(query)
                ? Visibility.Visible
                : Visibility.Collapsed;

            ApplyFilter(query);
        }

        private void ApplyFilter(string query)
        {
            if (_allModels.Count == 0)
            {
                ModelListBox.ItemsSource = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                ModelListBox.ItemsSource = _allModels;
                return;
            }

            // Fuzzy matching: split query into tokens, each token must match
            // somewhere in the model id or name (case-insensitive)
            var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var scored = _allModels
                .Select(m => new { Model = m, Score = FuzzyScore(m, tokens) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Model)
                .ToList();

            ModelListBox.ItemsSource = scored;
        }

        /// <summary>
        /// Returns a score > 0 if all tokens match somewhere in the model's id or name.
        /// Higher score = better match. Returns 0 if any token doesn't match.
        /// </summary>
        private static int FuzzyScore(OpenRouterModelInfo model, string[] tokens)
        {
            string haystack = $"{model.Id} {model.Name}".ToLowerInvariant();
            int score = 0;

            foreach (var token in tokens)
            {
                string lowerToken = token.ToLowerInvariant();
                int idx = haystack.IndexOf(lowerToken, StringComparison.Ordinal);
                if (idx < 0)
                    return 0; // token not found at all — no match

                // Bonus: exact start-of-word match
                if (idx == 0 || haystack[idx - 1] is '/' or '-' or ' ' or ':')
                    score += 10;

                score += lowerToken.Length; // longer token matches score higher
            }

            return score;
        }

        private void ModelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionEvent) return;

            if (ModelListBox.SelectedItem is OpenRouterModelInfo selected)
            {
                _selectedModel = selected;
                SelectedModelText.Text = selected.Id;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _currentSettings.SelectedOpenRouterModel = _selectedModel?.Id
                ?? _currentSettings.SelectedOpenRouterModel; // Keep old if nothing new selected

            string customPromptText = SystemPromptTextBox.Text;
            _currentSettings.CustomSystemPrompt = string.IsNullOrWhiteSpace(customPromptText)
                ? null
                : customPromptText.Trim();

            string serpKey = SerpApiKeyBox.Password;
            _currentSettings.SerpApiKey = string.IsNullOrWhiteSpace(serpKey) ? null : serpKey.Trim();

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
