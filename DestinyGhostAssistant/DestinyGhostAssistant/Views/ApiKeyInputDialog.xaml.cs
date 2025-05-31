using System.Windows;

namespace DestinyGhostAssistant.Views
{
    /// <summary>
    /// Interaction logic for ApiKeyInputDialog.xaml
    /// </summary>
    public partial class ApiKeyInputDialog : Window
    {
        public string ApiKey { get; private set; } = string.Empty;

        public ApiKeyInputDialog()
        {
            InitializeComponent();
            // Set focus to the TextBox when the dialog loads
            Loaded += (sender, e) => ApiKeyTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ApiKeyTextBox.Text))
            {
                MessageBox.Show(this, "API Key cannot be empty.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                ApiKeyTextBox.Focus();
                return;
            }
            ApiKey = ApiKeyTextBox.Text.Trim();
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
