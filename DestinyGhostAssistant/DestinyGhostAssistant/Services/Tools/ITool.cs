using System.Collections.Generic;
using System.Threading.Tasks;

namespace DestinyGhostAssistant.Services.Tools
{
    /// <summary>
    /// Defines the contract for a tool that can be executed by the AI assistant.
    /// </summary>
    public interface ITool
    {
        /// <summary>
        /// Gets the unique name of the tool.
        /// This name is used by the AI to identify and call the tool.
        /// Example: "create_file", "read_file", "web_search".
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a description of what the tool does, its expected parameters,
        /// and the format of its output. This description helps the AI
        /// understand how and when to use the tool.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Executes the tool's logic with the given parameters.
        /// </summary>
        /// <param name="parameters">
        /// A dictionary of parameters for the tool. Keys are parameter names (string),
        /// and values are the parameter values (object). The tool implementation
        /// will need to validate and cast these parameters as appropriate.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The result of the task is a string message indicating the outcome
        _        /// of the tool's execution (e.g., success message, data, or error details).
        /// </returns>
        Task<string> ExecuteAsync(Dictionary<string, object> parameters);
    }
}
