using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows; // For MessageBox and Application.Current.Dispatcher
using DestinyGhostAssistant.Models;
using DestinyGhostAssistant.Services.Tools; // For ITool
using DestinyGhostAssistant.ViewModels; // For MainViewModel (or an abstraction later)

namespace DestinyGhostAssistant.Services
{
    public class ToolExecutorService
    {
        private readonly Dictionary<string, ITool> _tools = new Dictionary<string, ITool>();
        private readonly MainViewModel _mainViewModel; // Consider an IUIService interface for loose coupling

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            // Add other options if needed, e.g., converters
        };

        public ToolExecutorService(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            RegisterTools();
        }

        private void RegisterTools()
        {
            // Tools will be registered here in later steps.
            var createFileTool = new CreateFileTool();
            _tools.Add(createFileTool.Name, createFileTool);

            var readFileContentTool = new ReadFileContentTool();
            _tools.Add(readFileContentTool.Name, readFileContentTool);
        }

        public List<ITool> GetAvailableTools()
        {
            return _tools.Values.ToList();
        }

        public ToolCallRequest? TryParseToolCall(string aiResponseContent)
        {
            if (string.IsNullOrWhiteSpace(aiResponseContent))
                return null;

            try
            {
                // Attempt to deserialize the entire content or a specific part if tools are embedded
                // For now, assume the entire content is a JSON object for a tool call
                var toolCallRequest = JsonSerializer.Deserialize<ToolCallRequest>(aiResponseContent, _jsonSerializerOptions);

                if (toolCallRequest != null && !string.IsNullOrWhiteSpace(toolCallRequest.ToolName))
                {
                    return toolCallRequest;
                }
                return null;
            }
            catch (JsonException ex)
            {
                // Not a valid JSON for a tool call, or structure mismatch. This is expected for normal chat messages.
                System.Diagnostics.Debug.WriteLine($"JSON parsing for tool call failed (this is okay for non-tool messages): {ex.Message}");
                return null;
            }
            catch (Exception ex) // Catch other potential errors during parsing
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error trying to parse tool call: {ex.Message}");
                return null;
            }
        }

        private bool NeedsConfirmation(string toolName)
        {
            // Define which tools need confirmation.
            if (toolName == "create_file")
            {
                return true;
            }
            return false;
        }

        private async Task<bool> ShowConfirmationDialogAsync(string toolName, Dictionary<string, object> parameters)
        {
            var parametersText = string.Join("\n", parameters.Select(p => $"- {p.Key}: {JsonSerializer.Serialize(p.Value)}"));
            var message = $"Ghost Assistant wants to use tool: '{toolName}'.\n\nParameters:\n{parametersText}\n\nDo you approve?";

            MessageBoxResult result = MessageBoxResult.None;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                result = MessageBox.Show(
                    Application.Current.MainWindow, // Owner window
                    message,
                    "Tool Execution Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            return result == MessageBoxResult.Yes;
        }

        public async Task<string> ProcessAssistantResponseForToolsAsync(string assistantResponseContent)
        {
            ToolCallRequest? toolCallRequest = TryParseToolCall(assistantResponseContent);

            if (toolCallRequest != null)
            {
                _mainViewModel.AddSystemMessage($"Ghost is attempting to use tool: '{toolCallRequest.ToolName}'.");

                if (_tools.TryGetValue(toolCallRequest.ToolName, out ITool? tool))
                {
                    bool confirmed = true; // Default to true if no confirmation needed
                    if (NeedsConfirmation(tool.Name))
                    {
                        _mainViewModel.AddSystemMessage($"Awaiting user confirmation for tool: {tool.Name}...");
                        confirmed = await ShowConfirmationDialogAsync(tool.Name, toolCallRequest.Parameters);
                    }

                    if (confirmed)
                    {
                        if (NeedsConfirmation(tool.Name)) // Only show approval message if confirmation was sought
                        {
                            _mainViewModel.AddSystemMessage($"User approved tool: {tool.Name}. Executing...");
                        }
                        else
                        {
                             _mainViewModel.AddSystemMessage($"Executing tool: {tool.Name}...");
                        }

                        string executionResult = await tool.ExecuteAsync(toolCallRequest.Parameters); // Actual execution
                        // string executionResult = $"Placeholder: Tool '{tool.Name}' executed successfully with parameters: {JsonSerializer.Serialize(toolCallRequest.Parameters)}."; // Placeholder removed

                        _mainViewModel.AddSystemMessage($"Tool '{tool.Name}' result: {executionResult}");
                        return executionResult; // This might be fed back to the AI in a subsequent call
                    }
                    else
                    {
                        _mainViewModel.AddSystemMessage($"Tool '{tool.Name}' execution denied by user.");
                        return $"User denied execution for tool {tool.Name}.";
                    }
                }
                else
                {
                    _mainViewModel.AddSystemMessage($"Unknown tool requested: '{toolCallRequest.ToolName}'.");
                    return $"Assistant requested an unknown tool: '{toolCallRequest.ToolName}'.";
                }
            }
            else
            {
                // Not a tool call, return the original assistant message
                return assistantResponseContent;
            }
        }
    }
}
