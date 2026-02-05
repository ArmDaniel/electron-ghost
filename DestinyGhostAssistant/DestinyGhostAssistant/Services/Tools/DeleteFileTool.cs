using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DestinyGhostAssistant.Utils;

namespace DestinyGhostAssistant.Services.Tools
{
    public class DeleteFileTool : ITool
    {
        public string Name => "delete_file";

        public string Description =>
            "Deletes a file or an empty directory. " +
            "Parameters: 'path' (string, required - the path to delete).";

        public Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            string? path = ToolParameterHelper.GetString(parameters, "path");
            if (string.IsNullOrWhiteSpace(path))
                return Task.FromResult("Error: 'path' parameter is required.");

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.WriteLine($"DeleteFileTool: Deleted file '{path}'.");
                    return Task.FromResult($"Successfully deleted file '{Path.GetFullPath(path)}'.");
                }
                else if (Directory.Exists(path))
                {
                    // Only delete empty directories for safety; non-empty requires explicit recursive flag
                    var entries = Directory.GetFileSystemEntries(path);
                    if (entries.Length > 0)
                        return Task.FromResult($"Error: Directory '{path}' is not empty ({entries.Length} items). Refusing to delete non-empty directory for safety.");

                    Directory.Delete(path);
                    Debug.WriteLine($"DeleteFileTool: Deleted empty directory '{path}'.");
                    return Task.FromResult($"Successfully deleted empty directory '{Path.GetFullPath(path)}'.");
                }
                else
                {
                    return Task.FromResult($"Error: Path not found: '{path}'.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult($"Error: Access denied when trying to delete '{path}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteFileTool: Error: {ex.Message}");
                return Task.FromResult($"Error deleting: {ex.Message}");
            }
        }
    }
}
