using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls; // Required for ScrollViewer
using System.Windows.Input; // Required for KeyEventArgs
using System.Windows.Media; // Required for VisualTreeHelper
using DestinyGhostAssistant.ViewModels; // Required for MainViewModel

namespace DestinyGhostAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Attempt to set up auto-scroll
            // The DataContext might not be available immediately,
            // so it's safer to subscribe once it's loaded.
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                if (viewModel.Messages is INotifyCollectionChanged notifyCollection)
                {
                    notifyCollection.CollectionChanged += Messages_CollectionChanged;
                    // Initial scroll to bottom in case there are already messages (e.g. welcome message)
                    ScrollChatToBottom();
                }
            }
        }

        private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ScrollChatToBottom();
        }

        private void ScrollChatToBottom()
        {
            // Ensure ChatScrollViewer is available and has content
            if (ChatScrollViewer != null && ChatScrollViewer.ScrollableHeight > 0)
            {
                 // If there are items in the ItemsControl hosted by the ScrollViewer
                if (VisualTreeHelper.GetChildrenCount(ChatScrollViewer) > 0)
                {
                    var itemsControl = VisualTreeHelper.GetChild(ChatScrollViewer, 0) as ItemsControl;
                    if (itemsControl != null && itemsControl.Items.Count > 0)
                    {
                        ChatScrollViewer.ScrollToBottom();
                    }
                } else if (ChatScrollViewer.Content is ItemsControl itemsControlContent && itemsControlContent.Items.Count > 0)
                {
                     ChatScrollViewer.ScrollToBottom();
                }

            } else if (ChatScrollViewer != null && ChatScrollViewer.Content is ItemsControl itemsControlDirect && itemsControlDirect.HasItems)
            {
                // This case handles when ScrollableHeight might be 0 but there is content (e.g. first item)
                ChatScrollViewer.ScrollToBottom();
            }
        }

        private void MessageInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    if (viewModel.SendMessageCommand.CanExecute(null))
                    {
                        viewModel.SendMessageCommand.Execute(null);
                    }
                }
            }
        }

        // It's good practice to unsubscribe from events when the window is closed
        // to prevent potential memory leaks, especially if the ViewModel outlives the View.
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (DataContext is MainViewModel viewModel)
            {
                if (viewModel.Messages is INotifyCollectionChanged notifyCollection)
                {
                    notifyCollection.CollectionChanged -= Messages_CollectionChanged;
                }
            }
            Loaded -= MainWindow_Loaded;
        }

        // Click handler for the Exit menu item
        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
