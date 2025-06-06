using System; // For String.IsNullOrWhiteSpace

namespace DestinyGhostAssistant.Models
{
    public class ProcessInfo
    {
        public int Id { get; }
        public string ProcessName { get; }
        public string MainWindowTitle { get; }
        public IntPtr MainWindowHandle { get; } // Added property
        public string DisplayName { get; }

        public ProcessInfo(int id, string processName, string mainWindowTitle, IntPtr mainWindowHandle)
        {
            Id = id;
            ProcessName = processName ?? string.Empty;
            MainWindowTitle = mainWindowTitle ?? string.Empty;
            MainWindowHandle = mainWindowHandle; // Initialize new property

            // Construct DisplayName
            string PName = string.IsNullOrWhiteSpace(ProcessName) ? "UnknownProcess" : ProcessName;

            if (!string.IsNullOrWhiteSpace(MainWindowTitle))
            {
                // Truncate MainWindowTitle if it's too long for display simplicity
                const int maxTitleLength = 50;
                string truncatedTitle = MainWindowTitle.Length > maxTitleLength
                                        ? MainWindowTitle.Substring(0, maxTitleLength) + "..."
                                        : MainWindowTitle;
                DisplayName = $"{PName} (PID: {Id}) - {truncatedTitle}";
            }
            else
            {
                DisplayName = $"{PName} (PID: {Id})";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
