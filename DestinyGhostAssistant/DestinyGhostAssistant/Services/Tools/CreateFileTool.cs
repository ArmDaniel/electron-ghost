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
            System.Diagnostics.Debug.WriteLine($"CreateFileTool: Validating path: '{filePath}'");
            if (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                System.Diagnostics.Debug.WriteLine($"CreateFileTool: Path '{filePath}' contains invalid path characters.");
                return $"Error: The provided path '{filePath}' contains invalid path characters.";
            }
            try
            {
                System.Diagnostics.Debug.WriteLine($"CreateFileTool: Attempting to process file: '{filePath}'. Content length: {fileContent.Length}.");
                // Ensure filename is valid
                string? fileName = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                     return $"Error: The filename part of the path '{filePath}' is invalid or empty.";
                }

                string? directoryPath = Path.GetDirectoryName(filePath);
                System.Diagnostics.Debug.WriteLine($"CreateFileTool: Determined directory path: '{directoryPath ?? "current (null)"}'.");

                if (!string.IsNullOrEmpty(directoryPath))
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"CreateFileTool: Directory '{directoryPath}' does not exist. Attempting to create.");
                        Directory.CreateDirectory(directoryPath);
                        System.Diagnostics.Debug.WriteLine($"CreateFileTool: Directory '{directoryPath}' created.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"CreateFileTool: Directory '{directoryPath}' already exists.");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"CreateFileTool: Attempting to write content to file '{filePath}'.");
                await File.WriteAllTextAsync(filePath, fileContent);
                string successMsg = $"Successfully created file '{Path.GetFullPath(filePath)}'.";
                System.Diagnostics.Debug.WriteLine($"CreateFileTool: {successMsg}");
                return successMsg;
            }
            catch (UnauthorizedAccessException uae)
            {
                System.Diagnostics.Debug.WriteLine($"CreateFileTool: UnauthorizedAccessException for '{filePath}'. {uae.Message}");
                return $"Error: Access denied. You do not have permission to create the file at '{filePath}'. {uae.Message}";
            }
            catch (PathTooLongException ptle)
            {
                System.Diagnostics.Debug.WriteLine($"CreateFileTool: PathTooLongException for '{filePath}'. {ptle.Message}");
                return $"Error: The specified path '{filePath}' is too long. {ptle.Message}";
            }
            catch (DirectoryNotFoundException dnfe)
            {
                System.Diagnostics.Debug.WriteLine($"CreateFileTool: DirectoryNotFoundException for '{filePath}'. {dnfe.Message}");
                return $"Error: The directory path for '{filePath}' could not be found or accessed. Ensure the path is correct. {dnfe.Message}";
            }
            catch (IOException ioe)
            {
                System.Diagnostics.Debug.WriteLine($"CreateFileTool: IOException for '{filePath}'. {ioe.Message}");
                return $"Error: An IO exception occurred while creating the file '{filePath}'. Details: {ioe.Message}";
            }
            catch (ArgumentException ae)
            {
                System.Diagnostics.Debug.WriteLine($"CreateFileTool: ArgumentException for '{filePath}'. {ae.Message}");
                return $"Error: The path or filename is invalid. Details: {ae.Message}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateFileTool: Unexpected Exception for '{filePath}'. {ex.ToString()}");
                return $"Error: An unexpected error occurred while creating file '{filePath}'. Details: {ex.Message}";
            }
        }
    }
}
