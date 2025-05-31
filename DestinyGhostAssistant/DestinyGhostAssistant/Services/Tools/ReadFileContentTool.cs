using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DestinyGhostAssistant.Services.Tools
{
    public class ReadFileContentTool : ITool
    {
        public string Name => "read_file_content";

        public string Description => "Reads the content of a specified text file. Parameters: 'path' (string, full path to the file, e.g. C:\\Users\\User\\Desktop\\my_file.txt or relative path like my_folder/my_file.txt). Returns the file content or an error message.";

        private const int MaxFileSize = 1 * 1024 * 1024; // 1 MB

        public async Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            // Validate 'path' parameter
            if (!parameters.TryGetValue("path", out object? pathObj) || !(pathObj is string filePath) || string.IsNullOrWhiteSpace(filePath))
            {
                return "Error: 'path' parameter is required and must be a non-empty string.";
            }

            // Basic path validation
            if (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return $"Error: The provided path '{filePath}' contains invalid path characters.";
            }

            try
            {
                 // Ensure filename part is also valid if it's just a filename or part of the path
                string? fileName = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                     return $"Error: The filename part of the path '{filePath}' is invalid or empty.";
                }

                if (!File.Exists(filePath))
                {
                    return $"Error: File not found at '{filePath}'.";
                }

                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > MaxFileSize)
                {
                    return $"Error: File '{filePath}' is too large to read (max size: {MaxFileSize / (1024*1024)}MB). Size: {fileInfo.Length / (1024.0*1024.0):F2}MB.";
                }

                string content = await File.ReadAllTextAsync(filePath);

                if (string.IsNullOrEmpty(content))
                {
                    return $"File '{Path.GetFullPath(filePath)}' is empty.";
                }
                // For very large contents, consider returning a summary or truncated version.
                // For now, returning full content as per typical 'read_file' behavior.
                return $"Content of '{Path.GetFullPath(filePath)}':\n{content}";
            }
            catch (UnauthorizedAccessException)
            {
                return $"Error: Access denied. You do not have permission to read the file at '{filePath}'.";
            }
            catch (PathTooLongException)
            {
                return $"Error: The specified path '{filePath}' is too long.";
            }
            catch (FileNotFoundException) // Should be caught by File.Exists, but good as a safeguard
            {
                return $"Error: File not found at '{filePath}'. (Safeguard catch)";
            }
            catch (DirectoryNotFoundException)
            {
                 return $"Error: Part of the directory path for '{filePath}' could not be found. Ensure the path is correct.";
            }
            catch (IOException ex)
            {
                return $"Error: An IO exception occurred while reading the file '{filePath}'. Details: {ex.Message}";
            }
            catch (ArgumentException ex) // Can be thrown by Path methods for invalid chars
            {
                return $"Error: The path or filename is invalid. Details: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: An unexpected error occurred while reading file '{filePath}'. Details: {ex.Message}";
            }
        }
    }
}
