using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DestinyGhostAssistant.Services.Tools
{
    public class WriteFileTool : ITool
    {
        public string Name => "write_file";

        public string Description => "Writes or overwrites content to a file. Creates the file if it doesn't exist. Parameters: 'path' (string, full path including filename, e.g. C:\\Users\\User\\Desktop\\my_file.txt or relative path like my_folder/my_file.txt), 'content' (string, the content to write to the file). If the directory does not exist, it will be created.";

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
            else if (parameters.ContainsKey("content") && contentObj == null)
            {
                fileContent = string.Empty; // Treat null content as empty
            }
            else if (parameters.ContainsKey("content") && !(contentObj is string))
            {
                return "Error: 'content' parameter, if provided, must be a string.";
            }

            // Basic path validation
            System.Diagnostics.Debug.WriteLine($"WriteFileTool: Validating path: '{filePath}'");
            if (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: Path '{filePath}' contains invalid path characters.");
                return $"Error: The provided path '{filePath}' contains invalid path characters.";
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: Attempting to write to file: '{filePath}'. Content length: {fileContent.Length}.");

                // Ensure filename is valid
                string? fileName = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return $"Error: The filename part of the path '{filePath}' is invalid or empty.";
                }

                string? directoryPath = Path.GetDirectoryName(filePath);
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: Determined directory path: '{directoryPath ?? "current (null)"}'.");

                if (!string.IsNullOrEmpty(directoryPath))
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"WriteFileTool: Directory '{directoryPath}' does not exist. Attempting to create.");
                        Directory.CreateDirectory(directoryPath);
                        System.Diagnostics.Debug.WriteLine($"WriteFileTool: Directory '{directoryPath}' created.");
                    }
                }

                bool fileExisted = File.Exists(filePath);
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: File exists: {fileExisted}. Writing content to '{filePath}'.");

                await File.WriteAllTextAsync(filePath, fileContent);

                string successMsg = fileExisted
                    ? $"Successfully updated file '{Path.GetFullPath(filePath)}'."
                    : $"Successfully created file '{Path.GetFullPath(filePath)}'.";
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: {successMsg}");
                return successMsg;
            }
            catch (UnauthorizedAccessException uae)
            {
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: UnauthorizedAccessException for '{filePath}'. {uae.Message}");
                return $"Error: Access denied. You do not have permission to write to the file at '{filePath}'. {uae.Message}";
            }
            catch (PathTooLongException ptle)
            {
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: PathTooLongException for '{filePath}'. {ptle.Message}");
                return $"Error: The specified path '{filePath}' is too long. {ptle.Message}";
            }
            catch (DirectoryNotFoundException dnfe)
            {
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: DirectoryNotFoundException for '{filePath}'. {dnfe.Message}");
                return $"Error: The directory path for '{filePath}' could not be found or accessed. Ensure the path is correct. {dnfe.Message}";
            }
            catch (IOException ioe)
            {
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: IOException for '{filePath}'. {ioe.Message}");
                return $"Error: An IO exception occurred while writing to the file '{filePath}'. Details: {ioe.Message}";
            }
            catch (ArgumentException ae)
            {
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: ArgumentException for '{filePath}'. {ae.Message}");
                return $"Error: The path or filename is invalid. Details: {ae.Message}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WriteFileTool: Unexpected Exception for '{filePath}'. {ex.ToString()}");
                return $"Error: An unexpected error occurred while writing to file '{filePath}'. Details: {ex.Message}";
            }
        }
    }
}
