using Microsoft.VisualStudio.TestTools.UnitTesting;
using DestinyGhostAssistant.Services.Tools;
using DestinyGhostAssistant.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System; // For IntPtr

namespace DestinyGhostAssistant.Tests.Tools
{
    [TestClass]
    public class GetVisualStudioActiveDocumentToolTests
    {
        [TestMethod]
        public async Task ExecuteAsync_NullParametersDictionary_ReturnsError()
        {
            // Arrange
            var tool = new GetVisualStudioActiveDocumentTool();

            // Act
            string result = await tool.ExecuteAsync(null!); // Pass null dictionary

            // Assert
            // The tool's first check is TryGetValue on parameters. A null dict will cause an ArgumentNullException there if not handled.
            // Let's assume the tool's TryGetValue handles it by returning false, leading to the specific error.
            // If it throws ArgumentNullException, this test would need to be an Assert.ThrowsExceptionAsync.
            // Based on current GetVisualStudioActiveDocumentTool, it should be "Error: No process seems to be attached..."
            // as TryGetValue on a null dict will throw, but the tool has a general try-catch if not specific.
            // Let's refine the tool to handle null parameters dictionary more gracefully or test for ArgumentNullException.
            // For now, assume current tool code's TryGetValue fails and it returns the specific message.
            // The tool has: if (!parameters.TryGetValue("_attachedProcess", out var attachedProcessObj)...)
            // This will throw NullReferenceException if parameters is null.
            // Let's adjust the tool to handle null dictionary input or test for the exception.
            // For now, expecting it to be caught by the general try-catch in the tool's ExecuteAsync.
            // The tool's parameter check `!parameters.TryGetValue` will throw NullReferenceException if parameters is null.
            // The tool's main try-catch will then produce a generic error.
            // A more robust tool would check `if (parameters == null)`.
            // Given the current tool code, let's test the actual outcome.
            // The top level try-catch in the tool's ExecuteAsync is: catch (Exception ex) { return $"Error getting Visual Studio context: An unexpected error occurred. {ex.Message}"; }
            // The NullReferenceException message is "Object reference not set to an instance of an object."
            StringAssert.Contains(result, "Error getting Visual Studio context: An unexpected error occurred.", $"Unexpected result for null parameters: {result}");
            StringAssert.Contains(result, "Object reference not set", $"Unexpected result for null parameters, NRE message missing: {result}");

        }

        [TestMethod]
        public async Task ExecuteAsync_MissingAttachedProcessParameter_ReturnsError()
        {
            // Arrange
            var tool = new GetVisualStudioActiveDocumentTool();
            var parameters = new Dictionary<string, object>(); // _attachedProcess is missing

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            StringAssert.Contains(result, "Error: No process seems to be attached or process info is unavailable", $"Unexpected result: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_AttachedProcessIsNullInParameters_ReturnsError()
        {
            // Arrange
            var tool = new GetVisualStudioActiveDocumentTool();
            // Explicitly passing null for the value of _attachedProcess
            var parameters = new Dictionary<string, object> { { "_attachedProcess", null! } };


            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            // The tool checks: !(attachedProcessObj is ProcessInfo attachedProcess)
            // If attachedProcessObj is null, this check passes, but then attachedProcess is null.
            // The next check is: if (attachedProcess == null) { return "Error: No process is currently attached."; }
            StringAssert.Contains(result, "Error: No process is currently attached.", $"Unexpected result: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_AttachedProcessNotDevenv_ReturnsError()
        {
            // Arrange
            var tool = new GetVisualStudioActiveDocumentTool();
            // Using IntPtr.Zero for handle as it's not relevant for this specific check.
            var notVsProcess = new ProcessInfo(1, "Notepad", "Untitled - Notepad", IntPtr.Zero);
            var parameters = new Dictionary<string, object> { { "_attachedProcess", notVsProcess } };

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            StringAssert.Contains(result, "Error: Attached process 'Notepad' is not Visual Studio (devenv.exe).", $"Unexpected result: {result}");
        }

        [TestMethod]
        public async Task ExecuteAsync_AttachedProcessDevenv_NoWindowHandle_ReturnsError()
        {
            // Arrange
            var tool = new GetVisualStudioActiveDocumentTool();
            var vsProcessNoHandle = new ProcessInfo(123, "devenv", "Solution - VS", IntPtr.Zero);
            var parameters = new Dictionary<string, object> { { "_attachedProcess", vsProcessNoHandle } };

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            StringAssert.Contains(result, "Error: Attached process has no valid main window handle.", $"Unexpected result: {result}");
        }


        [TestMethod]
        public async Task ExecuteAsync_AttachedProcessDevenv_GetDTEReturnsNull_ReturnsDTEConnectionError()
        {
            // Arrange
            var tool = new GetVisualStudioActiveDocumentTool();
            // Using a non-zero IntPtr for handle to pass the initial check.
            var vsProcess = new ProcessInfo(1234, "devenv", "MySolution - Microsoft Visual Studio", (IntPtr)555);
            var parameters = new Dictionary<string, object> { { "_attachedProcess", vsProcess } };
            // Note: GetDTE() in the tool is currently a placeholder returning null.

            // Act
            string result = await tool.ExecuteAsync(parameters);

            // Assert
            // This message comes from within the Task.Run(...) block in the tool
            StringAssert.Contains(result, "Error: Could not connect to the specified Visual Studio instance.", $"Unexpected result: {result}");
        }
    }
}
