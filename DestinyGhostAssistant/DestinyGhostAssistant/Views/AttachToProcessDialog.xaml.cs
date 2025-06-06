using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls; // Required for SelectionChangedEventArgs
using DestinyGhostAssistant.Models; // Required for ProcessInfo

namespace DestinyGhostAssistant.Views
{
    /// <summary>
    /// Interaction logic for AttachToProcessDialog.xaml
    /// </summary>
    public partial class AttachToProcessDialog : Window
    {
        public ProcessInfo? SelectedProcess { get; private set; }
        public ObservableCollection<ProcessInfo> Processes { get; set; }

        private readonly string _currentProcessName;

        public AttachToProcessDialog()
        {
            InitializeComponent();
            Processes = new ObservableCollection<ProcessInfo>();
            DataContext = this; // For ItemsSource="{Binding Processes}"

            // Get current process name to exclude it from the list
            using (Process currentProc = Process.GetCurrentProcess())
            {
                _currentProcessName = currentProc.ProcessName;
            }

            LoadProcesses();

            OkButton.IsEnabled = false; // Initially disable OK button
            // OkButton.Click += OkButton_Click;
            // CancelButton.Click += CancelButton_Click;
            // ProcessListBox.SelectionChanged event is wired in XAML, handler is below

            Loaded += (sender, e) => ProcessListBox.Focus();
        }

        private void LoadProcesses()
        {
            Processes.Clear();
            var tempProcessList = new List<ProcessInfo>();

            try
            {
                Process[] systemProcesses = Process.GetProcesses();
                foreach (Process proc in systemProcesses)
                {
                    try
                    {
                        // Filter out processes without a main window title early
                        if (string.IsNullOrWhiteSpace(proc.MainWindowTitle))
                        {
                            continue;
                        }

                        // Filter out common system/background processes and the current application itself
                        if (proc.Id == 0 || proc.ProcessName == "Idle" ||
                            proc.ProcessName == "System" || proc.ProcessName == "Registry" ||
                            proc.ProcessName == _currentProcessName)
                        {
                            continue;
                        }

                        // Additional common background processes to consider filtering (can be expanded)
                        string[] commonBackgroundProcesses = { "svchost", "lsass", "csrss", "wininit", "services", "conhost", "explorer" };
                        if (commonBackgroundProcesses.Contains(proc.ProcessName, StringComparer.OrdinalIgnoreCase))
                        {
                           // For 'explorer', only filter if it has no main window title, but we already did that.
                           // For now, let's keep explorer if it has a window title (e.g. a file explorer window)
                           if (proc.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(proc.MainWindowTitle)) continue;
                           else if (!proc.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase)) continue; // filter others
                        }


                        // MainWindowTitle can throw if process exited or access denied, though GetProcesses might only get accessible ones.
                        // The initial check for IsNullOrWhiteSpace(proc.MainWindowTitle) already handles most cases.
                        string mainWindowTitle = proc.MainWindowTitle; // Already checked, should be safe.
                        IntPtr mainWindowHandle = proc.MainWindowHandle; // Get the handle

                        tempProcessList.Add(new ProcessInfo(proc.Id, proc.ProcessName, mainWindowTitle, mainWindowHandle));
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Process might have exited or access denied for MainWindowTitle
                        Debug.WriteLine($"Error accessing process info for PID {proc.Id} ({proc.ProcessName}): {ex.Message}");
                    }
                    catch (Exception ex) // Catch other unexpected errors for a specific process
                    {
                         Debug.WriteLine($"Generic error for process PID {proc.Id} ({proc.ProcessName}): {ex.Message}");
                    }
                    finally
                    {
                        proc.Dispose(); // Dispose of process objects
                    }
                }
            }
            catch (Exception ex) // Catch error during GetProcesses() itself
            {
                Debug.WriteLine($"Error getting system processes: {ex.Message}");
                MessageBox.Show($"Error loading process list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Sort and populate the ObservableCollection
            var sortedProcesses = tempProcessList.OrderBy(p => p.ProcessName).ThenBy(p => p.DisplayName);
            foreach (var p in sortedProcesses)
            {
                Processes.Add(p);
            }

            if (Processes.Any())
            {
                ProcessListBox.SelectedIndex = 0; // Select first item by default
            }
        }

        private void ProcessListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OkButton.IsEnabled = ProcessListBox.SelectedItem != null;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessListBox.SelectedItem is ProcessInfo selected)
            {
                SelectedProcess = selected;
                DialogResult = true;
                Close();
            }
            // Else: OK button should be disabled if nothing is selected, so this path is unlikely.
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
