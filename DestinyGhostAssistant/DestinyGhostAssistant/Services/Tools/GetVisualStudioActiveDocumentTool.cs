using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes; // Required for IRunningObjectTable, IEnumMoniker, IMoniker, IBindCtx
using System.Threading.Tasks;
using DestinyGhostAssistant.Models; // For ProcessInfo
using System.Linq; // For Cast and Any

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

        private static class NativeMethods
        {
            [DllImport("ole32.dll")]
            internal static extern int GetRunningObjectTable(uint reserved, out IRunningObjectTable prot);

            [DllImport("ole32.dll")]
            internal static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        }

#if NETFRAMEWORK || WINDOWS
        private DTE2? GetDTE(int processId)
        {
            Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Attempting to get DTE for target PID: {processId}.");
            IRunningObjectTable? rot = null;
            IEnumMoniker? enumMoniker = null;
            IBindCtx? bindCtx = null;
            DTE2? targetDte = null;

            try
            {
                if (NativeMethods.GetRunningObjectTable(0, out rot) != 0 || rot == null)
                {
                    Debug.WriteLine("GetVisualStudioActiveDocumentTool: Failed to get Running Object Table (ROT).");
                    return null;
                }

                if (NativeMethods.CreateBindCtx(0, out bindCtx) != 0 || bindCtx == null)
                {
                    Debug.WriteLine("GetVisualStudioActiveDocumentTool: Failed to create Bind Context.");
                    return null;
                }

                rot.EnumRunning(out enumMoniker);
                if (enumMoniker == null)
                {
                     Debug.WriteLine("GetVisualStudioActiveDocumentTool: Failed to enumerate running objects in ROT.");
                    return null;
                }
                enumMoniker.Reset();

                IMoniker[] monikers = new IMoniker[1];

                while (targetDte == null && enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    IMoniker? moniker = monikers[0];
                    if (moniker == null) continue;

                    string? displayName = null;
                    object? comObject = null;
                    DTE2? dteInstance = null;

                    try
                    {
                        moniker.GetDisplayName(bindCtx, null, out displayName);
                        // Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Checking ROT entry: {displayName}");

                        if (displayName != null && displayName.StartsWith("!VisualStudio.DTE."))
                        {
                            if (rot.GetObject(moniker, out comObject) != 0 || comObject == null)
                            {
                                Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Failed to get COM object for moniker: {displayName}");
                                continue;
                            }

                            dteInstance = comObject as DTE2;
                            if (dteInstance != null)
                            {
                                try
                                {
                                    // Check if this DTE instance belongs to the target processId
                                    // Method 1: Check process ID via MainWindowHandle
                                    if (dteInstance.MainWindow != null && dteInstance.MainWindow.HWnd != 0)
                                    {
                                        NativeMethods.GetWindowThreadProcessId(new IntPtr(dteInstance.MainWindow.HWnd), out int dteProcessId);
                                        Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Found DTE instance for '{displayName}', PID from HWnd: {dteProcessId}");
                                        if (dteProcessId == processId)
                                        {
                                            targetDte = dteInstance; // Found the target DTE
                                            dteInstance = null; // Avoid releasing the targetDTE
                                            break; // Exit while loop
                                        }
                                    }
                                    else
                                    {
                                        // Method 2: Fallback if no MainWindow or HWnd (e.g. VS starting up)
                                        // Often the ROT display name includes the PID like "!VisualStudio.DTE.17.0:12345"
                                        string pidSuffix = ":" + processId.ToString();
                                        if (displayName.EndsWith(pidSuffix))
                                        {
                                             Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Matched DTE instance for PID {processId} via ROT display name heuristic: {displayName}");
                                             targetDte = dteInstance;
                                             dteInstance = null; // Avoid releasing the targetDTE
                                             break; // Exit while loop
                                        }
                                    }
                                }
                                catch (COMException ex)
                                {
                                    Debug.WriteLine($"GetVisualStudioActiveDocumentTool: COMException accessing DTE properties for '{displayName}'. PID may not match or DTE instance is not fully initialized/accessible. Error: {ex.Message}");
                                }
                                finally
                                {
                                    if (dteInstance != null) // If it's not the targetDte, release it
                                    {
                                        Marshal.ReleaseComObject(dteInstance);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Error processing moniker '{displayName ?? "Unknown"}'. Error: {ex.Message}");
                    }
                    finally
                    {
                        if (moniker != null) Marshal.ReleaseComObject(moniker);
                        if (comObject != null && !(comObject is DTE2 && targetDte == comObject)) // Release if not the target DTE
                        {
                            Marshal.ReleaseComObject(comObject);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetVisualStudioActiveDocumentTool: General error in GetDTE method: {ex.ToString()}");
                // Ensure targetDte is null if an error occurred before it was assigned
                if (targetDte != null) { /* This should not happen if break is used */ }
                targetDte = null;
            }
            finally
            {
                if (enumMoniker != null) Marshal.ReleaseComObject(enumMoniker);
                if (bindCtx != null) Marshal.ReleaseComObject(bindCtx);
                if (rot != null) Marshal.ReleaseComObject(rot);
            }

            if(targetDte != null)
            {
                Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Successfully found DTE for PID: {processId}.");
            }
            else
            {
                Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Failed to find DTE for PID: {processId}.");
            }
            return targetDte;
        }
#else
        private object? GetDTE(int processId) // Return object? or specific type if DTE types are aliased
        {
            Debug.WriteLine("GetVisualStudioActiveDocumentTool: GetDTE called, but EnvDTE COM components are not available in this build configuration. Returning null.");
            return null;
        }
#endif

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

#if NETFRAMEWORK || WINDOWS
            DTE2? dte = null;
#else
            object? dte = null; // Use object if DTE2 type is not available
#endif
            try
            {
                string result = await Task.Run(() =>
                {
                    dte = GetDTE(attachedProcess.Id);

                    if (dte == null)
                    {
                        Debug.WriteLine("GetVisualStudioActiveDocumentTool: Could not connect to the specified Visual Studio instance via DTE. ROT method failed or is not fully implemented/functional.");
                        return "Error: Could not connect to the specified Visual Studio instance. Ensure it's running, accessible, and not running with higher privileges. (DTE access is experimental).";
                    }

                    #if NETFRAMEWORK || WINDOWS
                    // Safe to cast here if DTE2 is the expected type from GetDTE
                    DTE2 actualDte = (DTE2)dte;
                    if (actualDte.ActiveDocument == null)
                    {
                        Debug.WriteLine("GetVisualStudioActiveDocumentTool: No active document in the DTE instance.");
                        return "No active document found in the attached Visual Studio instance.";
                    }

                    string? documentPath = actualDte.ActiveDocument.FullName;
                    if (string.IsNullOrEmpty(documentPath))
                    {
                        Debug.WriteLine("GetVisualStudioActiveDocumentTool: Active document has no path (e.g., not saved or special document).");
                        return "Active document found, but it has no path (e.g., it's not saved or is a special document type).";
                    }

                    Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Success - Active document path: {documentPath}");
                    return $"Active Visual Studio document: {documentPath}";
                    #else
                    Debug.WriteLine("GetVisualStudioActiveDocumentTool: EnvDTE COM components are not available in this build configuration. Cannot process DTE object.");
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
                    int refCount = Marshal.ReleaseComObject(dte); // dte here is DTE2? or object?
                    Debug.WriteLine($"GetVisualStudioActiveDocumentTool: Released DTE object from ExecuteAsync. Ref count: {refCount}");
                }
                #endif
            }
        }
    }
}
