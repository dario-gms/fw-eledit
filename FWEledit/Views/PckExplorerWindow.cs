using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FWEledit.DDSReader;

namespace FWEledit
{
    public sealed class PckExplorerEntry
    {
        public string RelativePath { get; set; }
        public string Extension { get; set; }
    }

    public sealed class PckExplorerWindow : Form
    {
        private sealed class ImportAssetRequest
        {
            public string SourcePackage { get; set; }
            public string SourceRelativePath { get; set; }
            public string TargetPackage { get; set; }
            public string TargetRelativePath { get; set; }
            public bool IsRoot { get; set; }
        }

        private readonly AssetManager assetManager;
        private readonly PckEntryReaderService reader = new PckEntryReaderService();
        private readonly TgaImageService tgaImageService = new TgaImageService();
        private readonly ModelPreviewService modelPreviewService = new ModelPreviewService();
        private readonly EmbeddedModelPreviewLoaderService previewDependencyLoader = new EmbeddedModelPreviewLoaderService(new PckEntryReaderService());
        private readonly List<PckExplorerEntry> entries = new List<PckExplorerEntry>();
        private readonly Dictionary<string, HashSet<string>> targetEntryColorCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly object gameRootSwitchSync = new object();

        private TextBox pckPathTextBox;
        private Button openButton;
        private TextBox filterTextBox;
        private Button extensionFilterButton;
        private Button extensionFilterAllButton;
        private Button extensionFilterNoneButton;
        private Button extensionFilterModelsButton;
        private Label extensionFilterStatusLabel;
        private CheckedListBox extensionFilterList;
        private ToolStripDropDown extensionFilterDropDown;
        private ListView entryListView;
        private Panel previewHostPanel;
        private PictureBox previewBox;
        private Button previewModelButton;
        private TextBox infoTextBox;
        private ComboBox targetPackageComboBox;
        private TextBox targetPrefixTextBox;
        private CheckBox preservePathCheckBox;
        private Button importButton;
        private Label statusLabel;
        private SplitContainer mainSplitContainer;

        private string sourcePckPath = string.Empty;
        private string sourcePkxPath = string.Empty;
        private string sourcePackageName = string.Empty;
        private readonly string targetClientRootPath;
        private int previewRequestId;
        private ModelPreviewWindow embeddedPreviewWindow;
        private bool updatingExtensionFilters;

        public PckExplorerWindow(AssetManager assetManager)
        {
            this.assetManager = assetManager;
            targetClientRootPath = AssetManager.GameRootPath ?? string.Empty;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "FWEledit - PCK Explorer";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 520);
            Size = new Size(1240, 780);
            ShowInTaskbar = false;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);

            Color back = Color.FromArgb(15, 19, 24);
            Color panel = Color.FromArgb(18, 23, 29);
            Color raised = Color.FromArgb(38, 46, 57);
            Color text = Color.White;
            Color accent = Color.FromArgb(83, 151, 213);

            BackColor = back;
            ForeColor = text;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(10);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            root.BackColor = back;
            Controls.Add(root);

            TableLayoutPanel top = new TableLayoutPanel();
            top.Dock = DockStyle.Fill;
            top.ColumnCount = 3;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            root.Controls.Add(top, 0, 0);

            Label pckLabel = BuildLabel("PCK:");
            top.Controls.Add(pckLabel, 0, 0);
            pckPathTextBox = BuildTextBox(panel, text);
            top.Controls.Add(pckPathTextBox, 1, 0);
            openButton = BuildButton("Open", raised, text);
            openButton.Click += delegate { OpenPackageDialog(); };
            top.Controls.Add(openButton, 2, 0);

