using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DestinyGhostAssistant.Utils;

namespace DestinyGhostAssistant.Services.Tools
{
    public class ListDirectoryTool : ITool
    {
        public string Name => "list_directory";

        public string Description =>
            "Lists the contents of a directory (files and subdirectories). " +
            "Parameters: 'path' (string, required - the directory path).";

        public Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            string? dirPath = ToolParameterHelper.GetString(parameters, "path");
            if (string.IsNullOrWhiteSpace(dirPath))
                return Task.FromResult("Error: 'path' parameter is required and must be a non-empty string.");

            try
            {
                if (!Directory.Exists(dirPath))
                    return Task.FromResult($"Error: Directory not found: '{dirPath}'.");

                var sb = new StringBuilder();
                sb.AppendLine($"Contents of '{Path.GetFullPath(dirPath)}':");
                sb.AppendLine();

                var dirs = Directory.GetDirectories(dirPath);
                var files = Directory.GetFiles(dirPath);

                if (dirs.Length == 0 && files.Length == 0)
                {
                    sb.AppendLine("  (empty directory)");
                    return Task.FromResult(sb.ToString());
                }

                // Directories first
                foreach (var dir in dirs.OrderBy(d => d))
                {
                    string name = Path.GetFileName(dir);
                    sb.AppendLine($"  📁 {name}/");
                }

                // Then files with size
                foreach (var file in files.OrderBy(f => f))
                {
                    string name = Path.GetFileName(file);
                    var info = new FileInfo(file);
                    string size = FormatSize(info.Length);
                    sb.AppendLine($"  📄 {name}  ({size})");
                }

                sb.AppendLine();
                sb.AppendLine($"Total: {dirs.Length} folder(s), {files.Length} file(s)");

                Debug.WriteLine($"ListDirectoryTool: Listed {dirs.Length} dirs, {files.Length} files in '{dirPath}'.");
                return Task.FromResult(sb.ToString());
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult($"Error: Access denied to directory '{dirPath}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ListDirectoryTool: Error: {ex.Message}");
                return Task.FromResult($"Error listing directory: {ex.Message}");
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}
