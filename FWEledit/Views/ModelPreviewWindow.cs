using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FWEledit.Properties;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;

namespace FWEledit
{
    public class ModelPreviewWindow : Form
    {
        private enum HardwareRenderBackend
        {
            DirectX11 = 0,
            OpenGL = 1
        }

        private struct ViewCameraState
        {
            public float Yaw;
            public float Pitch;
            public float Zoom;
            public bool IsValid;
        }

        private ModelPreviewViewport cpuViewport;
        private ModelPreviewDx11Viewport dx11Viewport;
        private string dx11ViewportCreationError;
        private ModelPreviewGpuViewport gpuViewport;
        private string gpuViewportCreationError;
        private readonly Panel viewportHost;
        private Control activeViewport;
        private readonly CheckBox wireframeCheck;
        private readonly CheckBox hardwareCheck;
        private readonly ComboBox backendCombo;
        private readonly Label backendLabel;
        private readonly Label helpLabel;
        private readonly Label summaryLabel;
        private readonly Label sourceLabel;
        private bool suppressRendererPreferenceSave;
        private static ViewCameraState persistedCameraState;
        private static bool cameraSettingsLoaded;

        public ModelPreviewWindow(ModelPreviewMeshData meshData)
        {
            Text = "Preview 3D Model";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 620);
            Size = new Size(1200, 820);
            KeyPreview = true;
            BackColor = Color.FromArgb(20, 20, 20);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

            summaryLabel = new Label();
            summaryLabel.Dock = DockStyle.Top;
            summaryLabel.Height = 56;
            summaryLabel.Padding = new Padding(10, 10, 10, 0);
            summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            summaryLabel.ForeColor = Color.Gainsboro;
            summaryLabel.BackColor = Color.FromArgb(20, 20, 20);
            summaryLabel.Text = BuildSummaryText(meshData);

            sourceLabel = new Label();
            sourceLabel.Dock = DockStyle.Top;
            sourceLabel.Padding = new Padding(10, 4, 10, 8);
            sourceLabel.TextAlign = ContentAlignment.TopLeft;
            sourceLabel.ForeColor = Color.FromArgb(170, 170, 170);
            sourceLabel.BackColor = Color.FromArgb(20, 20, 20);
            string sourceText = BuildSourceText(meshData);
            sourceLabel.Text = sourceText;
            sourceLabel.Height = ComputeSourceLabelHeight(sourceText);

            cpuViewport = new ModelPreviewViewport(meshData);
            cpuViewport.Dock = DockStyle.Fill;
            cpuViewport.Visible = false;

            try
            {
                dx11Viewport = new ModelPreviewDx11Viewport(meshData);
                dx11Viewport.Dock = DockStyle.Fill;
                dx11Viewport.Visible = false;
                dx11ViewportCreationError = string.Empty;
            }
            catch (Exception ex)
            {
                dx11Viewport = null;
                dx11ViewportCreationError = ex.GetType().Name + ": " + ex.Message;
            }

            try
            {
                gpuViewport = new ModelPreviewGpuViewport(meshData);
                gpuViewport.Dock = DockStyle.Fill;
                gpuViewport.Visible = false;
                gpuViewportCreationError = string.Empty;
            }
            catch (Exception ex)
            {
                gpuViewport = null;
                gpuViewportCreationError = ex.GetType().Name + ": " + ex.Message;
            }

            viewportHost = new Panel();
            viewportHost.Dock = DockStyle.Fill;
            viewportHost.Controls.Add(cpuViewport);
            if (dx11Viewport != null)
            {
                viewportHost.Controls.Add(dx11Viewport);
            }
            if (gpuViewport != null)
            {
                viewportHost.Controls.Add(gpuViewport);
            }

            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 40;
            footer.Padding = new Padding(10, 4, 10, 4);
            footer.BackColor = Color.FromArgb(24, 24, 24);

            helpLabel = new Label();
            helpLabel.Dock = DockStyle.Fill;
            helpLabel.TextAlign = ContentAlignment.MiddleLeft;
            helpLabel.ForeColor = Color.FromArgb(190, 190, 190);
            footer.Controls.Add(helpLabel);

            wireframeCheck = new CheckBox();
            wireframeCheck.Dock = DockStyle.Right;
            wireframeCheck.Width = 90;
            wireframeCheck.Checked = false;
            wireframeCheck.Text = "Wireframe";
            wireframeCheck.ForeColor = Color.Gainsboro;
            wireframeCheck.BackColor = footer.BackColor;
            wireframeCheck.CheckedChanged += (s, e) =>
            {
                ApplyWireframeMode();
                InvalidateActiveViewport();
            };
            footer.Controls.Add(wireframeCheck);

            backendCombo = new ComboBox();
            backendCombo.Dock = DockStyle.Right;
            backendCombo.Width = 118;
            backendCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            backendCombo.FlatStyle = FlatStyle.Flat;
            backendCombo.Items.Add("DirectX 11");
            backendCombo.Items.Add("OpenGL");
            backendCombo.SelectedIndex = 0;
            backendCombo.SelectedIndexChanged += (s, e) => ApplyRendererSettings(true);
            footer.Controls.Add(backendCombo);

            backendLabel = new Label();
            backendLabel.Dock = DockStyle.Right;
            backendLabel.Width = 70;
            backendLabel.TextAlign = ContentAlignment.MiddleRight;
            backendLabel.ForeColor = Color.FromArgb(190, 190, 190);
            backendLabel.Text = "Backend:";
            footer.Controls.Add(backendLabel);

            hardwareCheck = new CheckBox();
            hardwareCheck.Dock = DockStyle.Right;
            hardwareCheck.Width = 92;
            hardwareCheck.Checked = true;
            hardwareCheck.Text = "Hardware";
            hardwareCheck.ForeColor = Color.Gainsboro;
            hardwareCheck.BackColor = footer.BackColor;
            hardwareCheck.CheckedChanged += (s, e) => ApplyRendererSettings(true);
            footer.Controls.Add(hardwareCheck);

            Button resetButton = new Button();
            resetButton.Dock = DockStyle.Right;
            resetButton.Width = 120;
            resetButton.Text = "Reset View";
            resetButton.Click += (s, e) => ResetActiveViewport();
            footer.Controls.Add(resetButton);

            Controls.Add(viewportHost);
            Controls.Add(footer);
            Controls.Add(sourceLabel);
            Controls.Add(summaryLabel);

            KeyDown += OnWindowKeyDown;
            FormClosing += (s, e) => SaveCameraSettings();
            Shown += (s, e) =>
            {
                if (hardwareCheck != null && hardwareCheck.Checked)
                {
                    ApplyRendererSettings(false);
                    BeginInvoke((Action)(() => ApplyRendererSettings(false)));
                }
            };
            LoadRendererSettings();
            LoadCameraSettings();
            ApplyRendererSettings(false);
            ApplyPersistedCameraIfAny();
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (e.KeyCode == Keys.R)
            {
                ResetActiveViewport();
            }
        }

        private static string BuildSummaryText(ModelPreviewMeshData meshData)
        {
            if (meshData == null)
            {
                return "No mesh loaded";
            }

            int totalTextures = meshData.Textures == null ? 0 : meshData.Textures.Length;
            int loadedTextures = 0;
            if (meshData.Textures != null)
            {
                for (int i = 0; i < meshData.Textures.Length; i++)
                {
                    PreviewTextureData texture = meshData.Textures[i];
                    if (texture != null && texture.IsValid)
                    {
                        loadedTextures++;
                    }
                }
            }

            return "Vertices: " + meshData.VertexCount
                + "    Triangles: " + meshData.TriangleCount
                + "    Textures: " + loadedTextures + "/" + totalTextures;
        }

        private static string BuildSourceText(ModelPreviewMeshData meshData)
        {
            if (meshData == null)
            {
                return string.Empty;
            }

            List<string> lines = new List<string>(16);
            lines.Add("ECM: " + (meshData.EcmPath ?? string.Empty));
            lines.Add("SKI: " + (meshData.SkiPath ?? string.Empty));

            PreviewTextureData[] textures = meshData.Textures ?? new PreviewTextureData[0];
            if (textures.Length <= 0)
            {
                lines.Add("TEX: none");
            }
            else
            {
                int maxShown = 8;
                int shown = 0;
                for (int i = 0; i < textures.Length; i++)
                {
                    if (shown >= maxShown)
                    {
                        break;
                    }

                    PreviewTextureData texture = textures[i];
                    bool loaded = texture != null && texture.IsValid;
                    string name = texture != null ? (texture.Name ?? string.Empty) : string.Empty;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = "(path not resolved)";
                    }

                    string sizeLabel = string.Empty;
                    if (texture != null && texture.Width > 0 && texture.Height > 0)
                    {
                        sizeLabel = " (" + texture.Width + "x" + texture.Height + ")";
                    }

                    string alphaMode = "OPAQUE";
                    if (texture != null)
                    {
                        if (texture.HasTransparency)
                        {
                            alphaMode = "BLEND";
                        }
                        else if (texture.UsesAlphaAsOpacity)
                        {
                            alphaMode = "CUTOUT";
                        }
                    }

                    lines.Add("TEX[" + i + "]: " + (loaded ? "OK " : "MISS ") + name + sizeLabel + " [" + alphaMode + "]");
                    shown++;
                }

                if (textures.Length > shown)
                {
                    lines.Add("TEX: +" + (textures.Length - shown) + " more...");
                }
            }

            int[] triTextureIndices = meshData.TriangleTextureIndices ?? new int[0];
            int triCount = (meshData.Indices == null ? 0 : meshData.Indices.Length / 3);
            if (triTextureIndices.Length == triCount && triCount > 0)
            {
                Dictionary<int, int> usage = new Dictionary<int, int>();
                for (int i = 0; i < triTextureIndices.Length; i++)
                {
                    int texIndex = triTextureIndices[i];
                    if (!usage.ContainsKey(texIndex))
                    {
                        usage[texIndex] = 0;
                    }

                    usage[texIndex]++;
                }

                List<int> keys = new List<int>(usage.Keys);
                keys.Sort();
                for (int i = 0; i < keys.Count; i++)
                {
                    int texIndex = keys[i];
                    int count = usage[texIndex];
                    lines.Add("TRI TEX[" + texIndex + "]: " + count);
                }
            }