            TableLayoutPanel filterLayout = new TableLayoutPanel();
            filterLayout.Dock = DockStyle.Fill;
            filterLayout.ColumnCount = 6;
            filterLayout.RowCount = 1;
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78F));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            root.Controls.Add(filterLayout, 0, 1);

            filterTextBox = BuildTextBox(panel, text);
            filterTextBox.Margin = new Padding(0, 4, 8, 4);
            filterTextBox.TextChanged += delegate { RefreshEntries(); };
            filterLayout.Controls.Add(filterTextBox, 0, 0);

            extensionFilterButton = BuildButton("Types...", raised, text);
            extensionFilterButton.Margin = new Padding(0, 4, 6, 4);
            extensionFilterButton.Click += delegate { ShowExtensionFilterDropDown(); };
            filterLayout.Controls.Add(extensionFilterButton, 1, 0);

            extensionFilterAllButton = BuildButton("All", raised, text);
            extensionFilterAllButton.Margin = new Padding(0, 4, 6, 4);
            extensionFilterAllButton.Click += delegate { SetAllExtensionFilters(true); };
            filterLayout.Controls.Add(extensionFilterAllButton, 2, 0);

            extensionFilterNoneButton = BuildButton("None", raised, text);
            extensionFilterNoneButton.Margin = new Padding(0, 4, 6, 4);
            extensionFilterNoneButton.Click += delegate { SetAllExtensionFilters(false); };
            filterLayout.Controls.Add(extensionFilterNoneButton, 3, 0);

            extensionFilterModelsButton = BuildButton("Models", raised, text);
            extensionFilterModelsButton.Margin = new Padding(0, 4, 6, 4);
            extensionFilterModelsButton.Click += delegate { SetModelExtensionFilters(); };
            filterLayout.Controls.Add(extensionFilterModelsButton, 4, 0);

            extensionFilterStatusLabel = BuildLabel("All types");
            extensionFilterStatusLabel.Margin = new Padding(0, 4, 0, 4);
            filterLayout.Controls.Add(extensionFilterStatusLabel, 5, 0);

            mainSplitContainer = new SplitContainer();
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.BackColor = back;
            root.Controls.Add(mainSplitContainer, 0, 2);

            entryListView = new ListView();
            entryListView.Dock = DockStyle.Fill;
            entryListView.View = View.Details;
            entryListView.FullRowSelect = true;
            entryListView.MultiSelect = true;
            entryListView.HideSelection = false;
            entryListView.BackColor = panel;
            entryListView.ForeColor = text;
            entryListView.BorderStyle = BorderStyle.FixedSingle;
            entryListView.Columns.Add("Path", 500);
            entryListView.Columns.Add("Type", 80);
            entryListView.SelectedIndexChanged += delegate { RefreshPreview(); };
            entryListView.DoubleClick += delegate
            {
                if (previewModelButton != null && previewModelButton.Enabled)
                {
                    PreviewSelectedModelEntry();
                }
            };
            mainSplitContainer.Panel1.Controls.Add(entryListView);

            TableLayoutPanel right = new TableLayoutPanel();
            right.Dock = DockStyle.Fill;
            right.ColumnCount = 1;
            right.RowCount = 4;
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 122F));
            right.BackColor = back;
            mainSplitContainer.Panel2.Controls.Add(right);

            previewHostPanel = new Panel();
            previewHostPanel.Dock = DockStyle.Fill;
            previewHostPanel.BackColor = Color.FromArgb(10, 13, 17);
            previewHostPanel.BorderStyle = BorderStyle.FixedSingle;
            right.Controls.Add(previewHostPanel, 0, 0);

            previewBox = new PictureBox();
            previewBox.Dock = DockStyle.Fill;
            previewBox.BackColor = Color.FromArgb(10, 13, 17);
            previewBox.BorderStyle = BorderStyle.None;
            previewBox.SizeMode = PictureBoxSizeMode.Zoom;
            previewHostPanel.Controls.Add(previewBox);

            previewModelButton = BuildButton("Preview 3D Model", raised, text);
            previewModelButton.Enabled = false;
            previewModelButton.Margin = new Padding(0, 6, 0, 0);
            previewModelButton.Click += delegate { PreviewSelectedModelEntry(); };
            right.Controls.Add(previewModelButton, 0, 1);

            infoTextBox = BuildTextBox(panel, text);
            infoTextBox.Multiline = true;
            infoTextBox.ScrollBars = ScrollBars.Vertical;
            infoTextBox.ReadOnly = true;
            infoTextBox.Margin = new Padding(0, 8, 0, 8);
            right.Controls.Add(infoTextBox, 0, 2);

            GroupBox importGroup = new GroupBox();
            importGroup.Text = "Import to current client";
            importGroup.Dock = DockStyle.Fill;
            importGroup.ForeColor = text;
            importGroup.BackColor = back;
            right.Controls.Add(importGroup, 0, 3);

            TableLayoutPanel importLayout = new TableLayoutPanel();
            importLayout.Dock = DockStyle.Fill;
            importLayout.Padding = new Padding(8);
            importLayout.ColumnCount = 4;
            importLayout.RowCount = 3;
            importLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            importLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            importLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            importLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            importLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            importLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            importLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            importGroup.Controls.Add(importLayout);

            importLayout.Controls.Add(BuildLabel("Package:"), 0, 0);
            targetPackageComboBox = new ComboBox();
            targetPackageComboBox.DropDownStyle = ComboBoxStyle.DropDown;
            targetPackageComboBox.BackColor = panel;
            targetPackageComboBox.ForeColor = text;
            targetPackageComboBox.Dock = DockStyle.Fill;
            targetPackageComboBox.Items.AddRange(new object[] { "surfaces", "gfx", "models", "shaders", "sfx", "script", "interfaces", "textures" });
            targetPackageComboBox.Text = "surfaces";
            PopulateTargetPackageComboBox();
            importLayout.Controls.Add(targetPackageComboBox, 1, 0);

            preservePathCheckBox = new CheckBox();
            preservePathCheckBox.Text = "Preserve paths";
            preservePathCheckBox.Checked = true;
            preservePathCheckBox.Dock = DockStyle.Fill;
            preservePathCheckBox.ForeColor = text;
            importLayout.Controls.Add(preservePathCheckBox, 2, 0);
            importButton = BuildButton("Import Selected", raised, text);
            importButton.Click += delegate { ImportSelectedEntries(); };
            importLayout.Controls.Add(importButton, 3, 0);

            importLayout.Controls.Add(BuildLabel("Prefix:"), 0, 1);
            targetPrefixTextBox = BuildTextBox(panel, text);
            targetPrefixTextBox.Text = "imported";
            importLayout.Controls.Add(targetPrefixTextBox, 1, 1);
            importLayout.SetColumnSpan(targetPrefixTextBox, 3);

            statusLabel = BuildLabel("Open a PCK/PKX file to inspect it.");
            statusLabel.Dock = DockStyle.Fill;
            root.Controls.Add(statusLabel, 0, 3);

            Load += delegate { ApplySafeSplitterDistance(); };
            Shown += delegate { ApplySafeSplitterDistance(); };
            Resize += delegate { ApplySafeSplitterDistance(); };
            FormClosed += delegate
            {
                ClearEmbeddedPreview();
                DisposeExtensionFilterDropDown();
            };
        }

        private void ApplySafeSplitterDistance()
        {
            if (mainSplitContainer == null || mainSplitContainer.Width <= 0)
            {
                return;
            }

            int available = mainSplitContainer.Width - mainSplitContainer.SplitterWidth;
            const int minLeft = 220;
            const int minRight = 260;
            if (available <= minLeft + minRight)
            {
                return;
            }

            int desired = Math.Max(minLeft, Math.Min(640, (int)(available * 0.56)));
            int max = available - minRight;
            desired = Math.Min(desired, max);
            if (desired > 0 && desired != mainSplitContainer.SplitterDistance)
            {
                mainSplitContainer.SplitterDistance = desired;
            }
        }

        private static Label BuildLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.White
            };
        }

        private static TextBox BuildTextBox(Color back, Color fore)
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = back,
                ForeColor = fore
            };
        }

        private static Button BuildButton(string text, Color back, Color fore)
        {
            return new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore
            };
        }

        private void OpenPackageDialog()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Open PCK package";
                dialog.Filter = "PCK packages (*.pck;*.pkx)|*.pck;*.pkx|All files (*.*)|*.*";
                dialog.CheckFileExists = true;
                if (!string.IsNullOrWhiteSpace(sourcePckPath))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(sourcePckPath);
                }

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                LoadPackage(dialog.FileName);
            }
        }

        private void LoadPackage(string selectedPath)
        {
            string extension = Path.GetExtension(selectedPath) ?? string.Empty;
            if (extension.Equals(".pkx", StringComparison.OrdinalIgnoreCase))
            {
                sourcePkxPath = selectedPath;
                sourcePckPath = Path.ChangeExtension(selectedPath, ".pck");
            }
            else
            {
                sourcePckPath = selectedPath;
                sourcePkxPath = Path.ChangeExtension(selectedPath, ".pkx");
            }

            sourcePackageName = Path.GetFileNameWithoutExtension(sourcePckPath) ?? string.Empty;
            pckPathTextBox.Text = sourcePckPath;
            targetPackageComboBox.Text = sourcePackageName;
            targetPrefixTextBox.Text = "imported\\" + sourcePackageName;
            entries.Clear();
            targetEntryColorCache.Clear();
            entryListView.Items.Clear();
            ClearImagePreview();
            ClearEmbeddedPreview();
            infoTextBox.Clear();

            Cursor previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (!reader.TryEnumeratePackageFileEntries(sourcePackageName, sourcePckPath, sourcePkxPath, out List<string> packageEntries, out string error))
                {
                    statusLabel.Text = error;
                    MessageBox.Show(this, error, "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                entries.AddRange(packageEntries
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .Select(path => new PckExplorerEntry
                    {
                        RelativePath = path,
                        Extension = (Path.GetExtension(path) ?? string.Empty).TrimStart('.').ToUpperInvariant()
                    }));
                RebuildExtensionFilters();
                RefreshEntries();
                statusLabel.Text = "Loaded " + entries.Count.ToString("N0") + " entries from " + sourcePackageName + ".pck";
            }
            finally
            {
                Cursor.Current = previousCursor;
            }
        }

        private void RefreshEntries()
        {
            RefreshEntries(null, null);
        }

        private void RefreshEntries(IEnumerable<string> selectedPathsToRestore, string topPathToRestore)
        {
            string filter = (filterTextBox.Text ?? string.Empty).Trim();
            HashSet<string> selectedPaths = selectedPathsToRestore == null
                ? null
                : new HashSet<string>(selectedPathsToRestore.Where(path => !string.IsNullOrWhiteSpace(path)), StringComparer.OrdinalIgnoreCase);
            HashSet<string> allowedExtensions = GetVisibleExtensionFilter();
            entryListView.BeginUpdate();
            try
            {
                entryListView.Items.Clear();
                IEnumerable<PckExplorerEntry> source = entries;
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    source = source.Where(entry => entry.RelativePath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }
                if (allowedExtensions != null)
                {
                    source = source.Where(entry => allowedExtensions.Contains(NormalizeExtensionKey(entry.Extension)));
                }

                ListViewItem firstRestoredSelection = null;
                ListViewItem restoredTopItem = null;
                foreach (PckExplorerEntry entry in source.Take(5000))
                {
                    ListViewItem item = new ListViewItem(entry.RelativePath);
                    item.SubItems.Add(entry.Extension);
                    item.Tag = entry;
                    if (EntryExistsInCurrentClient(sourcePackageName, entry.RelativePath))
                    {
                        item.ForeColor = Color.FromArgb(118, 224, 139);
                    }
                    entryListView.Items.Add(item);
                    if (selectedPaths != null && selectedPaths.Contains(entry.RelativePath))
                    {
                        item.Selected = true;
                        if (firstRestoredSelection == null)
                        {
                            firstRestoredSelection = item;
                        }
                    }
                    if (restoredTopItem == null
                        && !string.IsNullOrWhiteSpace(topPathToRestore)
                        && string.Equals(entry.RelativePath, topPathToRestore, StringComparison.OrdinalIgnoreCase))
                    {
                        restoredTopItem = item;
                    }
                }

                if (restoredTopItem != null)
                {
                    entryListView.TopItem = restoredTopItem;
                }
                else if (firstRestoredSelection != null)
                {
                    firstRestoredSelection.EnsureVisible();
                }
            }
            finally
            {
                entryListView.EndUpdate();
            }
        }

        private void RebuildExtensionFilters()
        {
            updatingExtensionFilters = true;
            try
            {
                DisposeExtensionFilterDropDown();
                extensionFilterList = new CheckedListBox();
                extensionFilterList.CheckOnClick = true;
                extensionFilterList.BorderStyle = BorderStyle.None;
                extensionFilterList.BackColor = Color.FromArgb(18, 23, 29);
                extensionFilterList.ForeColor = Color.White;
                extensionFilterList.Font = Font;
                extensionFilterList.Width = 220;
                extensionFilterList.Height = 260;
                extensionFilterList.ItemCheck += delegate
                {
                    if (!updatingExtensionFilters)
                    {
                        BeginInvoke((Action)(() =>
                        {
                            UpdateExtensionFilterStatus();
                            SaveExtensionFilterSelection();
                            RefreshEntries();
                        }));
                    }
                };

                foreach (string extension in entries
                    .Select(entry => NormalizeExtensionKey(entry.Extension))
                    .Where(extension => !string.IsNullOrWhiteSpace(extension))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase))
                {
                    extensionFilterList.Items.Add(extension, true);
                }
                ApplySavedExtensionFilterSelection();

                ToolStripControlHost host = new ToolStripControlHost(extensionFilterList);
                host.Margin = Padding.Empty;
                host.Padding = Padding.Empty;
                extensionFilterDropDown = new ToolStripDropDown();
                extensionFilterDropDown.Padding = Padding.Empty;
                extensionFilterDropDown.Items.Add(host);
            }
            finally
            {
                updatingExtensionFilters = false;
            }
            UpdateExtensionFilterStatus();
        }

        private HashSet<string> GetVisibleExtensionFilter()
        {
            if (extensionFilterList == null || extensionFilterList.Items.Count == 0)
            {
                return null;
            }

            HashSet<string> allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (object item in extensionFilterList.CheckedItems)
            {
                allowed.Add(Convert.ToString(item));
            }
            return allowed;
        }

        private void ShowExtensionFilterDropDown()
        {
            if (extensionFilterDropDown == null || extensionFilterButton == null)
            {
                return;
            }

            extensionFilterDropDown.Show(extensionFilterButton, new Point(0, extensionFilterButton.Height));
        }

        private void SetAllExtensionFilters(bool isChecked)
        {
            if (extensionFilterList == null)
            {
                return;
            }

            updatingExtensionFilters = true;
            try
            {
                for (int i = 0; i < extensionFilterList.Items.Count; i++)
                {
                    extensionFilterList.SetItemChecked(i, isChecked);
                }
            }
            finally
            {
                updatingExtensionFilters = false;
            }
            UpdateExtensionFilterStatus();
            RefreshEntries();
            SaveExtensionFilterSelection();
        }

        private void SetModelExtensionFilters()
        {
            if (extensionFilterList == null)
            {
                return;
            }

            HashSet<string> modelExtensions = new HashSet<string>(new[] { "GFX", "ECM", "SMD", "SKI", "BON", "STCK", "SDR", "ATT", "SGC" }, StringComparer.OrdinalIgnoreCase);
            updatingExtensionFilters = true;
            try
            {
                for (int i = 0; i < extensionFilterList.Items.Count; i++)
                {
                    string extension = Convert.ToString(extensionFilterList.Items[i]);
                    extensionFilterList.SetItemChecked(i, modelExtensions.Contains(extension));
                }
            }
            finally
            {
                updatingExtensionFilters = false;
            }
            UpdateExtensionFilterStatus();
            RefreshEntries();
            SaveExtensionFilterSelection();
        }

        private void UpdateExtensionFilterStatus()
        {
            if (extensionFilterStatusLabel == null)
            {
                return;
            }

            int total = extensionFilterList == null ? 0 : extensionFilterList.Items.Count;
            int checkedCount = extensionFilterList == null ? 0 : extensionFilterList.CheckedItems.Count;
            if (total == 0 || checkedCount == total)
            {
                extensionFilterStatusLabel.Text = "All types";
            }
            else if (checkedCount == 0)
            {
                extensionFilterStatusLabel.Text = "No types";
            }
            else
            {
                extensionFilterStatusLabel.Text = checkedCount.ToString() + "/" + total.ToString() + " types";
            }
        }

        private void ApplySavedExtensionFilterSelection()
        {
            if (extensionFilterList == null || extensionFilterList.Items.Count == 0)
            {
                return;
            }

            string saved = Properties.Settings.Default.PckExplorerVisibleExtensions ?? string.Empty;
            if (string.IsNullOrWhiteSpace(saved) || string.Equals(saved.Trim(), "ALL", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.Equals(saved.Trim(), "NONE", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < extensionFilterList.Items.Count; i++)
                {
                    extensionFilterList.SetItemChecked(i, false);
                }
                return;
            }

            HashSet<string> allowed = new HashSet<string>(
                saved.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(NormalizeExtensionKey),
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < extensionFilterList.Items.Count; i++)
            {
                string extension = Convert.ToString(extensionFilterList.Items[i]);
                extensionFilterList.SetItemChecked(i, allowed.Contains(extension));
            }
        }

        private void SaveExtensionFilterSelection()
        {
            if (extensionFilterList == null || extensionFilterList.Items.Count == 0)
            {
                return;
            }

            int total = extensionFilterList.Items.Count;
            int checkedCount = extensionFilterList.CheckedItems.Count;
            string value;
            if (checkedCount <= 0)
            {
                value = "NONE";
            }
            else if (checkedCount == total)
            {
                value = "ALL";
            }
            else
            {
                value = string.Join(
                    "|",
                    extensionFilterList.CheckedItems
                        .Cast<object>()
                        .Select(item => NormalizeExtensionKey(Convert.ToString(item)))
                        .OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)
                        .ToArray());
            }

            try
            {
                Properties.Settings.Default.PckExplorerVisibleExtensions = value;
                Properties.Settings.Default.Save();
            }
            catch
            {
            }
        }

        private void DisposeExtensionFilterDropDown()
        {
            if (extensionFilterDropDown != null)
            {
                extensionFilterDropDown.Dispose();
                extensionFilterDropDown = null;
            }
            extensionFilterList = null;
        }

        private static string NormalizeExtensionKey(string extension)
        {
            return (extension ?? string.Empty).Trim().TrimStart('.').ToUpperInvariant();
        }

        private void RefreshPreview()
        {
            ClearImagePreview();

            if (entryListView.SelectedItems.Count == 0 || !(entryListView.SelectedItems[0].Tag is PckExplorerEntry entry))
            {
                infoTextBox.Clear();
                if (previewModelButton != null)
                {
                    previewModelButton.Enabled = false;
                }
                Interlocked.Increment(ref previewRequestId);
                return;
            }

            infoTextBox.Text = entry.RelativePath;
            if (previewModelButton != null)
            {
                previewModelButton.Enabled = IsPreviewableModel(entry.RelativePath);
            }
            if (IsPreviewableModel(entry.RelativePath))
            {
                ScheduleEmbeddedModelPreview(entry);
                return;
            }

            Interlocked.Increment(ref previewRequestId);
            SetEmbeddedPreviewVisible(false);
            if (!IsPreviewableImage(entry.RelativePath))
            {
                return;
            }

            if (!reader.TryReadPackageFileEntry(sourcePackageName, sourcePckPath, sourcePkxPath, entry.RelativePath, out byte[] payload, out string resolved, out string error))
            {
                infoTextBox.Text = entry.RelativePath + Environment.NewLine + error;
                return;
            }

            Bitmap bitmap = DecodeImage(payload, entry.RelativePath);
            if (bitmap == null)
            {
                infoTextBox.Text = resolved + Environment.NewLine + payload.Length.ToString("N0") + " bytes" + Environment.NewLine + "Preview is not available.";
                return;
            }

            previewBox.Image = bitmap;
            previewBox.Visible = true;
            infoTextBox.Text = resolved
                + Environment.NewLine
                + bitmap.Width + "x" + bitmap.Height
                + Environment.NewLine
                + payload.Length.ToString("N0") + " bytes";
        }

        private static bool IsPreviewableImage(string path)
        {
            string extension = (Path.GetExtension(path) ?? string.Empty).ToLowerInvariant();
            return extension == ".tga" || extension == ".dds" || extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".bmp";
        }

        private static bool IsPreviewableModel(string path)
        {
            string extension = (Path.GetExtension(path) ?? string.Empty).ToLowerInvariant();
            return extension == ".ecm"
                || extension == ".smd"
                || extension == ".ski"
                || extension == ".gfx";
        }

        private void ScheduleEmbeddedModelPreview(PckExplorerEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            int requestId = Interlocked.Increment(ref previewRequestId);
            string relativePath = NormalizePath(entry.RelativePath);
            string mappedPath = NormalizePackageName(sourcePackageName) + "\\" + relativePath;
            string sourceGameRoot = ResolveSourceGameRoot();
            if (string.IsNullOrWhiteSpace(sourceGameRoot))
            {
                SetEmbeddedPreviewVisible(false);
                infoTextBox.Text = relativePath
                    + Environment.NewLine
                    + "3D preview needs the selected PCK to be inside a resources folder so related packages can be resolved.";
                return;
            }

            previewBox.Visible = false;
            EnsureEmbeddedPreviewPlaceholder("Loading 3D preview...");
            Task.Run(delegate
            {
                ModelPreviewMeshData meshData = null;
                string error = string.Empty;
                lock (gameRootSwitchSync)
                {
                    string previousGameRoot = AssetManager.GameRootPath;
                    try
                    {
                        AssetManager.GameRootPath = sourceGameRoot;
                        modelPreviewService.TryBuildPreviewMeshDataFromMappedPath(assetManager ?? new AssetManager(), mappedPath, out meshData, out error);
                    }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                    }
                    finally
                    {
                        AssetManager.GameRootPath = previousGameRoot;
                    }
                }

                BeginInvoke((Action)delegate
                {
                    if (requestId != previewRequestId)
                    {
                        return;
                    }

                    if (meshData == null)
                    {
                        EnsureEmbeddedPreviewPlaceholder(string.IsNullOrWhiteSpace(error) ? "Model preview unavailable for this file." : error);
                        infoTextBox.Text = relativePath + Environment.NewLine + (string.IsNullOrWhiteSpace(error) ? "Preview is not available." : error);
                        return;
                    }

                    ShowEmbeddedPreview(meshData);
                    infoTextBox.Text = BuildModelInfoText(meshData, relativePath);
                });
            });
        }

        private void ShowEmbeddedPreview(ModelPreviewMeshData meshData)
        {
            if (meshData == null || previewHostPanel == null)
            {
                return;
            }

            previewBox.Visible = false;
            if (embeddedPreviewWindow == null || embeddedPreviewWindow.IsDisposed)
            {
                embeddedPreviewWindow = new ModelPreviewWindow(meshData, false, IntPtr.Zero);
                embeddedPreviewWindow.TopLevel = false;
                embeddedPreviewWindow.FormBorderStyle = FormBorderStyle.None;
                embeddedPreviewWindow.Dock = DockStyle.Fill;
                embeddedPreviewWindow.ShowInTaskbar = false;
                embeddedPreviewWindow.SetEmbeddedViewportOnlyMode(true);
                previewHostPanel.Controls.Add(embeddedPreviewWindow);
                embeddedPreviewWindow.Show();
            }
            else
            {
                embeddedPreviewWindow.ReplaceMeshData(meshData);
            }

            embeddedPreviewWindow.Visible = true;
            embeddedPreviewWindow.BringToFront();
        }

        private void EnsureEmbeddedPreviewPlaceholder(string message)
        {
            if (embeddedPreviewWindow != null && !embeddedPreviewWindow.IsDisposed)
            {
                embeddedPreviewWindow.ShowStatusMessage(message);
                embeddedPreviewWindow.Visible = true;
                embeddedPreviewWindow.BringToFront();
            }
            else
            {
                SetEmbeddedPreviewVisible(false);
            }
        }

        private void SetEmbeddedPreviewVisible(bool visible)
        {
            if (embeddedPreviewWindow != null && !embeddedPreviewWindow.IsDisposed)
            {
                embeddedPreviewWindow.Visible = visible;
            }
        }

        private void ClearEmbeddedPreview()
        {
            Interlocked.Increment(ref previewRequestId);
            if (embeddedPreviewWindow == null)
            {
                return;
            }

            try
            {
                embeddedPreviewWindow.Close();
                embeddedPreviewWindow.Dispose();
            }
            catch
            {
            }
            embeddedPreviewWindow = null;
        }

        private void ClearImagePreview()
        {
            if (previewBox != null && previewBox.Image != null)
            {
                Image old = previewBox.Image;
                previewBox.Image = null;
                old.Dispose();
            }
        }

        private string BuildModelInfoText(ModelPreviewMeshData meshData, string fallbackPath)
        {
            if (meshData == null)
            {
                return fallbackPath ?? string.Empty;
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

            return (meshData.SourceMappedPath ?? fallbackPath ?? string.Empty)
                + Environment.NewLine
                + "Vertices: " + meshData.VertexCount.ToString("N0")
                + Environment.NewLine
                + "Triangles: " + meshData.TriangleCount.ToString("N0")
                + Environment.NewLine
                + "Textures: " + loadedTextures.ToString() + "/" + totalTextures.ToString();
        }

        private void PreviewSelectedModelEntry()
        {
            if (entryListView.SelectedItems.Count == 0 || !(entryListView.SelectedItems[0].Tag is PckExplorerEntry entry))
            {
                return;
            }
            if (!IsPreviewableModel(entry.RelativePath))
            {
                MessageBox.Show(this, "3D preview is available for ECM, SMD, SKI, and GFX entries.", "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ScheduleEmbeddedModelPreview(entry);
        }

        private string ResolveSourceGameRoot()
        {
            string directory = Path.GetDirectoryName(sourcePckPath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return string.Empty;
            }

            DirectoryInfo resourcesDirectory = new DirectoryInfo(directory);
            if (resourcesDirectory.Parent != null
                && string.Equals(resourcesDirectory.Name, "resources", StringComparison.OrdinalIgnoreCase))
            {
                return resourcesDirectory.Parent.FullName;
            }

            return string.Empty;
        }

        private Bitmap DecodeImage(byte[] payload, string path)
        {
            string extension = (Path.GetExtension(path) ?? string.Empty).ToLowerInvariant();
            try
            {
                if (extension == ".tga")
                {
                    return tgaImageService.TryLoad(payload);
                }
                if (extension == ".dds")
                {
                    using (DDSImage dds = new DDSImage(payload, true))
                    {
                        return dds.IsValid && dds.BitmapImage != null
                            ? new Bitmap(dds.BitmapImage)
                            : null;
                    }
                }

                using (MemoryStream stream = new MemoryStream(payload, false))
                using (Image image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
            catch
            {
                return null;
            }
        }

        private void ImportSelectedEntries()
        {
            if (entryListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Select one or more package entries first.", "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string targetPackage = NormalizePackageName(targetPackageComboBox.Text);
            if (string.IsNullOrWhiteSpace(targetPackage))
            {
                MessageBox.Show(this, "Choose a target package.", "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(targetClientRootPath) || !Directory.Exists(targetClientRootPath))
            {
                MessageBox.Show(this, "Load a game client before importing into its PCK files.", "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Dictionary<string, string> targetPackageRemap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!EnsureImportTargetPackageExists(targetPackage, out targetPackage))
            {
                return;
            }
            targetPackageComboBox.Text = targetPackage;
            if (!string.Equals(sourcePackageName, targetPackage, StringComparison.OrdinalIgnoreCase))
            {
                targetPackageRemap[NormalizePackageName(sourcePackageName)] = targetPackage;
            }

            List<PckExplorerEntry> selectedEntries = entryListView.SelectedItems
                .Cast<ListViewItem>()
                .Select(item => item.Tag as PckExplorerEntry)
                .Where(entry => entry != null)
                .ToList();
            List<string> selectedPathsToRestore = selectedEntries
                .Select(entry => entry.RelativePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList();
            string topPathToRestore = entryListView.TopItem != null && entryListView.TopItem.Tag is PckExplorerEntry topEntry
                ? topEntry.RelativePath
                : string.Empty;

            string tempRoot = Path.Combine(Path.GetTempPath(), "FWEledit", "pck-explorer-import", Guid.NewGuid().ToString("N"));
            int staged = 0;
            int skipped = 0;
            int dependencyCount = 0;
            Dictionary<string, HashSet<string>> sourceEntryCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, HashSet<string>> targetEntryCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<ImportAssetRequest>> stagedByPackage = new Dictionary<string, List<ImportAssetRequest>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ImportAssetRequest> queued = new Dictionary<string, ImportAssetRequest>(StringComparer.OrdinalIgnoreCase);
            Queue<ImportAssetRequest> pending = new Queue<ImportAssetRequest>();
            Cursor previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            importButton.Enabled = false;
            try
            {
                foreach (PckExplorerEntry entry in selectedEntries)
                {
                    if (!reader.TryReadPackageFileEntry(sourcePackageName, sourcePckPath, sourcePkxPath, entry.RelativePath, out byte[] payload, out string resolvedPath, out string error))
                    {
                        MessageBox.Show(this, error, "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    ImportAssetRequest root = new ImportAssetRequest
                    {
                        SourcePackage = sourcePackageName,
                        SourceRelativePath = NormalizePath(resolvedPath),
                        TargetPackage = targetPackage,
                        TargetRelativePath = NormalizePath(BuildTargetRelativePath(resolvedPath)),
                        IsRoot = true
                    };

                    AddQueuedImport(queued, pending, root);
                    StageImportAsset(root, payload, tempRoot, stagedByPackage, targetEntryCache, ref staged, ref skipped);
                    QueuePreviewResolvedDependencies(root, targetPackageRemap, sourceEntryCache, queued, pending, ref dependencyCount);
                }

                int processed = 0;
                while (pending.Count > 0 && processed < 5000)
                {
                    processed++;
                    ImportAssetRequest current = pending.Dequeue();
                    if (!TryReadSourceAsset(current.SourcePackage, current.SourceRelativePath, out byte[] payload, out string resolvedPath, out string readError))
                    {
                        continue;
                    }

                    current.SourceRelativePath = NormalizePath(resolvedPath);
                    if (!current.IsRoot)
                    {
                        StageImportAsset(current, payload, tempRoot, stagedByPackage, targetEntryCache, ref staged, ref skipped);
                    }

                    if (!ShouldScanForDependencies(current.SourceRelativePath))
                    {
                        continue;
                    }

                    QueueModelDirectoryAssets(current, sourceEntryCache, queued, pending, ref dependencyCount);
                    QueueAssociatedAnimationTracks(current, targetPackageRemap, sourceEntryCache, queued, pending, ref dependencyCount);

                    foreach (string candidate in CollectReferenceCandidates(current.SourcePackage, current.SourceRelativePath, payload))
                    {
                        ImportAssetRequest dependency;
                        if (!TryResolveSourceDependency(candidate, sourceEntryCache, out dependency))
                        {
                            continue;
                        }

                        dependency.TargetPackage = ResolveTargetPackageForImport(dependency.SourcePackage, targetPackageRemap);
                        dependency.TargetRelativePath = dependency.SourceRelativePath;
                        dependency.IsRoot = false;

                        if (AddQueuedImport(queued, pending, dependency))
                        {
                            dependencyCount++;
                        }
                    }
                }

                List<string> updatedPackages = new List<string>();
                HashSet<string> rawImportedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int rawCopied = 0;
                if (preservePathCheckBox.Checked)
                {
                    IEnumerable<IGrouping<string, ImportAssetRequest>> rawGroups = queued.Values
                        .Where(CanUseRawPackageImport)
                        .GroupBy(request => NormalizePackageName(request.SourcePackage) + "\n" + NormalizePackageName(request.TargetPackage), StringComparer.OrdinalIgnoreCase);

                    foreach (IGrouping<string, ImportAssetRequest> rawGroup in rawGroups)
                    {
                        ImportAssetRequest firstRawRequest = rawGroup.First();
                        string packageName = NormalizePackageName(firstRawRequest.TargetPackage);
                        if (!EnsureImportTargetPackageExists(packageName, out packageName))
                        {
                            return;
                        }

                        string rawSourcePck;
                        string rawSourcePkx;
                        if (!TryGetSourcePackagePaths(firstRawRequest.SourcePackage, out rawSourcePck, out rawSourcePkx))
                        {
                            MessageBox.Show(this, "Source package was not found: " + firstRawRequest.SourcePackage + ".pck", "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        string importError = null;
                        int copied = 0;
                        bool imported;
                        lock (gameRootSwitchSync)
                        {
                            string previousGameRoot = AssetManager.GameRootPath;
                            try
                            {
                                AssetManager.GameRootPath = targetClientRootPath;
                                imported = assetManager != null && assetManager.ImportPackageAssetsByRawCopy(packageName, rawSourcePck, rawSourcePkx, BuildRawCopyItems(rawGroup), out copied, out importError);
                            }
                            finally
                            {
                                AssetManager.GameRootPath = previousGameRoot;
                            }
                        }

                        if (!imported)
                        {
                            MessageBox.Show(this, importError ?? "No active client is loaded.", "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        rawCopied += copied;
                        rawImportedPackages.Add(packageName);
                        updatedPackages.Add(packageName);
                    }
                }

                if (staged == 0 && rawCopied == 0)
                {
                    MessageBox.Show(this, "No files were imported. Selected entries already exist in the target package.", "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int stagedImported = 0;
                foreach (KeyValuePair<string, List<ImportAssetRequest>> packageStage in stagedByPackage)
                {
                    if (rawImportedPackages.Contains(NormalizePackageName(packageStage.Key)))
                    {
                        continue;
                    }

                    string packageStageRoot = Path.Combine(tempRoot, packageStage.Key);
                    if (!Directory.Exists(packageStageRoot))
                    {
                        continue;
                    }

                    string packageName = packageStage.Key;
                    if (!EnsureImportTargetPackageExists(packageName, out packageName))
                    {
                        return;
                    }
                    if (!string.Equals(packageName, packageStage.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        string remappedRoot = Path.Combine(tempRoot, packageName);
                        MoveStagedPackageFiles(packageStageRoot, remappedRoot);
                        packageStageRoot = remappedRoot;
                    }

                    string importError = null;
                    bool imported;
                    lock (gameRootSwitchSync)
                    {
                        string previousGameRoot = AssetManager.GameRootPath;
                        try
                        {
                            AssetManager.GameRootPath = targetClientRootPath;
                            imported = assetManager != null && assetManager.ImportStagedPackageAssetsIncrementalOnly(packageName, packageStageRoot, out importError);
                        }
                        finally
                        {
                            AssetManager.GameRootPath = previousGameRoot;
                        }
                    }

                    if (!imported)
                    {
                        MessageBox.Show(this, importError ?? "No active client is loaded.", "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    updatedPackages.Add(packageName);
                    stagedImported += packageStage.Value.Count;
                }

                int totalImported = rawCopied + stagedImported;
                string message = "Imported " + totalImported.ToString("N0") + " file(s).";
                if (dependencyCount > 0)
                {
                    message += Environment.NewLine + "Associated assets resolved: " + dependencyCount.ToString("N0");
                }
                if (updatedPackages.Count > 0)
                {
                    message += Environment.NewLine + "PCK packages updated: " + string.Join(", ", updatedPackages.Distinct(StringComparer.OrdinalIgnoreCase).Select(p => p + ".pck"));
                }
                if (skipped > 0 && rawCopied == 0)
                {
                    message += Environment.NewLine + "Skipped existing files: " + skipped.ToString("N0");
                }
                targetEntryColorCache.Clear();
                RefreshEntries(selectedPathsToRestore, topPathToRestore);
                statusLabel.Text = message.Replace(Environment.NewLine, " ");
                MessageBox.Show(this, message, "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                importButton.Enabled = true;
                Cursor.Current = previousCursor;
                TryDeleteDirectory(tempRoot);
            }
        }

        private void PopulateTargetPackageComboBox()
        {
            if (targetPackageComboBox == null)
            {
                return;
            }

            HashSet<string> existing = new HashSet<string>(
                targetPackageComboBox.Items.Cast<object>().Select(item => NormalizePackageName(Convert.ToString(item))),
                StringComparer.OrdinalIgnoreCase);

            foreach (string package in GetExistingTargetPackageNames())
            {
                if (existing.Add(package))
                {
                    targetPackageComboBox.Items.Add(package);
                }
            }
        }

        private List<string> GetExistingTargetPackageNames()
        {
            List<string> packages = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(targetClientRootPath))
                {
                    return packages;
                }

                string resourcesRoot = Path.Combine(targetClientRootPath, "resources");
                if (!Directory.Exists(resourcesRoot))
                {
                    return packages;
                }

                packages.AddRange(Directory.GetFiles(resourcesRoot, "*.pck", SearchOption.TopDirectoryOnly)
                    .Select(path => NormalizePackageName(Path.GetFileNameWithoutExtension(path)))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            }
            catch
            {
            }

            return packages;
        }

        private bool TargetPackageExists(string packageName)
        {
            string normalized = NormalizePackageName(packageName);
            if (string.IsNullOrWhiteSpace(normalized) || string.IsNullOrWhiteSpace(targetClientRootPath))
            {
                return false;
            }

            string pckPath = Path.Combine(targetClientRootPath, "resources", normalized + ".pck");
            return File.Exists(pckPath);
        }

        private string ResolveTargetPackageForImport(string sourcePackage, Dictionary<string, string> remap)
        {
            string normalized = NormalizePackageName(sourcePackage);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }

            string mapped;
            if (remap != null && remap.TryGetValue(normalized, out mapped))
            {
                return NormalizePackageName(mapped);
            }

            if (TargetPackageExists(normalized))
            {
                return normalized;
            }

            string resolved;
            if (!EnsureImportTargetPackageExists(normalized, out resolved))
            {
                return normalized;
            }

            if (remap != null)
            {
                remap[normalized] = resolved;
            }

            return resolved;
        }

        private bool EnsureImportTargetPackageExists(string requestedPackage, out string resolvedPackage)
        {
            resolvedPackage = NormalizePackageName(requestedPackage);
            if (TargetPackageExists(resolvedPackage))
            {
                return true;
            }

            List<string> availablePackages = GetExistingTargetPackageNames();
            if (availablePackages.Count == 0)
            {
                MessageBox.Show(this, "No target PCK packages were found in the current client's resources folder.", "PCK Explorer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string selected = ShowTargetPackagePicker(resolvedPackage, availablePackages);
            if (string.IsNullOrWhiteSpace(selected))
            {
                return false;
            }

            resolvedPackage = NormalizePackageName(selected);
            return TargetPackageExists(resolvedPackage);
        }

        private string ShowTargetPackagePicker(string missingPackage, List<string> availablePackages)
        {
            using (Form dialog = new Form())
            using (Label messageLabel = new Label())
            using (ComboBox packageCombo = new ComboBox())
            using (Button okButton = new Button())
            using (Button cancelButton = new Button())
            {
                dialog.Text = "Choose target PCK";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.ClientSize = new Size(420, 128);
                dialog.BackColor = Color.FromArgb(15, 19, 24);
                dialog.ForeColor = Color.White;
                dialog.Font = Font;

                messageLabel.Text = "Target package does not exist: " + missingPackage + ".pck\r\nChoose an existing PCK to receive these files:";
                messageLabel.SetBounds(12, 10, 396, 38);
                messageLabel.ForeColor = Color.White;
                dialog.Controls.Add(messageLabel);

                packageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
                packageCombo.SetBounds(12, 54, 396, 26);
                packageCombo.BackColor = Color.FromArgb(18, 23, 29);
                packageCombo.ForeColor = Color.White;
                packageCombo.Items.AddRange(availablePackages.Cast<object>().ToArray());
                if (packageCombo.Items.Count > 0)
                {
                    packageCombo.SelectedIndex = 0;
                }
                dialog.Controls.Add(packageCombo);

                okButton.Text = "OK";
                okButton.SetBounds(252, 92, 75, 26);
                okButton.DialogResult = DialogResult.OK;
                dialog.Controls.Add(okButton);

                cancelButton.Text = "Cancel";
                cancelButton.SetBounds(333, 92, 75, 26);
                cancelButton.DialogResult = DialogResult.Cancel;
                dialog.Controls.Add(cancelButton);

                dialog.AcceptButton = okButton;
                dialog.CancelButton = cancelButton;

                return dialog.ShowDialog(this) == DialogResult.OK
                    ? Convert.ToString(packageCombo.SelectedItem)
                    : string.Empty;
            }
        }

        private static void MoveStagedPackageFiles(string sourceRoot, string targetRoot)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot)
                || string.IsNullOrWhiteSpace(targetRoot)
                || string.Equals(sourceRoot, targetRoot, StringComparison.OrdinalIgnoreCase)
                || !Directory.Exists(sourceRoot))
            {
                return;
            }

            string fullSourceRoot = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (string sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string fullSourceFile = Path.GetFullPath(sourceFile);
                if (!fullSourceFile.StartsWith(fullSourceRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string relative = NormalizePackageRelativePath(Path.GetFileName(targetRoot), fullSourceFile.Substring(fullSourceRoot.Length));
                string targetFile = Path.Combine(targetRoot, relative);
                string targetDirectory = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                if (!File.Exists(targetFile))
                {
                    File.Move(sourceFile, targetFile);
                }
            }
        }

        private static bool AddQueuedImport(Dictionary<string, ImportAssetRequest> queued, Queue<ImportAssetRequest> pending, ImportAssetRequest request)
        {
            if (request == null)
            {
                return false;
            }

            request.SourcePackage = NormalizePackageName(request.SourcePackage);
            request.TargetPackage = NormalizePackageName(request.TargetPackage);
            request.SourceRelativePath = NormalizePath(request.SourceRelativePath);
            request.TargetRelativePath = NormalizePath(request.TargetRelativePath);
            if (string.IsNullOrWhiteSpace(request.SourcePackage)
                || string.IsNullOrWhiteSpace(request.TargetPackage)
                || string.IsNullOrWhiteSpace(request.SourceRelativePath)
                || string.IsNullOrWhiteSpace(request.TargetRelativePath))
            {
                return false;
            }

            string key = request.SourcePackage + "|" + request.SourceRelativePath + ">" + request.TargetPackage + "|" + request.TargetRelativePath;
            if (queued.ContainsKey(key))
            {
                return false;
            }

            queued[key] = request;
            pending.Enqueue(request);
            return true;
        }

        private void StageImportAsset(
            ImportAssetRequest request,
            byte[] payload,
            string tempRoot,
            Dictionary<string, List<ImportAssetRequest>> stagedByPackage,
            Dictionary<string, HashSet<string>> targetEntryCache,
            ref int staged,
            ref int skipped)
        {
            if (request == null || payload == null || payload.Length == 0)
            {
                return;
            }

            HashSet<string> existing = GetTargetEntries(request.TargetPackage, targetEntryCache, false);
            string targetRelativePath = NormalizePackageRelativePath(request.TargetPackage, request.TargetRelativePath);
            if (existing != null && existing.Contains(targetRelativePath))
            {
                skipped++;
                return;
            }

            string packageRoot = Path.Combine(tempRoot, request.TargetPackage);
            string targetFile = Path.Combine(packageRoot, targetRelativePath.Replace('\\', Path.DirectorySeparatorChar));
            string targetDirectory = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.WriteAllBytes(targetFile, payload);
            request.TargetRelativePath = targetRelativePath;
            if (existing != null)
            {
                existing.Add(targetRelativePath);
            }

            List<ImportAssetRequest> packageRequests;
            if (!stagedByPackage.TryGetValue(request.TargetPackage, out packageRequests))
            {
                packageRequests = new List<ImportAssetRequest>();
                stagedByPackage[request.TargetPackage] = packageRequests;
            }
            packageRequests.Add(request);
            staged++;
        }

        private static bool CanUseRawPackageImport(ImportAssetRequest request)
        {
            if (request == null)
            {
                return false;
            }

            string sourcePackage = NormalizePackageName(request.SourcePackage);
            string targetPackage = NormalizePackageName(request.TargetPackage);
            return !string.IsNullOrWhiteSpace(sourcePackage)
                && !string.IsNullOrWhiteSpace(targetPackage)
                && string.Equals(targetPackage, "models", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(request.SourceRelativePath)
                && !string.IsNullOrWhiteSpace(request.TargetRelativePath);
        }

        private static List<PckRawCopyImportItem> BuildRawCopyItems(IEnumerable<ImportAssetRequest> requests)
        {
            List<PckRawCopyImportItem> items = new List<PckRawCopyImportItem>();
            if (requests == null)
            {
                return items;
            }

            foreach (ImportAssetRequest request in requests)
            {
                if (!CanUseRawPackageImport(request))
                {
                    continue;
                }

                items.Add(new PckRawCopyImportItem
                {
                    SourceRelativePath = NormalizePackageRelativePath(request.SourcePackage, request.SourceRelativePath),
                    TargetRelativePath = NormalizePackageRelativePath(request.TargetPackage, request.TargetRelativePath)
                });
            }

            return items;
        }

        private HashSet<string> GetTargetEntries(string packageName, Dictionary<string, HashSet<string>> cache)
        {
            return GetTargetEntries(packageName, cache, true);
        }

        private HashSet<string> GetTargetEntries(string packageName, Dictionary<string, HashSet<string>> cache, bool includeAliases)
        {
            packageName = NormalizePackageName(packageName);
            string cacheKey = includeAliases ? packageName + "|aliases" : packageName + "|exact";
            HashSet<string> entries;
            if (cache.TryGetValue(cacheKey, out entries))
            {
                return entries;
            }

            entries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string resourcesRoot = string.IsNullOrWhiteSpace(targetClientRootPath)
                ? string.Empty
                : Path.Combine(targetClientRootPath, "resources");
            string pckPath = string.IsNullOrWhiteSpace(resourcesRoot)
                ? string.Empty
                : Path.Combine(resourcesRoot, packageName + ".pck");
            string pkxPath = string.IsNullOrWhiteSpace(resourcesRoot)
                ? string.Empty
                : Path.Combine(resourcesRoot, packageName + ".pkx");
            if (!string.IsNullOrWhiteSpace(pckPath) && File.Exists(pckPath))
            {
                if (!File.Exists(pkxPath))
                {
                    pkxPath = string.Empty;
                }

                List<string> exact;
                string error;
                if (reader.TryEnumeratePackageFileEntries(packageName, pckPath, pkxPath, out exact, out error) && exact != null)
                {
                    foreach (string entry in exact)
                    {
                        if (includeAliases)
                        {
                            AddEntryKey(entries, packageName, entry);
                        }
                        else
                        {
                            string normalized = NormalizePath(entry);
                            if (!string.IsNullOrWhiteSpace(normalized))
                            {
                                entries.Add(normalized);
                            }
                        }
                    }
                }
            }

            cache[cacheKey] = entries;
            return entries;
        }

        private bool EntryExistsInCurrentClient(string packageName, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(targetClientRootPath) || !Directory.Exists(targetClientRootPath))
            {
                return false;
            }

            HashSet<string> entries = GetTargetEntries(packageName, targetEntryColorCache);
            if (entries == null || entries.Count == 0)
            {
                return false;
            }

            string normalized = NormalizePath(relativePath);
            return entries.Contains(normalized);
        }

        private bool TryReadSourceAsset(string packageName, string relativePath, out byte[] payload, out string resolvedPath, out string error)
        {
            payload = null;
            resolvedPath = string.Empty;
            error = string.Empty;

            if (TryReadLooseSourceAsset(packageName, relativePath, out payload, out resolvedPath, out error))
            {
                return true;
            }

            string pckPath;
            string pkxPath;
            if (!TryGetSourcePackagePaths(packageName, out pckPath, out pkxPath))
            {
                error = "Source package was not found: " + packageName + ".pck";
                return false;
            }

            return reader.TryReadPackageFileEntry(packageName, pckPath, pkxPath, relativePath, out payload, out resolvedPath, out error);
        }

        private bool TryReadLooseSourceAsset(string packageName, string relativePath, out byte[] payload, out string resolvedPath, out string error)
        {
            payload = null;
            resolvedPath = NormalizePath(relativePath);
            error = string.Empty;

            string resourcesRoot = Path.GetDirectoryName(sourcePckPath) ?? string.Empty;
            string normalizedPackage = NormalizePackageName(packageName);
            string normalizedRelative = NormalizePath(relativePath);
            if (string.IsNullOrWhiteSpace(resourcesRoot)
                || string.IsNullOrWhiteSpace(normalizedPackage)
                || string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return false;
            }

            string absolute = Path.Combine(resourcesRoot, normalizedPackage, normalizedRelative.Replace('\\', Path.DirectorySeparatorChar));
            try
            {
                string fullResources = Path.GetFullPath(resourcesRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string fullAbsolute = Path.GetFullPath(absolute);
                if (!fullAbsolute.StartsWith(fullResources, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullAbsolute))
                {
                    return false;
                }

                payload = File.ReadAllBytes(fullAbsolute);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                payload = null;
                return false;
            }
        }

        private void QueuePreviewResolvedDependencies(
            ImportAssetRequest root,
            Dictionary<string, string> targetPackageRemap,
            Dictionary<string, HashSet<string>> sourceEntryCache,
            Dictionary<string, ImportAssetRequest> queued,
            Queue<ImportAssetRequest> pending,
            ref int dependencyCount)
        {
            if (root == null || !IsPreviewableModel(root.SourceRelativePath))
            {
                return;
            }

            string sourceGameRoot = ResolveSourceGameRoot();
            if (string.IsNullOrWhiteSpace(sourceGameRoot))
            {
                return;
            }

            string rootMappedPath = NormalizePackageName(root.SourcePackage) + "\\" + NormalizePath(root.SourceRelativePath);
            List<string> dependencies;
            string error;
            lock (gameRootSwitchSync)
            {
                string previousGameRoot = AssetManager.GameRootPath;
                try
                {
                    AssetManager.GameRootPath = sourceGameRoot;
                    if (!previewDependencyLoader.TryCollectDependencyPaths(assetManager ?? new AssetManager(), rootMappedPath, out dependencies, out error)
                        || dependencies == null)
                    {
                        return;
                    }
                }
                finally
                {
                    AssetManager.GameRootPath = previousGameRoot;
                }
            }

            for (int i = 0; i < dependencies.Count; i++)
            {
                string dependencyPath = NormalizePath(dependencies[i]);
                if (string.Equals(dependencyPath, rootMappedPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ImportAssetRequest dependency;
                if (!TryResolveSourceDependency(dependencyPath, sourceEntryCache, out dependency))
                {
                    continue;
                }

                dependency.TargetPackage = ResolveTargetPackageForImport(dependency.SourcePackage, targetPackageRemap);
                dependency.TargetRelativePath = dependency.SourceRelativePath;
                dependency.IsRoot = false;

                if (AddQueuedImport(queued, pending, dependency))
                {
                    dependencyCount++;
                }
            }
        }

        private void QueueAssociatedAnimationTracks(
            ImportAssetRequest current,
            Dictionary<string, string> targetPackageRemap,
            Dictionary<string, HashSet<string>> sourceEntryCache,
            Dictionary<string, ImportAssetRequest> queued,
            Queue<ImportAssetRequest> pending,
            ref int dependencyCount)
        {
            if (current == null || !IsModelDescriptorOrGeometry(current.SourceRelativePath))
            {
                return;
            }

            string currentDirectory = NormalizePath(Path.GetDirectoryName(current.SourceRelativePath) ?? string.Empty);
            if (string.IsNullOrWhiteSpace(currentDirectory))
            {
                return;
            }

            HashSet<string> entries = GetSourceEntries(current.SourcePackage, sourceEntryCache);
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            string directoryPrefix = currentDirectory.TrimEnd('\\') + "\\";
            foreach (string entry in entries)
            {
                string normalized = NormalizePath(entry);
                string extension = Path.GetExtension(normalized);
                if (!string.Equals(extension, ".stck", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(extension, ".sdr", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!normalized.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ImportAssetRequest dependency = new ImportAssetRequest
                {
                    SourcePackage = current.SourcePackage,
                    SourceRelativePath = normalized,
                    TargetPackage = ResolveTargetPackageForImport(current.SourcePackage, targetPackageRemap),
                    TargetRelativePath = normalized,
                    IsRoot = false
                };

                if (AddQueuedImport(queued, pending, dependency))
                {
                    dependencyCount++;
                }
            }
        }

        private void QueueModelDirectoryAssets(
            ImportAssetRequest current,
            Dictionary<string, HashSet<string>> sourceEntryCache,
            Dictionary<string, ImportAssetRequest> queued,
            Queue<ImportAssetRequest> pending,
            ref int dependencyCount)
        {
            if (current == null || !IsModelDescriptorOrGeometry(current.SourceRelativePath))
            {
                return;
            }

            string currentDirectory = NormalizePath(Path.GetDirectoryName(current.SourceRelativePath) ?? string.Empty);
            if (string.IsNullOrWhiteSpace(currentDirectory))
            {
                return;
            }

            HashSet<string> entries = GetSourceEntries(current.SourcePackage, sourceEntryCache);
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            string directoryPrefix = currentDirectory.TrimEnd('\\') + "\\";
            foreach (string entry in entries)
            {
                string normalized = NormalizePath(entry);
                if (!normalized.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ImportAssetRequest dependency = new ImportAssetRequest
                {
                    SourcePackage = current.SourcePackage,
                    SourceRelativePath = normalized,
                    TargetPackage = current.TargetPackage,
                    TargetRelativePath = normalized,
                    IsRoot = false
                };

                if (AddQueuedImport(queued, pending, dependency))
                {
                    dependencyCount++;
                }
            }
        }

        private static bool IsModelDescriptorOrGeometry(string relativePath)
        {
            string extension = Path.GetExtension(relativePath);
            return string.Equals(extension, ".ecm", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".smd", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".ski", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".bon", StringComparison.OrdinalIgnoreCase);
        }

        private bool TryResolveSourceDependency(
            string mappedPath,
            Dictionary<string, HashSet<string>> sourceEntryCache,
            out ImportAssetRequest request)
        {
            request = null;
            string package;
            string relative;
            if (!TrySplitPackagePath(mappedPath, out package, out relative))
            {
                return false;
            }

            package = NormalizePackageName(package);
            relative = NormalizePath(relative);
            if (string.IsNullOrWhiteSpace(package) || string.IsNullOrWhiteSpace(relative))
            {
                return false;
            }

            HashSet<string> entries = GetSourceEntries(package, sourceEntryCache);
            if (entries == null || !entries.Contains(relative))
            {
                return false;
            }

            request = new ImportAssetRequest
            {
                SourcePackage = package,
                SourceRelativePath = relative,
                TargetPackage = package,
                TargetRelativePath = relative
            };
            return true;
        }

        private HashSet<string> GetSourceEntries(string packageName, Dictionary<string, HashSet<string>> cache)
        {
            packageName = NormalizePackageName(packageName);
            HashSet<string> entries;
            if (cache.TryGetValue(packageName, out entries))
            {
                return entries;
            }

            entries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string pckPath;
            string pkxPath;
            if (TryGetSourcePackagePaths(packageName, out pckPath, out pkxPath))
            {
                List<string> rawEntries;
                string error;
                if (reader.TryEnumeratePackageFileEntries(packageName, pckPath, pkxPath, out rawEntries, out error) && rawEntries != null)
                {
                    foreach (string entry in rawEntries)
                    {
                        AddEntryKey(entries, packageName, entry);
                    }
                }
            }
            AddLooseSourceEntries(packageName, entries);

            cache[packageName] = entries;
            return entries;
        }

        private void AddLooseSourceEntries(string packageName, HashSet<string> entries)
        {
            string resourcesRoot = Path.GetDirectoryName(sourcePckPath) ?? string.Empty;
            string normalizedPackage = NormalizePackageName(packageName);
            if (entries == null || string.IsNullOrWhiteSpace(resourcesRoot) || string.IsNullOrWhiteSpace(normalizedPackage))
            {
                return;
            }

            string packageRoot = Path.Combine(resourcesRoot, normalizedPackage);
            if (!Directory.Exists(packageRoot))
            {
                return;
            }

            try
            {
                string fullRoot = Path.GetFullPath(packageRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                foreach (string file in Directory.GetFiles(packageRoot, "*", SearchOption.AllDirectories))
                {
                    string fullFile = Path.GetFullPath(file);
                    if (!fullFile.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string relative = NormalizePath(fullFile.Substring(fullRoot.Length));
                    AddEntryKey(entries, normalizedPackage, relative);
                }
            }
            catch
            {
            }
        }

        private static void AddEntryKey(HashSet<string> entries, string packageName, string entry)
        {
            if (entries == null)
            {
                return;
            }

            string normalized = NormalizePath(entry);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            entries.Add(normalized);
            string normalizedPackage = NormalizePackageName(packageName);
            string prefix = normalizedPackage + "\\";
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(NormalizePath(normalized.Substring(prefix.Length)));
            }
        }

        private bool TryGetSourcePackagePaths(string packageName, out string pckPath, out string pkxPath)
        {
            packageName = NormalizePackageName(packageName);
            string resourcesRoot = Path.GetDirectoryName(sourcePckPath) ?? string.Empty;
            pckPath = Path.Combine(resourcesRoot, packageName + ".pck");
            pkxPath = Path.Combine(resourcesRoot, packageName + ".pkx");
            if (!File.Exists(pckPath))
            {
                return false;
            }
            if (!File.Exists(pkxPath))
            {
                pkxPath = string.Empty;
            }
            return true;
        }

        private IEnumerable<string> CollectReferenceCandidates(string currentPackage, string currentRelativePath, byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                yield break;
            }

            HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in ExtractReferencePaths(payload))
            {
                foreach (string candidate in ResolveReferenceCandidates(currentPackage, currentRelativePath, raw))
                {
                    if (yielded.Add(candidate))
                    {
                        yield return candidate;
                    }
                }
            }
        }

        private IEnumerable<string> ExtractReferencePaths(byte[] payload)
        {
            string text = DecodeGbkPayload(payload);
            MatchCollection matches = Regex.Matches(
                text,
                @"[^\0\r\n\t""'<>|:*?]{1,220}\.(?:dds|tga|bmp|png|jpg|jpeg|ski|smd|ecm|gfx|att|sgc|bon|stck|sdr)",
                RegexOptions.IgnoreCase);
            for (int i = 0; i < matches.Count; i++)
            {
                string value = NormalizeExtractedPathCandidate(matches[i].Value);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return value;
                }
            }

            string[] extensions = { ".ecm", ".smd", ".ski", ".gfx", ".att", ".sgc", ".stck", ".sdr" };
            for (int i = 0; i < extensions.Length; i++)
            {
                foreach (string value in ExtractPathsByRawByteScan(payload, extensions[i]))
                {
                    string normalized = NormalizeExtractedPathCandidate(value);
                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        yield return normalized;
                    }
                }
            }
        }

        private IEnumerable<string> ResolveReferenceCandidates(string currentPackage, string currentRelativePath, string reference)
        {
            string normalizedReference = NormalizePath(reference);
            if (string.IsNullOrWhiteSpace(normalizedReference))
            {
                yield break;
            }

            string package;
            string relative;
            if (TrySplitPackagePath(normalizedReference, out package, out relative) && SourcePackageExists(package))
            {
                yield return NormalizePackageName(package) + "\\" + NormalizePath(relative);
                yield break;
            }

            string currentDirectory = Path.GetDirectoryName(NormalizePath(currentRelativePath)) ?? string.Empty;
            string current = NormalizePackageName(currentPackage);
            string extension = Path.GetExtension(normalizedReference);

            if (normalizedReference.Contains("\\"))
            {
                string firstSegment = normalizedReference.Split('\\')[0];
                if (SourcePackageExists(firstSegment))
                {
                    yield return NormalizePackageName(firstSegment) + "\\" + NormalizePath(normalizedReference.Substring(firstSegment.Length).TrimStart('\\'));
                }

                if (IsEffectDescriptorExtension(extension))
                {
                    yield return "gfx\\" + normalizedReference;
                }

                yield return current + "\\" + normalizedReference;
                if (!string.IsNullOrWhiteSpace(currentDirectory))
                {
                    yield return current + "\\" + currentDirectory + "\\" + normalizedReference;
                }
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(currentDirectory))
            {
                yield return current + "\\" + currentDirectory + "\\" + normalizedReference;
                yield return current + "\\" + currentDirectory + "\\textures\\" + normalizedReference;
                yield return current + "\\" + currentDirectory + "\\texture\\" + normalizedReference;
            }

            yield return current + "\\textures\\" + normalizedReference;
            yield return current + "\\texture\\" + normalizedReference;
            yield return current + "\\" + normalizedReference;
        }

        private bool SourcePackageExists(string packageName)
        {
            string pckPath;
            string pkxPath;
            return TryGetSourcePackagePaths(packageName, out pckPath, out pkxPath);
        }

        private static bool ShouldScanForDependencies(string relativePath)
        {
            string extension = Path.GetExtension(relativePath);
            return IsModelLikeExtension(extension)
                || IsEffectDescriptorExtension(extension)
                || string.Equals(extension, ".sdr", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsModelLikeExtension(string extension)
        {
            return string.Equals(extension, ".ecm", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".smd", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".ski", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".bon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".stck", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".att", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".sgc", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEffectDescriptorExtension(string extension)
        {
            return string.Equals(extension, ".gfx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".att", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".sgc", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TrySplitPackagePath(string mappedPath, out string package, out string relative)
        {
            package = string.Empty;
            relative = string.Empty;
            string normalized = NormalizePath(mappedPath);
            int slash = normalized.IndexOf('\\');
            if (slash <= 0 || slash >= normalized.Length - 1)
            {
                return false;
            }

            package = NormalizePackageName(normalized.Substring(0, slash));
            relative = NormalizePath(normalized.Substring(slash + 1));
            return !string.IsNullOrWhiteSpace(package) && !string.IsNullOrWhiteSpace(relative);
        }

        private static IEnumerable<string> ExtractPathsByRawByteScan(byte[] payload, string extension)
        {
            if (payload == null || payload.Length == 0 || string.IsNullOrWhiteSpace(extension))
            {
                yield break;
            }

            byte[] needle = Encoding.ASCII.GetBytes(extension.ToLowerInvariant());
            for (int i = 0; i <= payload.Length - needle.Length; i++)
            {
                bool match = true;
                for (int n = 0; n < needle.Length; n++)
                {
                    byte currentByte = payload[i + n];
                    if (currentByte >= (byte)'A' && currentByte <= (byte)'Z')
                    {
                        currentByte = (byte)(currentByte + 32);
                    }
                    if (currentByte != needle[n])
                    {
                        match = false;
                        break;
                    }
                }

                if (!match)
                {
                    continue;
                }

                int start = i;
                while (start > 0 && IsLikelyPathByte(payload[start - 1]))
                {
                    start--;
                }

                int end = i + needle.Length;
                while (end < payload.Length && IsLikelyPathByte(payload[end]))
                {
                    end++;
                }

                if (end <= start)
                {
                    continue;
                }

                string value = DecodePathBytes(payload, start, end - start);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return value;
                }
            }
        }

        private static bool IsLikelyPathByte(byte value)
        {
            if (value == 0 || value == '"' || value == '\'' || value == '<' || value == '>' || value == '|' || value == '*' || value == '?')
            {
                return false;
            }

            return value >= 32;
        }

        private static string DecodePathBytes(byte[] payload, int start, int count)
        {
            byte[] bytes = new byte[count];
            Buffer.BlockCopy(payload, start, bytes, 0, count);
            try
            {
                return Encoding.GetEncoding("GBK").GetString(bytes);
            }
            catch
            {
                return Encoding.Default.GetString(bytes);
            }
        }

        private static string DecodeGbkPayload(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                return Encoding.GetEncoding("GBK").GetString(payload).Replace('\0', '\n');
            }
            catch
            {
                char[] chars = new char[payload.Length];
                for (int i = 0; i < payload.Length; i++)
                {
                    byte value = payload[i];
                    chars[i] = value >= 32 && value != 127 ? (char)value : '\0';
                }
                return new string(chars);
            }
        }

        private static string NormalizeExtractedPathCandidate(string value)
        {
            value = (value ?? string.Empty)
                .Trim()
                .Trim('\0')
                .TrimStart('.', '\\', '/')
                .Replace('/', '\\');

            while (value.Contains("\\\\"))
            {
                value = value.Replace("\\\\", "\\");
            }

            return value.Trim();
        }

        private string BuildTargetRelativePath(string sourceRelativePath)
        {
            string normalized = NormalizePath(sourceRelativePath);
            if (preservePathCheckBox.Checked)
            {
                return normalized;
            }

            string prefix = (targetPrefixTextBox.Text ?? string.Empty).Replace('/', '\\').Trim('\\');
            return string.IsNullOrWhiteSpace(prefix)
                ? normalized
                : prefix + "\\" + normalized;
        }

        private static string NormalizePackageRelativePath(string packageName, string relativePath)
        {
            string normalized = NormalizePath(relativePath);
            string package = NormalizePackageName(packageName);
            if (string.IsNullOrWhiteSpace(normalized) || string.IsNullOrWhiteSpace(package))
            {
                return normalized;
            }

            string prefix = package + "\\";
            return normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? NormalizePath(normalized.Substring(prefix.Length))
                : normalized;
        }

        private static string NormalizePackageName(string value)
        {
            value = (value ?? string.Empty).Trim();
            if (value.EndsWith(".pck", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".pkx", StringComparison.OrdinalIgnoreCase))
            {
                value = Path.GetFileNameWithoutExtension(value);
            }
            return value.Replace('/', '\\').Trim('\\');
        }

        private static string NormalizePath(string value)
        {
            value = (value ?? string.Empty)
                .Trim()
                .TrimStart('\\', '/')
                .Replace('/', '\\');

            while (value.Contains("\\\\"))
            {
                value = value.Replace("\\\\", "\\");
            }

            return value;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
            }
        }
    }
}
