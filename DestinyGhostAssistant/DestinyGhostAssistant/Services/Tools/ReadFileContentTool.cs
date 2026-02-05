using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DestinyGhostAssistant.Utils;

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
            string? filePath = ToolParameterHelper.GetString(parameters, "path");
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return "Error: 'path' parameter is required and must be a non-empty string.";
            }

            // Basic path validation
            System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: Validating path: '{filePath}'");
            if (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: Path '{filePath}' contains invalid path characters.");
                return $"Error: The provided path '{filePath}' contains invalid path characters.";
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: Attempting to process file: '{filePath}'.");
                 // Ensure filename part is also valid if it's just a filename or part of the path
                string? fileName = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                     return $"Error: The filename part of the path '{filePath}' is invalid or empty.";
                }

                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: Checking file existence for '{filePath}'.");
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: File not found at '{filePath}'.");
                    return $"Error: File not found at '{filePath}'.";
                }
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: File exists at '{filePath}'.");

                FileInfo fileInfo = new FileInfo(filePath);
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: File size for '{filePath}' is {fileInfo.Length} bytes.");
                if (fileInfo.Length > MaxFileSize)
                {
                    System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: File '{filePath}' is too large ({fileInfo.Length} bytes). Max size: {MaxFileSize} bytes.");
                    return $"Error: File '{filePath}' is too large to read (max size: {MaxFileSize / (1024*1024)}MB). Size: {fileInfo.Length / (1024.0*1024.0):F2}MB.";
                }

                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: Attempting to read content from file '{filePath}'.");
                string content = await File.ReadAllTextAsync(filePath);
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: Successfully read {content.Length} characters from '{filePath}'.");

                if (string.IsNullOrEmpty(content))
                {
                    return $"File '{Path.GetFullPath(filePath)}' is empty.";
                }
                return $"Content of '{Path.GetFullPath(filePath)}':\n{content}";
            }
            catch (UnauthorizedAccessException uae)
            {
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: UnauthorizedAccessException for '{filePath}'. {uae.Message}");
                return $"Error: Access denied. You do not have permission to read the file at '{filePath}'. {uae.Message}";
            }
            catch (PathTooLongException ptle)
            {
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: PathTooLongException for '{filePath}'. {ptle.Message}");
                return $"Error: The specified path '{filePath}' is too long. {ptle.Message}";
            }
            catch (FileNotFoundException fnfe)
            {
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: FileNotFoundException for '{filePath}'. {fnfe.Message}");
                return $"Error: File not found at '{filePath}'. (Safeguard catch) {fnfe.Message}";
            }
            catch (DirectoryNotFoundException dnfe)
            {
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: DirectoryNotFoundException for '{filePath}'. {dnfe.Message}");
                 return $"Error: Part of the directory path for '{filePath}' could not be found. Ensure the path is correct. {dnfe.Message}";
            }
            catch (IOException ioe)
            {
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: IOException for '{filePath}'. {ioe.Message}");
                return $"Error: An IO exception occurred while reading the file '{filePath}'. Details: {ioe.Message}";
            }
            catch (ArgumentException ae)
            {
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: ArgumentException for '{filePath}'. {ae.Message}");
                return $"Error: The path or filename is invalid. Details: {ae.Message}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReadFileContentTool: Unexpected Exception for '{filePath}'. {ex.ToString()}");
                return $"Error: An unexpected error occurred while reading file '{filePath}'. Details: {ex.Message}";
            }
        }
    }
}
