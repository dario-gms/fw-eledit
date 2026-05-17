using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FWEledit
{
    public class ModelPickerWindow : Form
    {
        private readonly List<ModelPickerEntry> allEntries;
        private List<ModelPickerEntry> filteredEntries;
        private readonly HashSet<string> loadedPackages;
        private readonly Func<string, List<ModelPickerEntry>> packageEntriesLoader;
        private readonly Func<string, Bitmap> iconLoader;
        private readonly Dictionary<string, Bitmap> iconBitmapCache;
        private readonly ComboBox packageFilter;
        private readonly ComboBox folderFilter;
        private readonly TextBox searchBox;
        private readonly CheckBox onlyUnusedCheck;
        private readonly CheckBox onlyGfxCheck;
        private readonly Button exportCsvButton;
        private readonly Button previewButton;
        private readonly DataGridView grid;
        private readonly Label statusLabel;
        private readonly System.Windows.Forms.Timer filterTimer;
        private readonly System.Windows.Forms.Timer autoPreviewTimer;
        private readonly AssetManager previewAssetManager;
        private readonly ModelPreviewService modelPreviewService;
        private bool previewWindowOpened;
        private int previewLoadInProgress;
        private int previewPendingAuto;
        private int lastPreviewPathId;
        private int previewSelectionVersion;
        private int nextEntryIndex;
        private bool isInitializing;
        public int SelectedPathId { get; private set; }
        public string SelectedMappedPath { get; private set; }

        public ModelPickerWindow(
            List<ModelPickerEntry> entries,
            int currentPathId,
            string preferredPackage,
            Func<string, List<ModelPickerEntry>> packageEntriesLoader = null,
            Func<string, Bitmap> iconLoader = null)
        {
            allEntries = entries ?? new List<ModelPickerEntry>();
            filteredEntries = new List<ModelPickerEntry>();
            loadedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            this.packageEntriesLoader = packageEntriesLoader;
            this.iconLoader = iconLoader;
            iconBitmapCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
            SelectedPathId = currentPathId;
            SelectedMappedPath = string.Empty;
            previewAssetManager = new AssetManager();
            modelPreviewService = new ModelPreviewService();
            isInitializing = true;
            nextEntryIndex = 1;

            for (int i = 0; i < allEntries.Count; i++)
            {
                ModelPickerEntry entry = allEntries[i];
                if (!string.IsNullOrWhiteSpace(entry?.Package))
                {
                    loadedPackages.Add(entry.Package);
                }

                if (entry != null && entry.Index >= nextEntryIndex)
                {
                    nextEntryIndex = entry.Index + 1;
                }
            }

            Text = "Choice Model";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 550);
            Size = new Size(1200, 760);
            KeyPreview = true;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            KeyDown += OnKeyDown;

            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 40;
            top.Padding = new Padding(8, 8, 8, 4);

            packageFilter = new ComboBox();
            packageFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            packageFilter.Width = 120;
            packageFilter.Left = 8;
            packageFilter.Top = 8;
            packageFilter.SelectedIndexChanged += (s, e) =>
            {
                if (!isInitializing)
                {
                    EnsurePackageLoaded(Convert.ToString(packageFilter.SelectedItem));
                }
                BuildFolderFilter();
                ScheduleFilterApply();
            };
            top.Controls.Add(packageFilter);

            folderFilter = new ComboBox();
            folderFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            folderFilter.Width = 110;
            folderFilter.Left = packageFilter.Right + 8;
            folderFilter.Top = 8;
            folderFilter.SelectedIndexChanged += (s, e) => ScheduleFilterApply();
            top.Controls.Add(folderFilter);

            searchBox = new TextBox();
            searchBox.Left = folderFilter.Right + 8;
            searchBox.Top = 8;
            searchBox.Width = 400;
            searchBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            searchBox.TextChanged += (s, e) => ScheduleFilterApply();
            top.Controls.Add(searchBox);

            onlyUnusedCheck = new CheckBox();
            onlyUnusedCheck.Text = "Keep Only Unused";
            onlyUnusedCheck.AutoSize = true;
            onlyUnusedCheck.Left = searchBox.Right + 14;
            onlyUnusedCheck.Top = 10;
            onlyUnusedCheck.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            onlyUnusedCheck.CheckedChanged += (s, e) => ScheduleFilterApply();
            top.Controls.Add(onlyUnusedCheck);

            onlyGfxCheck = new CheckBox();
            onlyGfxCheck.Text = "Only .gfx";
            onlyGfxCheck.AutoSize = true;
            onlyGfxCheck.Left = onlyUnusedCheck.Right + 12;
            onlyGfxCheck.Top = 10;
            onlyGfxCheck.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            onlyGfxCheck.CheckedChanged += (s, e) => ScheduleFilterApply();
            top.Controls.Add(onlyGfxCheck);

            exportCsvButton = new Button();
            exportCsvButton.Text = "Export CSV";
            exportCsvButton.AutoSize = true;
            exportCsvButton.Height = 24;
            exportCsvButton.Left = onlyGfxCheck.Right + 12;
            exportCsvButton.Top = 8;
            exportCsvButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            exportCsvButton.Click += (s, e) => ExportCsv();
            top.Controls.Add(exportCsvButton);

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.VirtualMode = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.ReadOnly = true;
            grid.BackgroundColor = Color.FromArgb(24, 24, 24);
            grid.GridColor = Color.FromArgb(40, 40, 40);
            grid.DefaultCellStyle.BackColor = Color.FromArgb(24, 24, 24);
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(44, 118, 171);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.WhiteSmoke;
            grid.EnableHeadersVisualStyles = false;
            grid.RowTemplate.Height = 24;
            grid.DoubleClick += (s, e) => ConfirmSelection();
            grid.SelectionChanged += (s, e) => ScheduleAutoPreview();
            grid.CellValueNeeded += OnGridCellValueNeeded;
            grid.CellFormatting += OnGridCellFormatting;
            grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    ConfirmSelection();
                    return;
                }

                if (e.KeyCode == Keys.Space)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    PreviewSelectionAsync(true);
                }
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIndex", HeaderText = "#", Width = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPath", HeaderText = "Path", Width = 430 });
            DataGridViewImageColumn iconCol = new DataGridViewImageColumn();
            iconCol.Name = "colIcon";
            iconCol.HeaderText = string.Empty;
            iconCol.Width = 26;
            iconCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
            iconCol.DefaultCellStyle.NullValue = Properties.Resources.NoIcon;
            grid.Columns.Add(iconCol);
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Name", Width = 260 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", Width = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUses", HeaderText = "Uses", Width = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPathId", HeaderText = "PathID", Width = 90 });

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 48;
            bottom.Padding = new Padding(8, 6, 8, 6);

            statusLabel = new Label();
            statusLabel.Dock = DockStyle.Left;
            statusLabel.AutoSize = false;
            statusLabel.Width = 320;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            bottom.Controls.Add(statusLabel);

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel [ESC]";
            cancelButton.Dock = DockStyle.Right;
            cancelButton.Width = 140;
            cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            bottom.Controls.Add(cancelButton);

            previewButton = new Button();
            previewButton.Text = "Preview [Space]";
            previewButton.Dock = DockStyle.Right;
            previewButton.Width = 150;
            previewButton.Click += (s, e) => PreviewSelectionAsync(true);
            bottom.Controls.Add(previewButton);

            Button okButton = new Button();
            okButton.Text = "OK [Enter]";
            okButton.Dock = DockStyle.Right;
            okButton.Width = 140;
            okButton.Click += (s, e) => ConfirmSelection();
            bottom.Controls.Add(okButton);

            Controls.Add(grid);
            Controls.Add(bottom);
            Controls.Add(top);

            filterTimer = new System.Windows.Forms.Timer();
            filterTimer.Interval = 200;
            filterTimer.Tick += (s, e) =>
            {
                filterTimer.Stop();
                ApplyFilter();
            };

            autoPreviewTimer = new System.Windows.Forms.Timer();
            autoPreviewTimer.Interval = 280;
            autoPreviewTimer.Tick += (s, e) =>
            {
                autoPreviewTimer.Stop();
                PreviewSelectionAsync(false);
            };

            BuildPackageFilter();
            string resolvedPreferredPackage = preferredPackage;
            if (string.IsNullOrWhiteSpace(resolvedPreferredPackage))
            {
                resolvedPreferredPackage = GetPackageForPathId(currentPathId);
            }
            if (string.IsNullOrWhiteSpace(resolvedPreferredPackage))
            {
                resolvedPreferredPackage = "models";
            }
            if (!string.IsNullOrWhiteSpace(resolvedPreferredPackage))
            {
                for (int i = 0; i < packageFilter.Items.Count; i++)
                {
                    if (string.Equals(Convert.ToString(packageFilter.Items[i]), resolvedPreferredPackage, StringComparison.OrdinalIgnoreCase))
                    {
                        packageFilter.SelectedIndex = i;
                        break;
                    }
                }
            }
            EnsurePackageLoaded(Convert.ToString(packageFilter.SelectedItem));
            BuildFolderFilter(currentPathId);
            ApplyFilter();
            SelectCurrentPathId(currentPathId);
            isInitializing = false;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void BuildPackageFilter()
        {
            packageFilter.Items.Clear();
            for (int i = 0; i < ModelPickerCatalog.PackageOrder.Length; i++)
            {
                packageFilter.Items.Add(ModelPickerCatalog.PackageOrder[i]);
            }
            if (packageFilter.Items.Count > 0)
            {
                packageFilter.SelectedItem = "models";
                if (packageFilter.SelectedIndex < 0) { packageFilter.SelectedIndex = 0; }
            }
        }

        private void BuildFolderFilter(int currentPathId = 0)
        {
            string package = Convert.ToString(packageFilter.SelectedItem);
            folderFilter.Items.Clear();

            if (string.IsNullOrWhiteSpace(package))
            {
                return;
            }

            HashSet<string> folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < allEntries.Count; i++)
            {
                ModelPickerEntry e = allEntries[i];
                if (!string.Equals(e.Package, package, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string folder = ExtractTopFolder(e.RelativePath, package);
                if (string.IsNullOrWhiteSpace(folder))
                {
                    continue;
                }

                folders.Add(folder);
            }

            List<string> sorted = folders
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            folderFilter.Items.Add(string.Empty);
            for (int i = 0; i < sorted.Count; i++)
            {
                folderFilter.Items.Add(sorted[i]);
            }

            if (folderFilter.Items.Count == 0)
            {
                return;
            }

            folderFilter.SelectedIndex = 0;
        }

        private static string ExtractTopFolder(string relativePath, string package)
        {
            string value = (relativePath ?? string.Empty).Replace('/', '\\').Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(package))
            {
                string packagePrefix = package + "\\";
                if (value.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Substring(packagePrefix.Length);
                }
            }

            int slash = value.IndexOf('\\');
            if (slash <= 0)
            {
                return value;
            }

            return value.Substring(0, slash);
        }

        private void ScheduleFilterApply()
        {
            if (isInitializing)
            {
                return;
            }
            filterTimer.Stop();
            filterTimer.Start();
        }

        private void ApplyFilter()
        {
            EnsurePackageLoaded(Convert.ToString(packageFilter.SelectedItem));
            filteredEntries = GetFilteredEntries();
            grid.RowCount = filteredEntries.Count;
            grid.Invalidate();

            statusLabel.Text = filteredEntries.Count.ToString() + " entries";
            if (grid.RowCount > 0)
            {
                int selectedRow = grid.CurrentCell != null ? grid.CurrentCell.RowIndex : 0;
                if (selectedRow < 0 || selectedRow >= grid.RowCount)
                {
                    selectedRow = 0;
                }
                if (grid.ColumnCount > 0)
                {
                    grid.CurrentCell = grid.Rows[selectedRow].Cells[0];
                }
            }
            ScheduleAutoPreview();
        }

        private void OnGridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            ModelPickerEntry entry = TryGetEntryAtRow(e.RowIndex);
            if (entry == null)
            {
                e.Value = null;
                return;
            }

            switch (e.ColumnIndex)
            {
                case 0:
                    e.Value = e.RowIndex.ToString();
                    break;
                case 1:
                    e.Value = entry.RelativePath ?? string.Empty;
                    break;
                case 2:
                    e.Value = ResolveEntryIcon(entry);
                    break;
                case 3:
                    e.Value = entry.Uses > 0 ? entry.ItemName : "[NOT USING]";
                    break;
                case 4:
                    e.Value = entry.Uses > 0 && entry.ItemId > 0 ? entry.ItemId.ToString() : "-";
                    break;
                case 5:
                    e.Value = entry.Uses > 0 ? entry.Uses.ToString() : "-";
                    break;
                case 6:
                    e.Value = entry.PathId.ToString();
                    break;
            }
        }

        private void OnGridCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            ModelPickerEntry entry = TryGetEntryAtRow(e.RowIndex);
            if (entry == null)
            {
                return;
            }

            if (e.ColumnIndex == 3)
            {
                e.CellStyle.ForeColor = entry.Uses <= 0 ? Color.LimeGreen : Color.White;
            }
            else if (e.ColumnIndex == 5)
            {
                e.CellStyle.ForeColor = entry.Uses > 0 ? Color.Cyan : Color.White;
            }
            else if (e.ColumnIndex == 6)
            {
                e.CellStyle.ForeColor = entry.PathId <= 0 ? Color.Orange : Color.White;
            }
            else
            {
                e.CellStyle.ForeColor = Color.White;
            }
        }

        private ModelPickerEntry TryGetEntryAtRow(int rowIndex)
        {
            if (rowIndex < 0 || filteredEntries == null || rowIndex >= filteredEntries.Count)
            {
                return null;
            }

            return filteredEntries[rowIndex];
        }

        private Bitmap ResolveEntryIcon(ModelPickerEntry entry)
        {
            if (entry == null)
            {
                return Properties.Resources.blank;
            }

            if (entry.Icon != null)
            {
                return entry.Icon;
            }

            if (string.IsNullOrWhiteSpace(entry.IconKey)
                || iconLoader == null)
            {
                return Properties.Resources.NoIcon;
            }

            if (iconBitmapCache.TryGetValue(entry.IconKey, out Bitmap cached) && cached != null)
            {
                return cached;
            }

            Bitmap resolved = null;
            try
            {
                resolved = iconLoader(entry.IconKey);
            }
            catch
            { }

            if (resolved == null)
            {
                resolved = Properties.Resources.NoIcon;
            }

            iconBitmapCache[entry.IconKey] = resolved;
            return resolved;
        }

        private void EnsurePackageLoaded(string package)
        {
            if (string.IsNullOrWhiteSpace(package))
            {
                return;
            }
            if (loadedPackages.Contains(package))
            {
                return;
            }
            if (packageEntriesLoader == null)
            {
                return;
            }

            List<ModelPickerEntry> loaded = packageEntriesLoader(package);
            if (loaded == null || loaded.Count == 0)
            {
                loadedPackages.Add(package);
                return;
            }

            for (int i = 0; i < loaded.Count; i++)
            {
                ModelPickerEntry entry = loaded[i];
                if (entry == null)
                {
                    continue;
                }
                entry.Index = nextEntryIndex++;
                allEntries.Add(entry);
            }

            loadedPackages.Add(package);
        }

        private List<ModelPickerEntry> GetFilteredEntries()
        {
            string package = Convert.ToString(packageFilter.SelectedItem);
            if (string.IsNullOrWhiteSpace(package))
            {
                package = "models";
            }
            string folder = Convert.ToString(folderFilter.SelectedItem);
            string term = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            bool keepOnlyUnused = onlyUnusedCheck.Checked;
            bool onlyGfx = onlyGfxCheck.Checked;

            IEnumerable<ModelPickerEntry> query = allEntries;
            query = query.Where(x => string.Equals(x.Package, package, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(folder))
            {
                query = query.Where(x => string.Equals(ExtractTopFolder(x.RelativePath, package), folder, StringComparison.OrdinalIgnoreCase));
            }
            if (onlyGfx)
            {
                query = query.Where(x => string.Equals(Path.GetExtension(x.RelativePath ?? string.Empty), ".gfx", StringComparison.OrdinalIgnoreCase));
            }
            if (keepOnlyUnused)
            {
                query = query.Where(x => x.Uses <= 0);
            }
            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(x =>
                    x.Index.ToString().Contains(term)
                    || x.PathId.ToString().Contains(term)
                    || (!string.IsNullOrWhiteSpace(x.RelativePath) && x.RelativePath.ToLowerInvariant().Contains(term))
                    || (!string.IsNullOrWhiteSpace(x.ItemName) && x.ItemName.ToLowerInvariant().Contains(term))
                    || (x.ItemId > 0 && x.ItemId.ToString().Contains(term))
                );
            }

            return query.ToList();
        }

        private void ExportCsv()
        {
            try
            {
                List<ModelPickerEntry> filtered = GetFilteredEntries();
                if (filtered.Count == 0)
                {
                    MessageBox.Show("No entries to export.", "Choice Model", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string package = Convert.ToString(packageFilter.SelectedItem);
                if (string.IsNullOrWhiteSpace(package))
                {
                    package = "models";
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "Export Choice Model CSV";
                    sfd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                    sfd.FileName = "choice-model-" + package + ".csv";
                    if (sfd.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    using (StreamWriter sw = new StreamWriter(sfd.FileName, false, new UTF8Encoding(false)))
                    {
                        sw.WriteLine("package,path,pathId,id,name,uses");
                        for (int i = 0; i < filtered.Count; i++)
                        {
                            ModelPickerEntry e = filtered[i];
                            string name = e.Uses > 0 ? e.ItemName : "[NOT USING]";
                            string idText = e.Uses > 0 && e.ItemId > 0 ? e.ItemId.ToString() : "-";
                            string usesText = e.Uses > 0 ? e.Uses.ToString() : "-";
                            string pathIdText = e.PathId > 0 ? e.PathId.ToString() : "0";
                            sw.WriteLine(
                                CsvEscape(package) + "," +
                                CsvEscape(e.RelativePath ?? string.Empty) + "," +
                                CsvEscape(pathIdText) + "," +
                                CsvEscape(idText) + "," +
                                CsvEscape(name ?? string.Empty) + "," +
                                CsvEscape(usesText)
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("CSV export failed:\n" + ex.Message, "Choice Model", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string CsvEscape(string value)
        {
            string v = value ?? string.Empty;
            if (v.Contains("\""))
            {
                v = v.Replace("\"", "\"\"");
            }
            if (v.Contains(",") || v.Contains("\n") || v.Contains("\r"))
            {
                v = "\"" + v + "\"";
            }
            return v;
        }

        private string GetPackageForPathId(int pathId)
        {
            if (pathId <= 0)
            {
                return string.Empty;
            }
            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].PathId == pathId)
                {
                    return allEntries[i].Package;
                }
            }
            return string.Empty;
        }

        private string GetFolderForPathId(int pathId)
        {
            if (pathId <= 0)
            {
                return string.Empty;
            }

            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].PathId == pathId)
                {
                    return ExtractTopFolder(allEntries[i].RelativePath, allEntries[i].Package);
                }
            }
            return string.Empty;
        }

        private void SelectCurrentPathId(int pathId)
        {
            if (pathId <= 0)
            {
                return;
            }
            for (int i = 0; i < filteredEntries.Count; i++)
            {
                ModelPickerEntry entry = filteredEntries[i];
                if (entry != null && entry.PathId == pathId)
                {
                    if (grid.ColumnCount > 0 && i < grid.RowCount)
                    {
                        grid.CurrentCell = grid.Rows[i].Cells[0];
                    }
                    if (i > 0)
                    {
                        grid.FirstDisplayedScrollingRowIndex = i;
                    }
                    return;
                }
            }
        }

        private void ConfirmSelection()
        {
            int rowIndex = grid.CurrentCell != null ? grid.CurrentCell.RowIndex : -1;
            if (rowIndex < 0)
            {
                return;
            }

            ModelPickerEntry selected = TryGetEntryAtRow(rowIndex);
            if (selected == null)
            {
                return;
            }

            int pathId = selected.PathId;

            string mappedPath = selected.MappedPath;
            if (string.IsNullOrWhiteSpace(mappedPath))
            {
                mappedPath = BuildMappedPath(selected.Package, selected.RelativePath);
            }

            if (pathId <= 0 && string.IsNullOrWhiteSpace(mappedPath))
            {
                MessageBox.Show(
                    "This entry has no valid PathID or mapped path, so it cannot be applied to the field.",
                    "Choice Model",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SelectedPathId = pathId;
            SelectedMappedPath = mappedPath ?? string.Empty;
            DialogResult = DialogResult.OK;
            Close();
        }

        private async void PreviewSelectionAsync()
        {
            PreviewSelectionAsync(true);
        }

        private void ScheduleAutoPreview()
        {
            if (isInitializing)
            {
                return;
            }
            int rowIndex = grid != null && grid.CurrentCell != null ? grid.CurrentCell.RowIndex : -1;
            if (rowIndex < 0 || rowIndex >= grid.RowCount)
            {
                return;
            }

            autoPreviewTimer.Stop();
            autoPreviewTimer.Start();
        }

        private async void PreviewSelectionAsync(bool force)
        {
            int currentRowIndex = grid != null && grid.CurrentCell != null ? grid.CurrentCell.RowIndex : -1;
            if (currentRowIndex < 0 || modelPreviewService == null || previewAssetManager == null)
            {
                return;
            }
            if (!force && !previewWindowOpened)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref previewLoadInProgress, 1, 0) != 0)
            {
                if (!force)
                {
                    Interlocked.Exchange(ref previewPendingAuto, 1);
                }
                return;
            }

            int requestVersion = Interlocked.Increment(ref previewSelectionVersion);
            try
            {
                UseWaitCursor = false;
                Cursor.Current = Cursors.Default;
                Control focusedBeforeUpdate = FindFocusedDescendant(this);
                ModelPickerEntry selected = TryGetEntryAtRow(currentRowIndex);
                if (selected == null)
                {
                    return;
                }
                if (!force && selected.PathId > 0 && selected.PathId == lastPreviewPathId)
                {
                    return;
                }

                string mappedPath = selected.MappedPath;
                if (string.IsNullOrWhiteSpace(mappedPath))
                {
                    mappedPath = BuildMappedPath(selected.Package, selected.RelativePath);
                }
                if (string.IsNullOrWhiteSpace(mappedPath))
                {
                    MessageBox.Show("Invalid model path.", "Choice Model", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ModelPreviewMeshData meshData = null;
                string error = string.Empty;
                bool ok = await Task.Run(delegate
                {
                    return modelPreviewService.TryBuildPreviewMeshDataFromMappedPath(
                        previewAssetManager,
                        mappedPath,
                        out meshData,
                        out error);
                });

                if (requestVersion != previewSelectionVersion)
                {
                    return;
                }

                if (!ok)
                {
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        MessageBox.Show(error, "Choice Model", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }

                if (force)
                {
                    modelPreviewService.ShowPreviewWindow(meshData, true, Handle);
                    previewWindowOpened = true;
                }
                else
                {
                    if (!modelPreviewService.TryUpdateOpenPreviewWindow(meshData))
                    {
                        previewWindowOpened = false;
                        return;
                    }
                }
                if (selected.PathId > 0)
                {
                    lastPreviewPathId = selected.PathId;
                }

                if (!IsDisposed && IsHandleCreated && Visible)
                {
                    BeginInvoke((Action)delegate
                    {
                        if (IsDisposed || !Visible)
                        {
                            return;
                        }

                        if (focusedBeforeUpdate != null
                            && !focusedBeforeUpdate.IsDisposed
                            && focusedBeforeUpdate.CanFocus)
                        {
                            focusedBeforeUpdate.Focus();
                        }
                        else if (grid != null && grid.CanFocus)
                        {
                            grid.Focus();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("MODEL PREVIEW ERROR!\n" + ex.Message, "Choice Model", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                Cursor.Current = Cursors.Default;
                Interlocked.Exchange(ref previewLoadInProgress, 0);
                if (Interlocked.Exchange(ref previewPendingAuto, 0) == 1)
                {
                    ScheduleAutoPreview();
                }
            }
        }

        private static Control FindFocusedDescendant(Control root)
        {
            if (root == null)
            {
                return null;
            }

            if (root.Focused)
            {
                return root;
            }

            Control container = root;
            while (container != null)
            {
                ContainerControl cc = container as ContainerControl;
                if (cc == null || cc.ActiveControl == null)
                {
                    break;
                }

                Control active = cc.ActiveControl;
                if (active.Focused)
                {
                    return active;
                }

                container = active;
            }

            return root.ContainsFocus ? root : null;
        }

        private static string BuildMappedPath(string package, string relativePath)
        {
            string safePackage = (package ?? string.Empty).Trim();
            string safeRelative = (relativePath ?? string.Empty).Replace('/', '\\').Trim().TrimStart('\\');
            if (string.IsNullOrWhiteSpace(safeRelative))
            {
                return string.Empty;
            }
            if (string.IsNullOrWhiteSpace(safePackage))
            {
                return safeRelative;
            }
            return safePackage + "\\" + safeRelative;
        }
    }
}
