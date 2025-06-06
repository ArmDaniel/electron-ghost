using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
// using System.Runtime.InteropServices.ComTypes; // For full ROT implementation
using System.Threading.Tasks;
using DestinyGhostAssistant.Models; // For ProcessInfo

// Conditional import for DTE, will only work if COM references are correctly resolved by the build system
#if NETFRAMEWORK || WINDOWS // Or a more specific TFM like net8.0-windows
using EnvDTE;
using EnvDTE80;
#endif

namespace DestinyGhostAssistant.Services.Tools
{
    public class GetVisualStudioActiveDocumentTool : ITool
    {
        public string Name => "get_visual_studio_active_document";
        public string Description => "EXPERIMENTAL: Attempts to get the full path of the active document from the currently attached Visual Studio (devenv.exe) process. Parameters: None. Relies on EnvDTE COM automation and may not fully work in all environments or if VS is not running with appropriate permissions.";

        // DllImports for ROT access - these are complex to use correctly and release COM objects.
        // For this AI agent, a full, robust implementation is beyond scope.
        // [DllImport("ole32.dll")]
        // private static extern int GetRunningObjectTable(uint reserved, out System.Runtime.InteropServices.ComTypes.IRunningObjectTable prot);
        // [DllImport("ole32.dll")]
        // private static extern int CreateBindCtx(uint reserved, out System.Runtime.InteropServices.ComTypes.IBindCtx ppbc);

        private DTE2? GetDTE(int processId)
        {
            Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Attempting to get DTE for PID: {processId}. This is a placeholder/simplified implementation.");
            // --- Full ROT implementation is complex and error-prone without a real dev/test environment ---
            // The general idea is:
            // 1. Get the Running Object Table (ROT).
            // 2. Enumerate monikers in the ROT.
            // 3. For each moniker, get its display name.
            // 4. If the display name indicates a Visual Studio DTE object (e.g., "!VisualStudio.DTE.17.0:12345"),
            //    try to get the object itself from the ROT.
            // 5. Cast the object to DTE2.
            // 6. Check if this DTE instance corresponds to the target processId.
            //    - This can be done by checking currentDte.Debugger.DebuggedProcesses,
            //    - or by checking if the DTE's process ID (obtained via its MainWindowHandle and GetWindowThreadProcessId) matches.
            //    - Or, often the display name itself contains the PID: "!VisualStudio.DTE.17.0:PID"
            // 7. Crucially, all COM objects obtained (monikers, DTE instances not matching, etc.) must be released
            //    using Marshal.ReleaseComObject in finally blocks to prevent resource leaks.

            // Placeholder behavior:
            // In a real scenario, if the DTE object for the given processId was found, it would be returned.
            // For testing purposes or if direct COM access fails, this method will return null.

            // Example of how one might try to match by ROT display name (simplified, lacks full error handling and COM release)
            // This is illustrative and likely won't work directly without the DllImports and full ROT walk.
            /*
            #if NETFRAMEWORK || WINDOWS
            System.Runtime.InteropServices.ComTypes.IRunningObjectTable? rot = null;
            System.Runtime.InteropServices.ComTypes.IEnumMoniker? enumMoniker = null;
            System.Runtime.InteropServices.ComTypes.IBindCtx? bindCtx = null;
            try
            {
                if (GetRunningObjectTable(0, out rot) != 0 || rot == null) return null;
                if (CreateBindCtx(0, out bindCtx) != 0 || bindCtx == null) return null;

                rot.EnumRunning(out enumMoniker);
                if (enumMoniker == null) return null;
                enumMoniker.Reset();

                IMoniker[] moniker = new IMoniker[1];
                while (enumMoniker.Next(1, moniker, IntPtr.Zero) == 0)
                {
                    string displayName;
                    moniker[0].GetDisplayName(bindCtx, null, out displayName);
                    if (displayName.StartsWith("!VisualStudio.DTE.") && displayName.EndsWith(":" + processId.ToString()))
                    {
                        object comObject;
                        rot.GetObject(moniker[0], out comObject);
                        return comObject as DTE2; // Caller must release this if not null
                    }
                }
            }
            catch(Exception ex) { Debug.WriteLine($"GetDTE ROT walking exception: {ex.Message}"); }
            finally
            {
                // Proper COM release would be needed here for bindCtx, enumMoniker, rot, and individual monikers
            }
            #endif
            */
            return null; // Placeholder: DTE retrieval is complex and environment-dependent
        }


