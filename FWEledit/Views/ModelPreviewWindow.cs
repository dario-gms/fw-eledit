using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FWEledit
{
    public class ModelPreviewWindow : Form
    {
        private enum HardwareRenderBackend
        {
            DirectX11 = 0,
            Vulkan = 1,
            OpenGL = 2
        }

        private readonly ModelPreviewMeshData meshData;
        private readonly ModelPreviewViewport viewport;
        private readonly CheckBox wireframeCheck;
        private readonly CheckBox hardwareCheck;
        private readonly ComboBox backendCombo;
        private readonly Label backendLabel;
        private readonly Label helpLabel;

        public ModelPreviewWindow(ModelPreviewMeshData meshData)
        {
            this.meshData = meshData;
            Text = "Preview 3D Model";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 620);
            Size = new Size(1200, 820);
            KeyPreview = true;
            BackColor = Color.FromArgb(20, 20, 20);
            ForeColor = Color.Gainsboro;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

            Label summary = new Label();
            summary.Dock = DockStyle.Top;
            summary.Height = 56;
            summary.Padding = new Padding(10, 10, 10, 0);
            summary.TextAlign = ContentAlignment.MiddleLeft;
            summary.ForeColor = Color.Gainsboro;
            summary.BackColor = Color.FromArgb(20, 20, 20);
            summary.Text = BuildSummaryText(meshData);

            Label source = new Label();
            source.Dock = DockStyle.Top;
            source.Padding = new Padding(10, 4, 10, 8);
            source.TextAlign = ContentAlignment.TopLeft;
            source.ForeColor = Color.FromArgb(170, 170, 170);
            source.BackColor = Color.FromArgb(20, 20, 20);
            string sourceText = BuildSourceText(meshData);
            source.Text = sourceText;
            source.Height = ComputeSourceLabelHeight(sourceText);

            viewport = new ModelPreviewViewport(meshData);
            viewport.Dock = DockStyle.Fill;

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
                viewport.ShowWireframe = wireframeCheck.Checked;
                viewport.Invalidate();
            };
            footer.Controls.Add(wireframeCheck);

            backendCombo = new ComboBox();
            backendCombo.Dock = DockStyle.Right;
            backendCombo.Width = 118;
            backendCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            backendCombo.FlatStyle = FlatStyle.Flat;
            backendCombo.Items.Add("DirectX 11");
            backendCombo.Items.Add("Vulkan");
            backendCombo.Items.Add("OpenGL");
            backendCombo.SelectedIndex = 0;
            backendCombo.SelectedIndexChanged += (s, e) => ApplyRendererSettings();
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
            hardwareCheck.CheckedChanged += (s, e) => ApplyRendererSettings();
            footer.Controls.Add(hardwareCheck);

            Button resetButton = new Button();
            resetButton.Dock = DockStyle.Right;
            resetButton.Width = 120;
            resetButton.Text = "Reset View";
            resetButton.Click += (s, e) => viewport.ResetView();
            footer.Controls.Add(resetButton);

            Controls.Add(viewport);
            Controls.Add(footer);
            Controls.Add(source);
            Controls.Add(summary);

            KeyDown += OnWindowKeyDown;
            ApplyRendererSettings();
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
            else if (e.KeyCode == Keys.R)
            {
                viewport.ResetView();
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

                    lines.Add("TEX[" + i + "]: " + (loaded ? "OK " : "MISS ") + name);
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

        private void ApplyRendererSettings()
        {
            HardwareRenderBackend backend;
            switch (backendCombo.SelectedIndex)
            {
                case (int)HardwareRenderBackend.Vulkan:
                    backend = HardwareRenderBackend.Vulkan;
                    break;
                case (int)HardwareRenderBackend.OpenGL:
                    backend = HardwareRenderBackend.OpenGL;
                    break;
                default:
                    backend = HardwareRenderBackend.DirectX11;
                    break;
            }

            viewport.UseHardwareRendering = hardwareCheck.Checked;
            viewport.PreferredHardwareBackend = backend;

            bool hardwareEnabled = hardwareCheck.Checked;
            backendCombo.Enabled = hardwareEnabled;
            backendLabel.Enabled = hardwareEnabled;
            helpLabel.Text = "Drag: rotate | Shift+drag: fine rotate | Scroll: zoom | Double-click: reset | "
                + viewport.GetRendererStatusText();
            viewport.Invalidate();
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
            private Point lastMouse;
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

                if (preferredHardwareBackend == HardwareRenderBackend.Vulkan)
                {
                    return "Renderer: Vulkan (CPU fallback in this build)";
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
                ComputeExtents(vertices, out float extentX, out float extentY, out float extentZ);

                SetStyle(ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.UserPaint
                    | ControlStyles.ResizeRedraw, true);

                BackColor = Color.FromArgb(206, 206, 206);
                // Start from the front-facing side (previously opened from the back).
                defaultYaw = (float)(Math.PI + 0.45f);
                defaultPitch = extentY > (Math.Max(extentX, extentZ) * 1.12f) ? -0.55f : 0.08f;
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
                    case HardwareRenderBackend.Vulkan:
                        return InterpolationMode.HighQualityBilinear;
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
                    case HardwareRenderBackend.Vulkan:
                        return 1.08f;
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
                    // Backends are mapped to software render profiles in this build.
                    switch (preferredHardwareBackend)
                    {
                        case HardwareRenderBackend.DirectX11:
                            // DX11 profile: prioritize smooth interaction.
                            if (triCount > 7000 || (width * height) > 1600000)
                            {
                                return 4;
                            }
                            return 3;
                        case HardwareRenderBackend.Vulkan:
                            // Vulkan profile: prioritize image quality while dragging.
                            if (triCount > 9000 || (width * height) > 2200000)
                            {
                                return 3;
                            }
                            return 2;
                        case HardwareRenderBackend.OpenGL:
                            // OpenGL profile: balanced quality/performance.
                            if (triCount > 8000 || (width * height) > 1900000)
                            {
                                return 3;
                            }
                            return 2;
                        default:
                            return 2;
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
                            if (textureAlpha <= 2)
                            {
                                continue;
                            }

                            colorArgb = textureArgb;
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
                if (textureIndex < 0 || textureIndex >= textures.Length)
                {
                    return false;
                }

                PreviewTextureData found = textures[textureIndex];
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
                if (overlayTextureIndex < 0 || overlayTextureIndex >= textures.Length)
                {
                    return false;
                }
                if (overlayTextureIndex == baseTextureIndex)
                {
                    return false;
                }

                PreviewTextureData found = textures[overlayTextureIndex];
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
                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
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

                // Slight darkening to recover deeper blacks and avoid washed look.
                r = (float)Math.Pow(Clamp(r, 0f, 1f), 1.05f);
                g = (float)Math.Pow(Clamp(g, 0f, 1f), 1.05f);
                b = (float)Math.Pow(Clamp(b, 0f, 1f), 1.05f);

                float luma = (0.2126f * r) + (0.7152f * g) + (0.0722f * b);
                const float saturation = 1.20f;
                r = luma + ((r - luma) * saturation);
                g = luma + ((g - luma) * saturation);
                b = luma + ((b - luma) * saturation);

                const float contrast = 1.15f;
                r = ((r - 0.5f) * contrast) + 0.5f;
                g = ((g - 0.5f) * contrast) + 0.5f;
                b = ((b - 0.5f) * contrast) + 0.5f;

                // Gentle black-point compression for stronger deep tones.
                const float blackLift = 0.018f;
                r = Clamp((r - blackLift) / (1f - blackLift), 0f, 1f);
                g = Clamp((g - blackLift) / (1f - blackLift), 0f, 1f);
                b = Clamp((b - blackLift) / (1f - blackLift), 0f, 1f);

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

            private static void ComputeBounds(Vector3f[] input, out Vector3f center, out float radius)
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

            private static void ComputeExtents(Vector3f[] input, out float extentX, out float extentY, out float extentZ)
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

            private static Vector3f[] ComputeVertexNormals(Vector3f[] inputVertices, int[] inputIndices)
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
