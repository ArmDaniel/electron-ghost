using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Automation; // Requires UIAutomationClient and UIAutomationTypes
using DestinyGhostAssistant.Models; // For ProcessInfo
using System.Linq; // For Any

namespace DestinyGhostAssistant.Services.Tools
{
    public class GetBrowserContextTool : ITool
    {
        public string Name => "get_browser_context";
        public string Description => "EXPERIMENTAL: Attempts to get the URL and title of the active tab from the currently attached Chrome or Edge browser process. Parameters: None. Relies on UI Automation and may not always succeed.";

        public async Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            Debug.WriteLine("GetBrowserContextTool: Attempting to execute.");

            if (!parameters.TryGetValue("_attachedProcess", out var attachedProcessObj) || !(attachedProcessObj is ProcessInfo attachedProcess))
            {
                Debug.WriteLine("GetBrowserContextTool: Error - _attachedProcess parameter is missing or not a ProcessInfo object.");
                return "Error: No process seems to be attached or process info is unavailable for this tool. Please use 'Attach to Process' first.";
            }

            if (attachedProcess == null) // Should be caught by above, but as a safeguard
            {
                Debug.WriteLine("GetBrowserContextTool: Error - Attached process is null.");
                return "Error: No process is currently attached.";
            }

            string processNameLower = attachedProcess.ProcessName.ToLowerInvariant();
            if (processNameLower != "chrome" && processNameLower != "msedge")
            {
                Debug.WriteLine($"GetBrowserContextTool: Attached process '{attachedProcess.ProcessName}' is not Chrome or Edge.");
                return $"Error: The attached process '{attachedProcess.ProcessName}' is not supported. Please attach to Chrome or Edge.";
            }

            if (attachedProcess.MainWindowHandle == IntPtr.Zero)
            {
                Debug.WriteLine($"GetBrowserContextTool: Attached process '{attachedProcess.ProcessName}' has no valid main window handle.");
                return "Error: Attached process has no valid main window handle. It might be a background process or minimized with no active window.";
            }

            // UI Automation logic needs to run on a thread that allows it (STA typically).
            // Task.Run will use a thread pool thread which might be MTA.
            // For simplicity in this environment, we'll try direct execution. If issues arise,
            // a dedicated STA thread might be needed for UI Automation calls.
            // However, AutomationElement.FromHandle itself is often fine.
            try
            {
                return await Task.Run(() => // Execute potentially blocking UI Automation on a background thread
                {
                    string? url = null;
                    // The MainWindowTitle from ProcessInfo is often the active tab's title.
                    string title = attachedProcess.MainWindowTitle;

                    Debug.WriteLine($"GetBrowserContextTool: Accessing window handle {attachedProcess.MainWindowHandle} for process {attachedProcess.ProcessName}. Initial title: {title}");

                    AutomationElement? browserWindow = AutomationElement.FromHandle(attachedProcess.MainWindowHandle);
                    if (browserWindow == null)
                    {
                        Debug.WriteLine("GetBrowserContextTool: Could not get UI Automation element for the browser window.");
                        return "Error: Could not get UI Automation element for the browser window.";
                    }

                    // Heuristics to find the address bar (these are fragile and browser-version dependent)
                    // Condition for an Edit control type
                    Condition editCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit);
                    // Condition for an Edit control that also has a ValuePattern
                    Condition valuePatternCondition = new PropertyCondition(AutomationElement.IsValuePatternAvailableProperty, true);

                    // More specific conditions (try these first)
                    Condition nameConditionChrome = new PropertyCondition(AutomationElement.NameProperty, "Address and search bar", PropertyConditionFlags.IgnoreCase);
                    Condition nameConditionEdge = new PropertyCondition(AutomationElement.NameProperty, "Address bar", PropertyConditionFlags.IgnoreCase); // Common for Edge's older versions or some variants

                    // Try to find address bar using specific names (Chrome, then Edge heuristic)
                    AutomationElement? addressBar = browserWindow.FindFirst(TreeScope.Descendants, new AndCondition(editCondition, valuePatternCondition, nameConditionChrome));

                    if (addressBar == null) // If Chrome heuristic failed, try Edge
                    {
                        Debug.WriteLine("GetBrowserContextTool: Chrome 'Address and search bar' not found, trying Edge 'Address bar' heuristic.");
                        addressBar = browserWindow.FindFirst(TreeScope.Descendants, new AndCondition(editCondition, valuePatternCondition, nameConditionEdge));
                    }

                    if (addressBar == null) // Try more generic, but potentially less accurate, heuristics
                    {
                        Debug.WriteLine("GetBrowserContextTool: Specific name heuristics failed. Trying more generic heuristics for address bar.");
                        // Fallback: Look for the first editable field that might be an address bar.
                        // This is very broad and might pick up other text fields.
                        // We can try to refine by looking for elements that also have a specific AutomationId if known,
                        // or by checking for focusable edit fields.
                        var potentialAddressBars = browserWindow.FindAll(TreeScope.Descendants, new AndCondition(editCondition, valuePatternCondition));
                        if (potentialAddressBars.Count > 0)
                        {
                            // Heuristic: Often the address bar is one of the first few edit controls.
                            // This is highly unreliable.
                            addressBar = potentialAddressBars[0];
                            Debug.WriteLine($"GetBrowserContextTool: Generic heuristic found an edit field. Name: '{addressBar.Current.Name}', ID: '{addressBar.Current.AutomationId}'. This might not be the address bar.");
                        }
                    }


                    if (addressBar != null && addressBar.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
                    {
                        ValuePattern valuePattern = (ValuePattern)valuePatternObj;
                        url = valuePattern.Current.Value;
                        Debug.WriteLine($"GetBrowserContextTool: Successfully retrieved URL from address bar: {url}");
                    }
                    else
                    {
                        Debug.WriteLine("GetBrowserContextTool: Address bar not found or ValuePattern not supported.");
                    }

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return $"Active Tab: {title}\nURL: {url}";
                    }
                    else
                    {
                        return $"Active Tab Title: {title}\nURL: Could not retrieve (UI Automation failed to find address bar or URL is empty).";
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetBrowserContextTool: Exception during UI Automation. Error: {ex.ToString()}");
                return $"Error accessing browser context via UI Automation: {ex.Message}";
            }
        }
    }
}
