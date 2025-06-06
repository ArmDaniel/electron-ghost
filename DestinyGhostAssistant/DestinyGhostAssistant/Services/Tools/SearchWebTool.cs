using System;
using System.Collections.Generic;
using System.ComponentModel; // For Win32Exception
using System.Diagnostics;
using System.Net; // For Uri.EscapeDataString
using System.Threading.Tasks;

namespace DestinyGhostAssistant.Services.Tools
{
    public class SearchWebTool : ITool
    {
        public string Name => "search_web";

        public string Description => "Performs a web search using Google in the system's default web browser. Parameters: 'query' (string, the search term or question).";

        public async Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            Debug.WriteLine($"SearchWebTool: Received parameters - Query='{parameters.GetValueOrDefault("query", "Not Provided")}'");

            if (!parameters.TryGetValue("query", out object? queryObj) || !(queryObj is string queryString))
            {
                Debug.WriteLine("SearchWebTool: 'query' parameter is missing or not a string.");
                return "Error: 'query' parameter is missing or not a string.";
            }

            if (string.IsNullOrWhiteSpace(queryString))
            {
                Debug.WriteLine("SearchWebTool: 'query' parameter cannot be empty.");
                return "Error: 'query' parameter cannot be empty.";
            }

            string encodedQuery = Uri.EscapeDataString(queryString);
            string searchUrl = $"https://www.google.com/search?q={encodedQuery}";

            try
            {
                Debug.WriteLine($"SearchWebTool: Attempting to open search URL: {searchUrl}");
                Process.Start(new ProcessStartInfo(searchUrl) 
                { 
                    UseShellExecute = true,
                    Verb = "open" // Explicitly set the verb
                });

                await Task.CompletedTask; // Conforms to async ITool, actual operation is fire-and-forget.

                string successMsg = $"Successfully requested web search for: '{queryString}'";
                Debug.WriteLine($"SearchWebTool: {successMsg}");
                return successMsg;
            }
            catch (Win32Exception ex)
            {
                Debug.WriteLine($"SearchWebTool: Win32Exception performing search for '{queryString}'. URL: {searchUrl}. Error: {ex.Message}");
                return $"Error performing web search for '{queryString}': Could not start process. System error: {ex.Message}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SearchWebTool: Unexpected Exception performing search for '{queryString}'. URL: {searchUrl}. Error: {ex.ToString()}");
                return $"Error performing web search for '{queryString}': An unexpected error occurred. {ex.Message}";
            }
        }
    }
}
