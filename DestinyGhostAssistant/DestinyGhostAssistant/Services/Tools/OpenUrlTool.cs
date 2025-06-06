using System;
using System.Collections.Generic;
using System.ComponentModel; // For Win32Exception
using System.Diagnostics;
using System.Threading.Tasks;

namespace DestinyGhostAssistant.Services.Tools
{
    public class OpenUrlTool : ITool
    {
        public string Name => "open_url_in_browser";

        public string Description => "Opens the specified URL in the system's default web browser. Parameters: 'url' (string, the full URL to open, e.g., https://www.google.com).";

        public async Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            Debug.WriteLine($"OpenUrlTool: Received parameters - URL='{parameters.GetValueOrDefault("url", "Not Provided")}'");

            if (!parameters.TryGetValue("url", out object? urlObj) || !(urlObj is string urlString))
            {
                Debug.WriteLine("OpenUrlTool: 'url' parameter is missing or not a string.");
                return "Error: 'url' parameter is missing or not a string.";
            }

            if (string.IsNullOrWhiteSpace(urlString))
            {
                Debug.WriteLine("OpenUrlTool: 'url' parameter cannot be empty.");
                return "Error: 'url' parameter cannot be empty.";
            }

            Uri? uriResult;
            if (!Uri.TryCreate(urlString, UriKind.Absolute, out uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                Debug.WriteLine($"OpenUrlTool: Invalid URL provided: '{urlString}'. Must be http or https.");
                return $"Error: Invalid URL provided: '{urlString}'. Please provide a full URL starting with http:// or https://.";
            }

            string validatedUrl = uriResult.ToString(); // Use the validated and potentially cleaned URI

            try
            {
                Debug.WriteLine($"OpenUrlTool: Attempting to open URL: {validatedUrl}");
                // Process.Start with UseShellExecute = true is the common way to open URLs in default browser.
                // For .NET Core applications, ProcessStartInfo needs the URL directly in the constructor
                // or set to FileName for UseShellExecute = true.
                Process.Start(new ProcessStartInfo(validatedUrl) { UseShellExecute = true });

                // Task.CompletedTask is used as Process.Start with UseShellExecute for URLs is typically
                // a fire-and-forget operation that launches another process.
                // The method is async to conform to ITool interface.
                await Task.CompletedTask;

                string successMsg = $"Successfully requested to open URL: {validatedUrl}";
                Debug.WriteLine($"OpenUrlTool: {successMsg}");
                return successMsg;
            }
            catch (Win32Exception ex) // Often thrown for issues like no default browser or if the URL was somehow treated as a file not found
            {
                Debug.WriteLine($"OpenUrlTool: Win32Exception opening URL '{validatedUrl}'. Error: {ex.Message}");
                return $"Error opening URL '{validatedUrl}': Could not start process. System error: {ex.Message}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenUrlTool: Unexpected Exception opening URL '{validatedUrl}'. Error: {ex.ToString()}");
                return $"Error opening URL '{validatedUrl}': An unexpected error occurred. {ex.Message}";
            }
        }
    }
}
