using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ModelPreviewService
    {
        private sealed class WindowHandleWrapper : IWin32Window
        {
            public WindowHandleWrapper(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; private set; }
        }

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOOWNERZORDER = 0x0200;
        private const uint SWP_NOSENDCHANGING = 0x0400;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        private readonly PckEntryReaderService pckEntryReaderService = new PckEntryReaderService();
        private readonly EmbeddedModelPreviewLoaderService embeddedPreviewLoaderService;
        private ModelPreviewWindow activePreviewWindow;

        public ModelPreviewService()
        {
            embeddedPreviewLoaderService = new EmbeddedModelPreviewLoaderService(pckEntryReaderService);
        }

        public bool TryBuildPreviewMeshData(
            AssetManager assetManager,
            CacheSave database,
            int pathId,
            string fieldName,
            string listName,
            ModelPickerService modelPickerService,
            out ModelPreviewMeshData meshData,
            out string errorMessage)
        {
            meshData = null;
            errorMessage = string.Empty;
            if (assetManager == null || modelPickerService == null)
            {
                errorMessage = "Model preview unavailable.";
                return false;
            }
            if (pathId <= 0)
            {
                errorMessage = "Invalid model PathID.";
                return false;
            }

            int resolvedPathId;
            string mappedPath;
            if (!modelPickerService.TryResolveModelPathById(database, pathId, fieldName, listName, out resolvedPathId, out mappedPath, true))
            {
                errorMessage = "Model PathID not found in path.data:\n" + pathId;
                return false;
            }

            try
            {
                if (!embeddedPreviewLoaderService.TryLoadPreviewMesh(assetManager, mappedPath, out meshData, out errorMessage))
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    {
                        errorMessage = "Model preview unavailable for this file.";
                    }
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "MODEL PREVIEW ERROR!\n" + ex.Message;
                meshData = null;
                return false;
            }
        }

        public bool TryBuildPreviewMeshDataFromMappedPath(
            AssetManager assetManager,
            string mappedPath,
            out ModelPreviewMeshData meshData,
            out string errorMessage)
        {
            meshData = null;
            errorMessage = string.Empty;
            if (assetManager == null)
            {
                errorMessage = "Model preview unavailable.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(mappedPath))
            {
                errorMessage = "Invalid model path.";
                return false;
            }

            try
            {
                if (!embeddedPreviewLoaderService.TryLoadPreviewMesh(assetManager, mappedPath, out meshData, out errorMessage))
                {
                    if (string.IsNullOrWhiteSpace(errorMessage))
                    {
                        errorMessage = "Model preview unavailable for this file.";
                    }
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "MODEL PREVIEW ERROR!\n" + ex.Message;
                meshData = null;
                return false;
            }
        }

        public void ShowPreviewWindow(ModelPreviewMeshData meshData, bool activateWindow, IntPtr nonActivatingOwnerHandle)
        {
            if (meshData == null)
            {
                return;
            }

            bool requireNonActivating = !activateWindow;
            if (activePreviewWindow != null && !activePreviewWindow.IsDisposed)
            {
                if (activePreviewWindow.IsNonActivatingWindow != requireNonActivating)
                {
                    try
                    {
                        activePreviewWindow.Close();
                    }
                    catch
                    {
                    }

                    activePreviewWindow = null;
                }
            }

            if (activePreviewWindow != null && !activePreviewWindow.IsDisposed)
            {
                try
                {
                    activePreviewWindow.SetNonActivatingOwnerHandle(nonActivatingOwnerHandle);
                    activePreviewWindow.ReplaceMeshData(meshData);
                }
                catch
                {
                }

                if (activateWindow
                    && activePreviewWindow.WindowState == System.Windows.Forms.FormWindowState.Minimized)
                {
                    activePreviewWindow.WindowState = System.Windows.Forms.FormWindowState.Normal;
                }
                if (activateWindow)
                {
                    activePreviewWindow.BringToFront();
                    activePreviewWindow.Activate();
                }
                return;
            }

            ModelPreviewWindow window = new ModelPreviewWindow(meshData, !activateWindow, nonActivatingOwnerHandle);
            activePreviewWindow = window;
            window.FormClosed += (s, e) =>
            {
                if (ReferenceEquals(activePreviewWindow, window))
                {
                    activePreviewWindow = null;
                }
            };
            IWin32Window ownerWindow = null;
            if (nonActivatingOwnerHandle != IntPtr.Zero)
            {
                ownerWindow = new WindowHandleWrapper(nonActivatingOwnerHandle);
            }

            if (ownerWindow != null)
            {
                window.Show(ownerWindow);
            }
            else
            {
                window.Show();
            }
            if (activateWindow
                && window.WindowState == System.Windows.Forms.FormWindowState.Minimized)
            {
                window.WindowState = System.Windows.Forms.FormWindowState.Normal;
            }
            if (activateWindow)
            {
                window.BringToFront();
                window.Activate();
            }
        }

        public void ShowPreviewWindow(ModelPreviewMeshData meshData)
        {
            ShowPreviewWindow(meshData, true, IntPtr.Zero);
        }

        public void ShowPreviewWindow(ModelPreviewMeshData meshData, bool activateWindow)
        {
            ShowPreviewWindow(meshData, activateWindow, IntPtr.Zero);
        }

        public bool TryUpdateOpenPreviewWindow(ModelPreviewMeshData meshData)
        {
            if (meshData == null)
            {
                return false;
            }

            if (activePreviewWindow == null || activePreviewWindow.IsDisposed)
            {
                return false;
            }

            try
            {
                activePreviewWindow.ReplaceMeshData(meshData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ShowPreviewMessage(string message, bool activateWindow, IntPtr nonActivatingOwnerHandle)
        {
            string safeMessage = string.IsNullOrWhiteSpace(message)
                ? "Model preview unavailable for this file."
                : message.Trim();

            bool requireNonActivating = !activateWindow;
            if (activePreviewWindow != null && !activePreviewWindow.IsDisposed)
            {
                if (activePreviewWindow.IsNonActivatingWindow != requireNonActivating)
                {
                    try
                    {
                        activePreviewWindow.Close();
                    }
                    catch
                    {
                    }

                    activePreviewWindow = null;
                }
            }

            if (activePreviewWindow != null && !activePreviewWindow.IsDisposed)
            {
                try
                {
                    activePreviewWindow.SetNonActivatingOwnerHandle(nonActivatingOwnerHandle);
                    activePreviewWindow.ShowStatusMessage(safeMessage);
                }
                catch
                {
                }

                if (activateWindow
                    && activePreviewWindow.WindowState == FormWindowState.Minimized)
                {
                    activePreviewWindow.WindowState = FormWindowState.Normal;
                }
                if (activateWindow)
                {
                    activePreviewWindow.BringToFront();
                    activePreviewWindow.Activate();
                }
                return;
            }

            ModelPreviewWindow window = new ModelPreviewWindow(new ModelPreviewMeshData(), !activateWindow, nonActivatingOwnerHandle);
            activePreviewWindow = window;
            window.FormClosed += (s, e) =>
            {
                if (ReferenceEquals(activePreviewWindow, window))
                {
                    activePreviewWindow = null;
                }
            };

            IWin32Window ownerWindow = null;
            if (nonActivatingOwnerHandle != IntPtr.Zero)
            {
                ownerWindow = new WindowHandleWrapper(nonActivatingOwnerHandle);
            }

            if (ownerWindow != null)
            {
                window.Show(ownerWindow);
            }
            else
            {
                window.Show();
            }

            window.ShowStatusMessage(safeMessage);

            if (activateWindow
                && window.WindowState == FormWindowState.Minimized)
            {
                window.WindowState = FormWindowState.Normal;
            }
            if (activateWindow)
            {
                window.BringToFront();
                window.Activate();
            }
        }

        public void SetPreviewWindowInteractionEnabled(bool enabled)
        {
            if (activePreviewWindow == null || activePreviewWindow.IsDisposed)
            {
                return;
            }

            try
            {
                if (activePreviewWindow.InvokeRequired)
                {
                    activePreviewWindow.BeginInvoke((Action)(() =>
                    {
                        if (activePreviewWindow == null || activePreviewWindow.IsDisposed)
                        {
                            return;
                        }
                        activePreviewWindow.Enabled = enabled;
                    }));
                    return;
                }

                activePreviewWindow.Enabled = enabled;
            }
            catch
            {
            }
        }

        public void KeepPreviewBehindWindow(IntPtr targetWindowHandle)
        {
            if (targetWindowHandle == IntPtr.Zero)
            {
                return;
            }
            if (activePreviewWindow == null || activePreviewWindow.IsDisposed || !activePreviewWindow.IsHandleCreated)
            {
                return;
            }

            try
            {
                SetWindowPos(
                    activePreviewWindow.Handle,
                    HWND_NOTOPMOST,
                    0,
                    0,
                    0,
                    0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
                SetWindowPos(
                    activePreviewWindow.Handle,
                    targetWindowHandle,
                    0,
                    0,
                    0,
                    0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
                SetWindowPos(
                    targetWindowHandle,
                    HWND_TOP,
                    0,
                    0,
                    0,
                    0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
            }
            catch
            {
            }
        }

        public bool IsPreviewWindowOpen()
        {
            return activePreviewWindow != null && !activePreviewWindow.IsDisposed;
        }

        public bool TryOpenPreview(
            AssetManager assetManager,
            CacheSave database,
            int pathId,
            string fieldName,
            string listName,
            ModelPickerService modelPickerService,
            out string errorMessage)
        {
            if (!TryBuildPreviewMeshData(
                assetManager,
                database,
                pathId,
                fieldName,
                listName,
                modelPickerService,
                out ModelPreviewMeshData meshData,
                out errorMessage))
            {
                return false;
            }

            ShowPreviewWindow(meshData);
            return true;
        }
    }
}
