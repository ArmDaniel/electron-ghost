using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DestinyGhostAssistant.Utils;

namespace DestinyGhostAssistant.Services.Tools
{
    public class CreateDirectoryTool : ITool
    {
        public string Name => "create_directory";

        public string Description =>
            "Creates a new directory (and any parent directories as needed). " +
            "Parameters: 'path' (string, required - the directory path to create).";

        public Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            string? dirPath = ToolParameterHelper.GetString(parameters, "path");
            if (string.IsNullOrWhiteSpace(dirPath))
                return Task.FromResult("Error: 'path' parameter is required.");

            try
            {
                if (Directory.Exists(dirPath))
                    return Task.FromResult($"Directory already exists: '{Path.GetFullPath(dirPath)}'.");

                Directory.CreateDirectory(dirPath);
                Debug.WriteLine($"CreateDirectoryTool: Created directory '{dirPath}'.");
                return Task.FromResult($"Successfully created directory '{Path.GetFullPath(dirPath)}'.");
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult($"Error: Access denied creating directory '{dirPath}'.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CreateDirectoryTool: Error: {ex.Message}");
                return Task.FromResult($"Error creating directory: {ex.Message}");
            }
        }
    }
}