        public async Task<string> ExecuteAsync(Dictionary<string, object> parameters)
        {
            Debug.WriteLine("GetVisualStudioActiveDocumentTool: Executing tool.");

            if (!parameters.TryGetValue("_attachedProcess", out var attachedProcessObj) || !(attachedProcessObj is ProcessInfo attachedProcess))
            {
                Debug.WriteLine("GetVisualStudioActiveDocumentTool: Error - _attachedProcess parameter is missing or not a ProcessInfo object.");
                return "Error: No process seems to be attached or process info is unavailable for this tool. Please use 'Attach to Process' first.";
            }

            if (attachedProcess == null)
            {
                Debug.WriteLine("GetVisualStudioActiveDocumentTool: Error - Attached process is null.");
                return "Error: No process is currently attached.";
            }

            if (!attachedProcess.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Attached process '{attachedProcess.ProcessName}' is not Visual Studio (devenv.exe).");
                return $"Error: Attached process '{attachedProcess.ProcessName}' is not Visual Studio (devenv.exe).";
            }

            Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Attempting to connect to Visual Studio PID: {attachedProcess.Id}");

            DTE2? dte = null;
            try
            {
                // The COM operations can be lengthy/blocking, run on a separate thread.
                string result = await Task.Run(() =>
                {
                    #if NETFRAMEWORK || WINDOWS // Or other TFM where DTE is available
                    dte = GetDTE(attachedProcess.Id);

                    if (dte == null)
                    {
                        Debug.WriteLine("GetVisualStudioActiveDocumentTool: Could not connect to the specified Visual Studio instance via DTE. ROT method is complex and may have failed or is not fully implemented here.");
                        return "Error: Could not connect to the specified Visual Studio instance. Ensure it's running and accessible. (DTE access is experimental).";
                    }

                    if (dte.ActiveDocument == null)
                    {
                        Debug.WriteLine("GetVisualStudioActiveDocumentTool: No active document in the DTE instance.");
                        return "No active document found in the attached Visual Studio instance.";
                    }

                    string? documentPath = dte.ActiveDocument.FullName;
                    if (string.IsNullOrEmpty(documentPath))
                    {
                        Debug.WriteLine("GetVisualStudioActiveDocumentTool: Active document has no path (e.g., not saved or special document).");
                        return "Active document found, but it has no path (e.g., it's not saved or is a special document type).";
                    }

                    Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Success - Active document path: {documentPath}");
                    return $"Active Visual Studio document: {documentPath}";
                    #else
                    Debug.WriteLine("GetVisualStudioActiveDocumentTool: EnvDTE COM components are not available in this build configuration.");
                    return "Error: EnvDTE COM components are not available in this build configuration. Cannot get Visual Studio context.";
                    #endif
                });
                return result;
            }
            catch (COMException comEx)
            {
                Debug.WriteLine($"GetVisualStudioActiveDocumentTool: COMException. Error: {comEx.ToString()}");
                return $"Error interacting with Visual Studio (COM): {comEx.Message}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Unexpected Exception. Error: {ex.ToString()}");
                return $"Error getting Visual Studio context: An unexpected error occurred. {ex.Message}";
            }
            finally
            {
                #if NETFRAMEWORK || WINDOWS
                if (dte != null)
                {
                    // Release the DTE COM object. This is crucial.
                    // If GetDTE were to return an object, it should be released by the caller (this method).
                    // Since GetDTE is a placeholder returning null, this is more for show in this version.
                    int refCount = Marshal.ReleaseComObject(dte);
                    Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Released DTE object. Ref count: {refCount}");
                }
                #endif
            }
        }
    }
}