            return string.Join(Environment.NewLine, lines.ToArray());
        }

        private static int ComputeSourceLabelHeight(string sourceText)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
            {
                return 64;
            }

            int lines = 1;
            for (int i = 0; i < sourceText.Length; i++)
            {
                if (sourceText[i] == '\n')
                {
                    lines++;
                }
            }

            int contentHeight = 12 + (lines * 17);
            return ClampInt(contentHeight, 64, 210);
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) { return min; }
            if (value > max) { return max; }
            return value;
        }

        private static float ClampFloat(float value, float min, float max)
        {
            if (value < min) { return min; }
            if (value > max) { return max; }
            return value;
        }

        private static float NormalizeAngle(float value)
        {
            const float twoPi = (float)(Math.PI * 2.0);
            while (value > (float)Math.PI)
            {
                value -= twoPi;
            }
            while (value < (float)-Math.PI)
            {
                value += twoPi;
            }

            return value;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static ViewCameraState NormalizeCameraState(ViewCameraState state)
        {
            if (!state.IsValid
                || !IsFinite(state.Yaw)
                || !IsFinite(state.Pitch)
                || !IsFinite(state.Zoom))
            {
                return new ViewCameraState { IsValid = false };
            }

            return new ViewCameraState
            {
                Yaw = NormalizeAngle(state.Yaw),
                Pitch = ClampFloat(state.Pitch, -1.35f, 1.35f),
                Zoom = ClampFloat(state.Zoom, 0.22f, 4.5f),
                IsValid = true
            };
        }

        private void LoadRendererSettings()
        {
            bool savedUseHardware = true;
            int savedBackendIndex = 0;
            try
            {
                savedUseHardware = Settings.Default.ModelPreviewUseHardware;
                savedBackendIndex = Settings.Default.ModelPreviewBackend;
            }
            catch
            {
                savedUseHardware = true;
                savedBackendIndex = 0;
            }

            // Migrate old index mapping (0=DX11,1=Vulkan,2=OpenGL) to current (0=DX11,1=OpenGL).
            if (savedBackendIndex >= 2)
            {
                savedBackendIndex = (int)HardwareRenderBackend.OpenGL;
            }

            savedBackendIndex = ClampInt(savedBackendIndex, 0, Math.Max(0, backendCombo.Items.Count - 1));

            suppressRendererPreferenceSave = true;
            try
            {
                hardwareCheck.Checked = savedUseHardware;
                backendCombo.SelectedIndex = savedBackendIndex;
            }
            finally
            {
                suppressRendererPreferenceSave = false;
            }
        }

        private void SaveRendererSettings()
        {
            if (suppressRendererPreferenceSave)
            {
                return;
            }

            try
            {
                Settings.Default.ModelPreviewUseHardware = hardwareCheck.Checked;
                int selectedBackendIndex = backendCombo.SelectedIndex;
                Settings.Default.ModelPreviewBackend = selectedBackendIndex >= 0 ? selectedBackendIndex : 0;
                Settings.Default.Save();
            }
            catch
            {
                // Ignore persistence errors; preview should keep working even if settings can't be saved.
            }
        }

        private void LoadCameraSettings()
        {
            if (cameraSettingsLoaded)
            {
                return;
            }

            cameraSettingsLoaded = true;
            persistedCameraState = new ViewCameraState { IsValid = false };
            try
            {
                if (!Settings.Default.ModelPreviewCameraHasState)
                {
                    return;
                }

                ViewCameraState loaded = new ViewCameraState
                {
                    Yaw = Settings.Default.ModelPreviewCameraYaw,
                    Pitch = Settings.Default.ModelPreviewCameraPitch,
                    Zoom = Settings.Default.ModelPreviewCameraZoom,
                    IsValid = true
                };

                persistedCameraState = NormalizeCameraState(loaded);
            }
            catch
            {
                persistedCameraState = new ViewCameraState { IsValid = false };
            }
        }

        private void ApplyPersistedCameraIfAny()
        {
            ViewCameraState state = NormalizeCameraState(persistedCameraState);
            if (!state.IsValid)
            {
                return;
            }

            cpuViewport.SetCameraState(state);
            if (dx11Viewport != null)
            {
                dx11Viewport.SetCameraState(state);
            }
            if (gpuViewport != null)
            {
                gpuViewport.SetCameraState(state);
            }
            ApplyCameraState(activeViewport, state);
        }

        private void SaveCameraSettings()
        {
            ViewCameraState state = CaptureCameraState(activeViewport);
            if (!state.IsValid)
            {
                state = CaptureCameraState(cpuViewport);
            }
            if (!state.IsValid && gpuViewport != null)
            {
                state = CaptureCameraState(gpuViewport);
            }
            if (!state.IsValid && dx11Viewport != null)
            {
                state = CaptureCameraState(dx11Viewport);
            }

            state = NormalizeCameraState(state);
            if (!state.IsValid)
            {
                return;
            }

            persistedCameraState = state;
            try
            {
                Settings.Default.ModelPreviewCameraHasState = true;
                Settings.Default.ModelPreviewCameraYaw = state.Yaw;
                Settings.Default.ModelPreviewCameraPitch = state.Pitch;
                Settings.Default.ModelPreviewCameraZoom = state.Zoom;
                Settings.Default.Save();
            }
            catch
            {
                // Ignore camera persistence errors.
            }
        }

        public void PersistCurrentCameraState()
        {
            SaveCameraSettings();
        }

        public void ReplaceMeshData(ModelPreviewMeshData meshData)
        {
            if (meshData == null || IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => ReplaceMeshData(meshData)));
                return;
            }

            ViewCameraState state = NormalizeCameraState(CaptureCameraState(activeViewport));
            if (!state.IsValid)
            {
                state = NormalizeCameraState(persistedCameraState);
            }

            SuspendLayout();
            try
            {
                UpdateHeaderForMesh(meshData);
                RecreateViewports(meshData, state);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void UpdateHeaderForMesh(ModelPreviewMeshData meshData)
        {
            if (summaryLabel != null)
            {
                summaryLabel.Text = BuildSummaryText(meshData);
            }

            if (sourceLabel != null)
            {
                string sourceText = BuildSourceText(meshData);
                sourceLabel.Text = sourceText;
                sourceLabel.Height = ComputeSourceLabelHeight(sourceText);
            }
        }

        private void RecreateViewports(ModelPreviewMeshData meshData, ViewCameraState cameraState)
        {
            if (viewportHost == null)
            {
                return;
            }

            SafeDisposeViewport(ref cpuViewport);
            SafeDisposeViewport(ref dx11Viewport);
            SafeDisposeViewport(ref gpuViewport);

            cpuViewport = new ModelPreviewViewport(meshData);
            cpuViewport.Dock = DockStyle.Fill;
            cpuViewport.Visible = false;

            try
            {
                dx11Viewport = new ModelPreviewDx11Viewport(meshData);
                dx11Viewport.Dock = DockStyle.Fill;
                dx11Viewport.Visible = false;
                dx11ViewportCreationError = string.Empty;
            }
            catch (Exception ex)
            {
                dx11Viewport = null;
                dx11ViewportCreationError = ex.GetType().Name + ": " + ex.Message;
            }

            try
            {
                gpuViewport = new ModelPreviewGpuViewport(meshData);
                gpuViewport.Dock = DockStyle.Fill;
                gpuViewport.Visible = false;
                gpuViewportCreationError = string.Empty;
            }
            catch (Exception ex)
            {
                gpuViewport = null;
                gpuViewportCreationError = ex.GetType().Name + ": " + ex.Message;
            }

            viewportHost.Controls.Clear();
            viewportHost.Controls.Add(cpuViewport);
            if (dx11Viewport != null)
            {
                viewportHost.Controls.Add(dx11Viewport);
            }
            if (gpuViewport != null)
            {
                viewportHost.Controls.Add(gpuViewport);
            }

            activeViewport = null;
            ApplyWireframeMode();
            EnsureActiveViewport(hardwareCheck != null && hardwareCheck.Checked);

            if (cameraState.IsValid)
            {
                cpuViewport.SetCameraState(cameraState);
                if (dx11Viewport != null)
                {
                    dx11Viewport.SetCameraState(cameraState);
                }
                if (gpuViewport != null)
                {
                    gpuViewport.SetCameraState(cameraState);
                }

                ApplyCameraState(activeViewport, cameraState);
            }
            else
            {
                ApplyPersistedCameraIfAny();
            }

            helpLabel.Text = "Drag: rotate | Shift+drag: fine rotate | Scroll: zoom | Double-click: reset | "
                + GetActiveRendererStatusText();
            InvalidateActiveViewport();
        }

        private static void SafeDisposeViewport<TViewport>(ref TViewport viewport)
            where TViewport : Control
        {
            if (viewport == null)
            {
                return;
            }

            try
            {
                viewport.Dispose();
            }
            catch
            {
            }

            viewport = null;
        }

        private void ApplyRendererSettings(bool persistSettings)
        {
            HardwareRenderBackend backend = GetSelectedBackend();

            bool useHardware = hardwareCheck.Checked;
            cpuViewport.UseHardwareRendering = useHardware;
            cpuViewport.PreferredHardwareBackend = backend;
            if (gpuViewport != null)
            {
                gpuViewport.PreferredHardwareBackend = backend;
            }
            if (dx11Viewport != null)
            {
                dx11Viewport.PreferredHardwareBackend = backend;
            }

            ApplyWireframeMode();
            EnsureActiveViewport(useHardware);

            bool hardwareEnabled = useHardware;
            backendCombo.Enabled = hardwareEnabled;
            backendLabel.Enabled = hardwareEnabled;
            helpLabel.Text = "Drag: rotate | Shift+drag: fine rotate | Scroll: zoom | Double-click: reset | "
                + GetActiveRendererStatusText();
            if (persistSettings)
            {
                SaveRendererSettings();
            }

            InvalidateActiveViewport();
        }

        private void ApplyWireframeMode()
        {
            bool showWireframe = wireframeCheck != null && wireframeCheck.Checked;
            cpuViewport.ShowWireframe = showWireframe;
            if (dx11Viewport != null)
            {
                dx11Viewport.ShowWireframe = showWireframe;
            }
            if (gpuViewport != null)
            {
                gpuViewport.ShowWireframe = showWireframe;
            }
        }

        private void EnsureActiveViewport(bool useHardware)
        {
            if (useHardware)
            {
                HardwareRenderBackend selected = GetSelectedBackend();
                if (selected == HardwareRenderBackend.DirectX11)
                {
                    if (TryActivateDx11Viewport())
                    {
                        return;
                    }

                    if (TryActivateOpenGlViewport())
                    {
                        return;
                    }

                    SetActiveViewport(cpuViewport);
                    return;
                }

                if (selected == HardwareRenderBackend.OpenGL)
                {
                    if (TryActivateOpenGlViewport())
                    {
                        return;
                    }

                    if (TryActivateDx11Viewport())
                    {
                        return;
                    }

                    SetActiveViewport(cpuViewport);
                    return;
                }

            }

            SetActiveViewport(cpuViewport);
        }

        private bool TryActivateDx11Viewport()
        {
            if (dx11Viewport == null)
            {
                return false;
            }

            if (!IsHandleCreated || !Visible || !viewportHost.IsHandleCreated)
            {
                return false;
            }

            SetActiveViewport(dx11Viewport);
            return dx11Viewport.TryEnsureGpuReady();
        }

        private bool TryActivateOpenGlViewport()
        {
            if (gpuViewport == null)
            {
                return false;
            }

            // Avoid early context initialization attempts before the form/control handles exist.
            if (!IsHandleCreated || !Visible || !viewportHost.IsHandleCreated)
            {
                return false;
            }

            // Make GPU viewport active/visible before context init so native handle creation can succeed.
            SetActiveViewport(gpuViewport);
            return gpuViewport.TryEnsureGpuReady();
        }

        private HardwareRenderBackend GetSelectedBackend()
        {
            switch (backendCombo.SelectedIndex)
            {
                case (int)HardwareRenderBackend.OpenGL:
                    return HardwareRenderBackend.OpenGL;
                default:
                    return HardwareRenderBackend.DirectX11;
            }
        }

        private void SetActiveViewport(Control viewport)
        {
            if (viewport == null)
            {
                return;
            }

            if (activeViewport == viewport)
            {
                return;
            }

            ViewCameraState state = CaptureCameraState(activeViewport);

            if (cpuViewport != null)
            {
                cpuViewport.Visible = ReferenceEquals(viewport, cpuViewport);
            }

            if (dx11Viewport != null)
            {
                dx11Viewport.Visible = ReferenceEquals(viewport, dx11Viewport);
            }

            if (gpuViewport != null)
            {
                gpuViewport.Visible = ReferenceEquals(viewport, gpuViewport);
            }

            activeViewport = viewport;
            activeViewport.BringToFront();
            ApplyCameraState(activeViewport, state);
        }

        private string GetActiveRendererStatusText()
        {
            if (ReferenceEquals(activeViewport, dx11Viewport))
            {
                return dx11Viewport != null
                    ? dx11Viewport.GetRendererStatusText()
                    : "Renderer: Software (CPU)";
            }

            if (ReferenceEquals(activeViewport, gpuViewport))
            {
                return gpuViewport != null
                    ? gpuViewport.GetRendererStatusText()
                    : "Renderer: Software (CPU)";
            }

            if (hardwareCheck != null && hardwareCheck.Checked)
            {
                HardwareRenderBackend selected = GetSelectedBackend();
                string reason = GetSoftwareFallbackReason(selected);
                return "Renderer: Software (CPU fallback - " + reason + ")";
            }

            return cpuViewport.GetRendererStatusText();
        }

        private string GetSoftwareFallbackReason(HardwareRenderBackend selectedBackend)
        {
            if (selectedBackend == HardwareRenderBackend.DirectX11)
            {
                string dxReason = GetDx11UnavailableReason();
                string glReason = GetOpenGlUnavailableReason();
                return "DirectX11 unavailable (" + dxReason + "); OpenGL unavailable (" + glReason + ")";
            }

            string openGlReason = GetOpenGlUnavailableReason();
            string dx11Reason = GetDx11UnavailableReason();
            return "OpenGL unavailable (" + openGlReason + "); DirectX11 unavailable (" + dx11Reason + ")";
        }

        private string GetDx11UnavailableReason()
        {
            if (dx11Viewport == null)
            {
                return string.IsNullOrWhiteSpace(dx11ViewportCreationError)
                    ? "DirectX11 control unavailable"
                    : dx11ViewportCreationError;
            }

            if (dx11Viewport.IsGpuReady)
            {
                return "ready";
            }

            string reason = dx11Viewport.GpuInitializationError;
            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = "DirectX11 initialization failed";
            }

            return reason;
        }

        private string GetOpenGlUnavailableReason()
        {
            if (gpuViewport == null)
            {
                return string.IsNullOrWhiteSpace(gpuViewportCreationError)
                    ? "OpenGL control unavailable"
                    : gpuViewportCreationError;
            }

            if (gpuViewport.IsGpuReady)
            {
                return "ready";
            }

            string reason = gpuViewport.GpuInitializationError;
            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = "OpenGL initialization failed";
            }

            return reason;
        }

        private void ResetActiveViewport()
        {
            if (dx11Viewport != null && ReferenceEquals(activeViewport, dx11Viewport))
            {
                dx11Viewport.ResetView();
            }
            else if (gpuViewport != null && ReferenceEquals(activeViewport, gpuViewport))
            {
                gpuViewport.ResetView();
            }
            else
            {
                cpuViewport.ResetView();
            }
        }

        private void InvalidateActiveViewport()
        {
            if (activeViewport != null)
            {
                activeViewport.Invalidate();
            }
        }

        private static ViewCameraState CaptureCameraState(Control viewport)
        {
            if (viewport == null)
            {
                return new ViewCameraState { IsValid = false };
            }

            ModelPreviewViewport cpu = viewport as ModelPreviewViewport;
            if (cpu != null)
            {
                return cpu.GetCameraState();
            }

            ModelPreviewGpuViewport gpu = viewport as ModelPreviewGpuViewport;
            if (gpu != null)
            {
                return gpu.GetCameraState();
            }

            ModelPreviewDx11Viewport dx11 = viewport as ModelPreviewDx11Viewport;
            if (dx11 != null)
            {
                return dx11.GetCameraState();
            }

            return new ViewCameraState { IsValid = false };
        }

        private static void ApplyCameraState(Control viewport, ViewCameraState state)
        {
            if (viewport == null || !state.IsValid)
            {
                return;
            }

            ModelPreviewViewport cpu = viewport as ModelPreviewViewport;
            if (cpu != null)
            {
                cpu.SetCameraState(state);
                return;
            }

            ModelPreviewGpuViewport gpu = viewport as ModelPreviewGpuViewport;
            if (gpu != null)
            {
                gpu.SetCameraState(state);
                return;
            }

            ModelPreviewDx11Viewport dx11 = viewport as ModelPreviewDx11Viewport;
            if (dx11 != null)
            {
                dx11.SetCameraState(state);
            }
        }

        private sealed class ModelPreviewDx11Viewport : Control
        {
            private struct DrawSubset
            {
                public int StartIndex;
                public int IndexCount;
                public int TextureIndex;
                public bool HasTexture;
                public bool UsesAlphaAsOpacity;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DxVertex
            {
                public SharpDX.Vector3 Position;
                public SharpDX.Vector3 Normal;
                public SharpDX.Vector2 TexCoord;
                public SharpDX.Vector4 Color;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct PerDrawConstants
            {
                public SharpDX.Matrix WorldViewProjection;
                public SharpDX.Matrix World;
                public SharpDX.Vector4 LightDirection;
                public SharpDX.Vector4 DrawFlags;
            }

            private readonly Vector3f[] vertices;
            private readonly Vector2f[] uvs;
            private readonly int[] indices;
            private readonly int[] triangleColors;
            private readonly int[] triangleTextureIndices;
            private readonly PreviewTextureData[] textures;
            private readonly Vector3f[] normals;
            private readonly Vector3f center;
            private readonly float radius;
            private readonly float defaultYaw;
            private readonly float defaultPitch;
            private readonly List<DrawSubset> opaqueSubsets = new List<DrawSubset>();
            private readonly List<DrawSubset> cutoutSubsets = new List<DrawSubset>();
            private readonly List<DrawSubset> transparentSubsets = new List<DrawSubset>();
            private readonly DxVertex[] renderVertices;
            private readonly int[] renderIndices;

            private float yaw;
            private float pitch;
            private float zoom;
            private bool isDragging;
            private System.Drawing.Point lastMouse;
            private const float PitchLimit = 1.35f;

            private bool dxResourcesInitialized;
            private string gpuInitializationError = string.Empty;

            private D3D11.Device device;
            private DXGI.SwapChain swapChain;
            private D3D11.DeviceContext deviceContext;
            private D3D11.Texture2D depthStencilBuffer;
            private D3D11.DepthStencilView depthStencilView;
            private D3D11.RenderTargetView renderTargetView;
            private D3D11.VertexShader vertexShader;
            private D3D11.PixelShader pixelShader;
            private D3D11.InputLayout inputLayout;
            private D3D11.Buffer vertexBuffer;
            private D3D11.Buffer indexBuffer;
            private D3D11.Buffer constantBuffer;
            private D3D11.SamplerState samplerState;
            private D3D11.RasterizerState rasterStateSolid;
            private D3D11.RasterizerState rasterStateWireframe;
            private D3D11.BlendState blendStateOpaque;
            private D3D11.BlendState blendStateAlpha;
            private D3D11.DepthStencilState depthStateWrite;
            private D3D11.DepthStencilState depthStateRead;
            private D3D11.ShaderResourceView[] textureViews = new D3D11.ShaderResourceView[0];
            private D3D11.Texture2D[] textureResources = new D3D11.Texture2D[0];

            public bool ShowWireframe { get; set; }
            public HardwareRenderBackend PreferredHardwareBackend { get; set; }
            public bool IsGpuReady { get { return dxResourcesInitialized; } }
            public string GpuInitializationError { get { return gpuInitializationError ?? string.Empty; } }

            public ModelPreviewDx11Viewport(ModelPreviewMeshData meshData)
            {
                vertices = meshData != null && meshData.Vertices != null ? meshData.Vertices : new Vector3f[0];
                uvs = meshData != null && meshData.UVs != null ? meshData.UVs : new Vector2f[0];
                indices = meshData != null && meshData.Indices != null ? meshData.Indices : new int[0];
                triangleColors = meshData != null && meshData.TriangleColors != null ? meshData.TriangleColors : new int[0];
                triangleTextureIndices = meshData != null && meshData.TriangleTextureIndices != null ? meshData.TriangleTextureIndices : new int[0];
                textures = meshData != null && meshData.Textures != null ? meshData.Textures : new PreviewTextureData[0];
                normals = ModelPreviewViewport.ComputeVertexNormals(vertices, indices);
                ModelPreviewViewport.ComputeBounds(vertices, out center, out radius);
                BuildRenderData(out renderVertices, out renderIndices);

                defaultYaw = 0f;
                defaultPitch = 0f;
                yaw = defaultYaw;
                pitch = defaultPitch;
                zoom = 1.0f;
                PreferredHardwareBackend = HardwareRenderBackend.DirectX11;

                SetStyle(ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.Opaque
                    | ControlStyles.UserPaint
                    | ControlStyles.ResizeRedraw, true);

                BackColor = Color.FromArgb(206, 206, 206);
                TabStop = false;

                MouseDown += OnMouseDown;
                MouseUp += OnMouseUp;
                MouseMove += OnMouseMove;
                MouseWheel += OnMouseWheel;
                MouseDoubleClick += (s, e) => ResetView();
            }

            public bool TryEnsureGpuReady()
            {
                if (dxResourcesInitialized)
                {
                    return true;
                }

                try
                {
                    if (DesignMode)
                    {
                        gpuInitializationError = "Design mode";
                        return false;
                    }

                    if (!IsHandleCreated || Handle == IntPtr.Zero)
                    {
                        gpuInitializationError = "DirectX11 pending control handle";
                        return false;
                    }

                    InitializeDxResources();
                    dxResourcesInitialized = true;
                    gpuInitializationError = string.Empty;
                }
                catch (Exception ex)
                {
                    dxResourcesInitialized = false;
                    gpuInitializationError = ex.GetType().Name + ": " + ex.Message;
                    SafeDisposeDeviceResources();
                }

                return dxResourcesInitialized;
            }

            public string GetRendererStatusText()
            {
                if (!dxResourcesInitialized)
                {
                    string reason = string.IsNullOrWhiteSpace(gpuInitializationError)
                        ? "DirectX11 not available"
                        : gpuInitializationError;
                    return "Renderer: Software (CPU fallback - " + reason + ")";
                }

                if (PreferredHardwareBackend == HardwareRenderBackend.OpenGL)
                {
                    return "Renderer: DirectX11 (GPU fallback for OpenGL)";
                }

                return "Renderer: DirectX11 (GPU)";
            }

            public void ResetView()
            {
                yaw = defaultYaw;
                pitch = defaultPitch;
                zoom = 1.0f;
                Invalidate();
            }

            public ViewCameraState GetCameraState()
            {
                return new ViewCameraState
                {
                    Yaw = yaw,
                    Pitch = pitch,
                    Zoom = zoom,
                    IsValid = true
                };
            }

            public void SetCameraState(ViewCameraState state)
            {
                if (!state.IsValid)
                {
                    return;
                }

                yaw = state.Yaw;
                pitch = Math.Max(-PitchLimit, Math.Min(PitchLimit, state.Pitch));
                zoom = Math.Max(0.22f, Math.Min(4.5f, state.Zoom));
                Invalidate();
            }

            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
                // Scene clear handles background.
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (!TryEnsureGpuReady())
                {
                    e.Graphics.Clear(Color.FromArgb(206, 206, 206));
                    using (Brush brush = new SolidBrush(Color.FromArgb(80, 80, 80)))
                    {
                        e.Graphics.DrawString("DirectX11 init failed. Using CPU fallback.", Font, brush, new PointF(16f, 16f));
                    }
                    return;
                }

                RenderScene();
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                if (!dxResourcesInitialized)
                {
                    return;
                }

                try
                {
                    ResizeSwapChainBuffers();
                }
                catch (Exception ex)
                {
                    gpuInitializationError = ex.GetType().Name + ": " + ex.Message;
                    dxResourcesInitialized = false;
                    SafeDisposeDeviceResources();
                }
            }

            protected override void OnHandleDestroyed(EventArgs e)
            {
                SafeDisposeDeviceResources();
                base.OnHandleDestroyed(e);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    SafeDisposeDeviceResources();
                }

                base.Dispose(disposing);
            }

            private void InitializeDxResources()
            {
                SafeDisposeDeviceResources();

                DXGI.SwapChainDescription swapChainDesc = new DXGI.SwapChainDescription();
                swapChainDesc.BufferCount = 1;
                swapChainDesc.Flags = DXGI.SwapChainFlags.None;
                swapChainDesc.IsWindowed = true;
                swapChainDesc.ModeDescription = new DXGI.ModeDescription(
                    Math.Max(1, Width),
                    Math.Max(1, Height),
                    new DXGI.Rational(60, 1),
                    DXGI.Format.B8G8R8A8_UNorm);
                swapChainDesc.OutputHandle = Handle;
                swapChainDesc.SampleDescription = new DXGI.SampleDescription(1, 0);
                swapChainDesc.SwapEffect = DXGI.SwapEffect.Discard;
                swapChainDesc.Usage = DXGI.Usage.RenderTargetOutput;

                D3D11.Device.CreateWithSwapChain(
                    SharpDX.Direct3D.DriverType.Hardware,
                    D3D11.DeviceCreationFlags.BgraSupport,
                    new[]
                    {
                        SharpDX.Direct3D.FeatureLevel.Level_11_1,
                        SharpDX.Direct3D.FeatureLevel.Level_11_0,
                        SharpDX.Direct3D.FeatureLevel.Level_10_1,
                        SharpDX.Direct3D.FeatureLevel.Level_10_0
                    },
                    swapChainDesc,
                    out device,
                    out swapChain);

                deviceContext = device.ImmediateContext;

                using (DXGI.Factory factory = swapChain.GetParent<DXGI.Factory>())
                {
                    factory.MakeWindowAssociation(Handle, DXGI.WindowAssociationFlags.IgnoreAltEnter);
                }

                CreateRenderTargets();
                CreateShaders();
                CreatePipelineState();
                CreateGeometryBuffers();
                CreateTextureResources();
            }

            private void ResizeSwapChainBuffers()
            {
                if (swapChain == null || deviceContext == null)
                {
                    return;
                }

                deviceContext.OutputMerger.SetRenderTargets((D3D11.DepthStencilView)null, (D3D11.RenderTargetView)null);
                SharpDX.Utilities.Dispose(ref renderTargetView);
                SharpDX.Utilities.Dispose(ref depthStencilView);
                SharpDX.Utilities.Dispose(ref depthStencilBuffer);

                swapChain.ResizeBuffers(
                    1,
                    Math.Max(1, Width),
                    Math.Max(1, Height),
                    DXGI.Format.B8G8R8A8_UNorm,
                    DXGI.SwapChainFlags.None);

                CreateRenderTargets();
            }

            private void CreateRenderTargets()
            {
                using (D3D11.Texture2D backBuffer = D3D11.Texture2D.FromSwapChain<D3D11.Texture2D>(swapChain, 0))
                {
                    renderTargetView = new D3D11.RenderTargetView(device, backBuffer);
                }

                D3D11.Texture2DDescription depthDesc = new D3D11.Texture2DDescription();
                depthDesc.Width = Math.Max(1, Width);
                depthDesc.Height = Math.Max(1, Height);
                depthDesc.ArraySize = 1;
                depthDesc.MipLevels = 1;
                depthDesc.Format = DXGI.Format.D24_UNorm_S8_UInt;
                depthDesc.SampleDescription = new DXGI.SampleDescription(1, 0);
                depthDesc.Usage = D3D11.ResourceUsage.Default;
                depthDesc.BindFlags = D3D11.BindFlags.DepthStencil;
                depthDesc.CpuAccessFlags = D3D11.CpuAccessFlags.None;
                depthDesc.OptionFlags = D3D11.ResourceOptionFlags.None;

                depthStencilBuffer = new D3D11.Texture2D(device, depthDesc);
                depthStencilView = new D3D11.DepthStencilView(device, depthStencilBuffer);
            }

            private void CreateShaders()
            {
                const string shaderCode = @"
cbuffer PerDraw : register(b0)
{
    matrix WorldViewProjection;
    matrix World;
    float4 LightDirection;
    float4 DrawFlags;
};

struct VS_INPUT
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float3 Normal : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output;
    output.Position = mul(float4(input.Position, 1.0f), WorldViewProjection);
    output.Normal = mul(float4(input.Normal, 0.0f), World).xyz;
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    return output;
}

Texture2D DiffuseTexture : register(t0);
SamplerState DiffuseSampler : register(s0);

float4 PSMain(PS_INPUT input) : SV_Target
{
    float useTexture = DrawFlags.x;
    float alphaMin = DrawFlags.y;
    float alphaMax = DrawFlags.z;
    float useAlphaAsOpacity = DrawFlags.w;
    float4 baseColor = input.Color;
    if (useTexture > 0.5f)
    {
        baseColor = DiffuseTexture.Sample(DiffuseSampler, input.TexCoord);
        if (useAlphaAsOpacity > 0.5f)
        {
            clip(baseColor.a - alphaMin);
            clip(alphaMax - baseColor.a);
        }
        else
        {
            // Many FW textures use alpha for gloss/spec data, not opacity.
            baseColor.a = 1.0f;
        }
    }
    else if (useAlphaAsOpacity > 0.5f)
    {
        clip(baseColor.a - alphaMin);
        clip(alphaMax - baseColor.a);
    }

    float3 n = normalize(input.Normal);
    float3 l = normalize(-LightDirection.xyz);
    float ndotl = saturate(abs(dot(n, l)));
    float lighting = saturate(0.62f + (ndotl * 0.58f));
    return float4(baseColor.rgb * lighting, baseColor.a);
}";

                using (SharpDX.D3DCompiler.ShaderBytecode vsBytecode = SharpDX.D3DCompiler.ShaderBytecode.Compile(shaderCode, "VSMain", "vs_4_0", SharpDX.D3DCompiler.ShaderFlags.OptimizationLevel3))
                using (SharpDX.D3DCompiler.ShaderBytecode psBytecode = SharpDX.D3DCompiler.ShaderBytecode.Compile(shaderCode, "PSMain", "ps_4_0", SharpDX.D3DCompiler.ShaderFlags.OptimizationLevel3))
                {
                    vertexShader = new D3D11.VertexShader(device, vsBytecode);
                    pixelShader = new D3D11.PixelShader(device, psBytecode);
                    inputLayout = new D3D11.InputLayout(
                        device,
                        SharpDX.D3DCompiler.ShaderSignature.GetInputSignature(vsBytecode),
                        new[]
                        {
                            new D3D11.InputElement("POSITION", 0, DXGI.Format.R32G32B32_Float, 0, 0),
                            new D3D11.InputElement("NORMAL", 0, DXGI.Format.R32G32B32_Float, 12, 0),
                            new D3D11.InputElement("TEXCOORD", 0, DXGI.Format.R32G32_Float, 24, 0),
                            new D3D11.InputElement("COLOR", 0, DXGI.Format.R32G32B32A32_Float, 32, 0)
                        });
                }
            }

            private void CreatePipelineState()
            {
                D3D11.RasterizerStateDescription solidDesc = new D3D11.RasterizerStateDescription();
                solidDesc.CullMode = D3D11.CullMode.None;
                solidDesc.FillMode = D3D11.FillMode.Solid;
                solidDesc.IsFrontCounterClockwise = false;
                solidDesc.IsDepthClipEnabled = true;
                rasterStateSolid = new D3D11.RasterizerState(device, solidDesc);

                D3D11.RasterizerStateDescription wireDesc = new D3D11.RasterizerStateDescription();
                wireDesc.CullMode = D3D11.CullMode.None;
                wireDesc.FillMode = D3D11.FillMode.Wireframe;
                wireDesc.IsFrontCounterClockwise = false;
                wireDesc.IsDepthClipEnabled = true;
                rasterStateWireframe = new D3D11.RasterizerState(device, wireDesc);

                D3D11.BlendStateDescription blendOpaqueDesc = new D3D11.BlendStateDescription();
                blendOpaqueDesc.AlphaToCoverageEnable = false;
                blendOpaqueDesc.IndependentBlendEnable = false;
                blendOpaqueDesc.RenderTarget[0].IsBlendEnabled = false;
                blendOpaqueDesc.RenderTarget[0].RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All;
                blendStateOpaque = new D3D11.BlendState(device, blendOpaqueDesc);

                D3D11.BlendStateDescription blendAlphaDesc = new D3D11.BlendStateDescription();
                blendAlphaDesc.AlphaToCoverageEnable = false;
                blendAlphaDesc.IndependentBlendEnable = false;
                blendAlphaDesc.RenderTarget[0].IsBlendEnabled = true;
                blendAlphaDesc.RenderTarget[0].SourceBlend = D3D11.BlendOption.SourceAlpha;
                blendAlphaDesc.RenderTarget[0].DestinationBlend = D3D11.BlendOption.InverseSourceAlpha;
                blendAlphaDesc.RenderTarget[0].BlendOperation = D3D11.BlendOperation.Add;
                blendAlphaDesc.RenderTarget[0].SourceAlphaBlend = D3D11.BlendOption.One;
                blendAlphaDesc.RenderTarget[0].DestinationAlphaBlend = D3D11.BlendOption.InverseSourceAlpha;
                blendAlphaDesc.RenderTarget[0].AlphaBlendOperation = D3D11.BlendOperation.Add;
                blendAlphaDesc.RenderTarget[0].RenderTargetWriteMask = D3D11.ColorWriteMaskFlags.All;
                blendStateAlpha = new D3D11.BlendState(device, blendAlphaDesc);

                D3D11.DepthStencilStateDescription depthWriteDesc = new D3D11.DepthStencilStateDescription();
                depthWriteDesc.IsDepthEnabled = true;
                depthWriteDesc.DepthComparison = D3D11.Comparison.LessEqual;
                depthWriteDesc.DepthWriteMask = D3D11.DepthWriteMask.All;
                depthWriteDesc.IsStencilEnabled = false;
                depthStateWrite = new D3D11.DepthStencilState(device, depthWriteDesc);

                D3D11.DepthStencilStateDescription depthReadDesc = new D3D11.DepthStencilStateDescription();
                depthReadDesc.IsDepthEnabled = true;
                depthReadDesc.DepthComparison = D3D11.Comparison.LessEqual;
                depthReadDesc.DepthWriteMask = D3D11.DepthWriteMask.Zero;
                depthReadDesc.IsStencilEnabled = false;
                depthStateRead = new D3D11.DepthStencilState(device, depthReadDesc);

                D3D11.SamplerStateDescription samplerDesc = new D3D11.SamplerStateDescription();
                samplerDesc.Filter = D3D11.Filter.Anisotropic;
                samplerDesc.AddressU = D3D11.TextureAddressMode.Wrap;
                samplerDesc.AddressV = D3D11.TextureAddressMode.Wrap;
                samplerDesc.AddressW = D3D11.TextureAddressMode.Wrap;
                samplerDesc.MipLodBias = 0f;
                samplerDesc.MaximumAnisotropy = 8;
                samplerDesc.ComparisonFunction = D3D11.Comparison.Never;
                samplerDesc.MinimumLod = 0f;
                samplerDesc.MaximumLod = float.MaxValue;
                samplerState = new D3D11.SamplerState(device, samplerDesc);

                constantBuffer = new D3D11.Buffer(
                    device,
                    SharpDX.Utilities.SizeOf<PerDrawConstants>(),
                    D3D11.ResourceUsage.Default,
                    D3D11.BindFlags.ConstantBuffer,
                    D3D11.CpuAccessFlags.None,
                    D3D11.ResourceOptionFlags.None,
                    0);
            }

            private void CreateGeometryBuffers()
            {
                if (renderVertices == null || renderVertices.Length == 0 || renderIndices == null || renderIndices.Length == 0)
                {
                    return;
                }

                vertexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.VertexBuffer, renderVertices);
                indexBuffer = D3D11.Buffer.Create(device, D3D11.BindFlags.IndexBuffer, renderIndices);
            }

            private void CreateTextureResources()
            {
                if (textures == null || textures.Length == 0)
                {
                    textureViews = new D3D11.ShaderResourceView[0];
                    textureResources = new D3D11.Texture2D[0];
                    return;
                }

                textureViews = new D3D11.ShaderResourceView[textures.Length];
                textureResources = new D3D11.Texture2D[textures.Length];
                for (int i = 0; i < textures.Length; i++)
                {
                    PreviewTextureData texture = textures[i];
                    if (texture == null || !texture.IsValid || texture.Pixels == null || texture.Pixels.Length == 0)
                    {
                        textureViews[i] = null;
                        textureResources[i] = null;
                        continue;
                    }

                    D3D11.Texture2DDescription desc = new D3D11.Texture2DDescription();
                    desc.Width = texture.Width;
                    desc.Height = texture.Height;
                    desc.ArraySize = 1;
                    desc.MipLevels = 1;
                    desc.Format = DXGI.Format.B8G8R8A8_UNorm;
                    desc.SampleDescription = new DXGI.SampleDescription(1, 0);
                    desc.Usage = D3D11.ResourceUsage.Immutable;
                    desc.BindFlags = D3D11.BindFlags.ShaderResource;
                    desc.CpuAccessFlags = D3D11.CpuAccessFlags.None;
                    desc.OptionFlags = D3D11.ResourceOptionFlags.None;

                    SharpDX.DataStream dataStream = new SharpDX.DataStream(texture.Pixels.Length * sizeof(int), true, true);
                    try
                    {
                        dataStream.WriteRange(texture.Pixels);
                        dataStream.Position = 0;
                        SharpDX.DataRectangle dataRect = new SharpDX.DataRectangle(dataStream.DataPointer, texture.Width * sizeof(int));
                        D3D11.Texture2D texResource = new D3D11.Texture2D(device, desc, dataRect);
                        textureResources[i] = texResource;
                        textureViews[i] = new D3D11.ShaderResourceView(device, texResource);
                    }
                    finally
                    {
                        dataStream.Dispose();
                    }
                }
            }

            private void RenderScene()
            {
                if (deviceContext == null || swapChain == null || renderTargetView == null || depthStencilView == null)
                {
                    return;
                }

                int width = Math.Max(1, Width);
                int height = Math.Max(1, Height);

                deviceContext.Rasterizer.SetViewport(0f, 0f, width, height, 0f, 1f);
                deviceContext.OutputMerger.SetRenderTargets(depthStencilView, renderTargetView);
                deviceContext.ClearRenderTargetView(renderTargetView, new SharpDX.Mathematics.Interop.RawColor4(
                    206f / 255f,
                    206f / 255f,
                    206f / 255f,
                    1f));
                deviceContext.ClearDepthStencilView(depthStencilView, D3D11.DepthStencilClearFlags.Depth, 1f, 0);

                if (renderVertices == null || renderVertices.Length == 0
                    || renderIndices == null || renderIndices.Length == 0
                    || radius <= 0.0001f
                    || vertexBuffer == null
                    || indexBuffer == null)
                {
                    swapChain.Present(1, DXGI.PresentFlags.None);
                    return;
                }

                float aspect = width / (float)Math.Max(1, height);
                SharpDX.Matrix projection = SharpDX.Matrix.PerspectiveFovRH((float)(Math.PI / 4.0), aspect, 0.05f, 200.0f);
                SharpDX.Matrix world = SharpDX.Matrix.Translation(-center.X, -center.Y, -center.Z)
                    * SharpDX.Matrix.Scaling(1f / radius)
                    * SharpDX.Matrix.RotationY(yaw)
                    * SharpDX.Matrix.RotationX(pitch);
                float cameraDistance = 3.2f / Math.Max(0.15f, zoom);
                world *= SharpDX.Matrix.Translation(0f, 0f, -cameraDistance);
                SharpDX.Matrix worldViewProjection = world * projection;

                deviceContext.InputAssembler.InputLayout = inputLayout;
                deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
                deviceContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer, SharpDX.Utilities.SizeOf<DxVertex>(), 0));
                deviceContext.InputAssembler.SetIndexBuffer(indexBuffer, DXGI.Format.R32_UInt, 0);

                deviceContext.VertexShader.Set(vertexShader);
                deviceContext.VertexShader.SetConstantBuffer(0, constantBuffer);
                deviceContext.PixelShader.Set(pixelShader);
                deviceContext.PixelShader.SetConstantBuffer(0, constantBuffer);
                deviceContext.PixelShader.SetSampler(0, samplerState);
                deviceContext.Rasterizer.State = ShowWireframe ? rasterStateWireframe : rasterStateSolid;

                const float alphaEpsilon = 0.0001f;
                const float cutoutAlphaThreshold = 0.01f;
                DrawSubsets(opaqueSubsets, world, worldViewProjection, false, 0.0f, 1.0f + alphaEpsilon);
                DrawSubsets(cutoutSubsets, world, worldViewProjection, false, cutoutAlphaThreshold, 1.0f + alphaEpsilon);
                DrawSubsets(transparentSubsets, world, worldViewProjection, false, 0.98f, 1.0f + alphaEpsilon);
                DrawSubsets(transparentSubsets, world, worldViewProjection, true, 0.03f, 0.98f - alphaEpsilon);

                swapChain.Present(1, DXGI.PresentFlags.None);
            }

            private void DrawSubsets(
                List<DrawSubset> subsets,
                SharpDX.Matrix world,
                SharpDX.Matrix worldViewProjection,
                bool alphaBlend,
                float alphaMin,
                float alphaMax)
            {
                if (subsets == null || subsets.Count == 0)
                {
                    return;
                }

                deviceContext.OutputMerger.SetBlendState(alphaBlend ? blendStateAlpha : blendStateOpaque);
                deviceContext.OutputMerger.SetDepthStencilState(alphaBlend ? depthStateRead : depthStateWrite);

                SharpDX.Vector4 lightDirection = new SharpDX.Vector4(0.35f, 0.45f, -0.82f, 0f);
                for (int i = 0; i < subsets.Count; i++)
                {
                    DrawSubset subset = subsets[i];
                    if (subset.IndexCount <= 0)
                    {
                        continue;
                    }

                    D3D11.ShaderResourceView textureView = null;
                    if (subset.HasTexture && subset.TextureIndex >= 0 && subset.TextureIndex < textureViews.Length)
                    {
                        textureView = textureViews[subset.TextureIndex];
                    }

                    PerDrawConstants constants = new PerDrawConstants();
                    constants.WorldViewProjection = SharpDX.Matrix.Transpose(worldViewProjection);
                    constants.World = SharpDX.Matrix.Transpose(world);
                    constants.LightDirection = lightDirection;
                    constants.DrawFlags = new SharpDX.Vector4(
                        textureView != null ? 1f : 0f,
                        alphaMin,
                        alphaMax,
                        subset.UsesAlphaAsOpacity ? 1f : 0f);
                    deviceContext.UpdateSubresource(ref constants, constantBuffer);

                    deviceContext.PixelShader.SetShaderResource(0, textureView);
                    deviceContext.DrawIndexed(subset.IndexCount, subset.StartIndex, 0);
                }
            }

            private void BuildRenderData(out DxVertex[] outVertices, out int[] outIndices)
            {
                opaqueSubsets.Clear();
                cutoutSubsets.Clear();
                transparentSubsets.Clear();
                if (vertices == null || indices == null || indices.Length < 3)
                {
                    outVertices = new DxVertex[0];
                    outIndices = new int[0];
                    return;
                }

                Dictionary<int, List<DxVertex>> opaqueBuckets = new Dictionary<int, List<DxVertex>>();
                Dictionary<int, List<DxVertex>> cutoutBuckets = new Dictionary<int, List<DxVertex>>();
                Dictionary<int, List<DxVertex>> transparentBuckets = new Dictionary<int, List<DxVertex>>();

                int triangleCount = indices.Length / 3;
                for (int tri = 0; tri < triangleCount; tri++)
                {
                    int baseIndex = tri * 3;
                    int i0 = indices[baseIndex];
                    int i1 = indices[baseIndex + 1];
                    int i2 = indices[baseIndex + 2];
                    if (i0 < 0 || i1 < 0 || i2 < 0
                        || i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length)
                    {
                        continue;
                    }

                    int textureIndex = tri < triangleTextureIndices.Length ? triangleTextureIndices[tri] : -1;
                    int colorArgb = tri < triangleColors.Length
                        ? triangleColors[tri]
                        : Color.FromArgb(255, 200, 210, 220).ToArgb();

                    bool hasTexture = textureIndex >= 0
                        && textureIndex < textures.Length
                        && textures[textureIndex] != null
                        && textures[textureIndex].IsValid;
                    bool usesAlphaAsOpacity = hasTexture && textures[textureIndex].UsesAlphaAsOpacity;
                    bool transparent = hasTexture
                        ? textures[textureIndex].HasTransparency
                        : ((colorArgb >> 24) & 0xFF) < 245;

                    int bucketKey = hasTexture ? textureIndex : -1;
                    Dictionary<int, List<DxVertex>> target;
                    if (transparent)
                    {
                        target = transparentBuckets;
                    }
                    else if (usesAlphaAsOpacity)
                    {
                        target = cutoutBuckets;
                    }
                    else
                    {
                        target = opaqueBuckets;
                    }
                    if (!target.TryGetValue(bucketKey, out List<DxVertex> bucket))
                    {
                        bucket = new List<DxVertex>();
                        target[bucketKey] = bucket;
                    }

                    SharpDX.Vector4 color;
                    if (hasTexture)
                    {
                        color = new SharpDX.Vector4(1f, 1f, 1f, 1f);
                    }
                    else
                    {
                        Color c = Color.FromArgb(colorArgb);
                        color = new SharpDX.Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
                    }

                    AddVertex(bucket, i0, color);
                    AddVertex(bucket, i1, color);
                    AddVertex(bucket, i2, color);
                }

                List<DxVertex> flatVertices = new List<DxVertex>(Math.Max(32, indices.Length));
                List<int> flatIndices = new List<int>(Math.Max(32, indices.Length));
                AppendBuckets(opaqueBuckets, flatVertices, flatIndices, opaqueSubsets, false);
                AppendBuckets(cutoutBuckets, flatVertices, flatIndices, cutoutSubsets, true);
                AppendBuckets(transparentBuckets, flatVertices, flatIndices, transparentSubsets, true);

                outVertices = flatVertices.ToArray();
                outIndices = flatIndices.ToArray();
            }

            private void AddVertex(List<DxVertex> bucket, int sourceIndex, SharpDX.Vector4 color)
            {
                Vector3f v = vertices[sourceIndex];
                Vector3f n = sourceIndex < normals.Length ? normals[sourceIndex] : new Vector3f(0f, 1f, 0f);
                Vector2f uv = sourceIndex < uvs.Length ? uvs[sourceIndex] : new Vector2f(0f, 0f);
                DxVertex vertex = new DxVertex();
                vertex.Position = new SharpDX.Vector3(v.X, v.Y, v.Z);
                vertex.Normal = new SharpDX.Vector3(n.X, n.Y, n.Z);
                vertex.TexCoord = new SharpDX.Vector2(uv.X, uv.Y);
                vertex.Color = color;
                bucket.Add(vertex);
            }

            private static void AppendBuckets(
                Dictionary<int, List<DxVertex>> buckets,
                List<DxVertex> flatVertices,
                List<int> flatIndices,
                List<DrawSubset> targetSubsets,
                bool usesAlphaAsOpacity)
            {
                if (buckets == null || buckets.Count == 0)
                {
                    return;
                }

                List<int> keys = new List<int>(buckets.Keys);
                keys.Sort();
                for (int k = 0; k < keys.Count; k++)
                {
                    int key = keys[k];
                    List<DxVertex> bucket = buckets[key];
                    if (bucket == null || bucket.Count == 0)
                    {
                        continue;
                    }

                    int baseVertex = flatVertices.Count;
                    int startIndex = flatIndices.Count;
                    for (int i = 0; i < bucket.Count; i++)
                    {
                        flatVertices.Add(bucket[i]);
                        flatIndices.Add(baseVertex + i);
                    }

                    DrawSubset subset = new DrawSubset();
                    subset.StartIndex = startIndex;
                    subset.IndexCount = bucket.Count;
                    subset.TextureIndex = key;
                    subset.HasTexture = key >= 0;
                    subset.UsesAlphaAsOpacity = usesAlphaAsOpacity && key >= 0;
                    targetSubsets.Add(subset);
                }
            }

            private void SafeDisposeDeviceResources()
            {
                if (deviceContext != null)
                {
                    try
                    {
                        deviceContext.ClearState();
                        deviceContext.Flush();
                    }
                    catch
                    {
                    }
                }

                if (textureViews != null)
                {
                    for (int i = 0; i < textureViews.Length; i++)
                    {
                        SharpDX.Utilities.Dispose(ref textureViews[i]);
                    }
                }

                if (textureResources != null)
                {
                    for (int i = 0; i < textureResources.Length; i++)
                    {
                        SharpDX.Utilities.Dispose(ref textureResources[i]);
                    }
                }

                SharpDX.Utilities.Dispose(ref blendStateOpaque);
                SharpDX.Utilities.Dispose(ref blendStateAlpha);
                SharpDX.Utilities.Dispose(ref depthStateWrite);
                SharpDX.Utilities.Dispose(ref depthStateRead);
                SharpDX.Utilities.Dispose(ref rasterStateSolid);
                SharpDX.Utilities.Dispose(ref rasterStateWireframe);
                SharpDX.Utilities.Dispose(ref samplerState);
                SharpDX.Utilities.Dispose(ref constantBuffer);
                SharpDX.Utilities.Dispose(ref vertexBuffer);
                SharpDX.Utilities.Dispose(ref indexBuffer);
                SharpDX.Utilities.Dispose(ref inputLayout);
                SharpDX.Utilities.Dispose(ref vertexShader);
                SharpDX.Utilities.Dispose(ref pixelShader);
                SharpDX.Utilities.Dispose(ref renderTargetView);
                SharpDX.Utilities.Dispose(ref depthStencilView);
                SharpDX.Utilities.Dispose(ref depthStencilBuffer);
                SharpDX.Utilities.Dispose(ref deviceContext);
                SharpDX.Utilities.Dispose(ref swapChain);
                SharpDX.Utilities.Dispose(ref device);

                textureViews = new D3D11.ShaderResourceView[0];
                textureResources = new D3D11.Texture2D[0];
                dxResourcesInitialized = false;
            }

            private void OnMouseDown(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left)
                {
                    return;
                }

                isDragging = true;
                lastMouse = e.Location;
                Cursor = Cursors.SizeAll;
                Focus();
            }

            private void OnMouseUp(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left)
                {
                    return;
                }

                isDragging = false;
                Cursor = Cursors.Default;
            }

            private void OnMouseMove(object sender, MouseEventArgs e)
            {
                if (!isDragging)
                {
                    return;
                }

                int dx = e.X - lastMouse.X;
                int dy = e.Y - lastMouse.Y;
                float sensitivity = (ModifierKeys & Keys.Shift) == Keys.Shift ? 0.0017f : 0.0036f;
                yaw += dx * sensitivity;
                pitch += dy * sensitivity;
                pitch = Math.Max(-PitchLimit, Math.Min(PitchLimit, pitch));
                lastMouse = e.Location;
                Invalidate();
            }

            private void OnMouseWheel(object sender, MouseEventArgs e)
            {
                float factor = e.Delta > 0 ? 1.1f : (1f / 1.1f);
                zoom *= factor;
                zoom = Math.Max(0.22f, Math.Min(4.5f, zoom));
                Invalidate();
            }
        }

        private sealed class ModelPreviewGpuViewport : GLControl
        {
            private struct TriangleGpuItem
            {
                public int I0;
                public int I1;
                public int I2;
                public int TextureIndex;
                public int ColorArgb;
                public bool UsesAlphaAsOpacity;
                public bool Transparent;
            }

            private readonly Vector3f[] vertices;
            private readonly Vector2f[] uvs;
            private readonly int[] indices;
            private readonly int[] triangleColors;
            private readonly int[] triangleTextureIndices;
            private readonly PreviewTextureData[] textures;
            private readonly Vector3f[] normals;
            private readonly Vector3f center;
            private readonly float radius;
            private readonly float defaultYaw;
            private readonly float defaultPitch;
            private readonly List<TriangleGpuItem> opaqueTriangles = new List<TriangleGpuItem>();
            private readonly List<TriangleGpuItem> cutoutTriangles = new List<TriangleGpuItem>();
            private readonly List<TriangleGpuItem> transparentTriangles = new List<TriangleGpuItem>();
            private static readonly object toolkitInitSync = new object();
            private static bool toolkitInitialized;
            private const float PitchLimit = 1.35f;

            private float yaw;
            private float pitch;
            private float zoom;
            private bool isDragging;
            private System.Drawing.Point lastMouse;
            private bool glResourcesInitialized;
            private string gpuInitializationError = string.Empty;
            private int[] textureHandles = new int[0];

            public bool ShowWireframe { get; set; }
            public HardwareRenderBackend PreferredHardwareBackend { get; set; }
            public bool IsGpuReady { get { return glResourcesInitialized; } }
            public string GpuInitializationError { get { return gpuInitializationError ?? string.Empty; } }

            public ModelPreviewGpuViewport(ModelPreviewMeshData meshData)
                : base()
            {
                vertices = meshData != null && meshData.Vertices != null ? meshData.Vertices : new Vector3f[0];
                uvs = meshData != null && meshData.UVs != null ? meshData.UVs : new Vector2f[0];
                indices = meshData != null && meshData.Indices != null ? meshData.Indices : new int[0];
                triangleColors = meshData != null && meshData.TriangleColors != null ? meshData.TriangleColors : new int[0];
                triangleTextureIndices = meshData != null && meshData.TriangleTextureIndices != null ? meshData.TriangleTextureIndices : new int[0];
                textures = meshData != null && meshData.Textures != null ? meshData.Textures : new PreviewTextureData[0];
                normals = ModelPreviewViewport.ComputeVertexNormals(vertices, indices);
                ModelPreviewViewport.ComputeBounds(vertices, out center, out radius);

                defaultYaw = 0f;
                defaultPitch = 0f;
                yaw = defaultYaw;
                pitch = defaultPitch;
                zoom = 1.0f;
                PreferredHardwareBackend = HardwareRenderBackend.DirectX11;

                BackColor = Color.Black;
                TabStop = false;

                BuildTriangleLists();
                MouseDown += OnMouseDown;
                MouseUp += OnMouseUp;
                MouseMove += OnMouseMove;
                MouseWheel += OnMouseWheel;
                MouseDoubleClick += (s, e) => ResetView();
            }

            public bool TryEnsureGpuReady()
            {
                if (glResourcesInitialized)
                {
                    return true;
                }
                string stage = "start";
                try
                {
                    if (DesignMode)
                    {
                        gpuInitializationError = "Design mode";
                        return false;
                    }

                    if (Parent == null || !Parent.IsHandleCreated)
                    {
                        gpuInitializationError = "OpenGL context pending parent handle";
                        return false;
                    }

                    stage = "Toolkit.Init";
                    EnsureToolkitInitialized();

                    if (!IsHandleCreated || Handle == IntPtr.Zero)
                    {
                        stage = "CreateControl";
                        if (!Visible)
                        {
                            Visible = true;
                        }

                        CreateControl();
                        IntPtr nativeHandle = Handle; // Force native handle creation path.
                    }

                    if (!IsHandleCreated || Handle == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("OpenGL control handle unavailable.");
                    }

                    stage = "MakeCurrent";
                    MakeCurrent();
                    if (!HasValidContext || Context == null)
                    {
                        throw new InvalidOperationException("OpenGL context is invalid after MakeCurrent.");
                    }

                    stage = "InitializeGlState";
                    InitializeGlState();
                    stage = "UploadTextures";
                    UploadTextures();
                    glResourcesInitialized = true;
                    gpuInitializationError = string.Empty;
                }
                catch (Exception ex)
                {
                    glResourcesInitialized = false;
                    gpuInitializationError = ex.GetType().Name + ": " + ex.Message + " [" + stage + "]";
                }

                return glResourcesInitialized;
            }

            private static void EnsureToolkitInitialized()
            {
                if (toolkitInitialized)
                {
                    return;
                }

                lock (toolkitInitSync)
                {
                    if (toolkitInitialized)
                    {
                        return;
                    }

                    Toolkit.Init();
                    toolkitInitialized = true;
                }
            }

            public string GetRendererStatusText()
            {
                if (!glResourcesInitialized)
                {
                    string reason = string.IsNullOrWhiteSpace(gpuInitializationError)
                        ? "OpenGL not available"
                        : gpuInitializationError;
                    return "Renderer: Software (CPU fallback - " + reason + ")";
                }

                string profile;
                switch (PreferredHardwareBackend)
                {
                    case HardwareRenderBackend.OpenGL:
                        profile = "OpenGL profile";
                        break;
                    default:
                        profile = "DirectX11 profile";
                        break;
                }

                return "Renderer: OpenGL (GPU) - " + profile;
            }

            public void ResetView()
            {
                yaw = defaultYaw;
                pitch = defaultPitch;
                zoom = 1.0f;
                Invalidate();
            }

            public ViewCameraState GetCameraState()
            {
                return new ViewCameraState
                {
                    Yaw = yaw,
                    Pitch = pitch,
                    Zoom = zoom,
                    IsValid = true
                };
            }

            public void SetCameraState(ViewCameraState state)
            {
                if (!state.IsValid)
                {
                    return;
                }

                yaw = state.Yaw;
                pitch = Math.Max(-PitchLimit, Math.Min(PitchLimit, state.Pitch));
                zoom = Math.Max(0.22f, Math.Min(4.5f, state.Zoom));
                Invalidate();
            }

            protected override void OnHandleDestroyed(EventArgs e)
            {
                try
                {
                    ReleaseTextures();
                }
                catch
                {
                }

                base.OnHandleDestroyed(e);
            }

            protected override void OnPaintBackground(PaintEventArgs pevent)
            {
                // Prevent default background flicker; scene clear handles it.
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (!TryEnsureGpuReady())
                {
                    e.Graphics.Clear(Color.FromArgb(206, 206, 206));
                    using (Brush brush = new SolidBrush(Color.FromArgb(80, 80, 80)))
                    {
                        e.Graphics.DrawString("GPU init failed. Using CPU fallback.", Font, brush, new PointF(16f, 16f));
                    }
                    return;
                }

                RenderScene();
                SwapBuffers();
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                if (glResourcesInitialized)
                {
                    MakeCurrent();
                    GL.Viewport(0, 0, Math.Max(1, Width), Math.Max(1, Height));
                }
            }

            private void InitializeGlState()
            {
                GL.ClearColor(Color.FromArgb(206, 206, 206));
                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(DepthFunction.Lequal);
                // Match CPU renderer behavior (two-sided rasterization). Some game meshes
                // have mixed winding, and back-face culling creates "hollow" artifacts.
                GL.Disable(EnableCap.CullFace);
                GL.Disable(EnableCap.Lighting);
                GL.Enable(EnableCap.Texture2D);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.Enable(EnableCap.Normalize);
                GL.ShadeModel(ShadingModel.Smooth);
                GL.Viewport(0, 0, Math.Max(1, Width), Math.Max(1, Height));
            }

            private void UploadTextures()
            {
                PreviewTextureData[] activeTextures = GetActiveTextures();
                if (activeTextures == null || activeTextures.Length == 0)
                {
                    textureHandles = new int[0];
                    return;
                }

                textureHandles = new int[activeTextures.Length];
                for (int i = 0; i < activeTextures.Length; i++)
                {
                    PreviewTextureData texture = activeTextures[i];
                    if (texture == null || !texture.IsValid)
                    {
                        textureHandles[i] = 0;
                        continue;
                    }

                    int handle;
                    GL.GenTextures(1, out handle);
                    textureHandles[i] = handle;
                    GL.BindTexture(TextureTarget.Texture2D, handle);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                    GCHandle handlePin = GCHandle.Alloc(texture.Pixels, GCHandleType.Pinned);
                    try
                    {
                        GL.TexImage2D(
                            TextureTarget.Texture2D,
                            0,
                            PixelInternalFormat.Rgba,
                            texture.Width,
                            texture.Height,
                            0,
                            OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                            PixelType.UnsignedByte,
                            handlePin.AddrOfPinnedObject());

                        // Improve texture quality in preview when zooming/tilting.
                        try
                        {
                            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                        }
                        catch
                        {
                            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                        }

                        try
                        {
                            const GetPName maxAnisoEnum = (GetPName)0x84FF; // GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT
                            const TextureParameterName anisoParamEnum = (TextureParameterName)0x84FE; // GL_TEXTURE_MAX_ANISOTROPY_EXT
                            float[] maxAnisoValues = new float[1];
                            GL.GetFloat(maxAnisoEnum, maxAnisoValues);
                            float maxAniso = maxAnisoValues[0];
                            if (maxAniso > 1f)
                            {
                                float desired = Math.Min(8f, maxAniso);
                                GL.TexParameter(TextureTarget.Texture2D, anisoParamEnum, desired);
                            }
                        }
                        catch
                        {
                            // Extension not available; linear/trilinear filtering already set.
                        }
                    }
                    finally
                    {
                        handlePin.Free();
                    }
                }

                GL.BindTexture(TextureTarget.Texture2D, 0);
            }

            private void ReleaseTextures()
            {
                if (textureHandles == null || textureHandles.Length == 0 || !glResourcesInitialized)
                {
                    return;
                }

                MakeCurrent();
                for (int i = 0; i < textureHandles.Length; i++)
                {
                    int handle = textureHandles[i];
                    if (handle != 0)
                    {
                        GL.DeleteTexture(handle);
                        textureHandles[i] = 0;
                    }
                }
            }

            private void RenderScene()
            {
                int width = Math.Max(1, Width);
                int height = Math.Max(1, Height);
                GL.Viewport(0, 0, width, height);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                if (vertices.Length == 0 || indices.Length < 3 || radius <= 0.0001f)
                {
                    return;
                }

                float aspect = width / (float)Math.Max(1, height);
                Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                    (float)(Math.PI / 4.0),
                    aspect,
                    0.05f,
                    200.0f);

                Matrix4 model = Matrix4.Identity;
                model *= Matrix4.CreateTranslation(-center.X, -center.Y, -center.Z);
                model *= Matrix4.CreateScale(1f / radius);
                model *= Matrix4.CreateRotationY(yaw);
                model *= Matrix4.CreateRotationX(pitch);
                float cameraDistance = 3.2f / Math.Max(0.15f, zoom);
                model *= Matrix4.CreateTranslation(0f, 0f, -cameraDistance);

                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadMatrix(ref projection);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref model);

                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                // Pass 1: depth pre-pass for opaque fragments (including opaque parts of alpha textures).
                GL.DepthMask(true);
                GL.Disable(EnableCap.Blend);
                GL.Disable(EnableCap.AlphaTest);
                DrawTriangleList(opaqueTriangles, false, false);

                const float cutoutAlphaThreshold = 0.01f;
                GL.Enable(EnableCap.AlphaTest);
                GL.AlphaFunc(AlphaFunction.Greater, cutoutAlphaThreshold);
                DrawTriangleList(cutoutTriangles, false, false);
                DrawTriangleList(transparentTriangles, false, false);

                // Pass 2: blended fragments from alpha textures / transparent materials.
                GL.DepthMask(false);
                GL.Enable(EnableCap.Blend);
                GL.AlphaFunc(AlphaFunction.Less, 0.98f);
                DrawTriangleList(transparentTriangles, true, false);

                GL.Disable(EnableCap.AlphaTest);
                GL.DepthMask(true);
                GL.Enable(EnableCap.Blend);

                if (ShowWireframe)
                {
                    GL.Disable(EnableCap.Texture2D);
                    GL.Color4(245f / 255f, 255f / 255f, 255f / 255f, 150f / 255f);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    DrawTriangleList(opaqueTriangles, false, false);
                    DrawTriangleList(cutoutTriangles, false, false);
                    DrawTriangleList(transparentTriangles, false, false);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    GL.Enable(EnableCap.Texture2D);
                }
            }

            private void DrawTriangleList(
                List<TriangleGpuItem> triangles,
                bool forceBlend,
                bool useTriangleTransparency)
            {
                if (triangles == null || triangles.Count == 0)
                {
                    return;
                }

                int currentTexture = int.MinValue;
                bool currentBlend = false;
                bool currentTexturing = true;
                for (int i = 0; i < triangles.Count; i++)
                {
                    TriangleGpuItem tri = triangles[i];
                    int textureHandle = GetTextureHandle(tri.TextureIndex);
                    bool hasTexture = textureHandle != 0;
                    bool useBlend = forceBlend || (useTriangleTransparency && tri.Transparent);
                    if (useBlend != currentBlend)
                    {
                        if (useBlend)
                        {
                            GL.Enable(EnableCap.Blend);
                        }
                        else
                        {
                            GL.Disable(EnableCap.Blend);
                        }

                        currentBlend = useBlend;
                    }

                    if (hasTexture != currentTexturing)
                    {
                        if (hasTexture)
                        {
                            GL.Enable(EnableCap.Texture2D);
                        }
                        else
                        {
                            GL.Disable(EnableCap.Texture2D);
                            GL.BindTexture(TextureTarget.Texture2D, 0);
                            currentTexture = int.MinValue;
                        }

                        currentTexturing = hasTexture;
                    }

                    if (hasTexture && textureHandle != currentTexture)
                    {
                        GL.BindTexture(TextureTarget.Texture2D, textureHandle);
                        currentTexture = textureHandle;
                    }

                    Color triColor = hasTexture ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(tri.ColorArgb);
                    GL.Begin(PrimitiveType.Triangles);
                    DrawVertex(tri.I0, triColor);
                    DrawVertex(tri.I1, triColor);
                    DrawVertex(tri.I2, triColor);
                    GL.End();
                }

                GL.Enable(EnableCap.Texture2D);
                GL.Enable(EnableCap.Blend);
            }

            private void DrawVertex(int index, Color color)
            {
                if (index < 0 || index >= vertices.Length)
                {
                    return;
                }

                Vector3f normal = index < normals.Length ? normals[index] : Vector3f.Zero;
                Vector2f uv = index < uvs.Length ? uvs[index] : new Vector2f(0f, 0f);
                Vector3f vertex = vertices[index];

                GL.Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
                GL.Normal3(normal.X, normal.Y, normal.Z);
                GL.TexCoord2(uv.X, uv.Y);
                GL.Vertex3(vertex.X, vertex.Y, vertex.Z);
            }

            private int GetTextureHandle(int textureIndex)
            {
                if (textureHandles == null || textureIndex < 0 || textureIndex >= textureHandles.Length)
                {
                    return 0;
                }

                return textureHandles[textureIndex];
            }

            private void BuildTriangleLists()
            {
                opaqueTriangles.Clear();
                cutoutTriangles.Clear();
                transparentTriangles.Clear();
                if (indices == null || indices.Length < 3)
                {
                    return;
                }

                int triangleCount = indices.Length / 3;
                for (int tri = 0; tri < triangleCount; tri++)
                {
                    int baseIndex = tri * 3;
                    int i0 = indices[baseIndex];
                    int i1 = indices[baseIndex + 1];
                    int i2 = indices[baseIndex + 2];
                    if (i0 < 0 || i1 < 0 || i2 < 0
                        || i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length)
                    {
                        continue;
                    }

                    int textureIndex = tri < triangleTextureIndices.Length ? triangleTextureIndices[tri] : -1;
                    int colorArgb = tri < triangleColors.Length
                        ? triangleColors[tri]
                        : Color.FromArgb(255, 200, 210, 220).ToArgb();

                    bool hasTexture = textureIndex >= 0
                        && textureIndex < textures.Length
                        && textures[textureIndex] != null
                        && textures[textureIndex].IsValid;
                    bool usesAlphaAsOpacity = hasTexture && textures[textureIndex].UsesAlphaAsOpacity;

                    bool transparent;
                    if (hasTexture)
                    {
                        transparent = textures[textureIndex].HasTransparency;
                    }
                    else
                    {
                        transparent = ((colorArgb >> 24) & 0xFF) < 245;
                    }

                    TriangleGpuItem item = new TriangleGpuItem
                    {
                        I0 = i0,
                        I1 = i1,
                        I2 = i2,
                        TextureIndex = textureIndex,
                        ColorArgb = colorArgb,
                        UsesAlphaAsOpacity = usesAlphaAsOpacity,
                        Transparent = transparent
                    };

                    if (transparent)
                    {
                        transparentTriangles.Add(item);
                    }
                    else if (usesAlphaAsOpacity)
                    {
                        cutoutTriangles.Add(item);
                    }
                    else
                    {
                        opaqueTriangles.Add(item);
                    }
                }
            }

            private void OnMouseDown(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left)
                {
                    return;
                }

                isDragging = true;
                lastMouse = e.Location;
                Cursor = Cursors.SizeAll;
                Focus();
            }

            private void OnMouseUp(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left)
                {
                    return;
                }

                isDragging = false;
                Cursor = Cursors.Default;
            }

            private void OnMouseMove(object sender, MouseEventArgs e)
            {
                if (!isDragging)
                {
                    return;
                }

                int dx = e.X - lastMouse.X;
                int dy = e.Y - lastMouse.Y;
                float sensitivity = (ModifierKeys & Keys.Shift) == Keys.Shift ? 0.0017f : 0.0036f;
                yaw += dx * sensitivity;
                pitch += dy * sensitivity;
                pitch = Math.Max(-PitchLimit, Math.Min(PitchLimit, pitch));
                lastMouse = e.Location;
                Invalidate();
            }

            private void OnMouseWheel(object sender, MouseEventArgs e)
            {
                float factor = e.Delta > 0 ? 1.1f : (1f / 1.1f);
                zoom *= factor;
                zoom = Math.Max(0.22f, Math.Min(4.5f, zoom));
                Invalidate();
            }

            private PreviewTextureData[] GetActiveTextures()
            {
                return textures;
            }
        }

        private sealed class ModelPreviewViewport : Control
        {
            private readonly Vector3f[] vertices;
            private readonly Vector2f[] uvs;
            private readonly int[] indices;
            private readonly int[] triangleColors;
            private readonly int[] triangleTextureIndices;
            private readonly PreviewTextureData[] textures;
            private readonly int ecmSrcBlend;
            private readonly int ecmDestBlend;
            private readonly int ecmEmissiveColorArgb;
            private readonly int ecmOrgColorArgb;
            private readonly float[] ecmShaderFloats;
            private readonly int overlayTextureIndex;
            private readonly Vector3f[] baseNormals;
            private readonly Vector3f center;
            private readonly float radius;
            private const float RotationSensitivity = 0.0036f;
            private const float PitchLimit = 1.35f;
            private readonly float defaultYaw;
            private readonly float defaultPitch;

            private float yaw;
            private float pitch;
            private float zoom;
            private bool isDragging;
            private System.Drawing.Point lastMouse;
            private bool useHardwareRendering;
            private HardwareRenderBackend preferredHardwareBackend;

            public bool ShowWireframe { get; set; }
            public bool UseHardwareRendering
            {
                get { return useHardwareRendering; }
                set { useHardwareRendering = value; }
            }

            public HardwareRenderBackend PreferredHardwareBackend
            {
                get { return preferredHardwareBackend; }
                set { preferredHardwareBackend = value; }
            }

            public string GetRendererStatusText()
            {
                if (!useHardwareRendering)
                {
                    return "Renderer: Software (CPU)";
                }

                if (preferredHardwareBackend == HardwareRenderBackend.OpenGL)
                {
                    return "Renderer: OpenGL (CPU fallback in this build)";
                }

                return "Renderer: DirectX 11 (CPU fallback in this build)";
            }

            public ModelPreviewViewport(ModelPreviewMeshData meshData)
            {
                vertices = meshData != null && meshData.Vertices != null ? meshData.Vertices : new Vector3f[0];
                uvs = meshData != null && meshData.UVs != null ? meshData.UVs : new Vector2f[0];
                indices = meshData != null && meshData.Indices != null ? meshData.Indices : new int[0];
                triangleColors = meshData != null && meshData.TriangleColors != null ? meshData.TriangleColors : new int[0];
                triangleTextureIndices = meshData != null && meshData.TriangleTextureIndices != null ? meshData.TriangleTextureIndices : new int[0];
                textures = meshData != null && meshData.Textures != null ? meshData.Textures : new PreviewTextureData[0];
                ecmSrcBlend = meshData != null ? meshData.SrcBlend : 5;
                ecmDestBlend = meshData != null ? meshData.DestBlend : 6;
                ecmEmissiveColorArgb = meshData != null ? meshData.EmissiveColorArgb : 0;
                ecmOrgColorArgb = meshData != null ? meshData.OrgColorArgb : unchecked((int)0xFFFFFFFF);
                ecmShaderFloats = meshData != null && meshData.ShaderFloats != null ? meshData.ShaderFloats : new float[0];
                overlayTextureIndex = FindAttachmentTextureIndex(textures);
                baseNormals = ComputeVertexNormals(vertices, indices);
                ComputeBounds(vertices, out center, out radius);

                SetStyle(ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.UserPaint
                    | ControlStyles.ResizeRedraw, true);

                BackColor = Color.FromArgb(206, 206, 206);
                defaultYaw = 0f;
                defaultPitch = 0f;
                yaw = defaultYaw;
                pitch = defaultPitch;
                zoom = 1.0f;
                useHardwareRendering = false;
                preferredHardwareBackend = HardwareRenderBackend.DirectX11;
                ShowWireframe = false;

                MouseDown += OnMouseDown;
                MouseUp += OnMouseUp;
                MouseMove += OnMouseMove;
                MouseWheel += OnMouseWheel;
                MouseDoubleClick += (s, e) => ResetView();
            }

            public void ResetView()
            {
                yaw = defaultYaw;
                pitch = defaultPitch;
                zoom = 1.0f;
                Invalidate();
            }

            public ViewCameraState GetCameraState()
            {
                return new ViewCameraState
                {
                    Yaw = yaw,
                    Pitch = pitch,
                    Zoom = zoom,
                    IsValid = true
                };
            }

            public void SetCameraState(ViewCameraState state)
            {
                if (!state.IsValid)
                {
                    return;
                }

                yaw = state.Yaw;
                pitch = Math.Max(-PitchLimit, Math.Min(PitchLimit, state.Pitch));
                zoom = Math.Max(0.22f, Math.Min(4.5f, state.Zoom));
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                Graphics g = e.Graphics;
                g.Clear(BackColor);
                g.SmoothingMode = SmoothingMode.HighSpeed;

                if (vertices.Length == 0 || indices.Length < 3 || radius <= 0.0001f)
                {
                    DrawNoMeshBackground(g, ClientSize.Width, ClientSize.Height);
                    using (Brush brush = new SolidBrush(Color.FromArgb(165, 165, 165)))
                    {
                        g.DrawString("No preview mesh", Font, brush, new PointF(16f, 16f));
                    }
                    return;
                }

                int width = Math.Max(1, ClientSize.Width);
                int height = Math.Max(1, ClientSize.Height);
                int renderScale = GetRenderScale(width, height);
                int renderWidth = Math.Max(1, width / renderScale);
                int renderHeight = Math.Max(1, height / renderScale);
                float renderToDisplayX = width / (float)renderWidth;
                float renderToDisplayY = height / (float)renderHeight;
                float cx = renderWidth * 0.5f;
                float cy = renderHeight * 0.5f;

                float cosY = (float)Math.Cos(yaw);
                float sinY = (float)Math.Sin(yaw);
                float cosX = (float)Math.Cos(pitch);
                float sinX = (float)Math.Sin(pitch);

                float[] projectedX = new float[vertices.Length];
                float[] projectedY = new float[vertices.Length];
                float[] reciprocalDepth = new float[vertices.Length];
                bool[] visible = new bool[vertices.Length];
                Vector3f[] transformed = new Vector3f[vertices.Length];
                Vector3f[] transformedNormals = new Vector3f[vertices.Length];

                float baseScale = Math.Min(renderWidth, renderHeight) * 0.42f;
                float cameraDistance = 3.2f / Math.Max(0.15f, zoom);

                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3f local = (vertices[i] - center) / radius;

                    float x = local.X;
                    float y = local.Y;
                    float z = local.Z;

                    float ryX = (x * cosY) + (z * sinY);
                    float ryZ = (-x * sinY) + (z * cosY);

                    float rxY = (y * cosX) - (ryZ * sinX);
                    float rxZ = (y * sinX) + (ryZ * cosX);

                    transformed[i] = new Vector3f(ryX, rxY, rxZ);

                    Vector3f baseNormal = i < baseNormals.Length ? baseNormals[i] : Vector3f.Zero;
                    float nx = baseNormal.X;
                    float ny = baseNormal.Y;
                    float nz = baseNormal.Z;
                    float nryX = (nx * cosY) + (nz * sinY);
                    float nryZ = (-nx * sinY) + (nz * cosY);
                    float nrxY = (ny * cosX) - (nryZ * sinX);
                    float nrxZ = (ny * sinX) + (nryZ * cosX);
                    transformedNormals[i] = new Vector3f(nryX, nrxY, nrxZ).Normalize();

                    float d = rxZ + cameraDistance;
                    if (d <= 0.02f)
                    {
                        visible[i] = false;
                        continue;
                    }

                    projectedX[i] = cx + (ryX * baseScale / d);
                    projectedY[i] = cy - (rxY * baseScale / d);
                    reciprocalDepth[i] = 1f / d;
                    visible[i] = true;
                }

                List<TriangleRenderItem> allTriangles = new List<TriangleRenderItem>(Math.Max(1, indices.Length / 3));
                List<TriangleRenderItem> opaqueTriangles = new List<TriangleRenderItem>(Math.Max(1, indices.Length / 3));
                List<TriangleRenderItem> transparentTriangles = new List<TriangleRenderItem>(Math.Max(1, indices.Length / 6));
                Vector3f lightDirection = new Vector3f(0.35f, 0.45f, -0.82f).Normalize();
                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    int i0 = indices[i];
                    int i1 = indices[i + 1];
                    int i2 = indices[i + 2];
                    if (i0 < 0 || i1 < 0 || i2 < 0
                        || i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length)
                    {
                        continue;
                    }

                    if (!visible[i0] || !visible[i1] || !visible[i2])
                    {
                        continue;
                    }

                    Vector3f v0 = transformed[i0];
                    Vector3f v1 = transformed[i1];
                    Vector3f v2 = transformed[i2];
                    Vector3f normal = Vector3f.Cross(v1 - v0, v2 - v0);
                    float normalLen = normal.Length();
                    if (normalLen <= 0.000001f)
                    {
                        continue;
                    }

                    float backendShade = GetBackendShadeMultiplier();
                    float shade0 = ComputeVertexShade(transformedNormals[i0], lightDirection) * backendShade;
                    float shade1 = ComputeVertexShade(transformedNormals[i1], lightDirection) * backendShade;
                    float shade2 = ComputeVertexShade(transformedNormals[i2], lightDirection) * backendShade;

                    int triangleIndex = i / 3;
                    int baseColorArgb = triangleIndex < triangleColors.Length
                        ? triangleColors[triangleIndex]
                        : Color.FromArgb(255, 200, 210, 220).ToArgb();
                    int texIndex = triangleIndex < triangleTextureIndices.Length
                        ? triangleTextureIndices[triangleIndex]
                        : -1;

                    float avgInvDepth = (reciprocalDepth[i0] + reciprocalDepth[i1] + reciprocalDepth[i2]) / 3f;
                    TriangleRenderItem tri = new TriangleRenderItem
                    {
                        I0 = i0,
                        I1 = i1,
                        I2 = i2,
                        Shade0 = shade0,
                        Shade1 = shade1,
                        Shade2 = shade2,
                        BaseColorArgb = baseColorArgb,
                        TextureIndex = texIndex,
                        AverageInvDepth = avgInvDepth,
                        IsTransparent = IsTriangleTransparent(texIndex)
                    };

                    allTriangles.Add(tri);
                    if (tri.IsTransparent)
                    {
                        transparentTriangles.Add(tri);
                    }
                    else
                    {
                        opaqueTriangles.Add(tri);
                    }
                }

                int pixelCount = renderWidth * renderHeight;
                int[] colorBuffer = new int[pixelCount];
                float[] depthBuffer = new float[pixelCount];
                FillSceneBackgroundBuffer(renderWidth, renderHeight, colorBuffer);
                for (int i = 0; i < pixelCount; i++)
                {
                    depthBuffer[i] = float.NegativeInfinity;
                }

                for (int t = 0; t < opaqueTriangles.Count; t++)
                {
                    RasterizeTriangle(
                        opaqueTriangles[t],
                        projectedX,
                        projectedY,
                        reciprocalDepth,
                        renderWidth,
                        renderHeight,
                        colorBuffer,
                        depthBuffer,
                        false);
                }

                if (transparentTriangles.Count > 1)
                {
                    transparentTriangles.Sort((a, b) => a.AverageInvDepth.CompareTo(b.AverageInvDepth));
                }

                for (int t = 0; t < transparentTriangles.Count; t++)
                {
                    RasterizeTriangle(
                        transparentTriangles[t],
                        projectedX,
                        projectedY,
                        reciprocalDepth,
                        renderWidth,
                        renderHeight,
                        colorBuffer,
                        depthBuffer,
                        true);
                }

                using (Bitmap frame = CreateFrameBitmap(renderWidth, renderHeight, colorBuffer))
                {
                    if (renderScale == 1)
                    {
                        g.DrawImageUnscaled(frame, 0, 0);
                    }
                    else
                    {
                        g.InterpolationMode = GetUpscaleInterpolationMode();
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.DrawImage(frame, new Rectangle(0, 0, width, height));
                    }
                }

                if (ShowWireframe)
                {
                    using (Pen wirePen = new Pen(Color.FromArgb(130, 245, 255, 255), 1f))
                    {
                        for (int t = 0; t < allTriangles.Count; t++)
                        {
                            TriangleRenderItem tri = allTriangles[t];
                            float centroidX = (projectedX[tri.I0] + projectedX[tri.I1] + projectedX[tri.I2]) / 3f;
                            float centroidY = (projectedY[tri.I0] + projectedY[tri.I1] + projectedY[tri.I2]) / 3f;
                            int cxPixel = Clamp((int)Math.Round(centroidX), 0, renderWidth - 1);
                            int cyPixel = Clamp((int)Math.Round(centroidY), 0, renderHeight - 1);
                            float topDepth = depthBuffer[(cyPixel * renderWidth) + cxPixel];
                            if ((tri.AverageInvDepth + 0.0002f) < topDepth)
                            {
                                continue;
                            }

                            g.DrawLine(
                                wirePen,
                                projectedX[tri.I0] * renderToDisplayX,
                                projectedY[tri.I0] * renderToDisplayY,
                                projectedX[tri.I1] * renderToDisplayX,
                                projectedY[tri.I1] * renderToDisplayY);
                            g.DrawLine(
                                wirePen,
                                projectedX[tri.I1] * renderToDisplayX,
                                projectedY[tri.I1] * renderToDisplayY,
                                projectedX[tri.I2] * renderToDisplayX,
                                projectedY[tri.I2] * renderToDisplayY);
                            g.DrawLine(
                                wirePen,
                                projectedX[tri.I2] * renderToDisplayX,
                                projectedY[tri.I2] * renderToDisplayY,
                                projectedX[tri.I0] * renderToDisplayX,
                                projectedY[tri.I0] * renderToDisplayY);
                        }
                    }
                }
            }

            private static void DrawNoMeshBackground(Graphics g, int width, int height)
            {
                if (g == null || width <= 0 || height <= 0)
                {
                    return;
                }

                using (LinearGradientBrush sky = new LinearGradientBrush(
                    new Rectangle(0, 0, width, height),
                    Color.FromArgb(215, 215, 215),
                    Color.FromArgb(188, 188, 188),
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(sky, 0, 0, width, height);
                }

                float horizonY = height * 0.58f;
                float centerX = width * 0.5f;
                float topHalf = width * 0.12f;
                float bottomHalf = width * 0.48f;

                using (Pen gridPen = new Pen(Color.FromArgb(162, 174, 174, 174), 1f))
                {
                    for (int i = -28; i <= 28; i++)
                    {
                        float t = i / 28f;
                        float xTop = centerX + (topHalf * t);
                        float xBottom = centerX + (bottomHalf * t);
                        g.DrawLine(gridPen, xTop, horizonY, xBottom, height - 1f);
                    }

                    for (int i = 0; i <= 24; i++)
                    {
                        float t = i / 24f;
                        float p = (float)Math.Pow(t, 1.65f);
                        float y = horizonY + ((height - horizonY) * p);
                        float half = topHalf + ((bottomHalf - topHalf) * (float)Math.Pow(t, 1.1f));
                        g.DrawLine(gridPen, centerX - half, y, centerX + half, y);
                    }
                }
            }

            private static void FillSceneBackgroundBuffer(int width, int height, int[] colorBuffer)
            {
                if (colorBuffer == null || width <= 0 || height <= 0 || colorBuffer.Length < (width * height))
                {
                    return;
                }

                int horizon = Math.Max(1, Math.Min(height - 2, (int)(height * 0.58f)));
                Color skyTop = Color.FromArgb(216, 216, 216);
                Color skyHorizon = Color.FromArgb(196, 196, 196);
                Color groundNear = Color.FromArgb(188, 188, 188);
                Color groundFar = Color.FromArgb(174, 174, 174);

                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    int argb;
                    if (y <= horizon)
                    {
                        float t = y / (float)Math.Max(1, horizon);
                        argb = Color.FromArgb(
                            255,
                            LerpByte(skyTop.R, skyHorizon.R, t),
                            LerpByte(skyTop.G, skyHorizon.G, t),
                            LerpByte(skyTop.B, skyHorizon.B, t)).ToArgb();
                    }
                    else
                    {
                        float t = (y - horizon) / (float)Math.Max(1, (height - 1 - horizon));
                        argb = Color.FromArgb(
                            255,
                            LerpByte(groundNear.R, groundFar.R, t),
                            LerpByte(groundNear.G, groundFar.G, t),
                            LerpByte(groundNear.B, groundFar.B, t)).ToArgb();
                    }

                    for (int x = 0; x < width; x++)
                    {
                        colorBuffer[row + x] = argb;
                    }
                }

                int gridColor = Color.FromArgb(255, 175, 175, 175).ToArgb();
                float centerX = width * 0.5f;
                float topHalf = width * 0.12f;
                float bottomHalf = width * 0.48f;
                float topY = horizon;
                float bottomY = height - 1;

                for (int i = -28; i <= 28; i++)
                {
                    float t = i / 28f;
                    float xTop = centerX + (topHalf * t);
                    float xBottom = centerX + (bottomHalf * t);
                    DrawLineToBuffer(colorBuffer, width, height, xTop, topY, xBottom, bottomY, gridColor);
                }

                for (int i = 0; i <= 24; i++)
                {
                    float t = i / 24f;
                    float p = (float)Math.Pow(t, 1.65f);
                    float y = topY + ((bottomY - topY) * p);
                    float half = topHalf + ((bottomHalf - topHalf) * (float)Math.Pow(t, 1.1f));
                    DrawLineToBuffer(colorBuffer, width, height, centerX - half, y, centerX + half, y, gridColor);
                }
            }

            private static int LerpByte(int start, int end, float t)
            {
                float clamped = Clamp(t, 0f, 1f);
                return Clamp((int)(start + ((end - start) * clamped)), 0, 255);
            }

            private static void DrawLineToBuffer(
                int[] colorBuffer,
                int width,
                int height,
                float x0,
                float y0,
                float x1,
                float y1,
                int argb)
            {
                float dx = x1 - x0;
                float dy = y1 - y0;
                int steps = (int)Math.Max(Math.Abs(dx), Math.Abs(dy));
                if (steps <= 0)
                {
                    int ix = (int)Math.Round(x0);
                    int iy = (int)Math.Round(y0);
                    if (ix >= 0 && ix < width && iy >= 0 && iy < height)
                    {
                        colorBuffer[(iy * width) + ix] = argb;
                    }
                    return;
                }

                float inv = 1f / steps;
                for (int i = 0; i <= steps; i++)
                {
                    float t = i * inv;
                    int x = (int)Math.Round(x0 + (dx * t));
                    int y = (int)Math.Round(y0 + (dy * t));
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        colorBuffer[(y * width) + x] = argb;
                    }
                }
            }

            private InterpolationMode GetUpscaleInterpolationMode()
            {
                if (!useHardwareRendering)
                {
                    return InterpolationMode.Bilinear;
                }

                switch (preferredHardwareBackend)
                {
                    case HardwareRenderBackend.OpenGL:
                        return InterpolationMode.Bilinear;
                    default:
                        return InterpolationMode.NearestNeighbor;
                }
            }

            private float GetBackendShadeMultiplier()
            {
                if (!useHardwareRendering)
                {
                    return 1.0f;
                }

                switch (preferredHardwareBackend)
                {
                    case HardwareRenderBackend.OpenGL:
                        return 1.03f;
                    default:
                        return 0.98f;
                }
            }

            private bool UseBilinearTextureSampling()
            {
                // Keep filtering quality consistent across profiles; the "hardware" modes
                // in this build are still software fallback profiles.
                return true;
            }

            private int GetRenderScale(int width, int height)
            {
                if (!isDragging)
                {
                    return 1;
                }

                int triCount = Math.Max(0, indices.Length / 3);
                if (useHardwareRendering)
                {
                    int pixelCount = width * height;
                    switch (preferredHardwareBackend)
                    {
                        case HardwareRenderBackend.DirectX11:
                            // Prioritize full-resolution interaction and only downscale very heavy scenes.
                            if (triCount > 24000 || pixelCount > 5200000)
                            {
                                return 2;
                            }

                            return 1;
                        case HardwareRenderBackend.OpenGL:
                            if (triCount > 26000 || pixelCount > 5600000)
                            {
                                return 2;
                            }

                            return 1;
                        default:
                            return 1;
                    }
                }

                if (triCount > 7000 || (width * height) > 1600000)
                {
                    return 3;
                }

                return 2;
            }

            private void RasterizeTriangle(
                TriangleRenderItem tri,
                float[] projectedX,
                float[] projectedY,
                float[] reciprocalDepth,
                int width,
                int height,
                int[] colorBuffer,
                float[] depthBuffer,
                bool alphaBlend)
            {
                float x0 = projectedX[tri.I0];
                float y0 = projectedY[tri.I0];
                float x1 = projectedX[tri.I1];
                float y1 = projectedY[tri.I1];
                float x2 = projectedX[tri.I2];
                float y2 = projectedY[tri.I2];

                float area = Edge(x0, y0, x1, y1, x2, y2);
                if (Math.Abs(area) < 0.00001f)
                {
                    return;
                }

                int minX = Clamp((int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))), 0, width - 1);
                int minY = Clamp((int)Math.Floor(Math.Min(y0, Math.Min(y1, y2))), 0, height - 1);
                int maxX = Clamp((int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))), 0, width - 1);
                int maxY = Clamp((int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2))), 0, height - 1);
                if (maxX < minX || maxY < minY)
                {
                    return;
                }

                float invD0 = reciprocalDepth[tri.I0];
                float invD1 = reciprocalDepth[tri.I1];
                float invD2 = reciprocalDepth[tri.I2];
                bool hasUv = uvs != null && uvs.Length == vertices.Length;
                Vector2f uv0 = hasUv ? uvs[tri.I0] : new Vector2f(0f, 0f);
                Vector2f uv1 = hasUv ? uvs[tri.I1] : new Vector2f(0f, 0f);
                Vector2f uv2 = hasUv ? uvs[tri.I2] : new Vector2f(0f, 0f);
                PreviewTextureData texture = null;
                bool hasTexture = hasUv && TryGetTexture(tri.TextureIndex, out texture);
                bool textureUsesAlphaAsOpacity = hasTexture && texture.UsesAlphaAsOpacity;
                bool bilinearTextureSampling = UseBilinearTextureSampling();

                for (int py = minY; py <= maxY; py++)
                {
                    float sampleY = py + 0.5f;
                    int rowOffset = py * width;
                    for (int px = minX; px <= maxX; px++)
                    {
                        float sampleX = px + 0.5f;

                        float w0 = Edge(x1, y1, x2, y2, sampleX, sampleY);
                        float w1 = Edge(x2, y2, x0, y0, sampleX, sampleY);
                        float w2 = Edge(x0, y0, x1, y1, sampleX, sampleY);
                        if (!IsInsideTriangle(w0, w1, w2))
                        {
                            continue;
                        }

                        w0 /= area;
                        w1 /= area;
                        w2 /= area;

                        float invDepth = (w0 * invD0) + (w1 * invD1) + (w2 * invD2);
                        if (Math.Abs(invDepth) <= 0.0000001f)
                        {
                            continue;
                        }

                        int bufferIndex = rowOffset + px;
                        if (invDepth <= depthBuffer[bufferIndex])
                        {
                            continue;
                        }

                        int colorArgb;
                        if (hasTexture)
                        {
                            float u = ((w0 * uv0.X * invD0) + (w1 * uv1.X * invD1) + (w2 * uv2.X * invD2)) / invDepth;
                            float v = ((w0 * uv0.Y * invD0) + (w1 * uv1.Y * invD1) + (w2 * uv2.Y * invD2)) / invDepth;
                            int textureArgb = SampleTextureArgb(texture, u, v, bilinearTextureSampling);
                            int textureAlpha = (textureArgb >> 24) & 0xFF;
                            if (textureUsesAlphaAsOpacity && textureAlpha <= 2)
                            {
                                continue;
                            }

                            if (textureUsesAlphaAsOpacity)
                            {
                                colorArgb = textureArgb;
                            }
                            else
                            {
                                colorArgb = unchecked((int)0xFF000000) | (textureArgb & 0x00FFFFFF);
                            }
                        }
                        else
                        {
                            colorArgb = tri.BaseColorArgb;
                        }

                        float interpolatedShade = (w0 * tri.Shade0) + (w1 * tri.Shade1) + (w2 * tri.Shade2);
                        int shaded = ApplyShade(colorArgb, interpolatedShade);
                        shaded = ApplyEcmPostLighting(shaded, interpolatedShade);
                        shaded = ApplyColorGrade(shaded);
                        int alpha = (shaded >> 24) & 0xFF;

                        if (alphaBlend && alpha < 250)
                        {
                            if (alpha <= 3)
                            {
                                continue;
                            }

                            colorBuffer[bufferIndex] = AlphaBlendOver(shaded, colorBuffer[bufferIndex]);
                            continue;
                        }

                        colorBuffer[bufferIndex] = MakeOpaque(shaded);
                        depthBuffer[bufferIndex] = invDepth;
                    }
                }
            }

            private static float Edge(float ax, float ay, float bx, float by, float px, float py)
            {
                return ((px - ax) * (by - ay)) - ((py - ay) * (bx - ax));
            }

            private static bool IsInsideTriangle(float w0, float w1, float w2)
            {
                bool hasNeg = (w0 < 0f) || (w1 < 0f) || (w2 < 0f);
                bool hasPos = (w0 > 0f) || (w1 > 0f) || (w2 > 0f);
                return !(hasNeg && hasPos);
            }

            private bool IsTriangleTransparent(int textureIndex)
            {
                if (!TryGetTexture(textureIndex, out PreviewTextureData texture))
                {
                    return false;
                }

                return texture.HasTransparency;
            }

            private bool TryGetTexture(int textureIndex, out PreviewTextureData texture)
            {
                texture = null;
                PreviewTextureData[] textureSet = GetActiveTextures();
                if (textureSet == null || textureIndex < 0 || textureIndex >= textureSet.Length)
                {
                    return false;
                }

                PreviewTextureData found = textureSet[textureIndex];
                if (found == null || !found.IsValid)
                {
                    return false;
                }

                texture = found;
                return true;
            }

            private bool TryGetOverlayTexture(int baseTextureIndex, out PreviewTextureData texture)
            {
                texture = null;
                PreviewTextureData[] textureSet = GetActiveTextures();
                if (textureSet == null || overlayTextureIndex < 0 || overlayTextureIndex >= textureSet.Length)
                {
                    return false;
                }
                if (overlayTextureIndex == baseTextureIndex)
                {
                    return false;
                }

                PreviewTextureData found = textureSet[overlayTextureIndex];
                if (found == null || !found.IsValid)
                {
                    return false;
                }

                texture = found;
                return true;
            }

            private static int FindAttachmentTextureIndex(PreviewTextureData[] textureList)
            {
                if (textureList == null || textureList.Length == 0)
                {
                    return -1;
                }

                for (int i = 0; i < textureList.Length; i++)
                {
                    string name = textureList[i] != null ? (textureList[i].Name ?? string.Empty) : string.Empty;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (name.IndexOf("附件", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("装备", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("equip", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("armor", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return i;
                    }
                }

                if (textureList.Length == 2)
                {
                    return 1;
                }

                return -1;
            }

            private float GetShaderFloat(int index, float fallback)
            {
                if (ecmShaderFloats == null || index < 0 || index >= ecmShaderFloats.Length)
                {
                    return fallback;
                }

                float value = ecmShaderFloats[index];
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    return fallback;
                }

                return value;
            }

            private static int SampleTextureArgb(PreviewTextureData texture, float u, float v, bool bilinear)
            {
                if (texture == null || !texture.IsValid)
                {
                    return unchecked((int)0xFFFFFFFF);
                }

                float wrappedU = u - (float)Math.Floor(u);
                float wrappedV = v - (float)Math.Floor(v);
                if (!bilinear || texture.Width <= 1 || texture.Height <= 1)
                {
                    int x = Clamp((int)(wrappedU * (texture.Width - 1)), 0, texture.Width - 1);
                    int y = Clamp((int)(wrappedV * (texture.Height - 1)), 0, texture.Height - 1);
                    return texture.Pixels[(y * texture.Width) + x];
                }

                float fx = wrappedU * (texture.Width - 1);
                float fy = wrappedV * (texture.Height - 1);
                int x0 = Clamp((int)Math.Floor(fx), 0, texture.Width - 1);
                int y0 = Clamp((int)Math.Floor(fy), 0, texture.Height - 1);
                int x1 = Clamp(x0 + 1, 0, texture.Width - 1);
                int y1 = Clamp(y0 + 1, 0, texture.Height - 1);
                float tx = fx - x0;
                float ty = fy - y0;

                int c00 = texture.Pixels[(y0 * texture.Width) + x0];
                int c10 = texture.Pixels[(y0 * texture.Width) + x1];
                int c01 = texture.Pixels[(y1 * texture.Width) + x0];
                int c11 = texture.Pixels[(y1 * texture.Width) + x1];
                int top = LerpColor(c00, c10, tx);
                int bottom = LerpColor(c01, c11, tx);
                return LerpColor(top, bottom, ty);
            }

            private static Bitmap CreateFrameBitmap(int width, int height, int[] colorBuffer)
            {
                Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    int strideInts = data.Stride / 4;
                    if (strideInts == width)
                    {
                        Marshal.Copy(colorBuffer, 0, data.Scan0, colorBuffer.Length);
                    }
                    else
                    {
                        int[] stretched = new int[strideInts * height];
                        for (int y = 0; y < height; y++)
                        {
                            Array.Copy(colorBuffer, y * width, stretched, y * strideInts, width);
                        }
                        Marshal.Copy(stretched, 0, data.Scan0, stretched.Length);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }

                return bitmap;
            }

            private static float Clamp(float value, float min, float max)
            {
                if (value < min) { return min; }
                if (value > max) { return max; }
                return value;
            }

            private static int Clamp(int value, int min, int max)
            {
                if (value < min) { return min; }
                if (value > max) { return max; }
                return value;
            }

            private static int ApplyShade(int colorArgb, float shade)
            {
                float s = Clamp(shade, 0f, 2.4f);
                int a = (colorArgb >> 24) & 0xFF;
                int r = (colorArgb >> 16) & 0xFF;
                int g = (colorArgb >> 8) & 0xFF;
                int b = colorArgb & 0xFF;
                r = ShadeComponent(r, s);
                g = ShadeComponent(g, s);
                b = ShadeComponent(b, s);
                return (a << 24) | (r << 16) | (g << 8) | b;
            }

            private static int ShadeComponent(int component, float shade)
            {
                float baseValue = Clamp(component / 255f, 0f, 1f);
                float linear = SrgbToLinear(baseValue);
                float litLinear = linear * shade;
                float litSrgb = LinearToSrgb(litLinear);
                return Clamp((int)(litSrgb * 255f), 0, 255);
            }

            private static float SrgbToLinear(float v)
            {
                float c = Clamp(v, 0f, 1f);
                if (c <= 0.04045f)
                {
                    return c / 12.92f;
                }

                return (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);
            }

            private static float LinearToSrgb(float v)
            {
                float c = Clamp(v, 0f, 1f);
                if (c <= 0.0031308f)
                {
                    return c * 12.92f;
                }

                return (1.055f * (float)Math.Pow(c, 1f / 2.4f)) - 0.055f;
            }

            private static float ComputeVertexShade(Vector3f normal, Vector3f lightDirection)
            {
                float len = normal.Length();
                if (len <= 0.000001f)
                {
                    return 1.10f;
                }

                Vector3f n = normal / len;
                float ndotl = Math.Abs(Vector3f.Dot(n, lightDirection));
                // Two-sided soft lighting tuned to resemble in-game diffuse response.
                float shade = 0.68f + (ndotl * 0.92f);
                return Clamp(shade, 0.72f, 1.62f);
            }

            private static int BlendColor(int srcArgb, int dstArgb, float dstFactor)
            {
                float t = Clamp(dstFactor, 0f, 1f);
                float s = 1f - t;
                int sa = (srcArgb >> 24) & 0xFF;
                int sr = (srcArgb >> 16) & 0xFF;
                int sg = (srcArgb >> 8) & 0xFF;
                int sb = srcArgb & 0xFF;
                int dr = (dstArgb >> 16) & 0xFF;
                int dg = (dstArgb >> 8) & 0xFF;
                int db = dstArgb & 0xFF;

                int r = Clamp((int)((sr * s) + (dr * t)), 0, 255);
                int g = Clamp((int)((sg * s) + (dg * t)), 0, 255);
                int b = Clamp((int)((sb * s) + (db * t)), 0, 255);
                int a = Clamp(sa, 0, 255);
                return (a << 24) | (r << 16) | (g << 8) | b;
            }

            private static int BlendWithD3D9Modes(int dstArgb, int srcArgb, int srcBlend, int dstBlend, float extraAlpha)
            {
                float sa = Clamp(((srcArgb >> 24) & 0xFF) / 255f, 0f, 1f) * Clamp(extraAlpha, 0f, 1f);
                float da = Clamp(((dstArgb >> 24) & 0xFF) / 255f, 0f, 1f);
                float sr = ((srcArgb >> 16) & 0xFF) / 255f;
                float sg = ((srcArgb >> 8) & 0xFF) / 255f;
                float sb = (srcArgb & 0xFF) / 255f;
                float dr = ((dstArgb >> 16) & 0xFF) / 255f;
                float dg = ((dstArgb >> 8) & 0xFF) / 255f;
                float db = (dstArgb & 0xFF) / 255f;

                float outR = (sr * ResolveBlendFactor(srcBlend, sa, da, sr, dr))
                    + (dr * ResolveBlendFactor(dstBlend, sa, da, sr, dr));
                float outG = (sg * ResolveBlendFactor(srcBlend, sa, da, sg, dg))
                    + (dg * ResolveBlendFactor(dstBlend, sa, da, sg, dg));
                float outB = (sb * ResolveBlendFactor(srcBlend, sa, da, sb, db))
                    + (db * ResolveBlendFactor(dstBlend, sa, da, sb, db));

                int r = Clamp((int)(Clamp(outR, 0f, 1f) * 255f), 0, 255);
                int g = Clamp((int)(Clamp(outG, 0f, 1f) * 255f), 0, 255);
                int b = Clamp((int)(Clamp(outB, 0f, 1f) * 255f), 0, 255);
                int a = Clamp((int)(Math.Max(sa, da) * 255f), 0, 255);
                return (a << 24) | (r << 16) | (g << 8) | b;
            }

            private static float ResolveBlendFactor(int mode, float srcAlpha, float dstAlpha, float srcColor, float dstColor)
            {
                switch (mode)
                {
                    case 1: // ZERO
                        return 0f;
                    case 2: // ONE
                        return 1f;
                    case 3: // SRCCOLOR
                        return srcColor;
                    case 4: // INVSRCCOLOR
                        return 1f - srcColor;
                    case 5: // SRCALPHA
                        return srcAlpha;
                    case 6: // INVSRCALPHA
                        return 1f - srcAlpha;
                    case 7: // DESTALPHA
                        return dstAlpha;
                    case 8: // INVDESTALPHA
                        return 1f - dstAlpha;
                    case 9: // DESTCOLOR
                        return dstColor;
                    case 10: // INVDESTCOLOR
                        return 1f - dstColor;
                    case 11: // SRCALPHASAT
                        return Math.Min(srcAlpha, 1f - dstAlpha);
                    default:
                        return 1f;
                }
            }

            private int ApplyEcmPostLighting(int argb, float shade)
            {
                int a = (argb >> 24) & 0xFF;
                float r = (argb >> 16) & 0xFF;
                float g = (argb >> 8) & 0xFF;
                float b = argb & 0xFF;

                float shadeNorm = Clamp((shade - 0.78f) / 0.90f, 0f, 1f);
                float specStrength = Clamp(0.14f + (GetShaderFloat(0, 1f) * 0.14f), 0.05f, 0.50f);
                float specPower = Clamp(6f + (GetShaderFloat(2, 1f) * 8f), 4f, 22f);
                float metalMask = ComputeMetalMask(r, g, b);
                float specular = (float)Math.Pow(shadeNorm, specPower) * specStrength * (0.35f + (metalMask * 0.95f));

                r += (255f * specular);
                g += (230f * specular);
                b += (190f * specular);

                float emissiveStrength = Clamp(0.08f + (GetShaderFloat(1, 1f) * 0.22f), 0f, 0.80f);
                float lavaMask = ComputeLavaMask(r, g, b);
                int emissiveColor = ecmEmissiveColorArgb;
                if ((emissiveColor & 0x00FFFFFF) == 0)
                {
                    emissiveColor = Color.FromArgb(255, 255, 92, 34).ToArgb();
                }

                float er = (emissiveColor >> 16) & 0xFF;
                float eg = (emissiveColor >> 8) & 0xFF;
                float eb = emissiveColor & 0xFF;
                r += er * lavaMask * emissiveStrength;
                g += eg * lavaMask * emissiveStrength;
                b += eb * lavaMask * emissiveStrength;

                if ((ecmOrgColorArgb & 0x00FFFFFF) != 0x00FFFFFF && (ecmOrgColorArgb & 0x00FFFFFF) != 0)
                {
                    float tr = ((ecmOrgColorArgb >> 16) & 0xFF) / 255f;
                    float tg = ((ecmOrgColorArgb >> 8) & 0xFF) / 255f;
                    float tb = (ecmOrgColorArgb & 0xFF) / 255f;
                    r *= (0.88f + (tr * 0.12f));
                    g *= (0.88f + (tg * 0.12f));
                    b *= (0.88f + (tb * 0.12f));
                }

                int ri = Clamp((int)r, 0, 255);
                int gi = Clamp((int)g, 0, 255);
                int bi = Clamp((int)b, 0, 255);
                return (a << 24) | (ri << 16) | (gi << 8) | bi;
            }

            private static float ComputeMetalMask(float r, float g, float b)
            {
                float warm = Clamp((r - b) / 255f, 0f, 1f);
                float yellow = Clamp((Math.Min(r, g) - b) / 220f, 0f, 1f);
                float bright = Clamp((Math.Max(r, Math.Max(g, b)) - 85f) / 170f, 0f, 1f);
                return Clamp((warm * 0.45f) + (yellow * 0.35f) + (bright * 0.20f), 0f, 1f);
            }

            private static float ComputeLavaMask(float r, float g, float b)
            {
                float redDominance = Clamp((r - Math.Max(g, b)) / 180f, 0f, 1f);
                float darkMask = Clamp((150f - Math.Max(g, b)) / 150f, 0f, 1f);
                return Clamp(redDominance * darkMask, 0f, 1f);
            }

            private static int ApplyColorGrade(int argb)
            {
                int a = (argb >> 24) & 0xFF;
                float r = ((argb >> 16) & 0xFF) / 255f;
                float g = ((argb >> 8) & 0xFF) / 255f;
                float b = (argb & 0xFF) / 255f;

                // Keep a neutral gamma to preserve detail in dark regions.
                r = (float)Math.Pow(Clamp(r, 0f, 1f), 0.98f);
                g = (float)Math.Pow(Clamp(g, 0f, 1f), 0.98f);
                b = (float)Math.Pow(Clamp(b, 0f, 1f), 0.98f);

                float luma = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
                const float saturation = 1.12f;
                r = luma + ((r - luma) * saturation);
                g = luma + ((g - luma) * saturation);
                b = luma + ((b - luma) * saturation);

                const float contrast = 1.06f;
                r = ((r - 0.5f) * contrast) + 0.5f;
                g = ((g - 0.5f) * contrast) + 0.5f;
                b = ((b - 0.5f) * contrast) + 0.5f;

                // Small shadow lift to avoid crushed blacks in preview-only shading.
                const float shadowLift = 0.022f;
                r = Clamp(r + shadowLift, 0f, 1f);
                g = Clamp(g + shadowLift, 0f, 1f);
                b = Clamp(b + shadowLift, 0f, 1f);

                int ri = Clamp((int)(Clamp(r, 0f, 1f) * 255f), 0, 255);
                int gi = Clamp((int)(Clamp(g, 0f, 1f) * 255f), 0, 255);
                int bi = Clamp((int)(Clamp(b, 0f, 1f) * 255f), 0, 255);
                return (a << 24) | (ri << 16) | (gi << 8) | bi;
            }

            private static int LerpColor(int leftArgb, int rightArgb, float t)
            {
                float a = Clamp(t, 0f, 1f);
                int la = (leftArgb >> 24) & 0xFF;
                int lr = (leftArgb >> 16) & 0xFF;
                int lg = (leftArgb >> 8) & 0xFF;
                int lb = leftArgb & 0xFF;
                int ra = (rightArgb >> 24) & 0xFF;
                int rr = (rightArgb >> 16) & 0xFF;
                int rg = (rightArgb >> 8) & 0xFF;
                int rb = rightArgb & 0xFF;

                int oa = Clamp((int)(la + ((ra - la) * a)), 0, 255);
                int or = Clamp((int)(lr + ((rr - lr) * a)), 0, 255);
                int og = Clamp((int)(lg + ((rg - lg) * a)), 0, 255);
                int ob = Clamp((int)(lb + ((rb - lb) * a)), 0, 255);
                return (oa << 24) | (or << 16) | (og << 8) | ob;
            }

            private static int MakeOpaque(int argb)
            {
                return unchecked((int)0xFF000000) | (argb & 0x00FFFFFF);
            }

            private static int AlphaBlendOver(int srcArgb, int dstArgb)
            {
                int sa = (srcArgb >> 24) & 0xFF;
                if (sa <= 0)
                {
                    return dstArgb;
                }
                if (sa >= 255)
                {
                    return MakeOpaque(srcArgb);
                }

                float alpha = sa / 255f;
                float inv = 1f - alpha;
                int sr = (srcArgb >> 16) & 0xFF;
                int sg = (srcArgb >> 8) & 0xFF;
                int sb = srcArgb & 0xFF;
                int dr = (dstArgb >> 16) & 0xFF;
                int dg = (dstArgb >> 8) & 0xFF;
                int db = dstArgb & 0xFF;

                int r = Clamp((int)((sr * alpha) + (dr * inv)), 0, 255);
                int g = Clamp((int)((sg * alpha) + (dg * inv)), 0, 255);
                int b = Clamp((int)((sb * alpha) + (db * inv)), 0, 255);
                return unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
            }

            private void OnMouseDown(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left)
                {
                    return;
                }

                isDragging = true;
                Capture = true;
                lastMouse = e.Location;
                Focus();
            }

            private void OnMouseUp(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = false;
                    Capture = false;
                    Invalidate();
                }
            }

            private void OnMouseMove(object sender, MouseEventArgs e)
            {
                if (!isDragging)
                {
                    return;
                }

                int dx = e.X - lastMouse.X;
                int dy = e.Y - lastMouse.Y;
                lastMouse = e.Location;

                float sensitivity = RotationSensitivity;
                if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    sensitivity *= 0.35f;
                }

                // Drag direction follows mouse movement (right->right, up->up).
                yaw -= dx * sensitivity;
                pitch -= dy * sensitivity;
                if (pitch > PitchLimit)
                {
                    pitch = PitchLimit;
                }
                if (pitch < -PitchLimit)
                {
                    pitch = -PitchLimit;
                }

                if (yaw > (float)Math.PI)
                {
                    yaw -= (float)(Math.PI * 2.0);
                }
                else if (yaw < (float)-Math.PI)
                {
                    yaw += (float)(Math.PI * 2.0);
                }

                Invalidate();
            }

            private void OnMouseWheel(object sender, MouseEventArgs e)
            {
                if (e.Delta > 0)
                {
                    zoom *= 1.12f;
                }
                else if (e.Delta < 0)
                {
                    zoom /= 1.12f;
                }

                if (zoom < 0.2f)
                {
                    zoom = 0.2f;
                }
                if (zoom > 5f)
                {
                    zoom = 5f;
                }

                Invalidate();
            }

            private PreviewTextureData[] GetActiveTextures()
            {
                return textures;
            }

            internal static void ComputeBounds(Vector3f[] input, out Vector3f center, out float radius)
            {
                center = Vector3f.Zero;
                radius = 1f;
                if (input == null || input.Length == 0)
                {
                    return;
                }

                Vector3f min = input[0];
                Vector3f max = input[0];
                for (int i = 1; i < input.Length; i++)
                {
                    Vector3f p = input[i];
                    if (p.X < min.X) min.X = p.X;
                    if (p.Y < min.Y) min.Y = p.Y;
                    if (p.Z < min.Z) min.Z = p.Z;
                    if (p.X > max.X) max.X = p.X;
                    if (p.Y > max.Y) max.Y = p.Y;
                    if (p.Z > max.Z) max.Z = p.Z;
                }

                center = new Vector3f(
                    (min.X + max.X) * 0.5f,
                    (min.Y + max.Y) * 0.5f,
                    (min.Z + max.Z) * 0.5f);

                Vector3f size = max - min;
                radius = Math.Max(0.0001f, Math.Max(size.X, Math.Max(size.Y, size.Z)) * 0.5f);
            }

            internal static void ComputeExtents(Vector3f[] input, out float extentX, out float extentY, out float extentZ)
            {
                extentX = 1f;
                extentY = 1f;
                extentZ = 1f;
                if (input == null || input.Length == 0)
                {
                    return;
                }

                Vector3f min = input[0];
                Vector3f max = input[0];
                for (int i = 1; i < input.Length; i++)
                {
                    Vector3f p = input[i];
                    if (p.X < min.X) min.X = p.X;
                    if (p.Y < min.Y) min.Y = p.Y;
                    if (p.Z < min.Z) min.Z = p.Z;
                    if (p.X > max.X) max.X = p.X;
                    if (p.Y > max.Y) max.Y = p.Y;
                    if (p.Z > max.Z) max.Z = p.Z;
                }

                extentX = Math.Max(0.0001f, max.X - min.X);
                extentY = Math.Max(0.0001f, max.Y - min.Y);
                extentZ = Math.Max(0.0001f, max.Z - min.Z);
            }

            internal static Vector3f[] ComputeVertexNormals(Vector3f[] inputVertices, int[] inputIndices)
            {
                if (inputVertices == null || inputVertices.Length == 0)
                {
                    return new Vector3f[0];
                }

                Vector3f[] normals = new Vector3f[inputVertices.Length];
                if (inputIndices == null || inputIndices.Length < 3)
                {
                    for (int i = 0; i < normals.Length; i++)
                    {
                        normals[i] = new Vector3f(0f, 1f, 0f);
                    }
                    return normals;
                }

                for (int i = 0; i + 2 < inputIndices.Length; i += 3)
                {
                    int i0 = inputIndices[i];
                    int i1 = inputIndices[i + 1];
                    int i2 = inputIndices[i + 2];
                    if (i0 < 0 || i1 < 0 || i2 < 0
                        || i0 >= inputVertices.Length || i1 >= inputVertices.Length || i2 >= inputVertices.Length)
                    {
                        continue;
                    }

                    Vector3f a = inputVertices[i0];
                    Vector3f b = inputVertices[i1];
                    Vector3f c = inputVertices[i2];
                    Vector3f faceNormal = Vector3f.Cross(b - a, c - a);
                    if (faceNormal.Length() <= 0.000001f)
                    {
                        continue;
                    }

                    normals[i0] = normals[i0] + faceNormal;
                    normals[i1] = normals[i1] + faceNormal;
                    normals[i2] = normals[i2] + faceNormal;
                }

                for (int i = 0; i < normals.Length; i++)
                {
                    Vector3f n = normals[i];
                    if (n.Length() <= 0.000001f)
                    {
                        normals[i] = new Vector3f(0f, 1f, 0f);
                    }
                    else
                    {
                        normals[i] = n.Normalize();
                    }
                }

                return normals;
            }

            private struct TriangleRenderItem
            {
                public int I0;
                public int I1;
                public int I2;
                public float Shade0;
                public float Shade1;
                public float Shade2;
                public int BaseColorArgb;
                public int TextureIndex;
                public float AverageInvDepth;
                public bool IsTransparent;
            }
        }
    }
}

