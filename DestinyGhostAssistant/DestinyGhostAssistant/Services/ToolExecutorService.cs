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

            var openUrlTool = new OpenUrlTool();
            _tools.Add(openUrlTool.Name, openUrlTool);

            var searchWebTool = new SearchWebTool();
            _tools.Add(searchWebTool.Name, searchWebTool);

            var getBrowserContextTool = new GetBrowserContextTool();
            _tools.Add(getBrowserContextTool.Name, getBrowserContextTool);
        }

        public List<ITool> GetAvailableTools()
        {
            return _tools.Values.ToList();
        }

        public ToolCallRequest? TryParseToolCall(string aiResponseContent)
        {
            if (string.IsNullOrWhiteSpace(aiResponseContent))
                return null;

            string jsonToParse = aiResponseContent.Trim();

            // Check if it's wrapped in markdown ```json ... ```
            if (jsonToParse.StartsWith("```json", StringComparison.OrdinalIgnoreCase) && jsonToParse.EndsWith("```"))
            {
                jsonToParse = jsonToParse.Substring("```json".Length, jsonToParse.Length - "```json".Length - "```".Length).Trim();
            }
            // Or just ``` ... ```
            else if (jsonToParse.StartsWith("```") && jsonToParse.EndsWith("```"))
            {
                jsonToParse = jsonToParse.Substring("```".Length, jsonToParse.Length - "```".Length - "```".Length).Trim();
            }
            
            // If after stripping markdown, the string is empty, it's not valid JSON.
            if (string.IsNullOrWhiteSpace(jsonToParse))
            {
                System.Diagnostics.Debug.WriteLine($"TryParseToolCall: Content was all markdown or empty after trim. Original: '{aiResponseContent.Substring(0, Math.Min(aiResponseContent.Length,100))}'");
                return null;
            }

            try
            {
                var toolCallRequest = JsonSerializer.Deserialize<ToolCallRequest>(jsonToParse, _jsonSerializerOptions);

                if (toolCallRequest != null && !string.IsNullOrWhiteSpace(toolCallRequest.ToolName) && toolCallRequest.Parameters != null)
                {
                    return toolCallRequest;
                }
                System.Diagnostics.Debug.WriteLine($"TryParseToolCall: Deserialized but ToolName is null/whitespace or Parameters is null. ToolName: '{toolCallRequest?.ToolName}', Parameters Null: {toolCallRequest?.Parameters == null}. JSON attempted: {jsonToParse.Substring(0, Math.Min(jsonToParse.Length, 100))}");
                return null;
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON parsing for tool call failed. Attempted to parse: '{jsonToParse.Substring(0, Math.Min(jsonToParse.Length,100))}'. Error: {ex.Message}. Original content: '{aiResponseContent.Substring(0, Math.Min(aiResponseContent.Length,100))}'");
                return null;
            }
            catch (Exception ex) // Catch other potential errors during parsing
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error trying to parse tool call from: '{jsonToParse.Substring(0, Math.Min(jsonToParse.Length,100))}'. Error: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[ToolExecutorService] Parsed tool call: Name='{toolCallRequest.ToolName}', Parameters='{JsonSerializer.Serialize(toolCallRequest.Parameters, _jsonSerializerOptions)}'");
                _mainViewModel.AddSystemMessage($"Ghost is attempting to use tool: '{toolCallRequest.ToolName}'.");

                if (_tools.TryGetValue(toolCallRequest.ToolName, out ITool? tool))
                {
                    System.Diagnostics.Debug.WriteLine($"[ToolExecutorService] Found tool '{tool.Name}' in registered tools.");

                    // Special handling for get_browser_context to inject AttachedProcess
                    if (tool.Name == "get_browser_context")
                    {
                        if (_mainViewModel.AttachedProcess != null)
                        {
                            toolCallRequest.Parameters["_attachedProcess"] = _mainViewModel.AttachedProcess;
                            System.Diagnostics.Debug.WriteLine($"[ToolExecutorService] Injected AttachedProcess (PID: {_mainViewModel.AttachedProcess.Id}) into parameters for {tool.Name}.");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ToolExecutorService] {tool.Name} called but no process attached in MainViewModel.");
                            // The tool itself will return an error if _attachedProcess is missing,
                            // or we could short-circuit here. Let tool handle it for now.
                            // return $"Error: {tool.Name} requires an attached process, but none is attached.";
                        }
                    }

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

                        string executionResult;
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"[ToolExecutorService] Executing tool '{tool.Name}' with parameters: {JsonSerializer.Serialize(toolCallRequest.Parameters, _jsonSerializerOptions)}");
                            executionResult = await tool.ExecuteAsync(toolCallRequest.Parameters);
                            System.Diagnostics.Debug.WriteLine($"[ToolExecutorService] Tool '{tool.Name}' execution result: {executionResult.Substring(0, Math.Min(executionResult.Length, 100))}...");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ToolExecutorService] Exception during tool '{tool.Name}' execution: {ex.ToString()}");
                            executionResult = $"Error executing tool '{tool.Name}': {ex.Message}";
                            // Also add to UI for visibility of this unexpected tool error
                            _mainViewModel.AddSystemMessage($"Critical error during execution of tool '{tool.Name}': {ex.Message}");
                        }

                        _mainViewModel.AddSystemMessage($"Tool '{tool.Name}' result: {executionResult}");
                        return executionResult;
                    }
                    else
                    {
                        _mainViewModel.AddSystemMessage($"Tool '{tool.Name}' execution denied by user.");
                        System.Diagnostics.Debug.WriteLine($"[ToolExecutorService] Tool '{tool.Name}' execution denied by user.");
                        return $"User denied execution for tool {tool.Name}.";
                    }
                }
                else
                {
                    _mainViewModel.AddSystemMessage($"Unknown tool requested: '{toolCallRequest.ToolName}'.");
                    System.Diagnostics.Debug.WriteLine($"[ToolExecutorService] Unknown tool requested: '{toolCallRequest.ToolName}'.");
                    return $"Assistant requested an unknown tool: '{toolCallRequest.ToolName}'.";
                }
            }
            else
            {
                // Not a tool call, return the original assistant message
                // Log if assistantResponseContent was not empty but not a tool call, to see what it was.
                if(!string.IsNullOrWhiteSpace(assistantResponseContent))
                {
                    System.Diagnostics.Debug.WriteLine($"[ToolExecutorService] No tool call parsed. Assistant response: {assistantResponseContent.Substring(0, Math.Min(assistantResponseContent.Length,100))}...");
                }
                return assistantResponseContent;
            }
        }
    }
}
