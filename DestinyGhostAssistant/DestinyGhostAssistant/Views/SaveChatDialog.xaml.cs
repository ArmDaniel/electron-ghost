using System.IO;
using System.Linq;
using System.Windows;

namespace DestinyGhostAssistant.Views
{
    /// <summary>
    /// Interaction logic for SaveChatDialog.xaml
    /// </summary>
    public partial class SaveChatDialog : Window
    {
        public string ChatName { get; private set; } = string.Empty;

        public SaveChatDialog()
        {
            InitializeComponent();
            Loaded += (sender, e) => ChatNameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string potentialChatName = ChatNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(potentialChatName))
            {
                MessageBox.Show(this, "Chat name cannot be empty.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                ChatNameTextBox.Focus();
                return;
            }

            // Check for invalid filename characters
            if (potentialChatName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                // You could join the invalid chars for a more specific message, but this is generally enough.
                string invalidChars = string.Join(", ", Path.GetInvalidFileNameChars().Where(c => potentialChatName.Contains(c)).Select(c => $"'{c}'"));
                MessageBox.Show(this, $"Chat name contains invalid characters: {invalidChars}. Please remove them.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                ChatNameTextBox.Focus();
                return;
            }

            // Additional checks, e.g., length, reserved names (CON, PRN, etc.) could be added if necessary.

            ChatName = potentialChatName;
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
