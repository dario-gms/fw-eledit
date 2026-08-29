using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NpcGenMapPreviewWindow : Form
    {
        private readonly NpcGenMapPreviewData previewData;
        private readonly Action<int, int> selectNpcEntry;
        private readonly MapCanvas canvas;
        private readonly DataGridView markerGrid;
        private readonly Label statusLabel;
        private bool suppressSelection;

        public NpcGenMapPreviewWindow(NpcGenMapPreviewData previewData, Action<int, int> selectNpcEntry)
        {
            this.previewData = previewData ?? new NpcGenMapPreviewData();
            this.selectNpcEntry = selectNpcEntry;
            Text = "NPCGen Map View - " + this.previewData.MapName;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1120, 700);
            Size = new Size(1340, 820);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.FixedPanel = FixedPanel.Panel2;
            split.SizeChanged += (s, e) => SetSafeSplitterDistance(split);
            Controls.Add(split);

            canvas = new MapCanvas(this.previewData);
            canvas.Dock = DockStyle.Fill;
            canvas.MarkerSelected += canvas_MarkerSelected;
            split.Panel1.Controls.Add(canvas);

            TableLayoutPanel side = new TableLayoutPanel();
            side.Dock = DockStyle.Fill;
            side.RowCount = 3;
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            side.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            split.Panel2.Controls.Add(side);

            Label titleLabel = new Label
            {
                Text = this.previewData.MapName,
                Dock = DockStyle.Fill,
                Font = new Font(Font, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            side.Controls.Add(titleLabel, 0, 0);

            markerGrid = CreateGrid();
            markerGrid.RowTemplate.Height = 34;
            markerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "No", Width = 52 });
            markerGrid.Columns.Add(new DataGridViewImageColumn { HeaderText = "", Width = 34, ImageLayout = DataGridViewImageCellLayout.Zoom });
            markerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 72 });
            markerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "NPC / Monster", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            markerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "X", Width = 68 });
            markerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Y", Width = 68 });
            markerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Z", Width = 68 });
            markerGrid.SelectionChanged += markerGrid_SelectionChanged;
            side.Controls.Add(markerGrid, 0, 1);

            statusLabel = new Label
            {
                Text = this.previewData.Status,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            side.Controls.Add(statusLabel, 0, 2);

            PopulateMarkers();
            SelectInitialMarker();
            ApplyDarkTheme(this);
            Shown += (s, e) =>
            {
                split.Panel1MinSize = 300;
                split.Panel2MinSize = 470;
                SetSafeSplitterDistance(split);
                if (canvas.SelectedMarker != null)
                {
                    canvas.CenterOn(canvas.SelectedMarker);
                }
            };
        }

        private static void SetSafeSplitterDistance(SplitContainer split)
        {
            if (split == null || split.Width <= 0)
            {
                return;
            }

            int available = split.Width - split.SplitterWidth;
            int minLeft = split.Panel1MinSize;
            int minRight = split.Panel2MinSize;
            if (available <= minLeft + minRight)
            {
                return;
            }

            int desiredRight = Math.Min(560, Math.Max(minRight, available / 3));
            int desired = available - desiredRight;
            desired = Math.Max(minLeft, Math.Min(desired, available - minRight));
            if (split.SplitterDistance != desired)
            {
                split.SplitterDistance = desired;
            }
        }

        private void PopulateMarkers()
        {
            markerGrid.Rows.Clear();
            for (int i = 0; i < previewData.Markers.Count; i++)
            {
                NpcGenMapMarker marker = previewData.Markers[i];
                int row = markerGrid.Rows.Add(
                    marker.EntryIndex >= 0
                        ? string.Format(CultureInfo.InvariantCulture, "{0}.{1}", marker.AreaIndex, marker.EntryIndex)
                        : marker.AreaIndex.ToString(CultureInfo.InvariantCulture),
                    marker.Icon ?? Properties.Resources.blank,
                    marker.Id,
                    marker.Name,
                    marker.Position.X.ToString("0.##", CultureInfo.InvariantCulture),
                    marker.Position.Y.ToString("0.##", CultureInfo.InvariantCulture),
                    marker.Position.Z.ToString("0.##", CultureInfo.InvariantCulture));
                markerGrid.Rows[row].Tag = marker;
                markerGrid.Rows[row].DefaultCellStyle.ForeColor = marker.Color;
            }
        }

        private void SelectInitialMarker()
        {
            if (previewData == null || previewData.InitialAreaIndex < 0)
            {
                return;
            }

            suppressSelection = true;
            for (int i = 0; i < markerGrid.Rows.Count; i++)
            {
                NpcGenMapMarker marker = markerGrid.Rows[i].Tag as NpcGenMapMarker;
                if (marker == null
                    || marker.AreaIndex != previewData.InitialAreaIndex
                    || marker.EntryIndex != previewData.InitialEntryIndex)
                {
                    continue;
                }

                markerGrid.ClearSelection();
                markerGrid.Rows[i].Selected = true;
                markerGrid.CurrentCell = markerGrid.Rows[i].Cells[0];
                canvas.SelectedMarker = marker;
                break;
            }
            suppressSelection = false;
        }

        private void markerGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (suppressSelection || markerGrid.CurrentRow == null)
            {
                return;
            }

            NpcGenMapMarker marker = markerGrid.CurrentRow.Tag as NpcGenMapMarker;
            if (marker == null)
            {
                return;
            }

            canvas.SelectedMarker = marker;
            canvas.CenterOn(marker);
            if (marker.Kind == "Monster" || marker.Kind == "NPC")
            {
                selectNpcEntry?.Invoke(marker.AreaIndex, marker.EntryIndex);
            }
        }

        private void canvas_MarkerSelected(object sender, NpcGenMapMarker marker)
        {
            if (marker == null)
            {
                return;
            }

            suppressSelection = true;
            foreach (DataGridViewRow row in markerGrid.Rows)
            {
                if (ReferenceEquals(row.Tag, marker))
                {
                    row.Selected = true;
                    markerGrid.CurrentCell = row.Cells[0];
                    break;
                }
            }
            suppressSelection = false;

            if (marker.Kind == "Monster" || marker.Kind == "NPC")
            {
                selectNpcEntry?.Invoke(marker.AreaIndex, marker.EntryIndex);
            }
        }

        private static DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.BackgroundColor = Color.FromArgb(17, 21, 26);
            grid.GridColor = Color.FromArgb(48, 56, 64);
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(39, 45, 52);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.BackColor = Color.FromArgb(17, 21, 26);
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(68, 115, 155);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.EnableHeadersVisualStyles = false;
            return grid;
        }

        private void ApplyDarkTheme(Control root)
        {
            root.BackColor = Color.FromArgb(17, 21, 26);
            root.ForeColor = Color.White;
            foreach (Control control in root.Controls)
            {
                if (control is Button)
                {
                    control.BackColor = Color.FromArgb(21, 25, 30);
                    control.ForeColor = Color.White;
                }
                ApplyDarkTheme(control);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && previewData != null && previewData.HeightMapImage != null)
            {
                previewData.HeightMapImage.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class MapCanvas : Control
        {
            private const float MarkerHitRadius = 10f;

            private readonly NpcGenMapPreviewData previewData;
            private PointF pan;
            private float zoom = 1f;
            private bool dragging;
            private Point lastMouse;
            private NpcGenMapMarker selectedMarker;

            public event EventHandler<NpcGenMapMarker> MarkerSelected;

            public MapCanvas(NpcGenMapPreviewData previewData)
            {
                this.previewData = previewData;
                DoubleBuffered = true;
                BackColor = Color.FromArgb(12, 15, 18);
                ForeColor = Color.White;
                TabStop = true;
            }

            public NpcGenMapMarker SelectedMarker
            {
                get { return selectedMarker; }
                set
                {
                    selectedMarker = value;
                    Invalidate();
                }
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                FitToBounds();
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                if (previewData == null || previewData.WorldBounds.Width <= 0f)
                {
                    return;
                }

                PointF before = ScreenToWorld(e.Location);
                float factor = e.Delta > 0 ? 1.15f : 1f / 1.15f;
                zoom = Math.Max(0.05f, Math.Min(24f, zoom * factor));
                PointF after = WorldToScreen(before);
                pan.X += e.X - after.X;
                pan.Y += e.Y - after.Y;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                Focus();
                if (e.Button == MouseButtons.Left)
                {
                    NpcGenMapMarker marker = HitTest(e.Location);
                    if (marker != null)
                    {
                        selectedMarker = marker;
                        MarkerSelected?.Invoke(this, marker);
                        Invalidate();
                        return;
                    }
                }

                if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
                {
                    dragging = true;
                    lastMouse = e.Location;
                    Cursor = Cursors.SizeAll;
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                if (dragging)
                {
                    pan.X += e.X - lastMouse.X;
                    pan.Y += e.Y - lastMouse.Y;
                    lastMouse = e.Location;
                    Invalidate();
                    return;
                }

                Cursor = HitTest(e.Location) == null ? Cursors.Default : Cursors.Hand;
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                dragging = false;
                Cursor = Cursors.Default;
            }

            protected override void OnDoubleClick(EventArgs e)
            {
                base.OnDoubleClick(e);
                FitToBounds();
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (e.KeyCode == Keys.R)
                {
                    FitToBounds();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                DrawBackground(g);
                DrawGrid(g);
                DrawMarkers(g);
                DrawHud(g);
            }

            public void CenterOn(NpcGenMapMarker marker)
            {
                if (marker == null)
                {
                    return;
                }

                PointF markerPoint = new PointF(marker.Position.X, marker.Position.Z);
                RectangleF bounds = previewData != null ? previewData.WorldBounds : RectangleF.Empty;
                if (bounds.Width > 0f && bounds.Height > 0f && !bounds.Contains(markerPoint))
                {
                    FitToBoundsIncluding(markerPoint);
                    return;
                }

                PointF screen = WorldToScreen(markerPoint);
                pan.X += ClientSize.Width / 2f - screen.X;
                pan.Y += ClientSize.Height / 2f - screen.Y;
                Invalidate();
            }

            private void FitToBounds()
            {
                if (ClientSize.Width <= 0 || ClientSize.Height <= 0 || previewData == null || previewData.WorldBounds.Width <= 0f)
                {
                    return;
                }

                ApplyViewBounds(previewData.WorldBounds, 40f);
            }

            private void FitToBoundsIncluding(PointF point)
            {
                if (ClientSize.Width <= 0 || ClientSize.Height <= 0 || previewData == null || previewData.WorldBounds.Width <= 0f)
                {
                    return;
                }

                RectangleF mapBounds = previewData.WorldBounds;
                RectangleF bounds = RectangleF.FromLTRB(
                    Math.Min(mapBounds.Left, point.X),
                    Math.Min(mapBounds.Top, point.Y),
                    Math.Max(mapBounds.Right, point.X),
                    Math.Max(mapBounds.Bottom, point.Y));
                float padding = Math.Max(24f, Math.Max(bounds.Width, bounds.Height) * 0.08f);
                bounds.Inflate(padding, padding);
                ApplyViewBounds(bounds, 48f);
            }

            private void ApplyViewBounds(RectangleF bounds, float margin)
            {
                if (bounds.Width <= 0f || bounds.Height <= 0f)
                {
                    return;
                }

                float availableWidth = Math.Max(1f, ClientSize.Width - margin);
                float availableHeight = Math.Max(1f, ClientSize.Height - margin);
                float scaleX = availableWidth / bounds.Width;
                float scaleY = availableHeight / bounds.Height;
                zoom = Math.Max(0.05f, Math.Min(scaleX, scaleY));
                PointF center = WorldToScreenNoPan(new PointF(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f));
                pan = new PointF(ClientSize.Width / 2f - center.X, ClientSize.Height / 2f - center.Y);
                Invalidate();
            }

            private void DrawBackground(Graphics g)
            {
                RectangleF bounds = previewData != null ? previewData.WorldBounds : RectangleF.Empty;
                if (bounds.Width <= 0f || bounds.Height <= 0f)
                {
                    g.Clear(BackColor);
                    return;
                }

                RectangleF dest = WorldRectToScreen(bounds);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(33, 36, 34)))
                {
                    g.FillRectangle(brush, dest);
                }
                using (Pen border = new Pen(Color.FromArgb(118, 125, 116)))
                {
                    g.DrawRectangle(border, dest.X, dest.Y, dest.Width, dest.Height);
                }

                if (previewData.HeightMapImage != null)
                {
                    using (ImageAttributes attributes = new ImageAttributes())
                    {
                        attributes.SetWrapMode(WrapMode.TileFlipXY);
                        g.DrawImage(
                            previewData.HeightMapImage,
                            Rectangle.Round(dest),
                            0,
                            0,
                            previewData.HeightMapImage.Width,
                            previewData.HeightMapImage.Height,
                            GraphicsUnit.Pixel,
                            attributes);
                    }
                }
            }

            private void DrawGrid(Graphics g)
            {
                RectangleF bounds = previewData != null ? previewData.WorldBounds : RectangleF.Empty;
                if (bounds.Width <= 0f || bounds.Height <= 0f)
                {
                    return;
                }

                float step = ChooseGridStep(bounds);
                using (Pen pen = new Pen(Color.FromArgb(28, 255, 255, 255)))
                {
                    for (float x = (float)Math.Ceiling(bounds.Left / step) * step; x <= bounds.Right; x += step)
                    {
                        PointF a = WorldToScreen(new PointF(x, bounds.Top));
                        PointF b = WorldToScreen(new PointF(x, bounds.Bottom));
                        g.DrawLine(pen, a, b);
                    }
                    for (float z = (float)Math.Ceiling(bounds.Top / step) * step; z <= bounds.Bottom; z += step)
                    {
                        PointF a = WorldToScreen(new PointF(bounds.Left, z));
                        PointF b = WorldToScreen(new PointF(bounds.Right, z));
                        g.DrawLine(pen, a, b);
                    }
                }

                using (Pen axis = new Pen(Color.FromArgb(90, 255, 255, 255)))
                {
                    PointF x1 = WorldToScreen(new PointF(bounds.Left, 0f));
                    PointF x2 = WorldToScreen(new PointF(bounds.Right, 0f));
                    PointF z1 = WorldToScreen(new PointF(0f, bounds.Top));
                    PointF z2 = WorldToScreen(new PointF(0f, bounds.Bottom));
                    g.DrawLine(axis, x1, x2);
                    g.DrawLine(axis, z1, z2);
                }
            }

            private static float ChooseGridStep(RectangleF bounds)
            {
                float size = Math.Max(bounds.Width, bounds.Height);
                if (size > 4096f)
                {
                    return 512f;
                }
                if (size > 2048f)
                {
                    return 256f;
                }
                if (size > 1024f)
                {
                    return 128f;
                }
                return 64f;
            }

            private void DrawMarkers(Graphics g)
            {
                if (previewData == null || previewData.Markers == null)
                {
                    return;
                }

                float iconZoomThreshold = GetIconZoomThreshold();
                bool drawIcons = zoom >= iconZoomThreshold;
                foreach (NpcGenMapMarker marker in previewData.Markers)
                {
                    PointF center = WorldToScreen(new PointF(marker.Position.X, marker.Position.Z));
                    bool selected = ReferenceEquals(marker, selectedMarker);
                    if (drawIcons && marker.Icon != null)
                    {
                        DrawMarkerIcon(g, marker, center, selected, GetMarkerIconSize(selected, iconZoomThreshold));
                    }
                    else
                    {
                        DrawMarkerDot(g, marker, center, selected);
                    }

                    if (selected)
                    {
                        DrawSelectedMarkerLabel(g, marker, center);
                    }
                }
            }

            private static void DrawMarkerDot(Graphics g, NpcGenMapMarker marker, PointF center, bool selected)
            {
                float radius = selected ? 8f : 5f;
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(230, marker.Color)))
                using (Pen outline = new Pen(Color.Black, 2f))
                {
                    g.FillEllipse(brush, center.X - radius, center.Y - radius, radius * 2f, radius * 2f);
                    g.DrawEllipse(outline, center.X - radius, center.Y - radius, radius * 2f, radius * 2f);
                }
            }

            private static void DrawMarkerIcon(Graphics g, NpcGenMapMarker marker, PointF center, bool selected, float size)
            {
                RectangleF iconRect = new RectangleF(center.X - size / 2f, center.Y - size / 2f, size, size);
                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                using (Pen border = new Pen(marker.Color, selected ? 3f : 2f))
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    g.FillEllipse(shadow, iconRect.X - 2f, iconRect.Y + 2f, iconRect.Width + 4f, iconRect.Height + 4f);
                    g.DrawImage(
                        marker.Icon,
                        Rectangle.Round(iconRect),
                        0,
                        0,
                        marker.Icon.Width,
                        marker.Icon.Height,
                        GraphicsUnit.Pixel,
                        attributes);
                    g.DrawEllipse(border, iconRect);
                    using (Pen outline = new Pen(Color.Black, 1f))
                    {
                        g.DrawEllipse(outline, iconRect.X - 1f, iconRect.Y - 1f, iconRect.Width + 2f, iconRect.Height + 2f);
                    }
                }
            }

            private void DrawSelectedMarkerLabel(Graphics g, NpcGenMapMarker marker, PointF center)
            {
                string text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} [{1}] X:{2:0.##} Z:{3:0.##}",
                    marker.Name,
                    marker.Id,
                    marker.Position.X,
                    marker.Position.Z);
                SizeF size = g.MeasureString(text, Font);
                RectangleF box = new RectangleF(center.X + 10f, center.Y - size.Height - 8f, size.Width + 12f, size.Height + 8f);
                if (box.Right > ClientSize.Width - 8f)
                {
                    box.X = ClientSize.Width - box.Width - 8f;
                }
                if (box.Left < 8f)
                {
                    box.X = 8f;
                }
                if (box.Top < 8f)
                {
                    box.Y = center.Y + 12f;
                }
                if (box.Bottom > ClientSize.Height - 8f)
                {
                    box.Y = ClientSize.Height - box.Height - 8f;
                }
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(220, 20, 24, 30)))
                using (SolidBrush fg = new SolidBrush(Color.White))
                using (Pen border = new Pen(marker.Color))
                {
                    g.FillRectangle(bg, box);
                    g.DrawRectangle(border, box.X, box.Y, box.Width, box.Height);
                    g.DrawString(text, Font, fg, box.X + 6f, box.Y + 4f);
                }
            }

            private void DrawHud(Graphics g)
            {
                string text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Mouse wheel: zoom | Drag: pan | Double-click/R: reset | Zoom {0:0.##}x",
                    zoom);
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(190, 12, 15, 18)))
                using (SolidBrush fg = new SolidBrush(Color.White))
                {
                    SizeF size = g.MeasureString(text, Font);
                    RectangleF box = new RectangleF(12f, 12f, size.Width + 12f, size.Height + 8f);
                    g.FillRectangle(bg, box);
                    g.DrawString(text, Font, fg, box.X + 6f, box.Y + 4f);
                }
            }

            private NpcGenMapMarker HitTest(Point point)
            {
                if (previewData == null || previewData.Markers == null)
                {
                    return null;
                }

                for (int i = previewData.Markers.Count - 1; i >= 0; i--)
                {
                    NpcGenMapMarker marker = previewData.Markers[i];
                    PointF center = WorldToScreen(new PointF(marker.Position.X, marker.Position.Z));
                    float dx = center.X - point.X;
                    float dy = center.Y - point.Y;
                    float iconZoomThreshold = GetIconZoomThreshold();
                    float hitRadius = zoom >= iconZoomThreshold
                        ? Math.Max(IconHitRadius(), GetMarkerIconSize(false, iconZoomThreshold) / 2f)
                        : MarkerHitRadius;
                    if ((dx * dx) + (dy * dy) <= hitRadius * hitRadius)
                    {
                        return marker;
                    }
                }
                return null;
            }

            private float GetMarkerIconSize(bool selected, float iconZoomThreshold)
            {
                float growth = iconZoomThreshold > 0f ? zoom / iconZoomThreshold : 1f;
                growth = Math.Max(1f, Math.Min(2.2f, growth));
                float baseSize = selected ? 28f : 20f;
                float maxSize = selected ? 48f : 36f;
                return Math.Min(maxSize, baseSize * growth);
            }

            private static float IconHitRadius()
            {
                return 15f;
            }

            private float GetIconZoomThreshold()
            {
                RectangleF bounds = previewData != null ? previewData.WorldBounds : RectangleF.Empty;
                float mapSize = Math.Max(bounds.Width, bounds.Height);
                if (mapSize <= 0f)
                {
                    return 4f;
                }

                if (mapSize <= 600f)
                {
                    return 8f;
                }

                if (mapSize <= 1000f)
                {
                    return 5f;
                }

                return 2.5f;
            }

            private PointF WorldToScreen(PointF world)
            {
                PointF point = WorldToScreenNoPan(world);
                point.X += pan.X;
                point.Y += pan.Y;
                return point;
            }

            private PointF WorldToScreenNoPan(PointF world)
            {
                RectangleF bounds = previewData.WorldBounds;
                return new PointF(
                    (world.X - bounds.Left) * zoom,
                    (bounds.Bottom - world.Y) * zoom);
            }

            private PointF ScreenToWorld(Point point)
            {
                RectangleF bounds = previewData.WorldBounds;
                return new PointF(
                    ((point.X - pan.X) / zoom) + bounds.Left,
                    bounds.Bottom - ((point.Y - pan.Y) / zoom));
            }

            private RectangleF WorldRectToScreen(RectangleF world)
            {
                PointF topLeft = WorldToScreen(new PointF(world.Left, world.Bottom));
                PointF bottomRight = WorldToScreen(new PointF(world.Right, world.Top));
                return RectangleF.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
            }
        }
    }
}
