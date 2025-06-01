using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls; // Required for ListBox and SelectionChangedEventArgs

namespace DestinyGhostAssistant.Views
{
    /// <summary>
    /// Interaction logic for LoadChatDialog.xaml
    /// </summary>
    public partial class LoadChatDialog : Window
    {
        public string? SelectedChatName { get; private set; }

        public LoadChatDialog(IEnumerable<string> availableChatNames)
        {
            InitializeComponent();
            ChatsListBox.ItemsSource = availableChatNames;
            // Set focus to the ListBox when the dialog loads
            Loaded += (sender, e) =>
            {
                if (ChatsListBox.Items.Count > 0)
                {
                    ChatsListBox.SelectedIndex = 0; // Select first item by default if list is not empty
                }
                ChatsListBox.Focus();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChatsListBox.SelectedItem != null)
            {
                SelectedChatName = ChatsListBox.SelectedItem as string;
                DialogResult = true;
                Close();
            }
            else
            {
                // This case should ideally not be hit if OK button is properly disabled
                MessageBox.Show(this, "Please select a chat session.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ChatsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OkButton.IsEnabled = ChatsListBox.SelectedItem != null;
        }
    }
}
