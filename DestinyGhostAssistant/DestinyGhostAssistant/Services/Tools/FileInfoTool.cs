using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DestinyGhostAssistant.Utils;

namespace DestinyGhostAssistant.Services.Tools
{
    public class FileInfoTool : ITool
    {
        public string Name => "get_file_info";

        public string Description =>
            "Gets detailed information about a file or directory (size, dates, attributes). " +
            "Parameters: 'path' (string, required).";

        public Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            string? path = ToolParameterHelper.GetString(parameters, "path");
            if (string.IsNullOrWhiteSpace(path))
                return Task.FromResult("Error: 'path' parameter is required.");

            try
            {
                if (File.Exists(path))
                {
                    var info = new FileInfo(path);
                    var sb = new StringBuilder();
                    sb.AppendLine($"File: {info.FullName}");
                    sb.AppendLine($"  Size: {FormatSize(info.Length)} ({info.Length:N0} bytes)");
                    sb.AppendLine($"  Extension: {info.Extension}");
                    sb.AppendLine($"  Created: {info.CreationTime:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"  Modified: {info.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"  Accessed: {info.LastAccessTime:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"  Read-only: {info.IsReadOnly}");
                    sb.AppendLine($"  Attributes: {info.Attributes}");
                    return Task.FromResult(sb.ToString());
                }
                else if (Directory.Exists(path))
                {
                    var info = new DirectoryInfo(path);
                    int fileCount = 0;
                    int dirCount = 0;
                    long totalSize = 0;

                    try
                    {
                        var files = info.GetFiles("*", SearchOption.TopDirectoryOnly);
                        var dirs = info.GetDirectories("*", SearchOption.TopDirectoryOnly);
                        fileCount = files.Length;
                        dirCount = dirs.Length;
                        foreach (var f in files) totalSize += f.Length;
                    }
                    catch { /* access denied on enumeration is okay */ }

                    var sb = new StringBuilder();
                    sb.AppendLine($"Directory: {info.FullName}");
                    sb.AppendLine($"  Created: {info.CreationTime:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"  Modified: {info.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"  Attributes: {info.Attributes}");
                    sb.AppendLine($"  Contents: {dirCount} folder(s), {fileCount} file(s)");
                    sb.AppendLine($"  Top-level size: {FormatSize(totalSize)}");
                    return Task.FromResult(sb.ToString());
                }
                else
                {
                    return Task.FromResult($"Error: Path not found: '{path}'.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult($"Error: Access denied for '{path}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FileInfoTool: Error: {ex.Message}");
                return Task.FromResult($"Error getting info: {ex.Message}");
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
