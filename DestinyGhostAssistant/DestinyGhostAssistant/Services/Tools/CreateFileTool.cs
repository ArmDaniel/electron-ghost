using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DestinyGhostAssistant.Services.Tools
{
    public class CreateFileTool : ITool
    {
        public string Name => "create_file";

        public string Description => "Creates a new text file with specified content. Parameters: 'path' (string, full path including filename, e.g. C:\\Users\\User\\Desktop\\my_file.txt or relative path like my_folder/my_file.txt), 'content' (string). If the directory does not exist, it will be created. If 'content' is not provided, an empty file will be created.";

        public async Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            // Validate 'path' parameter
            if (!parameters.TryGetValue("path", out object? pathObj) || !(pathObj is string filePath) || string.IsNullOrWhiteSpace(filePath))
            {
                return "Error: 'path' parameter is required and must be a non-empty string.";
            }

            // Validate 'content' parameter (optional, defaults to empty string)
            string fileContent = string.Empty;
            if (parameters.TryGetValue("content", out object? contentObj) && contentObj is string tempContent)
            {
                fileContent = tempContent;
            }
            else if (parameters.ContainsKey("content") && contentObj == null) // Key exists but value is null
            {
                fileContent = string.Empty; // Treat null content as empty
            }
            else if (parameters.ContainsKey("content") && !(contentObj is string)) // Key exists but not a string
            {
                 return "Error: 'content' parameter, if provided, must be a string.";
            }


            // Basic path validation (more robust validation might be needed for production)
            if (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return $"Error: The provided path '{filePath}' contains invalid path characters.";
            }
            try
            {
                // Ensure filename is valid
                string? fileName = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                     return $"Error: The filename part of the path '{filePath}' is invalid or empty.";
                }

                string? directoryPath = Path.GetDirectoryName(filePath);

                // If directoryPath is null, it means filePath is just a filename (relative to current dir).
                // If directoryPath is empty string, it means filePath is a root path like "C:file.txt" (unlikely for user input but possible).
                // We allow file creation in current directory if directoryPath is null or empty after GetDirectoryName.
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }

                await File.WriteAllTextAsync(filePath, fileContent);
                return $"Successfully created file '{Path.GetFullPath(filePath)}'.";
            }
            catch (UnauthorizedAccessException)
            {
                return $"Error: Access denied. You do not have permission to create the file at '{filePath}'.";
            }
            catch (PathTooLongException)
            {
                return $"Error: The specified path '{filePath}' is too long.";
            }
            catch (DirectoryNotFoundException)
            {
                // This might happen if GetDirectoryName returns a valid-looking path but it's on an unmapped drive, etc.
                // Or if CreateDirectory fails silently and WriteAllTextAsync then finds it missing.
                return $"Error: The directory path for '{filePath}' could not be found or accessed. Ensure the path is correct.";
            }
            catch (IOException ex)
            {
                return $"Error: An IO exception occurred while creating the file '{filePath}'. Details: {ex.Message}";
            }
            catch (ArgumentException ex) // Can be thrown by Path methods for invalid chars
            {
                return $"Error: The path or filename is invalid. Details: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: An unexpected error occurred while creating file '{filePath}'. Details: {ex.Message}";
            }
        }
    }
}
