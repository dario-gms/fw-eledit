using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace sELedit
{
    public sealed class IconPickerWindow : Form
    {
        private sealed class IconEntry
        {
            public int PathId;
            public int AtlasIndex;
            public string Key;
            public string FileName;
        }

        private sealed class AtlasGrid : ScrollableControl
        {
            private readonly ToolTip tip;
            private List<IconEntry> entries = new List<IconEntry>();
            private Bitmap atlas;
            private int iconW = 32;
            private int iconH = 32;
            private int atlasCols = 1;
            private int cellW = 34;
            private int cellH = 34;
            private int columns = 1;
            private int selectedIndex = -1;
            private int hoverIndex = -1;
            private Func<IconEntry, string> tooltipFactory;

            public event EventHandler SelectedIndexChanged;
            public event EventHandler ItemDoubleClick;

            public AtlasGrid()
            {
                DoubleBuffered = true;
                AutoScroll = true;
                BackColor = Color.Black;
                tip = new ToolTip();
            }

            public int SelectedIndex
            {
                get { return selectedIndex; }
                set
                {
                    if (value < -1 || value >= entries.Count)
                    {
                        value = -1;
                    }
                    if (selectedIndex == value)
                    {
                        return;
                    }
                    int old = selectedIndex;
                    selectedIndex = value;
                    InvalidateIndex(old);
                    InvalidateIndex(selectedIndex);
                    if (SelectedIndexChanged != null)
                    {
                        SelectedIndexChanged(this, EventArgs.Empty);
                    }
                }
            }

            public IconEntry SelectedEntry
            {
                get
                {
                    if (selectedIndex < 0 || selectedIndex >= entries.Count)
                    {
                        return null;
                    }
                    return entries[selectedIndex];
                }
            }

            public void SetTooltipFactory(Func<IconEntry, string> factory)
            {
                tooltipFactory = factory;
            }

            public void SetData(Bitmap atlasBitmap, int iconWidth, int iconHeight, int cols, List<IconEntry> list)
            {
                atlas = atlasBitmap;
                iconW = Math.Max(1, iconWidth);
                iconH = Math.Max(1, iconHeight);
                atlasCols = Math.Max(1, cols);
                entries = list ?? new List<IconEntry>();
                cellW = iconW + 2;
                cellH = iconH + 2;
                RecalcLayout();
                SelectedIndex = entries.Count > 0 ? 0 : -1;
                Invalidate();
            }

            public void SetEntries(List<IconEntry> list)
            {
                entries = list ?? new List<IconEntry>();
                RecalcLayout();
                if (entries.Count == 0)
                {
                    SelectedIndex = -1;
                }
                else if (SelectedIndex >= entries.Count)
                {
                    SelectedIndex = entries.Count - 1;
                }
                Invalidate();
            }

            public void EnsureSelectionVisible()
            {
                if (selectedIndex < 0 || selectedIndex >= entries.Count)
                {
                    return;
                }

                int row = selectedIndex / Math.Max(1, columns);
                int y = row * cellH;
                int top = -AutoScrollPosition.Y;
                int bottom = top + ClientSize.Height;
                if (y < top)
                {
                    AutoScrollPosition = new Point(0, y);
                }
                else if (y + cellH > bottom)
                {
                    AutoScrollPosition = new Point(0, Math.Max(0, y + cellH - ClientSize.Height));
                }
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                RecalcLayout();
                Invalidate();
            }

            private void RecalcLayout()
            {
                columns = Math.Max(1, ClientSize.Width / Math.Max(1, cellW));
                int rows = entries.Count == 0 ? 0 : ((entries.Count + columns - 1) / columns);
                AutoScrollMinSize = new Size(0, Math.Max(0, rows * cellH + 2));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (atlas == null || entries.Count == 0)
                {
                    return;
                }

                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                e.Graphics.SmoothingMode = SmoothingMode.None;

                int offsetX = AutoScrollPosition.X;
                int offsetY = AutoScrollPosition.Y;
                int startRow = Math.Max(0, (-offsetY) / Math.Max(1, cellH));
                int endRow = Math.Min((entries.Count + columns - 1) / columns, ((-offsetY + ClientSize.Height) / Math.Max(1, cellH)) + 2);

                for (int row = startRow; row <= endRow; row++)
                {
                    int start = row * columns;
                    int end = Math.Min(entries.Count, start + columns);
                    for (int i = start; i < end; i++)
                    {
                        int col = i - start;
                        int x = offsetX + col * cellW + 1;
                        int y = offsetY + row * cellH + 1;

                        IconEntry entry = entries[i];
                        int aCol = entry.AtlasIndex % atlasCols;
                        int aRow = entry.AtlasIndex / atlasCols;
                        Rectangle src = new Rectangle(aCol * iconW, aRow * iconH, iconW, iconH);
                        Rectangle dst = new Rectangle(x, y, iconW, iconH);

                        if (src.X >= 0 && src.Y >= 0 && src.Right <= atlas.Width && src.Bottom <= atlas.Height)
                        {
                            e.Graphics.DrawImage(atlas, dst, src, GraphicsUnit.Pixel);
                        }
                        else
                        {
                            using (Brush b = new SolidBrush(Color.FromArgb(36, 36, 36)))
                            {
                                e.Graphics.FillRectangle(b, dst);
                            }
                        }

                        if (i == selectedIndex)
                        {
                            using (Pen p = new Pen(Color.Gold, 2))
                            {
                                e.Graphics.DrawRectangle(p, dst);
                            }
                            using (Pen p2 = new Pen(Color.FromArgb(255, 255, 220, 120), 1))
                            {
                                Rectangle glow = new Rectangle(dst.X - 1, dst.Y - 1, dst.Width + 2, dst.Height + 2);
                                e.Graphics.DrawRectangle(p2, glow);
                            }
                        }
                        else if (i == hoverIndex)
                        {
                            using (Pen hp = new Pen(Color.FromArgb(220, 120, 190, 255), 2))
                            {
                                e.Graphics.DrawRectangle(hp, dst);
                            }
                        }
                    }
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int idx = HitTestIndex(e.Location);
                if (idx != hoverIndex)
                {
                    int oldHover = hoverIndex;
                    hoverIndex = idx;
                    InvalidateIndex(oldHover);
                    InvalidateIndex(hoverIndex);
                }
                if (idx < 0 || idx >= entries.Count || tooltipFactory == null)
                {
                    tip.SetToolTip(this, string.Empty);
                    return;
                }

                string text = tooltipFactory(entries[idx]);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    tip.SetToolTip(this, text);
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (hoverIndex != -1)
                {
                    int oldHover = hoverIndex;
                    hoverIndex = -1;
                    InvalidateIndex(oldHover);
                }
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                int idx = HitTestIndex(e.Location);
                if (idx < 0 || idx >= entries.Count)
                {
                    return;
                }

                SelectedIndex = idx;
                if (e.Button == MouseButtons.Left)
                {
                    if (ItemDoubleClick != null)
                    {
                        ItemDoubleClick(this, EventArgs.Empty);
                    }
                }
            }

            private int HitTestIndex(Point p)
            {
                int x = p.X - AutoScrollPosition.X;
                int y = p.Y - AutoScrollPosition.Y;
                if (x < 0 || y < 0)
                {
                    return -1;
                }

                int col = x / Math.Max(1, cellW);
                int row = y / Math.Max(1, cellH);
                if (col < 0 || col >= columns)
                {
                    return -1;
                }
                int idx = row * columns + col;
                if (idx < 0 || idx >= entries.Count)
                {
                    return -1;
                }
                return idx;
            }

            private void InvalidateIndex(int idx)
            {
                Rectangle rect = GetCellRect(idx);
                if (!rect.IsEmpty)
                {
                    Invalidate(rect);
                }
            }

            private Rectangle GetCellRect(int idx)
            {
                if (idx < 0 || idx >= entries.Count || columns <= 0)
                {
                    return Rectangle.Empty;
                }

                int row = idx / columns;
                int col = idx % columns;
                int x = AutoScrollPosition.X + col * cellW + 1;
                int y = AutoScrollPosition.Y + row * cellH + 1;
                return new Rectangle(x - 2, y - 2, iconW + 5, iconH + 5);
            }
        }

        private readonly CacheSave database;
        private readonly Dictionary<int, int> usageByPathId;
        private readonly Dictionary<string, int> usageByIconKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly int pendingSelectPathId;
        private readonly List<IconEntry> allEntries = new List<IconEntry>();
        private List<IconEntry> currentEntries = new List<IconEntry>();

        private TextBox searchBox;
        private AtlasGrid grid;
        private Label statusLabel;
        private Button okButton;
        private Button cancelButton;

        public int SelectedPathId { get; private set; }

        public static void BeginBackgroundWarmup(CacheSave db)
        {
            // Atlas mode is instant and does not require thumbnail warmup.
        }

        public static void CancelBackgroundWarmup()
        {
            // Atlas mode is instant and does not require thumbnail warmup.
        }

        public IconPickerWindow(CacheSave db, int currentPathId, Dictionary<int, int> usageLookup)
        {
            database = db;
            pendingSelectPathId = currentPathId;
            usageByPathId = usageLookup ?? new Dictionary<int, int>();

            InitializeUi();
            Load += IconPickerWindow_Load;
            KeyPreview = true;
            KeyDown += IconPickerWindow_KeyDown;
        }

        private void InitializeUi()
        {
            Text = "Image Picker [ESC - Cancel]";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Normal;
            Size = new Size(1280, 820);
            MinimumSize = new Size(900, 600);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            Controls.Add(root);

            Panel top = new Panel { Dock = DockStyle.Fill };
            root.Controls.Add(top, 0, 0);

            Label searchLabel = new Label { Text = "Search:", AutoSize = true, Location = new Point(8, 9) };
            top.Controls.Add(searchLabel);

            searchBox = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(62, 6),
                Width = 450
            };
            searchBox.TextChanged += searchBox_TextChanged;
            top.Controls.Add(searchBox);

            grid = new AtlasGrid { Dock = DockStyle.Fill };
            grid.SelectedIndexChanged += grid_SelectedIndexChanged;
            grid.ItemDoubleClick += grid_ItemDoubleClick;
            grid.SetTooltipFactory(BuildTooltip);
            root.Controls.Add(grid, 0, 1);

            Panel bottom = new Panel { Dock = DockStyle.Fill };
            root.Controls.Add(bottom, 0, 2);

            statusLabel = new Label { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            bottom.Controls.Add(statusLabel);

            okButton = new Button { Text = "OK", Width = 90, Dock = DockStyle.Right };
            okButton.Click += okButton_Click;
            bottom.Controls.Add(okButton);

            cancelButton = new Button { Text = "Cancel", Width = 90, Dock = DockStyle.Right };
            cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            bottom.Controls.Add(cancelButton);
        }

        private void IconPickerWindow_Load(object sender, EventArgs e)
        {
            BuildAtlasOrderedEntries();
            ApplyFilter();
            SelectPending();
            searchBox.Focus();
        }

        private void BuildAtlasOrderedEntries()
        {
            allEntries.Clear();
            if (database == null || database.sourceBitmap == null || database.imagesx == null || database.imagesx.Count == 0)
            {
                return;
            }

            Dictionary<string, List<int>> pathIdsByKey = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            if (database.pathById != null)
            {
                foreach (KeyValuePair<int, string> kv in database.pathById)
                {
                    if (kv.Key <= 0)
                    {
                        continue;
                    }
                    string key = ResolveIconKeyForStoredPathId(kv.Key);
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }
                    List<int> ids;
                    if (!pathIdsByKey.TryGetValue(key, out ids))
                    {
                        ids = new List<int>();
                        pathIdsByKey[key] = ids;
                    }
                    if (!ids.Contains(kv.Key))
                    {
                        ids.Add(kv.Key);
                    }
                }
            }

            usageByIconKey.Clear();
            foreach (KeyValuePair<int, int> usage in usageByPathId)
            {
                string key = ResolveIconKeyForStoredPathId(usage.Key);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }
                int count;
                usageByIconKey.TryGetValue(key, out count);
                usageByIconKey[key] = count + usage.Value;
            }

            int cols = Math.Max(1, database.cols);
            for (int i = 0; i < database.imagesx.Count; i++)
            {
                string key = NormalizeKey(database.imagesx.Values[i]);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                List<int> candidates;
                if (!pathIdsByKey.TryGetValue(key, out candidates) || candidates.Count == 0)
                {
                    continue;
                }
                int pathId = ChooseBestPathId(candidates);
                if (pathId <= 0)
                {
                    continue;
                }

                allEntries.Add(new IconEntry
                {
                    PathId = pathId,
                    AtlasIndex = i,
                    Key = key,
                    FileName = Path.GetFileName(key)
                });
            }

            currentEntries = new List<IconEntry>(allEntries);
            statusLabel.Text = allEntries.Count + " icons";
            grid.SetData(database.sourceBitmap, Math.Max(1, database.iconWidth), Math.Max(1, database.iconHeight), cols, allEntries);
        }

        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string search = (searchBox.Text ?? string.Empty).Trim();
            List<IconEntry> filtered = allEntries;
            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = allEntries.Where(x =>
                    x.PathId.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                    || (!string.IsNullOrEmpty(x.FileName) && x.FileName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(x.Key) && x.Key.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();
            }

            currentEntries = filtered;
            grid.SetEntries(filtered);
            statusLabel.Text = filtered.Count + " icons";
        }

        private void SelectPending()
        {
            if (pendingSelectPathId <= 0)
            {
                return;
            }

            for (int i = 0; i < currentEntries.Count; i++)
            {
                if (currentEntries[i].PathId == pendingSelectPathId)
                {
                    grid.SelectedIndex = i;
                    grid.EnsureSelectionVisible();
                    break;
                }
            }
        }

        private void grid_SelectedIndexChanged(object sender, EventArgs e)
        {
            IconEntry entry = grid.SelectedEntry;
            if (entry == null)
            {
                statusLabel.Text = allEntries.Count + " icons";
                return;
            }

            int usedCount;
            usageByPathId.TryGetValue(entry.PathId, out usedCount);
            int keyCount;
            if (usageByIconKey.TryGetValue(entry.Key, out keyCount))
            {
                usedCount = Math.Max(usedCount, keyCount);
            }
            statusLabel.Text = "PathID: " + entry.PathId + " | Icon: " + entry.FileName + " | Used times: " + usedCount;
        }

        private void grid_ItemDoubleClick(object sender, EventArgs e)
        {
            CommitSelection();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            CommitSelection();
        }

        private void CommitSelection()
        {
            IconEntry entry = grid.SelectedEntry;
            if (entry == null)
            {
                return;
            }

            SelectedPathId = entry.PathId;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void IconPickerWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private string BuildTooltip(IconEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            int usedCount;
            usageByPathId.TryGetValue(entry.PathId, out usedCount);
            int keyCount;
            if (usageByIconKey.TryGetValue(entry.Key, out keyCount))
            {
                usedCount = Math.Max(usedCount, keyCount);
            }
            return "PathID: " + entry.PathId
                + "\nIcon: " + entry.FileName
                + "\nUsed times: " + usedCount;
        }

        private int ChooseBestPathId(List<int> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return 0;
            }

            if (pendingSelectPathId > 0 && candidates.Contains(pendingSelectPathId))
            {
                return pendingSelectPathId;
            }

            int bestId = candidates[0];
            int bestScore = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                int id = candidates[i];
                int score;
                usageByPathId.TryGetValue(id, out score);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = id;
                }
            }
            return bestId;
        }

        private string ResolveIconKeyForStoredPathId(int pathId)
        {
            if (database == null || database.pathById == null || database.pathById.Count == 0 || pathId <= 0)
            {
                return string.Empty;
            }

            int[] candidates = new int[] { pathId + 1, pathId, pathId - 1 };
            for (int i = 0; i < candidates.Length; i++)
            {
                int candidate = candidates[i];
                if (candidate <= 0 || !database.pathById.ContainsKey(candidate))
                {
                    continue;
                }
                string raw = database.pathById[candidate];
                string key = NormalizeKey(raw);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    return key;
                }
            }
            return string.Empty;
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            string k = value.Trim().Replace('/', '\\');
            return Path.GetFileName(k);
        }
    }
}
