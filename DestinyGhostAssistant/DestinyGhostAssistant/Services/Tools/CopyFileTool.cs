using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DestinyGhostAssistant.Utils;

namespace DestinyGhostAssistant.Services.Tools
{
    public class CopyFileTool : ITool
    {
        public string Name => "copy_file";

        public string Description =>
            "Copies a file to a new location. " +
            "Parameters: 'source' (string, required), 'destination' (string, required).";

        public Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            string? source = ToolParameterHelper.GetString(parameters, "source");
            string? destination = ToolParameterHelper.GetString(parameters, "destination");

            if (string.IsNullOrWhiteSpace(source))
                return Task.FromResult("Error: 'source' parameter is required.");
            if (string.IsNullOrWhiteSpace(destination))
                return Task.FromResult("Error: 'destination' parameter is required.");

            try
            {
                if (!File.Exists(source))
                    return Task.FromResult($"Error: Source file not found: '{source}'.");

                // Ensure destination directory exists
                string? destDir = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(source, destination, overwrite: false);
                Debug.WriteLine($"CopyFileTool: Copied '{source}' -> '{destination}'.");
                return Task.FromResult($"Successfully copied '{Path.GetFullPath(source)}' to '{Path.GetFullPath(destination)}'.");
            }
            catch (IOException ex) when (ex.Message.Contains("already exists"))
            {
                return Task.FromResult($"Error: Destination file already exists: '{destination}'.");
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult($"Error: Access denied when trying to copy '{source}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CopyFileTool: Error: {ex.Message}");
                return Task.FromResult($"Error copying file: {ex.Message}");
            }
        }
    }
}
