using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DestinyGhostAssistant.Utils;

namespace DestinyGhostAssistant.Services.Tools
{
    public class MoveFileTool : ITool
    {
        public string Name => "move_file";

        public string Description =>
            "Moves or renames a file or directory. " +
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
                bool isDir = Directory.Exists(source);
                bool isFile = File.Exists(source);

                if (!isFile && !isDir)
                    return Task.FromResult($"Error: Source not found: '{source}'.");

                // Ensure destination directory exists
                string? destDir = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                if (isDir)
                {
                    Directory.Move(source, destination);
                    Debug.WriteLine($"MoveFileTool: Moved directory '{source}' -> '{destination}'.");
                    return Task.FromResult($"Successfully moved directory '{Path.GetFullPath(source)}' to '{Path.GetFullPath(destination)}'.");
                }
                else
                {
                    File.Move(source, destination, overwrite: false);
                    Debug.WriteLine($"MoveFileTool: Moved file '{source}' -> '{destination}'.");
                    return Task.FromResult($"Successfully moved file '{Path.GetFullPath(source)}' to '{Path.GetFullPath(destination)}'.");
                }
            }
            catch (IOException ex) when (ex.Message.Contains("already exists"))
            {
                return Task.FromResult($"Error: Destination already exists: '{destination}'. Cannot overwrite.");
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult($"Error: Access denied when trying to move '{source}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MoveFileTool: Error: {ex.Message}");
                return Task.FromResult($"Error moving: {ex.Message}");
            }
        }
    }
}
