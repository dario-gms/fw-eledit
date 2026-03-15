using System;
using System.Collections;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

namespace sELedit
{
	public partial class MainWindow : Form
	{
        public static eListCollection eLC;
		public eListConversation conversationList;
		string[][] xrefs;
		private Point mouseMoveCheck;
		public bool EnableSelectionList = true;
		public bool EnableSelectionItem = true;
		string ElementsPath = "";
        public Bitmap raw_img;
        public static string[] buff_str;
        public static string[] item_ext_desc;
        public static string[] skillstr;
        public static SortedList addonslist;
        public static SortedList InstanceList;
        public static CacheSave database = null;
        private int proctypeLocation = 0;
        private int proctypeLocationvak = 0;
        private IToolType customTooltype;
        public static SortedList LocalizationText;

        public AssetManager asm;
        private bool fwLayoutInitialized = false;
        private SplitContainer fwMainSplit;
        private TabControl fwRightTabs;
        private TabPage fwValuesTab;
        private Button fwInlinePickIconButton;
        private int fwInlinePickIconRowIndex = -1;
        private TabControl fwEquipmentTabs;
        private TabPage fwEquipmentTabMain;
        private TabPage fwEquipmentTabRefine;
        private TabPage fwEquipmentTabModels;
        private TabPage fwEquipmentTabOther;
        private TabPage fwDescriptionTab;
        private TextBox fwDescriptionEditor;
        private RichTextBox fwDescriptionPreview;
        private Button fwDescriptionSaveButton;
        private Label fwDescriptionStatusLabel;
        private Dictionary<int, string> itemExtDescMap = new Dictionary<int, string>();
        private List<int> itemExtDescOrder = new List<int>();
        private string itemExtDescFilePath = string.Empty;
        private bool hasPendingDescriptionChanges = false;
        private bool hasUnsavedChanges = false;
        private bool startupSessionRestoreDone = false;
        private bool suppressClosePrompt = false;
        private bool isRestoringSessionState = false;
        private bool isUpdatingDescriptionUi = false;
        private bool suppressValuesUiRefresh = false;
        private ListBox searchSuggestionList;
        private const int SearchSuggestionMax = 200;
        private const int SearchSuggestionMaxHeight = 180;
        private System.Windows.Forms.Timer navigationPersistTimer;
        private bool hasPendingNavigationStateWrite = false;
        private int currentDescriptionItemId = 0;
        private readonly Dictionary<int, HashSet<int>> dirtyRowsByList = new Dictionary<int, HashSet<int>>();
        private readonly Dictionary<int, Dictionary<int, HashSet<int>>> dirtyFieldsByList = new Dictionary<int, Dictionary<int, HashSet<int>>>();
        private readonly Dictionary<int, Dictionary<int, HashSet<int>>> invalidFieldsByList = new Dictionary<int, Dictionary<int, HashSet<int>>>();
        private readonly Dictionary<int, string> list0DisplayNameCache = new Dictionary<int, string>();
        private readonly Dictionary<int, List<object[]>> listDisplayRowsCache = new Dictionary<int, List<object[]>>();
        private readonly Dictionary<int, string> modelPackageByPathIdCache = new Dictionary<int, string>();
        private string modelPackageCacheSignature = string.Empty;
        private static readonly string[] ModelPickerPackageOrder = new string[]
        {
            "building",
            "configs",
            "gfx",
            "grasses",
            "interfaces",
            "litmodels",
            "loddata",
            "models",
            "script",
            "sfx",
            "shaders",
            "surfaces",
            "textures"
        };
        private static readonly string[] ModelPickerPathPackages = new string[]
        {
            "building",
            "configs",
            "gfx",
            "grasses",
            "interfaces",
            "litmodels",
            "loddata",
            "models",
            "script",
            "sfx",
            "shaders",
            "surfaces",
            "textures",
            "moxing"
        };
        private class ModelPickerPackageCacheEntry
        {
            public DateTime PckTimestampUtc { get; set; }
            public List<string> Files { get; set; }
        }
        private static readonly Dictionary<string, ModelPickerPackageCacheEntry> modelPickerPackageCache
            = new Dictionary<string, ModelPickerPackageCacheEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> modelPickerMissingExtractNotified
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> ListFriendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "EQUIPMENT_ADDON", "Added Attribute" }
        };
        private static readonly List<QualityOption> ItemQualityOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Gray" },
            new QualityOption { Value = 1, Label = "White" },
            new QualityOption { Value = 2, Label = "Green" },
            new QualityOption { Value = 3, Label = "Blue" },
            new QualityOption { Value = 4, Label = "Purple" },
            new QualityOption { Value = 5, Label = "Gold" },
            new QualityOption { Value = 6, Label = "Red" },
            new QualityOption { Value = 7, Label = "Gold Red" },
            new QualityOption { Value = 8, Label = "Light Blue" },
            new QualityOption { Value = 9, Label = "Light Purple" }
        };
        private static readonly Dictionary<int, Color> ItemQualityColors = new Dictionary<int, Color>
        {
            { 0, Color.FromArgb(160, 160, 160) }, // Gray
            { 1, Color.White }, // White
            { 2, Color.FromArgb(0, 200, 80) }, // Green
            { 3, Color.RoyalBlue }, // Blue
            { 4, Color.DarkViolet }, // Purple
            { 5, Color.Gold }, // Gold
            { 6, Color.Red }, // Red
            { 7, Color.OrangeRed }, // Gold Red
            { 8, Color.LightSkyBlue }, // Light Blue
            { 9, Color.MediumPurple } // Light Purple
        };

        private class SearchSuggestion
        {
            public int ListIndex { get; set; }
            public int ElementIndex { get; set; }
            public string IdText { get; set; }
            public string NameText { get; set; }

            public override string ToString()
            {
                string listTag = "[" + ListIndex.ToString("D3") + "]: ";
                if (string.IsNullOrWhiteSpace(NameText))
                {
                    return listTag + (IdText ?? string.Empty);
                }
                if (string.IsNullOrWhiteSpace(IdText))
                {
                    return listTag + NameText;
                }
                return listTag + IdText + " - " + NameText;
            }
        }

        private class ModelPickerEntry
        {
            public int Index { get; set; }
            public int PathId { get; set; }
            public string Package { get; set; }
            public string RelativePath { get; set; }
            public Bitmap Icon { get; set; }
            public int ItemId { get; set; }
            public string ItemName { get; set; }
            public int Uses { get; set; }
        }

        private class AddonTypeOption
        {
            public int TypeId { get; set; }
            public string Display { get; set; }
        }

        private class QualityOption
        {
            public int Value { get; set; }
            public string Label { get; set; }

            public override string ToString()
            {
                return Value.ToString() + " - " + (Label ?? string.Empty);
            }
        }

        private enum EquipmentValuesTab
        {
            Main,
            Refine,
            Models,
            Other,
            All
        }

        private class AddonTypePickerWindow : Form
        {
            private readonly List<AddonTypeOption> allOptions;
            private readonly ListView listView;
            private readonly TextBox searchBox;
            public int SelectedType { get; private set; }

            public AddonTypePickerWindow(List<AddonTypeOption> options, int currentType)
            {
                allOptions = options ?? new List<AddonTypeOption>();
                SelectedType = currentType;

                Text = "Choose value...";
                StartPosition = FormStartPosition.CenterParent;
                MinimumSize = new Size(520, 420);
                Size = new Size(760, 560);
                BackColor = Color.FromArgb(24, 24, 24);
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                KeyPreview = true;
                KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        DialogResult = DialogResult.Cancel;
                        Close();
                    }
                    else if (e.KeyCode == Keys.Enter)
                    {
                        ConfirmSelection();
                    }
                };

                Panel top = new Panel();
                top.Dock = DockStyle.Top;
                top.Height = 36;
                top.Padding = new Padding(8, 6, 8, 6);

                searchBox = new TextBox();
                searchBox.Dock = DockStyle.Fill;
                searchBox.TextChanged += (s, e) => ApplyFilter();
                top.Controls.Add(searchBox);

                listView = new ListView();
                listView.Dock = DockStyle.Fill;
                listView.View = View.Details;
                listView.FullRowSelect = true;
                listView.HideSelection = false;
                listView.MultiSelect = false;
                listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
                listView.BackColor = Color.FromArgb(24, 24, 24);
                listView.ForeColor = Color.White;
                listView.DoubleClick += (s, e) => ConfirmSelection();
                listView.Columns.Add("Type", 80);
                listView.Columns.Add("Description", 560);

                Panel bottom = new Panel();
                bottom.Dock = DockStyle.Bottom;
                bottom.Height = 44;
                bottom.Padding = new Padding(8, 6, 8, 6);

                Button cancelButton = new Button();
                cancelButton.Text = "Cancel [ESC]";
                cancelButton.Dock = DockStyle.Right;
                cancelButton.Width = 140;
                cancelButton.FlatStyle = FlatStyle.Flat;
                cancelButton.UseVisualStyleBackColor = false;
                cancelButton.BackColor = Color.FromArgb(56, 56, 56);
                cancelButton.ForeColor = Color.Gainsboro;
                cancelButton.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
                cancelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
                cancelButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(84, 84, 84);
                cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
                bottom.Controls.Add(cancelButton);

                Button okButton = new Button();
                okButton.Text = "OK [Enter]";
                okButton.Dock = DockStyle.Right;
                okButton.Width = 140;
                okButton.FlatStyle = FlatStyle.Flat;
                okButton.UseVisualStyleBackColor = false;
                okButton.BackColor = Color.FromArgb(56, 56, 56);
                okButton.ForeColor = Color.Gainsboro;
                okButton.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
                okButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
                okButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(84, 84, 84);
                okButton.Click += (s, e) => ConfirmSelection();
                bottom.Controls.Add(okButton);

                Controls.Add(listView);
                Controls.Add(bottom);
                Controls.Add(top);

                AcceptButton = okButton;
                CancelButton = cancelButton;

                ApplyFilter();
                SelectType(currentType);
                Shown += (s, e) =>
                {
                    BeginInvoke(new Action(() => SelectType(SelectedType)));
                };
            }

            private void ApplyFilter()
            {
                string term = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
                listView.BeginUpdate();
                listView.Items.Clear();

                for (int i = 0; i < allOptions.Count; i++)
                {
                    AddonTypeOption option = allOptions[i];
                    if (!string.IsNullOrWhiteSpace(term))
                    {
                        if (!option.TypeId.ToString().Contains(term)
                            && (option.Display == null || !option.Display.ToLowerInvariant().Contains(term)))
                        {
                            continue;
                        }
                    }

                    ListViewItem item = new ListViewItem(option.TypeId.ToString());
                    item.SubItems.Add(option.Display ?? string.Empty);
                    item.Tag = option;
                    listView.Items.Add(item);
                }

                listView.EndUpdate();
            }

            private void SelectType(int typeId)
            {
                if (typeId < 0)
                {
                    return;
                }
                for (int i = 0; i < listView.Items.Count; i++)
                {
                    AddonTypeOption option = listView.Items[i].Tag as AddonTypeOption;
                    if (option != null && option.TypeId == typeId)
                    {
                        listView.Items[i].Selected = true;
                        listView.Items[i].Focused = true;
                        listView.EnsureVisible(i);
                        return;
                    }
                }
            }

            private void ConfirmSelection()
            {
                if (listView.SelectedItems.Count == 0)
                {
                    return;
                }
                AddonTypeOption option = listView.SelectedItems[0].Tag as AddonTypeOption;
                if (option == null)
                {
                    return;
                }
                SelectedType = option.TypeId;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private class QualityPickerWindow : Form
        {
            private readonly List<QualityOption> options;
            private readonly ListBox listBox;
            public int SelectedValue { get; private set; }

            public QualityPickerWindow(List<QualityOption> options, int currentValue)
            {
                this.options = options ?? new List<QualityOption>();
                SelectedValue = currentValue;

                Text = "Choose value...";
                StartPosition = FormStartPosition.CenterParent;
                MinimumSize = new Size(220, 260);
                Size = new Size(260, 340);
                BackColor = Color.FromArgb(24, 24, 24);
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                KeyPreview = true;
                KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        DialogResult = DialogResult.Cancel;
                        Close();
                    }
                    else if (e.KeyCode == Keys.Enter)
                    {
                        ConfirmSelection();
                    }
                };

                listBox = new ListBox();
                listBox.Dock = DockStyle.Fill;
                listBox.BorderStyle = BorderStyle.FixedSingle;
                listBox.BackColor = Color.FromArgb(24, 24, 24);
                listBox.ForeColor = Color.White;
                listBox.DoubleClick += (s, e) => ConfirmSelection();
                listBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        ConfirmSelection();
                        e.SuppressKeyPress = true;
                    }
                };

                Controls.Add(listBox);

                LoadOptions();
                SelectValue(currentValue);
                Shown += (s, e) =>
                {
                    listBox.Focus();
                };
            }

            private void LoadOptions()
            {
                listBox.BeginUpdate();
                listBox.Items.Clear();
                for (int i = 0; i < options.Count; i++)
                {
                    listBox.Items.Add(options[i]);
                }
                listBox.EndUpdate();
            }

            private void SelectValue(int value)
            {
                for (int i = 0; i < listBox.Items.Count; i++)
                {
                    QualityOption option = listBox.Items[i] as QualityOption;
                    if (option != null && option.Value == value)
                    {
                        listBox.SelectedIndex = i;
                        return;
                    }
                }
            }

            private void ConfirmSelection()
            {
                QualityOption option = listBox.SelectedItem as QualityOption;
                if (option == null)
                {
                    return;
                }
                SelectedValue = option.Value;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private class ModelPickerWindow : Form
        {
            private readonly List<ModelPickerEntry> allEntries;
            private readonly ComboBox packageFilter;
            private readonly ComboBox folderFilter;
            private readonly TextBox searchBox;
            private readonly CheckBox onlyUnusedCheck;
            private readonly Button exportCsvButton;
            private readonly DataGridView grid;
            private readonly Label statusLabel;
            private readonly System.Windows.Forms.Timer filterTimer;
            private bool isInitializing;
            public int SelectedPathId { get; private set; }

            public ModelPickerWindow(List<ModelPickerEntry> entries, int currentPathId)
            {
                allEntries = entries ?? new List<ModelPickerEntry>();
                SelectedPathId = currentPathId;
                isInitializing = true;

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

                exportCsvButton = new Button();
                exportCsvButton.Text = "Export CSV";
                exportCsvButton.AutoSize = true;
                exportCsvButton.Height = 24;
                exportCsvButton.Left = onlyUnusedCheck.Right + 12;
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
                grid.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.Handled = true;
                        ConfirmSelection();
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

                BuildPackageFilter();
                string preferredPackage = GetPackageForPathId(currentPathId);
                if (!string.IsNullOrWhiteSpace(preferredPackage))
                {
                    for (int i = 0; i < packageFilter.Items.Count; i++)
                    {
                        if (string.Equals(Convert.ToString(packageFilter.Items[i]), preferredPackage, StringComparison.OrdinalIgnoreCase))
                        {
                            packageFilter.SelectedIndex = i;
                            break;
                        }
                    }
                }
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
                for (int i = 0; i < ModelPickerPackageOrder.Length; i++)
                {
                    packageFilter.Items.Add(ModelPickerPackageOrder[i]);
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

                // Empty entry = no subfolder filter (all entries for selected package).
                folderFilter.Items.Add(string.Empty);
                for (int i = 0; i < sorted.Count; i++)
                {
                    folderFilter.Items.Add(sorted[i]);
                }

                if (folderFilter.Items.Count == 0)
                {
                    return;
                }

                // Default to full package scope when there is no current path context.
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
                List<ModelPickerEntry> filtered = GetFilteredEntries();
                grid.SuspendLayout();
                grid.Rows.Clear();
                for (int i = 0; i < filtered.Count; i++)
                {
                    ModelPickerEntry e = filtered[i];
                    string name = e.Uses > 0 ? e.ItemName : "[NOT USING]";
                    string idText = e.Uses > 0 && e.ItemId > 0 ? e.ItemId.ToString() : "-";
                    string usesText = e.Uses > 0 ? e.Uses.ToString() : "-";
                    int rowIndex = grid.Rows.Add(
                        i.ToString(),
                        e.RelativePath ?? string.Empty,
                        e.Icon ?? Properties.Resources.blank,
                        name,
                        idText,
                        usesText,
                        e.PathId.ToString()
                    );

                    if (e.Uses <= 0)
                    {
                        grid.Rows[rowIndex].Cells[3].Style.ForeColor = Color.LimeGreen;
                    }
                    else
                    {
                        grid.Rows[rowIndex].Cells[5].Style.ForeColor = Color.Cyan;
                    }
                }
                grid.ResumeLayout();

                statusLabel.Text = filtered.Count.ToString() + " entries";
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

                IEnumerable<ModelPickerEntry> query = allEntries;
                query = query.Where(x => string.Equals(x.Package, package, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    query = query.Where(x => string.Equals(ExtractTopFolder(x.RelativePath, package), folder, StringComparison.OrdinalIgnoreCase));
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
                for (int i = 0; i < grid.Rows.Count; i++)
                {
                    int rowPathId;
                    if (!int.TryParse(Convert.ToString(grid.Rows[i].Cells[6].Value), out rowPathId))
                    {
                        continue;
                    }
                    if (rowPathId == pathId)
                    {
                        grid.CurrentCell = grid.Rows[i].Cells[0];
                        grid.Rows[i].Selected = true;
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
                if (grid.CurrentRow == null)
                {
                    return;
                }
                int pathId;
                if (!int.TryParse(Convert.ToString(grid.CurrentRow.Cells[6].Value), out pathId) || pathId <= 0)
                {
                    return;
                }
                SelectedPathId = pathId;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void ResetList0DisplayCache()
        {
            list0DisplayNameCache.Clear();
            EQUIPMENT_ADDON.ResetRuntimeCaches();
        }

        private void ClearListDisplayCache()
        {
            listDisplayRowsCache.Clear();
        }

        private void InvalidateListDisplayCache(int listIndex)
        {
            if (listDisplayRowsCache.ContainsKey(listIndex))
            {
                listDisplayRowsCache.Remove(listIndex);
            }
        }

        private string GetFriendlyListName(string rawListName)
        {
            if (string.IsNullOrWhiteSpace(rawListName))
            {
                return "Unknown";
            }

            string[] split = rawListName.Split(new string[] { " - " }, StringSplitOptions.None);
            string key = split.Length > 1 ? split[1].Trim() : rawListName.Trim();
            string friendly;
            if (ListFriendlyNames.TryGetValue(key, out friendly))
            {
                return friendly;
            }
            return key;
        }

        private string GetDisplayEntryName(int listIndex, int entryIndex, int nameFieldIndex)
        {
            string fallback = nameFieldIndex >= 0 ? eLC.GetValue(listIndex, entryIndex, nameFieldIndex) : string.Empty;
            if (listIndex != 0)
            {
                return fallback;
            }

            string cached;
            if (list0DisplayNameCache.TryGetValue(entryIndex, out cached))
            {
                return cached;
            }

            try
            {
                string id = eLC.GetValue(listIndex, entryIndex, 0);
                string decoded = EQUIPMENT_ADDON.GetAddon(id);
                if (!string.IsNullOrWhiteSpace(decoded))
                {
                    string normalized = decoded.Replace("\r", " ").Replace("\n", " / ").Trim();
                    list0DisplayNameCache[entryIndex] = normalized;
                    return normalized;
                }
            }
            catch
            { }

            list0DisplayNameCache[entryIndex] = fallback;
            return fallback;
        }

        private string GetAddonTypeDisplayForUi(int listIndex, int entryIndex, string rawType)
        {
            if (string.IsNullOrWhiteSpace(rawType))
            {
                return rawType ?? string.Empty;
            }

            int typeId;
            if (!int.TryParse(rawType, out typeId))
            {
                return rawType;
            }

            Dictionary<int, string> hintedTypes = LoadAddonTypeHints();
            string hint;
            if (hintedTypes.TryGetValue(typeId, out hint) && !string.IsNullOrWhiteSpace(hint))
            {
                return hint.Trim();
            }

            string fallback = EQUIPMENT_ADDON.GetAddonTypeDisplay(rawType);
            if (!string.Equals(fallback, rawType, StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }

            if (listIndex == 0 && entryIndex >= 0)
            {
                try
                {
                    string id = eLC.GetValue(listIndex, entryIndex, 0);
                    string decoded = EQUIPMENT_ADDON.GetAddon(id);
                    if (!string.IsNullOrWhiteSpace(decoded))
                    {
                        return decoded.Replace("\r", " ").Replace("\n", " / ").Trim();
                    }
                }
                catch
                { }
            }

            return rawType;
        }

        private void MarkRowDirty(int listIndex, int rowIndex)
        {
            HashSet<int> set;
            if (!dirtyRowsByList.TryGetValue(listIndex, out set))
            {
                set = new HashSet<int>();
                dirtyRowsByList[listIndex] = set;
            }
            set.Add(rowIndex);
            InvalidateListDisplayCache(listIndex);
            hasUnsavedChanges = true;
        }

        private bool IsRowDirty(int listIndex, int rowIndex)
        {
            HashSet<int> set;
            return dirtyRowsByList.TryGetValue(listIndex, out set) && set.Contains(rowIndex);
        }

        private void MarkFieldDirty(int listIndex, int rowIndex, int fieldIndex)
        {
            Dictionary<int, HashSet<int>> rows;
            if (!dirtyFieldsByList.TryGetValue(listIndex, out rows))
            {
                rows = new Dictionary<int, HashSet<int>>();
                dirtyFieldsByList[listIndex] = rows;
            }
            HashSet<int> fields;
            if (!rows.TryGetValue(rowIndex, out fields))
            {
                fields = new HashSet<int>();
                rows[rowIndex] = fields;
            }
            fields.Add(fieldIndex);
            hasUnsavedChanges = true;
        }

        private bool IsFieldDirty(int listIndex, int rowIndex, int fieldIndex)
        {
            Dictionary<int, HashSet<int>> rows;
            HashSet<int> fields;
            return dirtyFieldsByList.TryGetValue(listIndex, out rows)
                && rows.TryGetValue(rowIndex, out fields)
                && fields.Contains(fieldIndex);
        }

        private void MarkFieldInvalid(int listIndex, int rowIndex, int fieldIndex)
        {
            Dictionary<int, HashSet<int>> rows;
            if (!invalidFieldsByList.TryGetValue(listIndex, out rows))
            {
                rows = new Dictionary<int, HashSet<int>>();
                invalidFieldsByList[listIndex] = rows;
            }
            HashSet<int> fields;
            if (!rows.TryGetValue(rowIndex, out fields))
            {
                fields = new HashSet<int>();
                rows[rowIndex] = fields;
            }
            fields.Add(fieldIndex);
        }

        private void ClearFieldInvalid(int listIndex, int rowIndex, int fieldIndex)
        {
            Dictionary<int, HashSet<int>> rows;
            HashSet<int> fields;
            if (invalidFieldsByList.TryGetValue(listIndex, out rows) &&
                rows.TryGetValue(rowIndex, out fields))
            {
                fields.Remove(fieldIndex);
                if (fields.Count == 0)
                {
                    rows.Remove(rowIndex);
                }
                if (rows.Count == 0)
                {
                    invalidFieldsByList.Remove(listIndex);
                }
            }
        }

        private bool IsFieldInvalid(int listIndex, int rowIndex, int fieldIndex)
        {
            Dictionary<int, HashSet<int>> rows;
            HashSet<int> fields;
            return invalidFieldsByList.TryGetValue(listIndex, out rows)
                && rows.TryGetValue(rowIndex, out fields)
                && fields.Contains(fieldIndex);
        }

        private string ComposeListDisplayName(int listIndex, int entryIndex, int nameFieldIndex)
        {
            string name = GetDisplayEntryName(listIndex, entryIndex, nameFieldIndex);
            if (IsRowDirty(listIndex, entryIndex))
            {
                return "* " + name;
            }
            return name;
        }

        private void BeginSaveProgress()
        {
            if (cpb2 == null)
            {
                return;
            }
            try
            {
                cpb2.Minimum = 0;
                cpb2.Maximum = 100;
                cpb2.Value = 0;
                cpb2.Refresh();
                Application.DoEvents();
            }
            catch
            {
            }
        }

        private void SetSaveProgress(int value)
        {
            if (cpb2 == null)
            {
                return;
            }
            try
            {
                int v = Math.Max(cpb2.Minimum, Math.Min(cpb2.Maximum, value));
                cpb2.Value = v;
                cpb2.Refresh();
                Application.DoEvents();
            }
            catch
            {
            }
        }

        private void EndSaveProgress()
        {
            if (cpb2 == null)
            {
                return;
            }
            try
            {
                cpb2.Value = 0;
                cpb2.Refresh();
                Application.DoEvents();
            }
            catch
            {
            }
        }

        private void ShowSaveConfirmation(string details)
        {
            string message = "Salvamento concluído.";
            if (!string.IsNullOrWhiteSpace(details))
            {
                message += "\n" + details.Trim();
            }
            MessageBox.Show(message, "FWEledit", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private struct NavigationSnapshot
        {
            public int ListIndex;
            public int ItemId;
            public int GridRowIndex;
            public int FirstDisplayedRow;
        }

        private NavigationSnapshot CaptureNavigationSnapshot()
        {
            NavigationSnapshot snapshot = new NavigationSnapshot
            {
                ListIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1,
                ItemId = -1,
                GridRowIndex = -1,
                FirstDisplayedRow = -1
            };

            if (dataGridView_elems != null)
            {
                if (dataGridView_elems.CurrentCell != null)
                {
                    snapshot.GridRowIndex = dataGridView_elems.CurrentCell.RowIndex;
                    if (snapshot.GridRowIndex >= 0 && snapshot.GridRowIndex < dataGridView_elems.Rows.Count)
                    {
                        int id;
                        if (int.TryParse(Convert.ToString(dataGridView_elems.Rows[snapshot.GridRowIndex].Cells[0].Value), out id))
                        {
                            snapshot.ItemId = id;
                        }
                    }
                }
                if (dataGridView_elems.Rows.Count > 0)
                {
                    snapshot.FirstDisplayedRow = dataGridView_elems.FirstDisplayedScrollingRowIndex;
                }
            }

            return snapshot;
        }

        private void RestoreNavigationSnapshot(NavigationSnapshot snapshot)
        {
            if (eLC == null || comboBox_lists == null || dataGridView_elems == null)
            {
                return;
            }
            if (snapshot.ListIndex < 0 || snapshot.ListIndex >= comboBox_lists.Items.Count)
            {
                return;
            }

            bool previousRestore = isRestoringSessionState;
            isRestoringSessionState = true;
            try
            {
                comboBox_lists.SelectedIndex = snapshot.ListIndex;

                int targetRow = -1;
                if (snapshot.ItemId > 0)
                {
                    for (int row = 0; row < dataGridView_elems.Rows.Count; row++)
                    {
                        int rowId;
                        if (int.TryParse(Convert.ToString(dataGridView_elems.Rows[row].Cells[0].Value), out rowId) && rowId == snapshot.ItemId)
                        {
                            targetRow = row;
                            break;
                        }
                    }
                }

                if (targetRow < 0 && snapshot.GridRowIndex >= 0 && snapshot.GridRowIndex < dataGridView_elems.Rows.Count)
                {
                    targetRow = snapshot.GridRowIndex;
                }

                if (targetRow >= 0 && targetRow < dataGridView_elems.Rows.Count)
                {
                    dataGridView_elems.ClearSelection();
                    dataGridView_elems.CurrentCell = dataGridView_elems[0, targetRow];
                    dataGridView_elems.Rows[targetRow].Selected = true;
                    try
                    {
                        if (snapshot.FirstDisplayedRow >= 0 && snapshot.FirstDisplayedRow < dataGridView_elems.Rows.Count)
                        {
                            dataGridView_elems.FirstDisplayedScrollingRowIndex = snapshot.FirstDisplayedRow;
                        }
                        else
                        {
                            dataGridView_elems.FirstDisplayedScrollingRowIndex = targetRow;
                        }
                    }
                    catch
                    {
                    }
                }
            }
            finally
            {
                isRestoringSessionState = previousRestore;
            }
        }

        private int GetItemQualityFieldIndex(int listIndex)
        {
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return -1;
            }

            for (int i = 0; i < eLC.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(eLC.Lists[listIndex].elementFields[i], "item_quality", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private Color? GetItemQualityColor(int quality)
        {
            Color color;
            if (ItemQualityColors.TryGetValue(quality, out color))
            {
                return color;
            }
            return null;
        }

        private int GetFieldIndexForValueRow(int rowIndex)
        {
            if (dataGridView_item == null || rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return -1;
            }
            object tag = dataGridView_item.Rows[rowIndex].Tag;
            if (tag is int)
            {
                return (int)tag;
            }
            if (tag != null)
            {
                int parsed;
                if (int.TryParse(tag.ToString(), out parsed))
                {
                    return parsed;
                }
            }
            return rowIndex;
        }

        private int FindValueRowByFieldIndex(int fieldIndex)
        {
            if (dataGridView_item == null || fieldIndex < 0)
            {
                return -1;
            }
            for (int row = 0; row < dataGridView_item.Rows.Count; row++)
            {
                if (GetFieldIndexForValueRow(row) == fieldIndex)
                {
                    return row;
                }
            }
            return -1;
        }

        private bool IsEquipmentEssenceList(int listIndex)
        {
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return false;
            }
            string listName = eLC.Lists[listIndex].listName ?? string.Empty;
            return string.Equals(listName, "EQUIPMENT_ESSENCE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "Equipment", StringComparison.OrdinalIgnoreCase);
        }

        private EquipmentValuesTab GetSelectedEquipmentTab()
        {
            if (fwEquipmentTabs == null || !fwEquipmentTabs.Visible)
            {
                return EquipmentValuesTab.All;
            }
            switch (fwEquipmentTabs.SelectedIndex)
            {
                case 1:
                    return EquipmentValuesTab.Refine;
                case 2:
                    return EquipmentValuesTab.Models;
                case 3:
                    return EquipmentValuesTab.Other;
                default:
                    return EquipmentValuesTab.Main;
            }
        }

        private bool IsEquipmentModelsField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            string name = fieldName.Trim();
            return name.StartsWith("models_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("model_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("file_model", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_change_model", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsEquipmentRefineField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            string name = fieldName.Trim();
            return name.StartsWith("refine_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("level_stone_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("id_estone_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("enhanced_prop_package_", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_sign_addon_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "refine_max_level", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_identify", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "basic_show_level", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "can_sign", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsEquipmentOtherField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            string name = fieldName.Trim();
            return name.StartsWith("color_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("decompose_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("max_recast_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("min_recast_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("extend_identify_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("auction_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("fashion_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("gfx_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("id_full_pvp_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("id_cancel_full_pvp_", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "can_decompose", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "can_auction", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "auction_fee", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "show_gfx_need_gem_value", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_special_addon_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "equip_transform_cfg_id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "fashion_dye_cfg_id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "fashion_adorn_hook_name", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "unknown_608_1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "pile_num_max", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "proc_type", StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldIncludeFieldInValuesTab(int listIndex, string fieldName)
        {
            if (!IsEquipmentEssenceList(listIndex))
            {
                return true;
            }
            EquipmentValuesTab tab = GetSelectedEquipmentTab();
            if (tab == EquipmentValuesTab.All)
            {
                return true;
            }
            bool isModels = IsEquipmentModelsField(fieldName);
            bool isRefine = IsEquipmentRefineField(fieldName);
            bool isOther = IsEquipmentOtherField(fieldName);

            switch (tab)
            {
                case EquipmentValuesTab.Models:
                    return isModels;
                case EquipmentValuesTab.Refine:
                    return isRefine;
                case EquipmentValuesTab.Other:
                    return isOther;
                case EquipmentValuesTab.Main:
                default:
                    return !isModels && !isRefine && !isOther;
            }
        }

        private void EnsureEquipmentTabForField(int listIndex, string fieldName)
        {
            if (!IsEquipmentEssenceList(listIndex) || fwEquipmentTabs == null || !fwEquipmentTabs.Visible)
            {
                return;
            }

            int targetIndex = 0;
            if (IsEquipmentRefineField(fieldName))
            {
                targetIndex = 1;
            }
            else if (IsEquipmentModelsField(fieldName))
            {
                targetIndex = 2;
            }
            else if (IsEquipmentOtherField(fieldName))
            {
                targetIndex = 3;
            }

            if (fwEquipmentTabs.SelectedIndex != targetIndex)
            {
                fwEquipmentTabs.SelectedIndex = targetIndex;
            }
        }

        private void UpdateEquipmentTabsVisibility(int listIndex)
        {
            if (fwEquipmentTabs == null)
            {
                return;
            }
            bool showTabs = IsEquipmentEssenceList(listIndex);
            fwEquipmentTabs.Visible = showTabs;
            if (showTabs && fwEquipmentTabs.SelectedIndex < 0)
            {
                fwEquipmentTabs.SelectedIndex = 0;
            }
            if (showTabs)
            {
                fwEquipmentTabs.BringToFront();
            }
        }

        private Color DarkenColor(Color color, float factor)
        {
            float f = Math.Max(0.0f, Math.Min(1.0f, factor));
            int r = Math.Max(0, Math.Min(255, (int)(color.R * f)));
            int g = Math.Max(0, Math.Min(255, (int)(color.G * f)));
            int b = Math.Max(0, Math.Min(255, (int)(color.B * f)));
            return Color.FromArgb(r, g, b);
        }

        private void ApplyItemQualityColorToRow(int listIndex, int entryIndex, DataGridViewRow row)
        {
            if (row == null || row.Cells == null || row.Cells.Count < 3)
            {
                return;
            }

            int qualityFieldIndex = GetItemQualityFieldIndex(listIndex);
            if (qualityFieldIndex < 0 || eLC == null)
            {
                row.Cells[2].Style.ForeColor = dataGridView_elems.DefaultCellStyle.ForeColor;
                row.Cells[2].Style.SelectionForeColor = dataGridView_elems.DefaultCellStyle.SelectionForeColor;
                row.Cells[2].Style.SelectionBackColor = dataGridView_elems.DefaultCellStyle.SelectionBackColor;
                row.Cells[0].Style.SelectionBackColor = dataGridView_elems.DefaultCellStyle.SelectionBackColor;
                row.Cells[1].Style.SelectionBackColor = dataGridView_elems.DefaultCellStyle.SelectionBackColor;
                return;
            }

            string raw = eLC.GetValue(listIndex, entryIndex, qualityFieldIndex);
            int quality;
            if (!int.TryParse(raw, out quality))
            {
                row.Cells[2].Style.ForeColor = dataGridView_elems.DefaultCellStyle.ForeColor;
                row.Cells[2].Style.SelectionForeColor = dataGridView_elems.DefaultCellStyle.SelectionForeColor;
                row.Cells[2].Style.SelectionBackColor = dataGridView_elems.DefaultCellStyle.SelectionBackColor;
                row.Cells[0].Style.SelectionBackColor = dataGridView_elems.DefaultCellStyle.SelectionBackColor;
                row.Cells[1].Style.SelectionBackColor = dataGridView_elems.DefaultCellStyle.SelectionBackColor;
                return;
            }

            Color? color = GetItemQualityColor(quality);
            if (color.HasValue)
            {
                Color baseColor = color.Value;
                Color hover = DarkenColor(baseColor, 0.25f);
                row.Cells[2].Style.ForeColor = baseColor;
                row.Cells[2].Style.SelectionForeColor = baseColor;
                row.Cells[2].Style.SelectionBackColor = hover;
                row.Cells[0].Style.SelectionBackColor = hover;
                row.Cells[1].Style.SelectionBackColor = hover;
            }
            else
            {
                row.Cells[2].Style.ForeColor = dataGridView_elems.DefaultCellStyle.ForeColor;
                row.Cells[2].Style.SelectionForeColor = dataGridView_elems.DefaultCellStyle.SelectionForeColor;
                row.Cells[2].Style.SelectionBackColor = dataGridView_elems.DefaultCellStyle.SelectionBackColor;
                row.Cells[0].Style.SelectionBackColor = dataGridView_elems.DefaultCellStyle.SelectionBackColor;
                row.Cells[1].Style.SelectionBackColor = dataGridView_elems.DefaultCellStyle.SelectionBackColor;
            }
        }

        private void ClearDirtyTrackingAfterSave()
        {
            dirtyRowsByList.Clear();
            dirtyFieldsByList.Clear();
            invalidFieldsByList.Clear();
            hasPendingDescriptionChanges = false;
            hasUnsavedChanges = false;
            if (comboBox_lists.SelectedIndex > -1)
            {
                ClearListDisplayCache();
                change_list(null, null);
            }
        }

        private List<object[]> BuildListDisplayRowsForList(int listIndex)
        {
            List<object[]> rows = new List<object[]>();
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return rows;
            }

            if (listIndex == eLC.ConversationListIndex)
            {
                if (conversationList != null)
                {
                    for (int e = 0; e < conversationList.talk_proc_count; e++)
                    {
                        rows.Add(new object[] { conversationList.talk_procs[e].id_talk, Properties.Resources.blank, conversationList.talk_procs[e].id_talk + " - Dialog" });
                    }
                }
                else
                {
                    rows.Add(new object[] { 0, Properties.Resources.blank, "Conversation parser unavailable for this data format" });
                }
                return rows;
            }

            int pos = -1;
            int pos2 = -1;
            for (int i = 0; i < eLC.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(eLC.Lists[listIndex].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
                {
                    pos = i;
                }
                if (string.Equals(eLC.Lists[listIndex].elementFields[i], "file_icon", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(eLC.Lists[listIndex].elementFields[i], "file_icon1", StringComparison.OrdinalIgnoreCase))
                {
                    pos2 = i;
                }
                if (pos != -1 && pos2 != -1)
                {
                    break;
                }
            }
            if (pos < 0)
            {
                pos = 0;
            }

            for (int e = 0; e < eLC.Lists[listIndex].elementValues.Length; e++)
            {
                if (string.Equals(eLC.Lists[listIndex].elementFields[0], "ID", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(eLC.Lists[listIndex].elementFields[0], "id", StringComparison.OrdinalIgnoreCase))
                {
                    Bitmap img = Properties.Resources.NoIcon;
                    if (pos2 > -1)
                    {
                        string path = ResolveIconKeyForList(listIndex, eLC.GetValue(listIndex, e, pos2));
                        if (database != null && database.sourceBitmap != null && database.ContainsKey(path))
                        {
                            img = database.images(path);
                        }
                    }
                    rows.Add(new object[] { eLC.GetValue(listIndex, e, 0), img, ComposeListDisplayName(listIndex, e, pos) });
                }
                else
                {
                    rows.Add(new object[] { 0, Properties.Resources.NoIcon, ComposeListDisplayName(listIndex, e, pos) });
                }
            }

            return rows;
        }

        private void WarmupAllListDisplayRows()
        {
            if (eLC == null)
            {
                return;
            }

            ClearListDisplayCache();
            for (int l = 0; l < eLC.Lists.Length; l++)
            {
                try
                {
                    listDisplayRowsCache[l] = BuildListDisplayRowsForList(l);
                }
                catch
                {
                    listDisplayRowsCache[l] = new List<object[]>();
                }
            }
        }

        private void PersistNavigationState()
        {
            try
            {
                if (isRestoringSessionState)
                {
                    return;
                }
                if (comboBox_lists.SelectedIndex > -1)
                {
                    Properties.Settings.Default.LastListIndex = comboBox_lists.SelectedIndex;
                }
                if (dataGridView_elems.CurrentCell != null && dataGridView_elems.CurrentCell.RowIndex > -1)
                {
                    int id;
                    if (int.TryParse(Convert.ToString(dataGridView_elems.Rows[dataGridView_elems.CurrentCell.RowIndex].Cells[0].Value), out id))
                    {
                        Properties.Settings.Default.LastItemId = id;
                    }
                }
                hasPendingNavigationStateWrite = true;
                if (navigationPersistTimer != null)
                {
                    navigationPersistTimer.Stop();
                    navigationPersistTimer.Start();
                }
            }
            catch
            { }
        }

        private void FlushNavigationStateToDisk()
        {
            if (!hasPendingNavigationStateWrite)
            {
                return;
            }
            try
            {
                Properties.Settings.Default.Save();
                hasPendingNavigationStateWrite = false;
            }
            catch
            { }
        }

        private bool SaveCurrentSessionNoDialog()
        {
            try
            {
                if (eLC == null)
                {
                    return true;
                }

                NavigationSnapshot navSnapshot = CaptureNavigationSnapshot();
                BeginSaveProgress();
                SetSaveProgress(5);

                if (ElementsPath == "" || !File.Exists(ElementsPath))
                {
                    using (SaveFileDialog eSave = new SaveFileDialog())
                    {
                        eSave.InitialDirectory = Environment.CurrentDirectory;
                        eSave.Filter = "Elements File (*.data)|*.data|All Files (*.*)|*.*";
                        if (eSave.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(eSave.FileName))
                        {
                            return false;
                        }
                        ElementsPath = eSave.FileName;
                    }
                }

                Cursor = Cursors.AppStarting;
                if (!ValidateUniqueIdsBeforeSave())
                {
                    Cursor = Cursors.Default;
                    return false;
                }
                if (!FlushPendingDescriptionsToDisk())
                {
                    Cursor = Cursors.Default;
                    EndSaveProgress();
                    return false;
                }
                if (eLC.ConversationListIndex > -1 && eLC.Lists.Length > eLC.ConversationListIndex && conversationList != null)
                {
                    eLC.Lists[eLC.ConversationListIndex].elementValues[0][0] = conversationList.GetBytes();
                }
                SetSaveProgress(25);
                if (!SaveElementsFileSafely(ElementsPath))
                {
                    Cursor = Cursors.Default;
                    EndSaveProgress();
                    return false;
                }
                if (asm != null)
                {
                    SetSaveProgress(70);
                    string syncSummary;
                    if (!asm.ApplyWorkspaceChangesToGame(out syncSummary))
                    {
                        MessageBox.Show("Saved elements.data, but resource sync failed:\n" + syncSummary);
                    }
                }
                ClearDirtyTrackingAfterSave();
                SetSaveProgress(100);
                RestoreNavigationSnapshot(navSnapshot);
                Cursor = Cursors.Default;
                EndSaveProgress();
                return true;
            }
            catch (Exception ex)
            {
                LogError("SaveCurrentSessionNoDialog", ex);
                Cursor = Cursors.Default;
                EndSaveProgress();
                return false;
            }
        }

        private void RemapDescriptionIdIfNeeded(int oldId, int newId)
        {
            if (oldId <= 0 || newId <= 0 || oldId == newId)
            {
                return;
            }
            string existing;
            if (!itemExtDescMap.TryGetValue(oldId, out existing))
            {
                return;
            }

            // Avoid overwriting an existing explicit description for the target ID.
            if (itemExtDescMap.ContainsKey(newId))
            {
                return;
            }

            itemExtDescMap.Remove(oldId);
            itemExtDescMap[newId] = existing;

            int oldIndex = itemExtDescOrder.IndexOf(oldId);
            if (oldIndex >= 0)
            {
                itemExtDescOrder[oldIndex] = newId;
            }
            else if (!itemExtDescOrder.Contains(newId))
            {
                itemExtDescOrder.Add(newId);
            }

            hasPendingDescriptionChanges = true;
            hasUnsavedChanges = true;
            RebuildRuntimeItemExtDescArray();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (suppressClosePrompt)
            {
                return;
            }

            PersistNavigationState();
            FlushNavigationStateToDisk();
            if (!hasUnsavedChanges && !hasPendingDescriptionChanges)
            {
                return;
            }

            DialogResult dr = MessageBox.Show(
                "There are pending changes. Save before closing?",
                "FWEledit",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (dr == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            if (dr == DialogResult.No)
            {
                return;
            }

            if (!SaveCurrentSessionNoDialog())
            {
                e.Cancel = true;
            }
        }

        private string ResolveIconKey(string rawValue)
        {
            if (database == null || string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            string value = rawValue.Trim();
            int index;
            if (int.TryParse(value, out index))
            {
                if (database.imagesx != null && database.imagesx.ContainsKey(index))
                {
                    return database.imagesx[index];
                }
                if (database.imagesById != null && database.imagesById.ContainsKey(index))
                {
                    return database.imagesById[index];
                }
                return string.Empty;
            }

            string key = Path.GetFileName(value);
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (database.ContainsKey(key))
            {
                return key;
            }

            if (!Path.HasExtension(key))
            {
                string dds = key + ".dds";
                if (database.ContainsKey(dds))
                {
                    return dds;
                }
                string png = key + ".png";
                if (database.ContainsKey(png))
                {
                    return png;
                }
            }

            return key;
        }

        private static object[] CloneElementValuesDeep(object[] source)
        {
            object[] clone = new object[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                byte[] bytes = source[i] as byte[];
                if (bytes != null)
                {
                    byte[] copy = new byte[bytes.Length];
                    Array.Copy(bytes, copy, bytes.Length);
                    clone[i] = copy;
                }
                else
                {
                    clone[i] = source[i];
                }
            }
            return clone;
        }

        private int GetIdFieldIndex(int listIndex)
        {
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return -1;
            }

            for (int i = 0; i < eLC.Lists[listIndex].elementFields.Length; i++)
            {
                string field = eLC.Lists[listIndex].elementFields[i];
                if (string.Equals(field, "id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(field, "ID", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private HashSet<int> BuildUsedIds(int listIndex, int idFieldIndex)
        {
            HashSet<int> used = new HashSet<int>();
            if (idFieldIndex < 0 || eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return used;
            }

            for (int i = 0; i < eLC.Lists[listIndex].elementValues.Length; i++)
            {
                int id;
                if (int.TryParse(eLC.GetValue(listIndex, i, idFieldIndex), out id))
                {
                    used.Add(id);
                }
            }
            return used;
        }

        private int GetNextUniqueId(HashSet<int> used, int startCandidate)
        {
            int candidate = Math.Max(1, startCandidate);
            while (used.Contains(candidate))
            {
                candidate++;
            }
            used.Add(candidate);
            return candidate;
        }

        private string ResolveIconKeyFromPathId(int pathId)
        {
            if (database == null || database.pathById == null || database.pathById.Count == 0)
            {
                return string.Empty;
            }

            // FW clients frequently store file_icon one index behind path.data icon entries.
            int[] candidates = new int[] { pathId + 1, pathId, pathId - 1 };
            for (int i = 0; i < candidates.Length; i++)
            {
                int candidate = candidates[i];
                if (candidate < 0 || !database.pathById.ContainsKey(candidate))
                {
                    continue;
                }

                string mapped = database.pathById[candidate];
                if (string.IsNullOrWhiteSpace(mapped))
                {
                    continue;
                }

                string key = ResolveIconKey(mapped);
                if (!string.IsNullOrWhiteSpace(key) && database.ContainsKey(key))
                {
                    return key;
                }

                string baseName = Path.GetFileName(mapped);
                if (!string.IsNullOrWhiteSpace(baseName) && !Path.HasExtension(baseName))
                {
                    string dds = baseName + ".dds";
                    if (database.ContainsKey(dds))
                    {
                        return dds;
                    }
                }
            }

            return string.Empty;
        }

        private string GetIconKeyByIndex(int index)
        {
            if (database == null || index < 0)
            {
                return string.Empty;
            }
            if (database.imagesById != null && database.imagesById.ContainsKey(index))
            {
                return database.imagesById[index];
            }
            if (database.imagesx != null && database.imagesx.ContainsKey(index))
            {
                return database.imagesx[index];
            }
            return string.Empty;
        }

        private string ResolveIconKeyForList(int listIndex, string rawValue)
        {
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return ResolveIconKey(rawValue);
            }

            int iconId;
            if (!int.TryParse((rawValue ?? string.Empty).Trim(), out iconId))
            {
                return ResolveIconKey(rawValue);
            }

            // FW: file_icon stores a PathID that is resolved by data\\path.data.
            string mappedByPathData = ResolveIconKeyFromPathId(iconId);
            if (!string.IsNullOrWhiteSpace(mappedByPathData))
            {
                return mappedByPathData;
            }

            // Fallback when the numeric value is already an atlas index.
            string directIndexKey = GetIconKeyByIndex(iconId);
            if (!string.IsNullOrWhiteSpace(directIndexKey))
            {
                return directIndexKey;
            }

            return ResolveIconKey(rawValue);
        }

        private bool IsIconFieldName(string fieldName)
        {
            return string.Equals(fieldName, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "file_icon1", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsItemQualityFieldName(string fieldName)
        {
            return string.Equals(fieldName, "item_quality", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsModelFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return normalized.StartsWith("file_model", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("file_models", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("model_name", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsAddonTypeField(int listIndex, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            if (!string.Equals(fieldName.Trim(), "type", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return false;
            }
            string listName = eLC.Lists[listIndex].listName ?? string.Empty;
            if (string.Equals(listName, "EQUIPMENT_ADDON", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return listIndex == 0;
        }

        private bool IsModelUsageFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            if (IsModelFieldName(fieldName))
            {
                return true;
            }
            return fieldName.Trim().StartsWith("gfx_file", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsShiftedModelFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            string normalized = fieldName.Trim();
            return normalized.StartsWith("file_models_", StringComparison.OrdinalIgnoreCase);
        }

        private bool TryExtractPathId(string rawValue, out int pathId)
        {
            pathId = 0;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            string trimmed = rawValue.Trim();
            int pipeIndex = trimmed.IndexOf('|');
            if (pipeIndex > 0)
            {
                trimmed = trimmed.Substring(0, pipeIndex).Trim();
            }

            return int.TryParse(trimmed, out pathId);
        }

        private bool TryResolvePathById(int pathId, out string mappedPath, bool allowNeighborOffsets)
        {
            mappedPath = string.Empty;
            if (database == null || database.pathById == null || database.pathById.Count == 0 || pathId <= 0)
            {
                return false;
            }

            List<int> candidates = new List<int> { pathId };
            if (allowNeighborOffsets)
            {
                candidates.Add(pathId + 1);
                candidates.Add(pathId - 1);
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                int candidate = candidates[i];
                string path;
                if (!database.pathById.TryGetValue(candidate, out path) || string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                mappedPath = path.Replace('/', '\\');
                return true;
            }

            return false;
        }

        private bool TryResolveModelPathById(int pathId, out int resolvedPathId, out string mappedPath, bool allowNeighborOffsets)
        {
            return TryResolveModelPathById(pathId, null, out resolvedPathId, out mappedPath, allowNeighborOffsets);
        }

        private bool TryResolveModelPathById(int pathId, string fieldName, out int resolvedPathId, out string mappedPath, bool allowNeighborOffsets)
        {
            resolvedPathId = 0;
            mappedPath = string.Empty;
            if (database == null || database.pathById == null || database.pathById.Count == 0 || pathId <= 0)
            {
                return false;
            }

            List<int> candidates = new List<int>();
            if (IsShiftedModelFieldName(fieldName))
            {
                // Mount-like fields (file_models_1..N) are commonly stored one ID behind.
                candidates.Add(pathId + 1);
                candidates.Add(pathId);
                candidates.Add(pathId - 1);
            }
            else
            {
                candidates.Add(pathId);
                candidates.Add(pathId + 1);
                candidates.Add(pathId - 1);
            }
            if (allowNeighborOffsets)
            {
                // keep unique ordering
                if (!candidates.Contains(pathId + 1)) { candidates.Add(pathId + 1); }
                if (!candidates.Contains(pathId - 1)) { candidates.Add(pathId - 1); }
            }
            else
            {
                // when neighbor fallback is off, only keep first candidate
                while (candidates.Count > 1)
                {
                    candidates.RemoveAt(candidates.Count - 1);
                }
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                int candidate = candidates[i];
                string path;
                if (!database.pathById.TryGetValue(candidate, out path) || string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                string normalized = path.Replace('/', '\\');
                if (!LooksLikeModelMappedPath(normalized))
                {
                    continue;
                }

                resolvedPathId = candidate;
                mappedPath = normalized;
                return true;
            }

            return false;
        }

        private string GuessModelPackageFromMappedPath(string mappedPath)
        {
            if (string.IsNullOrWhiteSpace(mappedPath))
            {
                return "models";
            }

            string p = mappedPath.Replace('/', '\\').TrimStart('\\').ToLowerInvariant();
            int sep = p.IndexOf('\\');
            string head = sep > 0 ? p.Substring(0, sep) : p;
            string[] knownPackages = new string[]
            {
                "building","configs","gfx","grasses","interfaces","litmodels","loddata",
                "models","script","sfx","shaders","surfaces","textures","moxing"
            };
            for (int i = 0; i < knownPackages.Length; i++)
            {
                if (string.Equals(head, knownPackages[i], StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(knownPackages[i], "moxing", StringComparison.OrdinalIgnoreCase))
                    {
                        return "models";
                    }
                    return knownPackages[i];
                }
            }

            if (p.StartsWith("litmodels\\"))
            {
                return "litmodels";
            }
            if (p.StartsWith("moxing\\"))
            {
                return "moxing";
            }
            if (p.StartsWith("surfaces\\"))
            {
                return "surfaces";
            }
            if (p.StartsWith("configs\\") || p.StartsWith("data\\"))
            {
                return "configs";
            }

            if (p.StartsWith("models\\")
                || p.StartsWith("npcs\\")
                || p.StartsWith("players\\")
                || p.StartsWith("monsters\\")
                || p.StartsWith("creatures\\"))
            {
                return "models";
            }

            return "models";
        }

        private string GuessModelPackageFromExtractedSource(int pathId, string mappedPath)
        {
            string signature = (AssetManager.WorkspaceRootPath ?? string.Empty) + "|" + (AssetManager.GameRootPath ?? string.Empty);
            if (!string.Equals(signature, modelPackageCacheSignature, StringComparison.OrdinalIgnoreCase))
            {
                modelPackageByPathIdCache.Clear();
                modelPackageCacheSignature = signature;
            }

            if (pathId > 0)
            {
                string cached;
                if (modelPackageByPathIdCache.TryGetValue(pathId, out cached) && !string.IsNullOrWhiteSpace(cached))
                {
                    return cached;
                }
            }

            string normalized = (mappedPath ?? string.Empty).Replace('/', '\\').TrimStart('\\');
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return GuessModelPackageFromMappedPath(mappedPath);
            }

            string[] packages = new string[]
            {
                "models",
                "gfx",
                "grasses",
                "building",
                "configs",
                "interfaces",
                "litmodels",
                "loddata",
                "script",
                "sfx",
                "shaders",
                "surfaces",
                "textures"
            };

            string[] roots = new string[]
            {
                AssetManager.WorkspaceRootPath,
                AssetManager.GameRootPath
            };

            for (int r = 0; r < roots.Length; r++)
            {
                string root = roots[r];
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                {
                    continue;
                }

                string resourcesRoot = Path.Combine(root, "resources");
                if (!Directory.Exists(resourcesRoot))
                {
                    continue;
                }

                for (int i = 0; i < packages.Length; i++)
                {
                    string pkg = packages[i];
                    try
                    {
                        string candidate = Path.Combine(resourcesRoot, pkg + ".pck.files", normalized);
                        if (File.Exists(candidate))
                        {
                            if (pathId > 0)
                            {
                                modelPackageByPathIdCache[pathId] = pkg;
                            }
                            return pkg;
                        }
                    }
                    catch
                    { }
                }
            }

            string fallback = GuessModelPackageFromMappedPath(mappedPath);
            if (pathId > 0)
            {
                modelPackageByPathIdCache[pathId] = fallback;
            }
            return fallback;
        }

        private bool LooksLikeModelMappedPath(string mappedPath)
        {
            if (string.IsNullOrWhiteSpace(mappedPath))
            {
                return false;
            }

            string p = mappedPath.Replace('/', '\\').TrimStart('\\').ToLowerInvariant();
            if (p.EndsWith(".ecm"))
            {
                return true;
            }
            if (p.StartsWith("models\\")
                || p.StartsWith("npcs\\")
                || p.StartsWith("players\\")
                || p.StartsWith("monsters\\")
                || p.StartsWith("creatures\\")
                || p.StartsWith("litmodels\\")
                || p.StartsWith("moxing\\"))
            {
                return true;
            }
            return false;
        }

        private int[] GetModelFieldIndices(int listIndex)
        {
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return new int[0];
            }

            List<int> indices = new List<int>();
            for (int i = 0; i < eLC.Lists[listIndex].elementFields.Length; i++)
            {
                if (IsModelFieldName(eLC.Lists[listIndex].elementFields[i]))
                {
                    indices.Add(i);
                }
            }
            return indices.ToArray();
        }

        private int[] GetModelUsageFieldIndices(int listIndex)
        {
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return new int[0];
            }

            List<int> indices = new List<int>();
            for (int i = 0; i < eLC.Lists[listIndex].elementFields.Length; i++)
            {
                if (IsModelUsageFieldName(eLC.Lists[listIndex].elementFields[i]))
                {
                    indices.Add(i);
                }
            }
            return indices.ToArray();
        }

        private int ResolveElementIndexFromGridRow(int listIndex, int gridRowIndex)
        {
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return -1;
            }
            if (gridRowIndex < 0 || gridRowIndex >= dataGridView_elems.Rows.Count)
            {
                return -1;
            }
            if (listIndex == eLC.ConversationListIndex)
            {
                return gridRowIndex;
            }

            if (eLC.Lists[listIndex].elementFields == null || eLC.Lists[listIndex].elementFields.Length == 0)
            {
                return gridRowIndex;
            }

            string firstField = eLC.Lists[listIndex].elementFields[0];
            bool hasIdField = string.Equals(firstField, "id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(firstField, "ID", StringComparison.OrdinalIgnoreCase);
            if (!hasIdField)
            {
                return gridRowIndex;
            }

            int selectedId;
            if (!int.TryParse(Convert.ToString(dataGridView_elems.Rows[gridRowIndex].Cells[0].Value), out selectedId))
            {
                return gridRowIndex;
            }

            for (int i = 0; i < eLC.Lists[listIndex].elementValues.Length; i++)
            {
                int itemId;
                if (!int.TryParse(eLC.GetValue(listIndex, i, 0), out itemId))
                {
                    continue;
                }
                if (itemId == selectedId)
                {
                    return i;
                }
            }

            return gridRowIndex;
        }

        private int GetActiveGridRowIndex()
        {
            if (dataGridView_elems == null || dataGridView_elems.Rows.Count == 0)
            {
                return -1;
            }

            int[] selected = gridSelectedIndices(dataGridView_elems);
            if (selected != null && selected.Length > 0)
            {
                if (dataGridView_elems.CurrentCell != null)
                {
                    int current = dataGridView_elems.CurrentCell.RowIndex;
                    for (int i = 0; i < selected.Length; i++)
                    {
                        if (selected[i] == current)
                        {
                            return current;
                        }
                    }
                }
                return selected[0];
            }

            if (dataGridView_elems.CurrentCell != null)
            {
                return dataGridView_elems.CurrentCell.RowIndex;
            }

            return 0;
        }

        private List<ModelPickerEntry> BuildModelPickerEntriesForList(int listIndex)
        {
            List<ModelPickerEntry> entries = new List<ModelPickerEntry>();
            if (database == null || database.pathById == null || database.pathById.Count == 0)
            {
                return entries;
            }

            int[] modelFields = GetModelUsageFieldIndices(listIndex);
            int idFieldIndex = GetIdFieldIndex(listIndex);
            int nameFieldIndex = -1;
            int iconFieldIndex = -1;
            if (eLC != null && listIndex >= 0 && listIndex < eLC.Lists.Length)
            {
                for (int i = 0; i < eLC.Lists[listIndex].elementFields.Length; i++)
                {
                    if (string.Equals(eLC.Lists[listIndex].elementFields[i], "name", StringComparison.OrdinalIgnoreCase))
                    {
                        nameFieldIndex = i;
                    }
                    if (iconFieldIndex < 0 && IsIconFieldName(eLC.Lists[listIndex].elementFields[i]))
                    {
                        iconFieldIndex = i;
                    }
                    if (nameFieldIndex >= 0 && iconFieldIndex >= 0)
                    {
                        break;
                    }
                }
            }

            Dictionary<int, int> usesByPathId = new Dictionary<int, int>();
            Dictionary<int, int> sampleItemIdByPathId = new Dictionary<int, int>();
            Dictionary<int, string> sampleItemNameByPathId = new Dictionary<int, string>();
            Dictionary<int, string> sampleIconKeyByPathId = new Dictionary<int, string>();
            Dictionary<string, Bitmap> iconCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

            if (modelFields.Length > 0 && eLC != null && listIndex >= 0 && listIndex < eLC.Lists.Length)
            {
                for (int row = 0; row < eLC.Lists[listIndex].elementValues.Length; row++)
                {
                    int itemId = 0;
                    if (idFieldIndex >= 0)
                    {
                        int.TryParse(eLC.GetValue(listIndex, row, idFieldIndex), out itemId);
                    }
                    string itemName = nameFieldIndex >= 0 ? eLC.GetValue(listIndex, row, nameFieldIndex) : string.Empty;

                    for (int mf = 0; mf < modelFields.Length; mf++)
                    {
                        int rawPathId;
                        if (!int.TryParse(eLC.GetValue(listIndex, row, modelFields[mf]), out rawPathId) || rawPathId <= 0)
                        {
                            continue;
                        }

                        int pathId = rawPathId;
                        int resolvedPathId;
                        string resolvedMappedPath;
                        string modelFieldName = eLC.Lists[listIndex].elementFields[modelFields[mf]];
                        bool allowNeighborOffsets = IsModelFieldName(modelFieldName);
                        if (TryResolveModelPathById(rawPathId, modelFieldName, out resolvedPathId, out resolvedMappedPath, allowNeighborOffsets) && resolvedPathId > 0)
                        {
                            pathId = resolvedPathId;
                        }

                        int c;
                        usesByPathId.TryGetValue(pathId, out c);
                        usesByPathId[pathId] = c + 1;
                        if (!sampleItemIdByPathId.ContainsKey(pathId))
                        {
                            sampleItemIdByPathId[pathId] = itemId;
                            sampleItemNameByPathId[pathId] = itemName;
                            if (iconFieldIndex >= 0)
                            {
                                string iconKey = ResolveIconKeyForList(listIndex, eLC.GetValue(listIndex, row, iconFieldIndex));
                                if (!string.IsNullOrWhiteSpace(iconKey))
                                {
                                    sampleIconKeyByPathId[pathId] = iconKey;
                                }
                            }
                        }
                    }
                }
            }

            Dictionary<string, List<int>> pathIdsByNormalizedPath = BuildModelPathIdLookup();
            int idx = 1;
            for (int p = 0; p < ModelPickerPackageOrder.Length; p++)
            {
                string package = ModelPickerPackageOrder[p];
                List<string> packageFiles = EnumerateModelPickerPackageFiles(package);
                for (int i = 0; i < packageFiles.Count; i++)
                {
                    string relativePath = packageFiles[i];
                    int pathId = ResolveModelEntryPathId(package, relativePath, pathIdsByNormalizedPath, usesByPathId);
                    int uses = 0;
                    int itemId = 0;
                    string itemName = string.Empty;
                    Bitmap icon = Properties.Resources.NoIcon;
                    if (pathId > 0)
                    {
                        usesByPathId.TryGetValue(pathId, out uses);
                        sampleItemIdByPathId.TryGetValue(pathId, out itemId);
                        sampleItemNameByPathId.TryGetValue(pathId, out itemName);
                        string iconKey;
                        if (sampleIconKeyByPathId.TryGetValue(pathId, out iconKey)
                            && !string.IsNullOrWhiteSpace(iconKey)
                            && database != null
                            && database.sourceBitmap != null
                            && database.ContainsKey(iconKey))
                        {
                            Bitmap cachedIcon;
                            if (!iconCache.TryGetValue(iconKey, out cachedIcon))
                            {
                                cachedIcon = database.images(iconKey);
                                iconCache[iconKey] = cachedIcon;
                            }
                            icon = cachedIcon ?? Properties.Resources.NoIcon;
                        }
                    }

                    entries.Add(new ModelPickerEntry
                    {
                        Index = idx++,
                        PathId = pathId,
                        Package = package,
                        RelativePath = relativePath,
                        Icon = icon,
                        ItemId = itemId,
                        ItemName = itemName,
                        Uses = uses
                    });
                }
            }

            return entries;
        }

        private List<string> EnumerateModelPickerPackageFiles(string package)
        {
            List<string> files = new List<string>();
            try
            {
                string resourcesRoot = string.Empty;
                if (!string.IsNullOrWhiteSpace(AssetManager.GameRootPath))
                {
                    resourcesRoot = Path.Combine(AssetManager.GameRootPath, "resources");
                }
                string packagePck = string.IsNullOrWhiteSpace(resourcesRoot)
                    ? string.Empty
                    : Path.Combine(resourcesRoot, package + ".pck");
                string packagePkx = string.IsNullOrWhiteSpace(resourcesRoot)
                    ? string.Empty
                    : Path.Combine(resourcesRoot, package + ".pkx");

                DateTime pckTimestampUtc = DateTime.MinValue;
                if (!string.IsNullOrWhiteSpace(packagePck) && File.Exists(packagePck))
                {
                    pckTimestampUtc = File.GetLastWriteTimeUtc(packagePck);
                }
                if (!string.IsNullOrWhiteSpace(packagePkx) && File.Exists(packagePkx))
                {
                    DateTime pkxTime = File.GetLastWriteTimeUtc(packagePkx);
                    if (pkxTime > pckTimestampUtc)
                    {
                        pckTimestampUtc = pkxTime;
                    }
                }

                string cacheKey = (packagePck ?? string.Empty).Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(packagePkx) && File.Exists(packagePkx))
                {
                    cacheKey += "|" + packagePkx.Trim().ToLowerInvariant();
                }
                ModelPickerPackageCacheEntry cached;
                if (modelPickerPackageCache.TryGetValue(cacheKey, out cached)
                    && cached != null
                    && cached.Files != null
                    && cached.PckTimestampUtc == pckTimestampUtc)
                {
                    return new List<string>(cached.Files);
                }

                List<string> packageEntries = null;
                if (asm == null || !asm.TryEnumeratePckIndexEntries(package, out packageEntries))
                {
                    TryNotifyMissingModelPackageExtraction(package);
                    return files;
                }

                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < packageEntries.Count; i++)
                {
                    string relative = packageEntries[i];
                    string ext = Path.GetExtension(relative);
                    if (!IsModelPickerExtensionAllowed(package, ext))
                    {
                        continue;
                    }

                    relative = relative.Trim().Replace('/', '\\');
                    if (string.IsNullOrWhiteSpace(relative))
                    {
                        continue;
                    }

                    if (seen.Add(relative))
                    {
                        files.Add(relative);
                    }
                }

                files.Reverse();

                modelPickerPackageCache[cacheKey] = new ModelPickerPackageCacheEntry
                {
                    PckTimestampUtc = pckTimestampUtc,
                    Files = new List<string>(files)
                };
            }
            catch
            { }

            return files;
        }

        private void TryNotifyMissingModelPackageExtraction(string package)
        {
            if (string.IsNullOrWhiteSpace(package))
            {
                return;
            }
            if (!string.Equals(package, "models", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(package, "gfx", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(package, "grasses", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (modelPickerMissingExtractNotified.Contains(package))
            {
                return;
            }

            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return;
            }
            string gamePck = Path.Combine(gameRoot, "resources", package + ".pck");
            if (!File.Exists(gamePck))
            {
                return;
            }

            modelPickerMissingExtractNotified.Add(package);
            MessageBox.Show(
                "Unable to read package index:\n" + package + ".pck\n\n" +
                "FWEledit now reads PCK index tables directly (no extraction).\n" +
                "If the list stays empty, check that the PCK/PKX files are accessible and try reopening Choice Model.",
                "Choice Model",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static bool IsModelPickerExtensionAllowed(string package, string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }
            if (string.Equals(extension, ".ecm", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private Dictionary<string, List<int>> BuildModelPathIdLookup()
        {
            Dictionary<string, List<int>> lookup = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            if (database == null || database.pathById == null)
            {
                return lookup;
            }

            foreach (KeyValuePair<int, string> kv in database.pathById)
            {
                int pathId = kv.Key;
                if (pathId <= 0)
                {
                    continue;
                }

                string normalized = NormalizeModelPathLookupKey(kv.Value);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                AddPathLookupKey(lookup, normalized, pathId);

                string package;
                string relative;
                if (TrySplitModelPackagePath(normalized, out package, out relative)
                    && !string.IsNullOrWhiteSpace(relative))
                {
                    AddPathLookupKey(lookup, relative, pathId);
                }
            }

            return lookup;
        }

        private static void AddPathLookupKey(Dictionary<string, List<int>> lookup, string key, int pathId)
        {
            List<int> ids;
            if (!lookup.TryGetValue(key, out ids))
            {
                ids = new List<int>();
                lookup[key] = ids;
            }
            if (!ids.Contains(pathId))
            {
                ids.Add(pathId);
            }
        }

        private static string NormalizeModelPathLookupKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace('/', '\\').Trim().TrimStart('\\').ToLowerInvariant();
        }

        private static bool TrySplitModelPackagePath(string normalizedPath, out string package, out string relative)
        {
            package = string.Empty;
            relative = normalizedPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return false;
            }

            for (int i = 0; i < ModelPickerPathPackages.Length; i++)
            {
                string pkg = ModelPickerPathPackages[i];
                string prefix = pkg + "\\";
                if (normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    package = pkg;
                    relative = normalizedPath.Substring(prefix.Length);
                    return !string.IsNullOrWhiteSpace(relative);
                }
            }

            return false;
        }

        private int ResolveModelEntryPathId(string package, string relativePath, Dictionary<string, List<int>> lookup, Dictionary<int, int> usesByPathId)
        {
            if (lookup == null || lookup.Count == 0)
            {
                return 0;
            }

            string normalizedRelative = NormalizeModelPathLookupKey(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return 0;
            }

            List<int> candidates = new List<int>();
            string[] keys = new string[]
            {
                NormalizeModelPathLookupKey(package + "\\" + normalizedRelative),
                normalizedRelative
            };

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                List<int> ids;
                if (!lookup.TryGetValue(key, out ids))
                {
                    continue;
                }

                for (int j = 0; j < ids.Count; j++)
                {
                    if (!candidates.Contains(ids[j]))
                    {
                        candidates.Add(ids[j]);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return 0;
            }
            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            int bestId = candidates[0];
            int bestUses = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                int id = candidates[i];
                int uses = 0;
                usesByPathId.TryGetValue(id, out uses);
                if (uses > bestUses)
                {
                    bestUses = uses;
                    bestId = id;
                    continue;
                }
                if (uses == bestUses && id < bestId)
                {
                    bestId = id;
                }
            }

            return bestId;
        }

        private string FormatModelPathIdDisplay(string rawValue, string fieldName)
        {
            int pathId;
            if (!TryExtractPathId(rawValue, out pathId))
            {
                return rawValue;
            }

            int resolvedPathId;
            string mappedPath;
            if (TryResolveModelPathById(pathId, fieldName, out resolvedPathId, out mappedPath, true))
            {
                return pathId.ToString() + " | " + mappedPath;
            }

            return pathId.ToString();
        }

        private bool IsAddonParamField(string fieldName)
        {
            return string.Equals(fieldName, "param1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "param2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "param3", StringComparison.OrdinalIgnoreCase);
        }

        private int GetAddonTypeForEntry(int listIndex, int entryIndex)
        {
            if (eLC == null || listIndex != 0 || entryIndex < 0 || entryIndex >= eLC.Lists[listIndex].elementValues.Length)
            {
                return -1;
            }
            int typeFieldIndex = -1;
            for (int i = 0; i < eLC.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(eLC.Lists[listIndex].elementFields[i], "type", StringComparison.OrdinalIgnoreCase))
                {
                    typeFieldIndex = i;
                    break;
                }
            }
            if (typeFieldIndex < 0)
            {
                return -1;
            }
            int addonType;
            return int.TryParse(eLC.GetValue(listIndex, entryIndex, typeFieldIndex), out addonType) ? addonType : -1;
        }

        private bool IsAddonParamFloat(int addonType, int paramIndex)
        {
            switch (addonType)
            {
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                case 48:
                case 49:
                case 50:
                case 51:
                case 52:
                case 53:
                case 54:
                case 55:
                case 56:
                case 57:
                case 58:
                case 59:
                case 60:
                case 61:
                case 62:
                case 63:
                case 64:
                case 65:
                case 66:
                case 67:
                case 68:
                case 69:
                case 70:
                case 71:
                case 72:
                case 84:
                case 85:
                case 86:
                case 87:
                case 88:
                case 93:
                case 94:
                case 95:
                case 96:
                case 99:
                case 121:
                case 143:
                    return true;
                case 142:
                    return paramIndex == 1;
                default:
                    return false;
            }
        }

        private bool TryParseFloatFlexible(string raw, out float value)
        {
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private string FormatAddonParamValueForUi(int listIndex, int entryIndex, string fieldName, string rawValue)
        {
            if (listIndex != 0 || !IsAddonParamField(fieldName))
            {
                return rawValue;
            }
            int addonType = GetAddonTypeForEntry(listIndex, entryIndex);
            if (addonType < 0)
            {
                return rawValue;
            }
            int paramIndex = string.Equals(fieldName, "param1", StringComparison.OrdinalIgnoreCase) ? 1
                : string.Equals(fieldName, "param2", StringComparison.OrdinalIgnoreCase) ? 2 : 3;
            if (!IsAddonParamFloat(addonType, paramIndex))
            {
                return rawValue;
            }
            int rawInt;
            if (!int.TryParse(rawValue, out rawInt))
            {
                return rawValue;
            }
            float decoded = BitConverter.ToSingle(BitConverter.GetBytes(rawInt), 0);
            return decoded.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private bool TryNormalizeAddonParamValueForStorage(int listIndex, int entryIndex, string fieldName, string rawValue, out string normalized)
        {
            normalized = rawValue;
            if (listIndex != 0 || !IsAddonParamField(fieldName))
            {
                return false;
            }
            int addonType = GetAddonTypeForEntry(listIndex, entryIndex);
            if (addonType < 0)
            {
                return false;
            }
            int paramIndex = string.Equals(fieldName, "param1", StringComparison.OrdinalIgnoreCase) ? 1
                : string.Equals(fieldName, "param2", StringComparison.OrdinalIgnoreCase) ? 2 : 3;
            if (!IsAddonParamFloat(addonType, paramIndex))
            {
                return false;
            }
            string trimmed = (rawValue ?? string.Empty).Trim();
            bool hasDecimal = trimmed.IndexOf('.') >= 0 || trimmed.IndexOf(',') >= 0;
            long rawInt;
            if (!hasDecimal && long.TryParse(trimmed, out rawInt) && Math.Abs(rawInt) >= 1000000)
            {
                normalized = rawInt.ToString();
                return true;
            }
            float parsed;
            if (!TryParseFloatFlexible(trimmed, out parsed))
            {
                return false;
            }
            int packed = BitConverter.ToInt32(BitConverter.GetBytes(parsed), 0);
            normalized = packed.ToString();
            return true;
        }

        private void OpenModelPickerForValueRow(int rowIndex)
        {
            if (eLC == null || database == null || comboBox_lists.SelectedIndex < 0)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[0].Value);
            if (!IsModelFieldName(fieldName))
            {
                return;
            }

            int currentPathId = TryGetCurrentPathIdFromValueRow(rowIndex);
            int currentResolvedPathId = currentPathId;
            int resolvedCurrentPathId;
            string resolvedCurrentMappedPath;
            if (TryResolveModelPathById(currentPathId, fieldName, out resolvedCurrentPathId, out resolvedCurrentMappedPath, true) && resolvedCurrentPathId > 0)
            {
                currentResolvedPathId = resolvedCurrentPathId;
            }
            int listIndex = comboBox_lists.SelectedIndex;
            List<ModelPickerEntry> entries = BuildModelPickerEntriesForList(listIndex);
            using (ModelPickerWindow picker = new ModelPickerWindow(entries, currentResolvedPathId))
            {
                if (picker.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                int selectedPathId = picker.SelectedPathId;
                if (selectedPathId <= 0 || selectedPathId == currentPathId)
                {
                    return;
                }

                if (dataGridView_item.CurrentCell == null || dataGridView_item.CurrentCell.RowIndex != rowIndex)
                {
                    dataGridView_item.CurrentCell = dataGridView_item.Rows[rowIndex].Cells[2];
                }

                dataGridView_item.Rows[rowIndex].Cells[2].Value = selectedPathId.ToString();
            }
        }

        private void OpenAddonTypePickerForValueRow(int rowIndex)
        {
            if (eLC == null || comboBox_lists.SelectedIndex < 0 || dataGridView_item == null)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[0].Value);
            if (!IsAddonTypeField(comboBox_lists.SelectedIndex, fieldName))
            {
                return;
            }

            int fieldIndex = GetFieldIndexForValueRow(rowIndex);
            if (fieldIndex < 0)
            {
                return;
            }

            int currentType = 0;
            if (dataGridView_elems != null && dataGridView_elems.CurrentCell != null)
            {
                int itemRow = dataGridView_elems.CurrentCell.RowIndex;
                int listIndex = comboBox_lists.SelectedIndex;
                if (itemRow >= 0 && itemRow < eLC.Lists[listIndex].elementValues.Length)
                {
                    int.TryParse(eLC.GetValue(listIndex, itemRow, fieldIndex), out currentType);
                }
            }
            if (currentType <= 0)
            {
                int.TryParse(Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[2].Value), out currentType);
            }

            List<AddonTypeOption> options = BuildAddonTypeOptions(comboBox_lists.SelectedIndex);
            if (options.Count == 0)
            {
                MessageBox.Show("No addon types found.", "Added Attribute", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (AddonTypePickerWindow picker = new AddonTypePickerWindow(options, currentType))
            {
                if (picker.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                dataGridView_item.Rows[rowIndex].Cells[2].Value = picker.SelectedType.ToString();
            }
        }

        private void OpenItemQualityPickerForValueRow(int rowIndex)
        {
            if (dataGridView_item == null || rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[0].Value);
            if (!IsItemQualityFieldName(fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[2].Value);
            int.TryParse(rawValue, out currentValue);

            using (QualityPickerWindow picker = new QualityPickerWindow(ItemQualityOptions, currentValue))
            {
                if (picker.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                dataGridView_item.Rows[rowIndex].Cells[2].Value = picker.SelectedValue.ToString(CultureInfo.InvariantCulture);
            }
        }

        private List<AddonTypeOption> BuildAddonTypeOptions(int listIndex)
        {
            List<AddonTypeOption> options = new List<AddonTypeOption>();
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return options;
            }

            bool isAddonList = listIndex == 0;
            int typeFieldIndex = -1;
            for (int i = 0; i < eLC.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(eLC.Lists[listIndex].elementFields[i], "type", StringComparison.OrdinalIgnoreCase))
                {
                    typeFieldIndex = i;
                    break;
                }
            }
            if (typeFieldIndex < 0)
            {
                return options;
            }

            HashSet<int> usedTypes = new HashSet<int>();
            Dictionary<int, int> firstRowByType = new Dictionary<int, int>();
            for (int row = 0; row < eLC.Lists[listIndex].elementValues.Length; row++)
            {
                int typeValue;
                if (!int.TryParse(eLC.GetValue(listIndex, row, typeFieldIndex), out typeValue))
                {
                    continue;
                }
                if (typeValue < 0)
                {
                    continue;
                }
                usedTypes.Add(typeValue);
                if (!firstRowByType.ContainsKey(typeValue))
                {
                    firstRowByType[typeValue] = row;
                }
            }

            Dictionary<int, string> hintedTypes = LoadAddonTypeHints();
            HashSet<int> knownTypes = new HashSet<int>(usedTypes);
            if (isAddonList)
            {
                int maxType = 145;
                if (usedTypes.Count > 0)
                {
                    maxType = Math.Max(maxType, usedTypes.Max());
                }
                for (int i = 0; i <= maxType; i++)
                {
                    knownTypes.Add(i);
                }
            }
            else
            {
                foreach (int key in hintedTypes.Keys)
                {
                    knownTypes.Add(key);
                }
            }

            if (knownTypes.Count == 0)
            {
                return options;
            }

            List<int> sorted = knownTypes.OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int typeId = sorted[i];
                string display;
                if (isAddonList)
                {
                    display = EQUIPMENT_ADDON.GetAddonTypeDisplay(typeId.ToString());
                    if (string.Equals(display, typeId.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        int rowIndex;
                        if (firstRowByType.TryGetValue(typeId, out rowIndex))
                        {
                            try
                            {
                                string id = eLC.GetValue(listIndex, rowIndex, 0);
                                string decoded = EQUIPMENT_ADDON.GetAddon(id);
                                if (!string.IsNullOrWhiteSpace(decoded))
                                {
                                    display = decoded.Replace("\r", " ").Replace("\n", " / ").Trim();
                                }
                            }
                            catch
                            { }
                        }
                    }
                    if (string.Equals(display, typeId.ToString(), StringComparison.OrdinalIgnoreCase) && !usedTypes.Contains(typeId))
                    {
                        display = "Not using " + typeId;
                    }
                }
                else
                {
                    string hint;
                    if (hintedTypes.TryGetValue(typeId, out hint) && !string.IsNullOrWhiteSpace(hint))
                    {
                        display = hint.Trim();
                    }
                    else
                    {
                        display = EQUIPMENT_ADDON.GetAddonTypeDisplay(typeId.ToString());
                        if (string.Equals(display, typeId.ToString(), StringComparison.OrdinalIgnoreCase) && !usedTypes.Contains(typeId))
                        {
                            display = "Not using " + typeId;
                        }
                    }
                }

                options.Add(new AddonTypeOption
                {
                    TypeId = typeId,
                    Display = display
                });
            }

            return options;
        }

        private Dictionary<int, string> LoadAddonTypeHints()
        {
            Dictionary<int, string> hints = new Dictionary<int, string>();
            try
            {
                List<string> candidates = new List<string>();
                string appPath = Application.StartupPath ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(appPath))
                {
                    candidates.Add(Path.Combine(appPath, "resources", "data", "addon_table_en.txt"));
                    candidates.Add(Path.Combine(appPath, "resources", "data", "addon_table_pt.txt"));
                    candidates.Add(Path.Combine(appPath, "resources", "data", "addon_table.txt"));
                }
                if (!string.IsNullOrWhiteSpace(AssetManager.GameRootPath))
                {
                    candidates.Add(Path.Combine(AssetManager.GameRootPath, "data", "addon_table_en.txt"));
                    candidates.Add(Path.Combine(AssetManager.GameRootPath, "data", "addon_table_pt.txt"));
                    candidates.Add(Path.Combine(AssetManager.GameRootPath, "data", "addon_table.txt"));
                }

                string path = candidates.FirstOrDefault(p => File.Exists(p)) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path))
                {
                    return hints;
                }

                string fileName = Path.GetFileName(path) ?? string.Empty;
                bool allowCjk = fileName.IndexOf("_en", StringComparison.OrdinalIgnoreCase) >= 0
                    || fileName.IndexOf("_pt", StringComparison.OrdinalIgnoreCase) >= 0;

                using (StreamReader sr = new StreamReader(path, Encoding.UTF8, true))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }
                        string trimmed = line.Trim();
                        if (!trimmed.StartsWith("//type:", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string payload = trimmed.Substring(7).Trim();
                        if (string.IsNullOrWhiteSpace(payload))
                        {
                            continue;
                        }

                        string[] parts = payload.Split(new char[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 0)
                        {
                            continue;
                        }
                        int typeId;
                        if (!int.TryParse(parts[0], out typeId) || typeId < 0)
                        {
                            continue;
                        }
                        string desc = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                        if (!allowCjk && ContainsCjk(desc))
                        {
                            continue;
                        }
                        if (!hints.ContainsKey(typeId))
                        {
                            hints.Add(typeId, desc);
                        }
                    }
                }
            }
            catch
            { }

            return hints;
        }

        private static bool ContainsCjk(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                int code = value[i];
                if ((code >= 0x4E00 && code <= 0x9FFF)
                    || (code >= 0x3400 && code <= 0x4DBF)
                    || (code >= 0xF900 && code <= 0xFAFF)
                    || (code >= 0x3040 && code <= 0x30FF)
                    || (code >= 0xAC00 && code <= 0xD7AF))
                {
                    return true;
                }
            }

            return false;
        }

        private int TryGetCurrentPathIdFromValueRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return 0;
            }

            object valueObj = dataGridView_item.Rows[rowIndex].Cells[2].Value;
            int value;
            return TryExtractPathId(Convert.ToString(valueObj), out value) ? value : 0;
        }

        private Dictionary<int, int> BuildIconUsageLookup(int listIndex, int iconFieldRowIndex)
        {
            Dictionary<int, int> usage = new Dictionary<int, int>();
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return usage;
            }
            if (iconFieldRowIndex < 0 || iconFieldRowIndex >= eLC.Lists[listIndex].elementFields.Length)
            {
                return usage;
            }

            for (int row = 0; row < eLC.Lists[listIndex].elementValues.Length; row++)
            {
                int pathId;
                if (!int.TryParse(eLC.GetValue(listIndex, row, iconFieldRowIndex), out pathId))
                {
                    continue;
                }
                if (pathId <= 0)
                {
                    continue;
                }

                int count;
                usage.TryGetValue(pathId, out count);
                usage[pathId] = count + 1;
            }

            return usage;
        }

        private void OpenIconPickerForValueRow(int rowIndex)
        {
            if (eLC == null || database == null || comboBox_lists.SelectedIndex < 0)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[0].Value);
            if (!IsIconFieldName(fieldName))
            {
                return;
            }

            if (comboBox_lists.SelectedIndex == eLC.ConversationListIndex)
            {
                return;
            }

            int currentPathId = TryGetCurrentPathIdFromValueRow(rowIndex);
            int listIndex = comboBox_lists.SelectedIndex;
            int fieldIndex = GetFieldIndexForValueRow(rowIndex);
            if (fieldIndex < 0)
            {
                return;
            }
            Dictionary<int, int> usageLookup = BuildIconUsageLookup(listIndex, fieldIndex);
            using (IconPickerWindow picker = new IconPickerWindow(database, currentPathId, usageLookup))
            {
                if (picker.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                int selectedPathId = picker.SelectedPathId;
                if (selectedPathId <= 0)
                {
                    return;
                }

                if (dataGridView_item.CurrentCell == null || dataGridView_item.CurrentCell.RowIndex != rowIndex)
                {
                    dataGridView_item.CurrentCell = dataGridView_item.Rows[rowIndex].Cells[2];
                }

                if (currentPathId == selectedPathId)
                {
                    return;
                }

                dataGridView_item.Rows[rowIndex].Cells[2].Value = selectedPathId.ToString();
            }
        }

        private string FindExternalModelViewerExecutable()
        {
            List<string> candidates = new List<string>();
            try
            {
                if (!string.IsNullOrWhiteSpace(AssetManager.GameRootPath))
                {
                    candidates.Add(Path.Combine(AssetManager.GameRootPath, "fELedit", "Elements Editor Pago", "SKIPreview_RAE.exe"));
                    candidates.Add(Path.Combine(AssetManager.GameRootPath, "fELedit", "Elements Editor Pago", "Angelica Editor.exe"));
                    candidates.Add(Path.Combine(AssetManager.GameRootPath, "fELedit", "Elements Editor Pago", "rae_api.exe"));
                }
            }
            catch
            { }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return string.Empty;
        }

        private void OpenModelPreviewForValueRow(int rowIndex)
        {
            if (eLC == null || asm == null || comboBox_lists.SelectedIndex < 0)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[0].Value);
            if (!IsModelFieldName(fieldName))
            {
                return;
            }

            int pathId = TryGetCurrentPathIdFromValueRow(rowIndex);
            if (pathId <= 0)
            {
                MessageBox.Show("Invalid model PathID.");
                return;
            }

            int resolvedPathId;
            string mappedPath;
            if (!TryResolveModelPathById(pathId, fieldName, out resolvedPathId, out mappedPath, true))
            {
                MessageBox.Show("Model PathID not found in path.data:\n" + pathId);
                return;
            }

            string fullPath = asm.ResolveResourcePath(mappedPath);
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                MessageBox.Show("Model file not found.\nPathID: " + pathId + "\nMapped: " + mappedPath);
                return;
            }

            try
            {
                string viewerExe = FindExternalModelViewerExecutable();
                if (!string.IsNullOrWhiteSpace(viewerExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = viewerExe,
                        Arguments = "\"" + fullPath + "\"",
                        UseShellExecute = true
                    });
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("MODEL PREVIEW ERROR!\n" + ex.Message);
            }
        }

        private void UpdatePickIconButtonState()
        {
            if (fwInlinePickIconButton == null || dataGridView_item == null)
            {
                return;
            }
            if (suppressValuesUiRefresh)
            {
                return;
            }

            bool enabled = false;
            Rectangle rect = Rectangle.Empty;
            fwInlinePickIconRowIndex = -1;
            if (dataGridView_item != null && dataGridView_item.Rows != null && dataGridView_item.Rows.Count > 0)
            {
                int preferredRow = -1;
                if (dataGridView_item.CurrentCell != null && dataGridView_item.CurrentCell.RowIndex >= 0)
                {
                    int currentRow = dataGridView_item.CurrentCell.RowIndex;
                    if (currentRow < dataGridView_item.Rows.Count)
                    {
                        string currentField = Convert.ToString(dataGridView_item.Rows[currentRow].Cells[0].Value);
                        if (IsIconFieldName(currentField))
                        {
                            preferredRow = currentRow;
                        }
                    }
                }

                if (preferredRow < 0)
                {
                    for (int row = 0; row < dataGridView_item.Rows.Count; row++)
                    {
                        string fieldName = Convert.ToString(dataGridView_item.Rows[row].Cells[0].Value);
                        if (IsIconFieldName(fieldName))
                        {
                            preferredRow = row;
                            break;
                        }
                    }
                }

                if (preferredRow >= 0 && preferredRow < dataGridView_item.Rows.Count)
                {
                    enabled = true;
                    fwInlinePickIconRowIndex = preferredRow;
                    rect = dataGridView_item.GetCellDisplayRectangle(2, preferredRow, true);
                }
            }

            bool valuesTabActive = fwRightTabs != null && fwRightTabs.SelectedTab == fwValuesTab;
            if (!enabled || rect.Width < 80 || rect.Height < 18 || !valuesTabActive)
            {
                fwInlinePickIconButton.Visible = false;
                return;
            }

            fwInlinePickIconButton.Enabled = true;
            int width = Math.Min(96, Math.Max(74, rect.Width / 3));
            int height = Math.Max(18, rect.Height - 2);
            fwInlinePickIconButton.SetBounds(rect.Right - width - 1, rect.Top + 1, width, height);
            fwInlinePickIconButton.BringToFront();
            fwInlinePickIconButton.Visible = true;
        }

        private void click_pick_icon(object sender, EventArgs e)
        {
            if (dataGridView_item == null)
            {
                return;
            }

            int targetRow = fwInlinePickIconRowIndex;
            if (targetRow < 0 && dataGridView_item.CurrentCell != null)
            {
                targetRow = dataGridView_item.CurrentCell.RowIndex;
            }
            if (targetRow < 0)
            {
                return;
            }
            OpenIconPickerForValueRow(targetRow);
        }

        private void dataGridView_item_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            try
            {
                string fieldName = Convert.ToString(dataGridView_item.Rows[e.RowIndex].Cells[0].Value);
                if (IsIconFieldName(fieldName))
                {
                    OpenIconPickerForValueRow(e.RowIndex);
                }
                else if (IsAddonTypeField(comboBox_lists.SelectedIndex, fieldName))
                {
                    OpenAddonTypePickerForValueRow(e.RowIndex);
                }
                else if (IsItemQualityFieldName(fieldName))
                {
                    OpenItemQualityPickerForValueRow(e.RowIndex);
                }
                else if (IsModelFieldName(fieldName))
                {
                    OpenModelPickerForValueRow(e.RowIndex);
                }
                UpdatePickIconButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("FIELD ACTION ERROR!\n" + ex.Message);
            }
        }

		public MainWindow()
		{
            InitializeComponent();
            foreach (DataGridViewColumn col in dataGridView_elems.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            InitializeFwEditorLayout();
            EnableGridDoubleBuffer(dataGridView_elems);
            EnableGridDoubleBuffer(dataGridView_item);
            this.Shown += MainWindow_Shown;
            this.Resize += MainWindow_Resize;
            this.FormClosing += MainWindow_FormClosing;
            textBox_search.TextChanged += textBox_search_TextChanged;
            textBox_search.KeyDown += textBox_search_KeyDown;

            Assembly assembly = Assembly.GetExecutingAssembly();
            string displayVersion = "0.4.9";
            try
            {
                object[] attrs = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    AssemblyInformationalVersionAttribute info = attrs[0] as AssemblyInformationalVersionAttribute;
                    if (info != null && !string.IsNullOrWhiteSpace(info.InformationalVersion))
                    {
                        displayVersion = info.InformationalVersion.Trim();
                    }
                }
                else
                {
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                    if (!string.IsNullOrWhiteSpace(fileVersionInfo.ProductVersion))
                    {
                        displayVersion = fileVersionInfo.ProductVersion;
                    }
                }
            }
            catch
            { }
            label_Version.Text = "FWEledit v" + displayVersion;

            asm = new AssetManager();
            asm.load();
            cpb2.Value = 0;
            colorTheme();

            mouseMoveCheck = new Point(0, 0);

            navigationPersistTimer = new System.Windows.Forms.Timer();
            navigationPersistTimer.Interval = 700;
            navigationPersistTimer.Tick += (s, e) =>
            {
                if (navigationPersistTimer != null)
                {
                    navigationPersistTimer.Stop();
                }
                FlushNavigationStateToDisk();
            };
        }

        private static void EnableGridDoubleBuffer(DataGridView grid)
        {
            if (grid == null)
            {
                return;
            }
            try
            {
                typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(grid, true, null);
            }
            catch
            { }
        }

        private void InitializeFwEditorLayout()
        {
            if (fwLayoutInitialized)
            {
                return;
            }

            SuspendLayout();

            // Legacy control from old UI, no longer used in the FW-first layout.
            if (listBox_items != null)
            {
                listBox_items.Visible = false;
            }
            if (progressBar_progress != null)
            {
                progressBar_progress.Visible = false;
            }

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            int topInset = (menuStrip_mainMenu != null ? menuStrip_mainMenu.Height : 24) + 6;
            root.Padding = new Padding(6, topInset, 6, 6);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));

            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Fill;
            topPanel.Margin = new Padding(0);

            if (label_Version != null)
            {
                label_Version.Parent = topPanel;
                label_Version.Dock = DockStyle.Right;
                label_Version.Width = 180;
                label_Version.TextAlign = ContentAlignment.MiddleRight;
                label_Version.Margin = new Padding(0);
            }

            comboBox_lists.Parent = topPanel;
            comboBox_lists.Dock = DockStyle.Fill;
            comboBox_lists.Margin = new Padding(0);

            TableLayoutPanel offsetPanel = new TableLayoutPanel();
            offsetPanel.Dock = DockStyle.Right;
            offsetPanel.Width = 480;
            offsetPanel.Margin = new Padding(0);
            offsetPanel.ColumnCount = 2;
            offsetPanel.RowCount = 1;
            offsetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));
            offsetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            offsetPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            label1.Parent = offsetPanel;
            label1.AutoSize = false;
            label1.Dock = DockStyle.Fill;
            label1.Margin = new Padding(0);
            label1.TextAlign = ContentAlignment.MiddleLeft;

            textBox_offset.Parent = offsetPanel;
            textBox_offset.Dock = DockStyle.Fill;
            textBox_offset.Margin = new Padding(0);

            offsetPanel.Controls.Add(label1, 0, 0);
            offsetPanel.Controls.Add(textBox_offset, 1, 0);

            topPanel.Controls.Add(comboBox_lists);
            topPanel.Controls.Add(offsetPanel);
            if (label_Version != null)
            {
                topPanel.Controls.Add(label_Version);
            }

            SplitContainer mainSplit = new SplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.Margin = new Padding(0);
            mainSplit.FixedPanel = FixedPanel.Panel1;
            mainSplit.SplitterWidth = 5;
            fwMainSplit = mainSplit;

            Panel leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Fill;

            dataGridView_elems.Parent = leftPanel;
            dataGridView_elems.Dock = DockStyle.Fill;

            Panel searchOptionsPanel = new Panel();
            searchOptionsPanel.Dock = DockStyle.Bottom;
            searchOptionsPanel.Height = 26;

            checkBox_SearchAll.Parent = searchOptionsPanel;
            checkBox_SearchAll.Dock = DockStyle.Left;
            checkBox_SearchAll.Width = 82;

            checkBox_SearchExactMatching.Parent = searchOptionsPanel;
            checkBox_SearchExactMatching.Dock = DockStyle.Left;
            checkBox_SearchExactMatching.Width = 105;

            checkBox_SearchMatchCase.Parent = searchOptionsPanel;
            checkBox_SearchMatchCase.Dock = DockStyle.Left;
            checkBox_SearchMatchCase.Width = 96;

            TableLayoutPanel searchPanel = new TableLayoutPanel();
            searchPanel.Dock = DockStyle.Bottom;
            searchPanel.Height = 30;
            searchPanel.Margin = new Padding(0);
            searchPanel.ColumnCount = 2;
            searchPanel.RowCount = 1;
            searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94F));
            searchPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            textBox_search.Dock = DockStyle.Fill;
            textBox_search.Margin = new Padding(0);
            searchPanel.Controls.Add(textBox_search, 0, 0);

            button_search.Dock = DockStyle.Fill;
            button_search.Margin = new Padding(0);
            searchPanel.Controls.Add(button_search, 1, 0);

            searchSuggestionList = new ListBox();
            searchSuggestionList.Dock = DockStyle.Bottom;
            searchSuggestionList.Height = 140;
            searchSuggestionList.Visible = false;
            searchSuggestionList.IntegralHeight = false;
            searchSuggestionList.BorderStyle = BorderStyle.FixedSingle;
            searchSuggestionList.SelectionMode = SelectionMode.One;
            searchSuggestionList.TabStop = false;
            searchSuggestionList.MouseClick += searchSuggestionList_MouseClick;
            searchSuggestionList.KeyDown += searchSuggestionList_KeyDown;

            leftPanel.Controls.Add(dataGridView_elems);
            leftPanel.Controls.Add(searchSuggestionList);
            leftPanel.Controls.Add(searchOptionsPanel);
            leftPanel.Controls.Add(searchPanel);
            mainSplit.Panel1.Controls.Add(leftPanel);

            Panel rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;

            TableLayoutPanel setValuePanel = new TableLayoutPanel();
            setValuePanel.Dock = DockStyle.Bottom;
            setValuePanel.Height = 30;
            setValuePanel.Margin = new Padding(0);
            setValuePanel.ColumnCount = 2;
            setValuePanel.RowCount = 1;
            setValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            setValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F));
            setValuePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            setValuePanel.Visible = false;

            textBox_SetValue.Dock = DockStyle.Fill;
            textBox_SetValue.Margin = new Padding(0);
            textBox_SetValue.TextAlign = HorizontalAlignment.Center;
            setValuePanel.Controls.Add(textBox_SetValue, 0, 0);

            button_SetValue.Dock = DockStyle.Fill;
            button_SetValue.Margin = new Padding(0);
            button_SetValue.Enabled = false;
            setValuePanel.Controls.Add(button_SetValue, 1, 0);

            fwRightTabs = new TabControl();
            fwRightTabs.Dock = DockStyle.Fill;
            fwRightTabs.Margin = new Padding(0);
            fwRightTabs.SelectedIndexChanged += (s, e) => UpdatePickIconButtonState();

            fwValuesTab = new TabPage("Values");
            fwValuesTab.Padding = new Padding(0);

            fwEquipmentTabs = new TabControl();
            fwEquipmentTabs.Dock = DockStyle.Top;
            fwEquipmentTabs.Height = 26;
            fwEquipmentTabs.Margin = new Padding(0);
            fwEquipmentTabs.Visible = false;
            fwEquipmentTabs.SelectedIndexChanged += (s, e) =>
            {
                if (EnableSelectionItem)
                {
                    change_item(null, null);
                }
            };

            fwEquipmentTabMain = new TabPage("Main");
            fwEquipmentTabRefine = new TabPage("Refine");
            fwEquipmentTabModels = new TabPage("Models");
            fwEquipmentTabOther = new TabPage("Other");

            fwEquipmentTabs.TabPages.Add(fwEquipmentTabMain);
            fwEquipmentTabs.TabPages.Add(fwEquipmentTabRefine);
            fwEquipmentTabs.TabPages.Add(fwEquipmentTabModels);
            fwEquipmentTabs.TabPages.Add(fwEquipmentTabOther);

            dataGridView_item.Parent = fwValuesTab;
            dataGridView_item.Dock = DockStyle.Fill;
            dataGridView_item.CurrentCellChanged += (s, e) => UpdatePickIconButtonState();
            dataGridView_item.Scroll += (s, e) => UpdatePickIconButtonState();
            dataGridView_item.SizeChanged += (s, e) => UpdatePickIconButtonState();
            fwValuesTab.Controls.Add(dataGridView_item);
            fwValuesTab.Controls.Add(fwEquipmentTabs);
            fwValuesTab.Controls.Add(setValuePanel);
            fwValuesTab.Controls.SetChildIndex(dataGridView_item, 0);
            fwValuesTab.Controls.SetChildIndex(fwEquipmentTabs, 1);
            fwValuesTab.Controls.SetChildIndex(setValuePanel, 2);

            fwInlinePickIconButton = new Button();
            fwInlinePickIconButton.Text = "...";
            fwInlinePickIconButton.Visible = false;
            fwInlinePickIconButton.Enabled = false;
            fwInlinePickIconButton.TabStop = false;
            fwInlinePickIconButton.Click += click_pick_icon;
            dataGridView_item.Controls.Add(fwInlinePickIconButton);

            fwDescriptionTab = new TabPage("Description");
            fwDescriptionTab.Padding = new Padding(6);

            TableLayoutPanel descriptionLayout = new TableLayoutPanel();
            descriptionLayout.Dock = DockStyle.Fill;
            descriptionLayout.Margin = new Padding(0);
            descriptionLayout.ColumnCount = 1;
            descriptionLayout.RowCount = 3;
            descriptionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

            Panel descriptionHeader = new Panel();
            descriptionHeader.Dock = DockStyle.Fill;
            descriptionHeader.Margin = new Padding(0);

            fwDescriptionSaveButton = new Button();
            fwDescriptionSaveButton.Text = "Stage Description";
            fwDescriptionSaveButton.Dock = DockStyle.Right;
            fwDescriptionSaveButton.Width = 140;
            fwDescriptionSaveButton.Click += click_save_description;

            fwDescriptionStatusLabel = new Label();
            fwDescriptionStatusLabel.Dock = DockStyle.Fill;
            fwDescriptionStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            fwDescriptionStatusLabel.Text = "Loaded from configs.pck.files";

            descriptionHeader.Controls.Add(fwDescriptionStatusLabel);
            descriptionHeader.Controls.Add(fwDescriptionSaveButton);

            fwDescriptionEditor = new TextBox();
            fwDescriptionEditor.Dock = DockStyle.Fill;
            fwDescriptionEditor.Multiline = true;
            fwDescriptionEditor.ScrollBars = ScrollBars.Both;
            fwDescriptionEditor.AcceptsReturn = true;
            fwDescriptionEditor.AcceptsTab = true;
            fwDescriptionEditor.WordWrap = false;
            fwDescriptionEditor.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            fwDescriptionEditor.TextChanged += fw_description_changed;

            fwDescriptionPreview = new RichTextBox();
            fwDescriptionPreview.Dock = DockStyle.Fill;
            fwDescriptionPreview.ReadOnly = true;
            fwDescriptionPreview.WordWrap = true;
            fwDescriptionPreview.BorderStyle = BorderStyle.FixedSingle;
            fwDescriptionPreview.ScrollBars = RichTextBoxScrollBars.Vertical;
            fwDescriptionPreview.BackColor = Color.FromArgb(28, 29, 33);
            fwDescriptionPreview.ForeColor = Color.White;
            fwDescriptionPreview.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

            descriptionLayout.Controls.Add(descriptionHeader, 0, 0);
            descriptionLayout.Controls.Add(fwDescriptionEditor, 0, 1);
            descriptionLayout.Controls.Add(fwDescriptionPreview, 0, 2);
            fwDescriptionTab.Controls.Add(descriptionLayout);

            fwRightTabs.TabPages.Add(fwValuesTab);
            fwRightTabs.TabPages.Add(fwDescriptionTab);

            rightPanel.Controls.Add(fwRightTabs);
            mainSplit.Panel2.Controls.Add(rightPanel);

            cpb2.Parent = root;
            cpb2.Dock = DockStyle.Fill;
            cpb2.Margin = new Padding(0);

            root.Controls.Add(topPanel, 0, 0);
            root.Controls.Add(mainSplit, 0, 1);
            root.Controls.Add(cpb2, 0, 2);

            Controls.Add(root);
            if (menuStrip_mainMenu != null) { menuStrip_mainMenu.BringToFront(); }

            ApplyItemListDarkTheme();
            ResumeLayout();
            fwLayoutInitialized = true;
        }

        private void EnsureMainSplitSizing()
        {
            if (fwMainSplit == null || fwMainSplit.IsDisposed)
            {
                return;
            }
            int minLeft = 260;
            int minRight = 420;
            int minTotal = minLeft + minRight + fwMainSplit.SplitterWidth;

            // Form can be in a transient tiny state during startup; avoid invalid splitter math.
            if (fwMainSplit.Width < minTotal)
            {
                return;
            }

            fwMainSplit.Panel1MinSize = minLeft;
            fwMainSplit.Panel2MinSize = minRight;

            int desiredLeft = Math.Max(280, Math.Min(380, (int)(fwMainSplit.Width * 0.33)));
            int maxLeft = fwMainSplit.Width - fwMainSplit.Panel2MinSize - fwMainSplit.SplitterWidth;
            desiredLeft = Math.Max(fwMainSplit.Panel1MinSize, Math.Min(desiredLeft, maxLeft));

            if (fwMainSplit.SplitterDistance < fwMainSplit.Panel1MinSize || fwMainSplit.SplitterDistance > maxLeft)
            {
                fwMainSplit.SplitterDistance = desiredLeft;
            }
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            EnsureMainSplitSizing();
            if (startupSessionRestoreDone)
            {
                return;
            }
            startupSessionRestoreDone = true;
            try
            {
                string savedFolder = Properties.Settings.Default.LastGameFolder;
                if (!string.IsNullOrWhiteSpace(savedFolder) && Directory.Exists(savedFolder))
                {
                    LoadGameFolder(savedFolder);
                }
            }
            catch
            { }
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            EnsureMainSplitSizing();
        }

        private void colorTheme()
        {
            if (database.arrTheme != null)
            {
                comboBox_lists.DrawMode = DrawMode.OwnerDrawFixed;
                comboBox_lists.FlatStyle = FlatStyle.Flat;

                menuStrip_mainMenu.RenderMode = ToolStripRenderMode.Professional;

                BackColor = Color.FromName(database.arrTheme[0]);
                cpb2.BarColor = Color.FromName(database.arrTheme[17]);

                label1.ForeColor = Color.FromName(database.arrTheme[1]);

                checkBox_SearchAll.ForeColor = Color.FromName(database.arrTheme[1]);
                checkBox_SearchExactMatching.ForeColor = Color.FromName(database.arrTheme[1]);
                checkBox_SearchMatchCase.ForeColor = Color.FromName(database.arrTheme[1]);

                textBox_offset.ForeColor = Color.FromName(database.arrTheme[1]);
                textBox_search.ForeColor = Color.FromName(database.arrTheme[1]);
                textBox_SetValue.ForeColor = Color.FromName(database.arrTheme[1]);

                menuStrip_mainMenu.BackColor = Color.FromName(database.arrTheme[2]);
                textBox_offset.BackColor = Color.FromName(database.arrTheme[6]);
                textBox_search.BackColor = Color.FromName(database.arrTheme[6]);
                textBox_SetValue.BackColor = Color.FromName(database.arrTheme[6]);
                comboBox_lists.BackColor = Color.FromName(database.arrTheme[7]);

                if (searchSuggestionList != null)
                {
                    searchSuggestionList.BackColor = Color.FromName(database.arrTheme[6]);
                    searchSuggestionList.ForeColor = Color.FromName(database.arrTheme[1]);
                }

                dataGridView_elems.BackgroundColor = Color.FromName(database.arrTheme[12]);
                dataGridView_elems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                dataGridView_elems.RowHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                dataGridView_elems.DefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                dataGridView_elems.DefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
                dataGridView_elems.DefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);
                dataGridView_elems.DefaultCellStyle.SelectionForeColor = Color.FromName(database.arrTheme[15]);
                dataGridView_elems.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);
                dataGridView_elems.RowHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);

                ApplyItemListDarkTheme();
                dataGridView_item.BackgroundColor = Color.FromName(database.arrTheme[12]);
                dataGridView_item.ColumnHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                dataGridView_item.RowHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                dataGridView_item.DefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                dataGridView_item.DefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
                dataGridView_item.RowHeadersDefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
                dataGridView_item.DefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);
                dataGridView_item.DefaultCellStyle.SelectionForeColor = Color.FromName(database.arrTheme[15]);
                dataGridView_item.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);
                dataGridView_item.RowHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);

                button_search.BackColor = Color.FromName(database.arrTheme[11]);
                button_SetValue.BackColor = Color.FromName(database.arrTheme[11]);
                button_search.ForeColor = Color.FromName(database.arrTheme[1]);
                button_SetValue.ForeColor = Color.FromName(database.arrTheme[1]);
                if (fwInlinePickIconButton != null)
                {
                    fwInlinePickIconButton.BackColor = Color.FromName(database.arrTheme[11]);
                    fwInlinePickIconButton.ForeColor = Color.FromName(database.arrTheme[1]);
                }
                if (fwDescriptionSaveButton != null)
                {
                    fwDescriptionSaveButton.BackColor = Color.FromName(database.arrTheme[11]);
                    fwDescriptionSaveButton.ForeColor = Color.FromName(database.arrTheme[1]);
                }
                if (fwDescriptionEditor != null)
                {
                    fwDescriptionEditor.BackColor = Color.FromName(database.arrTheme[6]);
                    fwDescriptionEditor.ForeColor = Color.FromName(database.arrTheme[1]);
                }
                if (fwDescriptionPreview != null)
                {
                    fwDescriptionPreview.BackColor = Color.FromName(database.arrTheme[12]);
                    fwDescriptionPreview.ForeColor = Color.FromName(database.arrTheme[15]);
                }
                if (fwDescriptionStatusLabel != null)
                {
                    fwDescriptionStatusLabel.ForeColor = Color.FromName(database.arrTheme[1]);
                }

                menuStrip_mainMenu.Renderer = new MyRenderer();
                contextMenuStrip_items.Renderer = new MyRenderer();
            }
        }

        private void ApplyItemListDarkTheme()
        {
            if (dataGridView_elems == null)
            {
                return;
            }

            dataGridView_elems.BackgroundColor = Color.Black;
            dataGridView_elems.DefaultCellStyle.BackColor = Color.Black;
            dataGridView_elems.RowsDefaultCellStyle.BackColor = Color.Black;
            dataGridView_elems.AlternatingRowsDefaultCellStyle.BackColor = Color.Black;
            dataGridView_elems.RowHeadersDefaultCellStyle.BackColor = Color.Black;
            dataGridView_elems.GridColor = Color.FromArgb(40, 40, 40);
            dataGridView_elems.DefaultCellStyle.ForeColor = Color.Gainsboro;
            dataGridView_elems.DefaultCellStyle.SelectionForeColor = Color.White;
        }

        public class MyRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (database.arrTheme != null)
                {
                    if (!e.Item.Selected)
                    {
                        base.OnRenderMenuItemBackground(e);
                        e.Item.BackColor = Color.FromName(database.arrTheme[7]);
                    }
                    else
                    {
                        Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
                        Brush myBrush = new SolidBrush(Color.FromName(database.arrTheme[3]));
                        e.Graphics.FillRectangle(myBrush, rc);
                        Pen myPen = new Pen(Color.FromName(database.arrTheme[2]));
                        e.Graphics.DrawRectangle(myPen, 1, 0, rc.Width - 2, rc.Height - 1);
                        e.Item.BackColor = Color.FromName(database.arrTheme[3]);
                    }
                }
            }
            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                if (database.arrTheme != null)
                {
                    Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
                    Brush myBrush = new SolidBrush(Color.FromName(database.arrTheme[3]));
                    e.Graphics.FillRectangle(myBrush, rc);
                    Pen myPen = new Pen(Color.FromName(database.arrTheme[2]));
                    e.Graphics.DrawRectangle(myPen, 1, 0, rc.Width - 2, rc.Height - 1);
                    e.Item.BackColor = Color.FromName(database.arrTheme[3]);
                }
            }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                if (database.arrTheme != null)
                {
                    base.OnRenderItemText(e);
                    if (!e.Item.Selected)
                    {
                        e.Item.ForeColor = Color.FromName(database.arrTheme[4]);
                    }
                    else
                    {
                        e.Item.ForeColor = Color.FromName(database.arrTheme[5]);
                    }
                }
            }
        }

        private void comboBoxDb_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (database.arrTheme != null)
            {
                try
                {
                    var combo = sender as ComboBox;

                    if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    {
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromName(database.arrTheme[8])), e.Bounds);
                        e.Graphics.DrawString(combo.Items[e.Index].ToString(), e.Font, new SolidBrush(Color.FromName(database.arrTheme[10])), new Point(e.Bounds.X, e.Bounds.Y));
                    }
                    else
                    {
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromName(database.arrTheme[7])), e.Bounds);
                        e.Graphics.DrawString(combo.Items[e.Index].ToString(), e.Font, new SolidBrush(Color.FromName(database.arrTheme[9])), new Point(e.Bounds.X, e.Bounds.Y));
                    }
                }
                catch { }
            }
        }

        private void click_load(object sender, EventArgs e)
		{
            FolderBrowserDialog gameFolder = new FolderBrowserDialog();
            gameFolder.Description = "Select the Forsaken World game folder";
            string savedFolder = Properties.Settings.Default.LastGameFolder;
            if (!string.IsNullOrWhiteSpace(savedFolder) && Directory.Exists(savedFolder))
            {
                gameFolder.SelectedPath = savedFolder;
            }
			if (gameFolder.ShowDialog() == DialogResult.OK && Directory.Exists(gameFolder.SelectedPath))
			{
                LoadGameFolder(gameFolder.SelectedPath);
			}
		}

        private void click_load_last_folder(object sender, EventArgs e)
        {
            string savedFolder = Properties.Settings.Default.LastGameFolder;
            if (string.IsNullOrWhiteSpace(savedFolder) || !Directory.Exists(savedFolder))
            {
                MessageBox.Show("No saved game folder found. Use File > Load... first.");
                return;
            }
            LoadGameFolder(savedFolder);
        }

        private void LoadGameFolder(string gameFolderPath)
        {
            if (string.IsNullOrWhiteSpace(gameFolderPath) || !Directory.Exists(gameFolderPath))
            {
                MessageBox.Show("Invalid game folder.");
                return;
            }

            string elementsFile = Path.Combine(gameFolderPath, "data", "elements.data");
                if (!File.Exists(elementsFile))
                {
                    string directElements = Path.Combine(gameFolderPath, "elements.data");
                    if (File.Exists(directElements))
                    {
                        elementsFile = directElements;
                    }
                    else
                    {
                        try
                        {
                            string[] found = Directory.GetFiles(gameFolderPath, "elements.data", SearchOption.AllDirectories);
                            if (found.Length > 0)
                            {
                                Array.Sort(found, (a, b) => a.Length.CompareTo(b.Length));
                                elementsFile = found[0];
                            }
                        }
                        catch
                        { }
                    }
                }

                if (!File.Exists(elementsFile))
                {
                    MessageBox.Show("Could not find elements.data inside the selected game folder.");
                    return;
                }

            try
            {
                Cursor = Cursors.AppStarting;
                //progressBar_progress.Style = ProgressBarStyle.Continuous;
                dirtyRowsByList.Clear();
                dirtyFieldsByList.Clear();
                invalidFieldsByList.Clear();
                hasPendingDescriptionChanges = false;
                hasUnsavedChanges = false;

                // Switch resource context to the selected FW/PW game root.
                IconPickerWindow.CancelBackgroundWarmup();
                asm.SetGameRootFromElements(elementsFile);
                asm.load();
                try
                {
                    // Warm icon thumbnails in background so the picker opens instantly later.
                    IconPickerWindow.BeginBackgroundWarmup(database);
                }
                catch
                { }
                colorTheme();

                eLC = new eListCollection(elementsFile, ref cpb2);
                ResetList0DisplayCache();
                // Defer list display warmup to avoid blocking initial load.

					this.exportContainerToolStripMenuItem.DropDownItems.Clear();

					// search for available export rules
					if (eLC.ConfigFile != null)
					{
						this.exportContainerToolStripMenuItem.DropDownItems.Add(new ToolStripLabel("Select a valid Conversation Rules Set"));
						this.exportContainerToolStripMenuItem.DropDownItems[0].Font = new Font("Tahoma", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
						this.exportContainerToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
						string[] files = Directory.GetFiles(Application.StartupPath + "\\rules", "PW_v" + eLC.Version.ToString() + "*.rules");
						for (int i = 0; i < files.Length; i++)
						{
							files[i] = files[i].Replace("=", "=>");
							files[i] = files[i].Replace(".rules", "");
							files[i] = files[i].Replace(Application.StartupPath + "\\rules\\", "");
							this.exportContainerToolStripMenuItem.DropDownItems.Add(files[i], null, new EventHandler(this.click_export));
						}
					}
					// load cross references list
					if (eLC.ConfigFile != null)//Вроде работает
					{
                        xrefs = new string[eLC.Lists.Length][];
                        string configDir = Path.GetDirectoryName(eLC.ConfigFile);
						string[] referencefiles = Directory.GetFiles(configDir, "references.txt");
						if (referencefiles.Length > 0)
						{
							StreamReader sr = new StreamReader(referencefiles[0]);
							char[] chars = { ';', ',' };
							string[] x;
							xrefs = new string[eLC.Lists.Length][];
							string line;
							while (!sr.EndOfStream)
							{
								line = sr.ReadLine();
								if (!line.StartsWith("#"))
								{
									x = line.Split(chars);
									if (int.Parse(x[0]) < eLC.Lists.Length)
									{
										xrefs[int.Parse(x[0])] = x;
									}
								}
							}
							this.toolStripSeparator6.Visible = true;
							this.xrefItemToolStripMenuItem.Visible = true;
						}
                        else
                        {
                            this.toolStripSeparator6.Visible = false;
                            this.xrefItemToolStripMenuItem.Visible = false;
                        }
					}

					if (eLC.ConversationListIndex > -1 && eLC.Lists.Length > eLC.ConversationListIndex)
					{
                        try
                        {
						    conversationList = new eListConversation((byte[])eLC.Lists[eLC.ConversationListIndex].elementValues[0][0]);
                        }
                        catch
                        {
                            conversationList = null;
                        }
					}

					dataGridView_item.Rows.Clear();
					comboBox_lists.Items.Clear();
					for (int l = 0; l < eLC.Lists.Length; l++)
					{
                        string friendlyListName = GetFriendlyListName(eLC.Lists[l].listName);
						comboBox_lists.Items.Add("[" + l + "] " + friendlyListName + " (" + eLC.Lists[l].elementValues.Length + ")");
					}
					string timestamp = "";
					if (eLC.Lists[0].listOffset.Length > 0)
						timestamp = ", Timestamp: " + timestamp_to_string(BitConverter.ToUInt32(eLC.Lists[0].listOffset, 0));
                this.Text = "FWEledit (" + elementsFile + " [Version: " + eLC.Version.ToString() + timestamp + "])";
                ElementsPath = elementsFile;
                LoadItemDescriptionsFromConfigs();

					cpb2.Value = 0;
					//progressBar_progress.Style = ProgressBarStyle.Continuous;

                int savedList = Properties.Settings.Default.LastListIndex;
                int savedItemId = Properties.Settings.Default.LastItemId;
                isRestoringSessionState = true;
                try
                {
                    if (savedList < 0 || savedList >= comboBox_lists.Items.Count)
                    {
                        savedList = 0;
                    }
                    comboBox_lists.SelectedIndex = savedList;
                    if (savedItemId > 0 && comboBox_lists.SelectedIndex > -1)
                    {
                        for (int row = 0; row < dataGridView_elems.Rows.Count; row++)
                        {
                            int rowId;
                            if (int.TryParse(Convert.ToString(dataGridView_elems.Rows[row].Cells[0].Value), out rowId) && rowId == savedItemId)
                            {
                                dataGridView_elems.ClearSelection();
                                dataGridView_elems.CurrentCell = dataGridView_elems[0, row];
                                dataGridView_elems.Rows[row].Selected = true;
                                dataGridView_elems.FirstDisplayedScrollingRowIndex = row;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    isRestoringSessionState = false;
                }
                PersistNavigationState();

                Properties.Settings.Default.LastGameFolder = gameFolderPath;
                Properties.Settings.Default.Save();

                Cursor = Cursors.Default;
            }
            catch
            {
                    //MessageBox.Show(eListCollection.SStat[0].ToString() + "\n" + eListCollection.SStat[1].ToString() + "\n" + eListCollection.SStat[2].ToString());
                    MessageBox.Show("LOADING ERROR!\n\nThis error usually occurs if incorrect configuration, structure, or encrypted elements.data file...\nIf you are using elements.list.count trying to decrypt, its likely the last list item count is incorrect... \nUse details below to assist... \n\nRead Failed at this point :\n" + eListCollection.SStat[0].ToString() + " - List #\n" + eListCollection.SStat[1].ToString() + " - # Items This List\n" + eListCollection.SStat[2].ToString() + " - Item ID");
                //progressBar_progress.Style = ProgressBarStyle.Continuous;
                Cursor = Cursors.Default;
            }
        }

        private string ResolveItemExtDescFilePath()
        {
            if (string.IsNullOrWhiteSpace(AssetManager.GameRootPath) || !Directory.Exists(AssetManager.GameRootPath))
            {
                return string.Empty;
            }

            List<string> candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(AssetManager.WorkspaceRootPath) && Directory.Exists(AssetManager.WorkspaceRootPath))
            {
                candidates.Add(Path.Combine(AssetManager.WorkspaceRootPath, "resources", "configs.pck.files", "item_ext_desc.txt"));
                candidates.Add(Path.Combine(AssetManager.WorkspaceRootPath, "resources", "configs.pck.files", "data", "item_ext_desc.txt"));
            }
            candidates.Add(Path.Combine(AssetManager.GameRootPath, "resources", "configs.pck.files", "item_ext_desc.txt"));
            candidates.Add(Path.Combine(AssetManager.GameRootPath, "resources", "configs.pck.files", "data", "item_ext_desc.txt"));

            for (int i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            try
            {
                string[] found = Directory.GetFiles(
                    Path.Combine(AssetManager.GameRootPath, "resources"),
                    "item_ext_desc.txt",
                    SearchOption.AllDirectories);
                if (found.Length > 0)
                {
                    return found[0];
                }
            }
            catch
            { }

            return string.Empty;
        }

        private static string DecodeDescriptionForEditor(string storedValue)
        {
            if (string.IsNullOrEmpty(storedValue))
            {
                return string.Empty;
            }
            return storedValue.Replace("\\r", Environment.NewLine).Replace("\\n", Environment.NewLine).Replace("\\t", "\t");
        }

        private static string EncodeDescriptionForStorage(string editorValue)
        {
            if (string.IsNullOrEmpty(editorValue))
            {
                return string.Empty;
            }

            string normalized = editorValue.Replace("\r\n", "\n").Replace('\r', '\n');
            normalized = normalized.Replace("\\", "\\\\");
            normalized = normalized.Replace("\"", "\\\"");
            normalized = normalized.Replace("\t", "\\t");
            normalized = normalized.Replace("\n", "\\r");
            return normalized;
        }

        private void LoadItemDescriptionsFromConfigs()
        {
            itemExtDescMap = new Dictionary<int, string>();
            itemExtDescOrder = new List<int>();
            itemExtDescFilePath = ResolveItemExtDescFilePath();

            if (string.IsNullOrWhiteSpace(itemExtDescFilePath) || !File.Exists(itemExtDescFilePath))
            {
                if (fwDescriptionStatusLabel != null)
                {
                    fwDescriptionStatusLabel.Text = "item_ext_desc.txt not found in configs.pck.files";
                }
                RebuildRuntimeItemExtDescArray();
                return;
            }

            try
            {
                Regex rx = new Regex("^\\s*(\\d+)\\s+\"(.*)\"\\s*$", RegexOptions.Compiled);
                using (StreamReader sr = new StreamReader(itemExtDescFilePath, Encoding.Unicode, true))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
                        {
                            continue;
                        }

                        Match m = rx.Match(line);
                        if (!m.Success)
                        {
                            continue;
                        }

                        int id;
                        if (!int.TryParse(m.Groups[1].Value, out id))
                        {
                            continue;
                        }

                        string desc = m.Groups[2].Value;
                        if (!itemExtDescMap.ContainsKey(id))
                        {
                            itemExtDescOrder.Add(id);
                        }
                        itemExtDescMap[id] = desc;
                    }
                }

                if (fwDescriptionStatusLabel != null)
                {
                    fwDescriptionStatusLabel.Text = "Loaded: " + Path.GetFileName(itemExtDescFilePath);
                }
            }
            catch
            {
                if (fwDescriptionStatusLabel != null)
                {
                    fwDescriptionStatusLabel.Text = "Failed to parse item_ext_desc.txt";
                }
            }

            RebuildRuntimeItemExtDescArray();
        }

        private void RebuildRuntimeItemExtDescArray()
        {
            List<string> arr = new List<string>();
            for (int i = 0; i < itemExtDescOrder.Count; i++)
            {
                int id = itemExtDescOrder[i];
                string value;
                if (!itemExtDescMap.TryGetValue(id, out value))
                {
                    continue;
                }
                arr.Add(id.ToString());
                arr.Add(value ?? string.Empty);
            }

            string[] data = arr.ToArray();
            MainWindow.item_ext_desc = data;
            if (MainWindow.database != null)
            {
                MainWindow.database.item_ext_desc = data;
            }
        }

        private void UpdateDescriptionTabForSelection()
        {
            if (fwDescriptionEditor == null || eLC == null || comboBox_lists.SelectedIndex < 0 || dataGridView_elems.CurrentCell == null)
            {
                return;
            }

            int l = comboBox_lists.SelectedIndex;
            if (l == eLC.ConversationListIndex)
            {
                currentDescriptionItemId = 0;
                isUpdatingDescriptionUi = true;
                fwDescriptionEditor.Text = string.Empty;
                fwDescriptionPreview.Text = string.Empty;
                isUpdatingDescriptionUi = false;
                return;
            }

            int row = dataGridView_elems.CurrentCell.RowIndex;
            if (row < 0 || row >= eLC.Lists[l].elementValues.Length)
            {
                return;
            }

            int id;
            object idCell = dataGridView_elems.Rows[row].Cells[0].Value;
            if (!int.TryParse(Convert.ToString(idCell), out id))
            {
                int.TryParse(eLC.GetValue(l, row, 0), out id);
            }
            currentDescriptionItemId = id;

            string raw;
            if (!itemExtDescMap.TryGetValue(id, out raw))
            {
                raw = Extensions.ItemDesc(id);
            }

            isUpdatingDescriptionUi = true;
            string decoded = DecodeDescriptionForEditor(raw);
            fwDescriptionEditor.Text = decoded;
            RenderDescriptionPreview(decoded);
            isUpdatingDescriptionUi = false;
        }

        private string SafeColorClean(string value)
        {
            try
            {
                return Extensions.ColorClean(value ?? string.Empty);
            }
            catch
            {
                return value ?? string.Empty;
            }
        }

        private void fw_description_changed(object sender, EventArgs e)
        {
            if (isUpdatingDescriptionUi || fwDescriptionPreview == null || fwDescriptionEditor == null)
            {
                return;
            }
            RenderDescriptionPreview(fwDescriptionEditor.Text);
            StageCurrentDescriptionChange(false);
        }

        private void StageCurrentDescriptionChange(bool updateStatus)
        {
            if (isUpdatingDescriptionUi || currentDescriptionItemId <= 0)
            {
                return;
            }

            string encoded = EncodeDescriptionForStorage(fwDescriptionEditor.Text ?? string.Empty);
            string existing;
            bool hasExisting = itemExtDescMap.TryGetValue(currentDescriptionItemId, out existing);
            if (!hasExisting)
            {
                itemExtDescOrder.Add(currentDescriptionItemId);
            }
            if (!hasExisting || !string.Equals(existing ?? string.Empty, encoded ?? string.Empty, StringComparison.Ordinal))
            {
                itemExtDescMap[currentDescriptionItemId] = encoded;
                hasPendingDescriptionChanges = true;
                hasUnsavedChanges = true;
                RebuildRuntimeItemExtDescArray();
                if (comboBox_lists.SelectedIndex > -1 && dataGridView_elems.CurrentCell != null)
                {
                    int rowIndex = dataGridView_elems.CurrentCell.RowIndex;
                    if (rowIndex > -1)
                    {
                        MarkRowDirty(comboBox_lists.SelectedIndex, rowIndex);
                    }
                }
            }

            if (updateStatus && fwDescriptionStatusLabel != null)
            {
                fwDescriptionStatusLabel.Text = "Description staged for item " + currentDescriptionItemId + " (save with File > Save)";
            }
        }

        private bool FlushPendingDescriptionsToDisk()
        {
            if (!hasPendingDescriptionChanges)
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(itemExtDescFilePath))
            {
                MessageBox.Show("item_ext_desc.txt was not found in configs.pck.files.");
                return false;
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(itemExtDescFilePath, false, Encoding.Unicode))
                {
                    sw.WriteLine("//  Element item extend descriptions.");
                    sw.WriteLine();
                    sw.WriteLine("#_index");
                    sw.WriteLine("#_begin");

                    for (int i = 0; i < itemExtDescOrder.Count; i++)
                    {
                        int id = itemExtDescOrder[i];
                        string value;
                        if (!itemExtDescMap.TryGetValue(id, out value))
                        {
                            continue;
                        }
                        sw.WriteLine(id + "\t\"" + value + "\"");
                    }
                }

                RebuildRuntimeItemExtDescArray();
                if (asm != null)
                {
                    asm.MarkWorkspaceFileChanged(itemExtDescFilePath);
                }
                hasPendingDescriptionChanges = false;
                if (fwDescriptionStatusLabel != null)
                {
                    fwDescriptionStatusLabel.Text = "Description file saved with main Save";
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save description file.\n" + ex.Message);
                return false;
            }
        }

        private void LogError(string context, Exception ex)
        {
            try
            {
                string logsDir = Path.Combine(Application.StartupPath, "logs");
                Directory.CreateDirectory(logsDir);
                string logFile = Path.Combine(logsDir, "fweledit-errors.log");
                using (StreamWriter sw = new StreamWriter(logFile, true, Encoding.UTF8))
                {
                    sw.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + context);
                    if (ex != null)
                    {
                        sw.WriteLine(ex.GetType().FullName + ": " + ex.Message);
                        sw.WriteLine(ex.StackTrace ?? string.Empty);
                    }
                    sw.WriteLine();
                }
            }
            catch
            { }
        }

        private bool ValidateUniqueIdsBeforeSave()
        {
            if (eLC == null)
            {
                return false;
            }

            List<string> issues = new List<string>();
            for (int l = 0; l < eLC.Lists.Length; l++)
            {
                if (l == eLC.ConversationListIndex)
                {
                    continue;
                }

                int idFieldIndex = GetIdFieldIndex(l);
                if (idFieldIndex < 0)
                {
                    continue;
                }

                Dictionary<int, int> firstRowById = new Dictionary<int, int>();
                for (int r = 0; r < eLC.Lists[l].elementValues.Length; r++)
                {
                    int id;
                    string raw = eLC.GetValue(l, r, idFieldIndex);
                    if (!int.TryParse(raw, out id))
                    {
                        issues.Add("List [" + l + "] has non-numeric ID at row " + (r + 1) + ": " + raw);
                        if (issues.Count >= 30)
                        {
                            break;
                        }
                        continue;
                    }

                    int firstRow;
                    if (firstRowById.TryGetValue(id, out firstRow))
                    {
                        issues.Add("List [" + l + "] duplicate ID " + id + " at rows " + (firstRow + 1) + " and " + (r + 1));
                        if (issues.Count >= 30)
                        {
                            break;
                        }
                    }
                    else
                    {
                        firstRowById[id] = r;
                    }
                }

                if (issues.Count >= 30)
                {
                    break;
                }
            }

            if (issues.Count == 0)
            {
                return true;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Save canceled: ID validation failed.");
            sb.AppendLine();
            for (int i = 0; i < issues.Count; i++)
            {
                sb.AppendLine("- " + issues[i]);
            }
            if (issues.Count >= 30)
            {
                sb.AppendLine();
                sb.AppendLine("More issues may exist. Fix IDs and try again.");
            }
            MessageBox.Show(sb.ToString());
            return false;
        }

        private bool SaveElementsFileSafely(string targetPath)
        {
            string tempPath = string.Empty;
            try
            {
                string dir = Path.GetDirectoryName(targetPath);
                if (string.IsNullOrWhiteSpace(dir))
                {
                    return false;
                }
                Directory.CreateDirectory(dir);

                tempPath = Path.Combine(dir, Path.GetFileName(targetPath) + ".tmp_fweledit");
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                eLC.Save(tempPath);
                if (!File.Exists(tempPath))
                {
                    return false;
                }

                if (File.Exists(targetPath))
                {
                    string backupRoot = !string.IsNullOrWhiteSpace(AssetManager.GameRootPath)
                        ? AssetManager.GameRootPath
                        : dir;
                    string backupDir = Path.Combine(backupRoot, "backup_elements");
                    AssetManager.CreateTimestampedZipBackup(targetPath, backupDir, "elements");
                    File.Copy(targetPath, targetPath + ".bak", true);
                }

                File.Copy(tempPath, targetPath, true);
                File.Delete(tempPath);
                return true;
            }
            catch (Exception ex)
            {
                LogError("SaveElementsFileSafely(" + targetPath + ")", ex);
                try
                {
                    if (!string.IsNullOrWhiteSpace(tempPath) && File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                { }
                return false;
            }
        }

        private bool IsPlaceholderOrEmptySetValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }
            return string.Equals(value.Trim(), "Set Value", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsValueCompatibleWithFieldType(string fieldType, string value)
        {
            try
            {
                if (fieldType == null)
                {
                    return false;
                }
                if (fieldType == "int16")
                {
                    short tmp;
                    return short.TryParse(value, out tmp);
                }
                if (fieldType == "int32")
                {
                    int tmp;
                    return int.TryParse(value, out tmp);
                }
                if (fieldType == "int64")
                {
                    long tmp;
                    return long.TryParse(value, out tmp);
                }
                if (fieldType == "float")
                {
                    float tmp;
                    return float.TryParse(value, out tmp);
                }
                if (fieldType == "double")
                {
                    double tmp;
                    return double.TryParse(value, out tmp);
                }
                if (fieldType.Contains("byte:"))
                {
                    string[] hex = value.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (hex.Length == 0)
                    {
                        return false;
                    }
                    for (int i = 0; i < hex.Length; i++)
                    {
                        byte b;
                        if (!byte.TryParse(hex[i], System.Globalization.NumberStyles.HexNumber, null, out b))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsHexSequence(string text, int start, int len)
        {
            if (string.IsNullOrEmpty(text) || start < 0 || start + len > text.Length)
            {
                return false;
            }
            for (int i = 0; i < len; i++)
            {
                char c = text[start + i];
                bool isHex = (c >= '0' && c <= '9')
                    || (c >= 'a' && c <= 'f')
                    || (c >= 'A' && c <= 'F');
                if (!isHex)
                {
                    return false;
                }
            }
            return true;
        }

        private void AppendPreviewText(string value, Color color)
        {
            if (fwDescriptionPreview == null || string.IsNullOrEmpty(value))
            {
                return;
            }
            int start = fwDescriptionPreview.TextLength;
            fwDescriptionPreview.SelectionStart = start;
            fwDescriptionPreview.SelectionLength = 0;
            fwDescriptionPreview.SelectionColor = color;
            fwDescriptionPreview.AppendText(value);
        }

        private void RenderDescriptionPreview(string rawText)
        {
            if (fwDescriptionPreview == null)
            {
                return;
            }

            string text = rawText ?? string.Empty;
            fwDescriptionPreview.SuspendLayout();
            fwDescriptionPreview.Clear();

            Color currentColor = Color.White;
            StringBuilder chunk = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '^')
                {
                    // FW color tag: ^RRGGBB
                    if (i + 7 <= text.Length && IsHexSequence(text, i + 1, 6))
                    {
                        if (chunk.Length > 0)
                        {
                            AppendPreviewText(chunk.ToString(), currentColor);
                            chunk.Clear();
                        }
                        string hex = text.Substring(i + 1, 6);
                        try
                        {
                            currentColor = ColorTranslator.FromHtml("#" + hex);
                        }
                        catch
                        {
                            currentColor = Color.White;
                        }
                        i += 7;
                        continue;
                    }

                    // Some FW strings include short formatting markers like ^0037 before a real color tag.
                    if (i + 5 <= text.Length && IsHexSequence(text, i + 1, 4))
                    {
                        if (chunk.Length > 0)
                        {
                            AppendPreviewText(chunk.ToString(), currentColor);
                            chunk.Clear();
                        }
                        i += 5;
                        continue;
                    }
                }

                chunk.Append(text[i]);
                i++;
            }

            if (chunk.Length > 0)
            {
                AppendPreviewText(chunk.ToString(), currentColor);
            }

            fwDescriptionPreview.SelectionStart = 0;
            fwDescriptionPreview.SelectionLength = 0;
            fwDescriptionPreview.ResumeLayout();
        }

        private void click_save_description(object sender, EventArgs e)
        {
            if (currentDescriptionItemId <= 0)
            {
                MessageBox.Show("No item selected.");
                return;
            }
            StageCurrentDescriptionChange(true);
        }

		private void click_save(object sender, EventArgs e)
		{
            NavigationSnapshot navSnapshot = CaptureNavigationSnapshot();
            BeginSaveProgress();
            SetSaveProgress(5);
			SaveFileDialog eSave = new SaveFileDialog();
			eSave.InitialDirectory = Environment.CurrentDirectory;
			eSave.Filter = "Elements File (*.data)|*.data|All Files (*.*)|*.*";
			if (eSave.ShowDialog() == DialogResult.OK && eSave.FileName != "")
			{
				try
				{
					Cursor = Cursors.AppStarting;
                    if (!ValidateUniqueIdsBeforeSave())
                    {
                        Cursor = Cursors.Default;
                        EndSaveProgress();
                        return;
                    }
                    if (!FlushPendingDescriptionsToDisk())
                    {
                        Cursor = Cursors.Default;
                        EndSaveProgress();
                        return;
                    }
					//progressBar_progress.Style = ProgressBarStyle.Marquee;
					//File.Copy(eSave.FileName, eSave.FileName + ".bak", true);
					if (eLC.ConversationListIndex > -1 && eLC.Lists.Length > eLC.ConversationListIndex)
					{
                        if (conversationList != null)
                        {
                            eLC.Lists[eLC.ConversationListIndex].elementValues[0][0] = conversationList.GetBytes();
                        }
					}
                    SetSaveProgress(25);
                    if (!SaveElementsFileSafely(eSave.FileName))
                    {
                        MessageBox.Show("SAVING ERROR!\nFailed to write output file safely.");
                        Cursor = Cursors.Default;
                        EndSaveProgress();
                        return;
                    }
                    ClearDirtyTrackingAfterSave();
                    SetSaveProgress(100);
                    RestoreNavigationSnapshot(navSnapshot);
					//progressBar_progress.Style = ProgressBarStyle.Continuous;
					Cursor = Cursors.Default;
                    EndSaveProgress();
                    ShowSaveConfirmation(string.Empty);
                }
                catch (Exception ex)
                {
                    LogError("click_save", ex);
                    MessageBox.Show("SAVING ERROR!\nThis error mostly occurs of configuration and elements.data mismatch");
                    //progressBar_progress.Style = ProgressBarStyle.Continuous;
                    Cursor = Cursors.Default;
                    EndSaveProgress();
                }
            }
            else
            {
                EndSaveProgress();
            }
		}

		private void click_save2(object sender, EventArgs e)
		{
			if (ElementsPath != "" && File.Exists(ElementsPath))
			{
                NavigationSnapshot navSnapshot = CaptureNavigationSnapshot();
                BeginSaveProgress();
                SetSaveProgress(5);
                string syncSummaryForUi = string.Empty;
				try
				{
					Cursor = Cursors.AppStarting;
                    if (!ValidateUniqueIdsBeforeSave())
                    {
                        Cursor = Cursors.Default;
                        EndSaveProgress();
                        return;
                    }
                    if (!FlushPendingDescriptionsToDisk())
                    {
                        Cursor = Cursors.Default;
                        EndSaveProgress();
                        return;
                    }
					//progressBar_progress.Style = ProgressBarStyle.Marquee;
					if (eLC.ConversationListIndex > -1 && eLC.Lists.Length > eLC.ConversationListIndex)
					{
                        if (conversationList != null)
                        {
                            eLC.Lists[eLC.ConversationListIndex].elementValues[0][0] = conversationList.GetBytes();
                        }
					}
                    SetSaveProgress(25);
                    if (!SaveElementsFileSafely(ElementsPath))
                    {
                        MessageBox.Show("SAVING ERROR!\nFailed to write elements.data safely.");
                        Cursor = Cursors.Default;
                        EndSaveProgress();
                        return;
                    }

                    if (asm != null)
                    {
                        SetSaveProgress(70);
                        string syncSummary;
                        if (!asm.ApplyWorkspaceChangesToGame(out syncSummary))
                        {
                            MessageBox.Show("Saved elements.data, but resource sync failed:\n" + syncSummary);
                        }
                        else if (!string.IsNullOrWhiteSpace(syncSummary) && !syncSummary.StartsWith("No PCK", StringComparison.OrdinalIgnoreCase))
                        {
                            if (fwDescriptionStatusLabel != null)
                            {
                                fwDescriptionStatusLabel.Text = syncSummary;
                            }
                            syncSummaryForUi = syncSummary;
                        }
                    }
                    ClearDirtyTrackingAfterSave();
                    SetSaveProgress(100);
                    RestoreNavigationSnapshot(navSnapshot);
					//progressBar_progress.Style = ProgressBarStyle.Continuous;
					Cursor = Cursors.Default;
                    EndSaveProgress();
                    ShowSaveConfirmation(syncSummaryForUi);
				}
				catch (Exception ex)
				{
                    LogError("click_save2", ex);
					MessageBox.Show("SAVING ERROR!\nThis error mostly occurs of configuration and elements.data mismatch");
					//progressBar_progress.Style = ProgressBarStyle.Continuous;
					Cursor = Cursors.Default;
                    EndSaveProgress();
				}
			}
		}

		private void click_export(object sender, EventArgs e)
		{
			SaveFileDialog eSave = new SaveFileDialog();
			eSave.Filter = "Elements File (*.data)|*.data|All Files (*.*)|*.*";
			if (eSave.ShowDialog() == DialogResult.OK && eSave.FileName != "")
			{
				try
				{
					int start = ((ToolStripMenuItem)sender).Text.IndexOf(" ==> ") + 5;

					Cursor = Cursors.WaitCursor;
					//progressBar_progress.Style = ProgressBarStyle.Marquee;
					string rulesFile = Application.StartupPath + "\\rules\\" + ((ToolStripMenuItem)sender).Text.Replace("=>", "=") + ".rules";
					eLC.Export(rulesFile, eSave.FileName);
					//progressBar_progress.Style = ProgressBarStyle.Continuous;
					Cursor = Cursors.Default;
				}
				catch
				{
					MessageBox.Show("EXPORTING ERROR!\nThis error mostly occurs if selected rules fileset is invalid");
					//progressBar_progress.Style = ProgressBarStyle.Continuous;
					Cursor = Cursors.Default;
				}
			}
		}

		private void change_list(object sender, EventArgs ea)
		{
		if (comboBox_lists.SelectedIndex > -1 && EnableSelectionList)
			{
				int l = comboBox_lists.SelectedIndex;
                UpdateEquipmentTabsVisibility(l);
                if (l == 0 && list0DisplayNameCache.Count != eLC.Lists[0].elementValues.Length)
                {
                    ResetList0DisplayCache();
                    InvalidateListDisplayCache(0);
                }
                dataGridView_elems.SuspendLayout();
                dataGridView_elems.Rows.Clear();
				textBox_offset.Text = eLC.GetOffset(l);
				dataGridView_item.Rows.Clear();
                bool hasXRef = xrefs != null && l < xrefs.Length && xrefs[l] != null && xrefs[l].Length > 1;
				this.xrefItemToolStripMenuItem.Enabled = hasXRef;

                List<object[]> rows;
                if (!listDisplayRowsCache.TryGetValue(l, out rows))
                {
                    rows = BuildListDisplayRowsForList(l);
                    listDisplayRowsCache[l] = rows;
                }
                if (rows.Count > 0)
                {
                    DataGridViewRow[] dgRows = new DataGridViewRow[rows.Count];
                    for (int i = 0; i < rows.Count; i++)
                    {
                        DataGridViewRow row = (DataGridViewRow)dataGridView_elems.RowTemplate.Clone();
                        row.CreateCells(dataGridView_elems, rows[i]);
                        if (l != eLC.ConversationListIndex)
                        {
                            ApplyItemQualityColorToRow(l, i, row);
                        }
                        dgRows[i] = row;
                    }
                    dataGridView_elems.Rows.AddRange(dgRows);
                }
                dataGridView_elems.ResumeLayout();
                if (dataGridView_elems.Rows.Count > 0 && dataGridView_elems.CurrentCell == null)
                {
                    dataGridView_elems.CurrentCell = dataGridView_elems.Rows[0].Cells[0];
                }
                UpdateDescriptionTabForSelection();
                UpdatePickIconButtonState();
                PersistNavigationState();
			}
		}

		private void change_item(object sender, EventArgs ea)
		{
			if (EnableSelectionItem)
			{
				int l = comboBox_lists.SelectedIndex;
                int selectedGridRow = GetActiveGridRowIndex();
                if (selectedGridRow < 0) { return; }
                if (dataGridView_elems.CurrentCell == null || dataGridView_elems.CurrentCell.RowIndex != selectedGridRow)
                {
                    dataGridView_elems.CurrentCell = dataGridView_elems.Rows[selectedGridRow].Cells[0];
                }
                int e = ResolveElementIndexFromGridRow(l, selectedGridRow);
                if (e < 0) { return; }
                int scroll = dataGridView_item.FirstDisplayedScrollingRowIndex;
                suppressValuesUiRefresh = true;
                dataGridView_item.SuspendLayout();
                dataGridView_item.Rows.Clear();
                proctypeLocation = 0;
                proctypeLocationvak = 0;
                try
                {
                    if (l != eLC.ConversationListIndex)
                    {
                        if (e > -1)
                        {
                            dataGridView_item.Enabled = false;
                            for (int f = 0; f < eLC.Lists[l].elementValues[e].Length; f++)
                            {
                                string fieldName = eLC.Lists[l].elementFields[f];
                                if (!ShouldIncludeFieldInValuesTab(l, fieldName))
                                {
                                    continue;
                                }
                                string fieldValue = eLC.GetValue(l, e, f);
                                if (l == 0)
                                {
                                    if (string.Equals(fieldName, "name", StringComparison.OrdinalIgnoreCase))
                                    {
                                        fieldValue = GetDisplayEntryName(l, e, f);
                                    }
                                    else if (string.Equals(fieldName, "type", StringComparison.OrdinalIgnoreCase))
                                    {
                                        fieldValue = GetAddonTypeDisplayForUi(l, e, fieldValue);
                                    }
                                    else if (IsAddonParamField(fieldName))
                                    {
                                        fieldValue = FormatAddonParamValueForUi(l, e, fieldName, fieldValue);
                                    }
                                }
                                if (IsModelFieldName(fieldName))
                                {
                                    fieldValue = FormatModelPathIdDisplay(fieldValue, fieldName);
                                }
                                int rowIndex = dataGridView_item.Rows.Add(new string[] { fieldName, eLC.Lists[l].elementTypes[f], fieldValue });
                                DataGridViewRow row = dataGridView_item.Rows[rowIndex];
                                row.Tag = f;
                                row.HeaderCell.Value = f.ToString();
                                if (IsFieldInvalid(l, e, f))
                                {
                                    row.Cells[2].Style.ForeColor = Color.Red;
                                }
                                else if (IsFieldDirty(l, e, f))
                                {
                                    row.Cells[2].Style.ForeColor = Color.DeepSkyBlue;
                                }
                            }
                            dataGridView_item.Enabled = true;
                            dataGridView_item.ResumeLayout();
                        }
                    }
                    else
                    {
                        if (conversationList == null)
                        {
                            return;
                        }
                        if (e > -1)
                        {
                            dataGridView_item.Rows.Add(new string[] { "id_talk", "int32", conversationList.talk_procs[e].id_talk.ToString() });
                            dataGridView_item.Rows.Add(new string[] { "text", "wstring:128", conversationList.talk_procs[e].GetText() });
                            for (int q = 0; q < conversationList.talk_procs[e].num_window; q++)
                            {
                                dataGridView_item.Rows.Add(new string[] { "window_" + q + "_id", "int32", conversationList.talk_procs[e].windows[q].id.ToString() });
                                dataGridView_item.Rows.Add(new string[] { "window_" + q + "_id_parent", "int32", conversationList.talk_procs[e].windows[q].id_parent.ToString() });
                                dataGridView_item.Rows.Add(new string[] { "window_" + q + "_talk_text", "wstring:" + conversationList.talk_procs[e].windows[q].talk_text_len, conversationList.talk_procs[e].windows[q].GetText() });
                                for (int c = 0; c < conversationList.talk_procs[e].windows[q].num_option; c++)
                                {
                                    dataGridView_item.Rows.Add(new string[] { "window_" + q + "_option_" + c + "_param", "int32", conversationList.talk_procs[e].windows[q].options[c].param.ToString() });
                                    dataGridView_item.Rows.Add(new string[] { "window_" + q + "_option_" + c + "_text", "wstring:128", conversationList.talk_procs[e].windows[q].options[c].GetText() });
                                    dataGridView_item.Rows.Add(new string[] { "window_" + q + "_option_" + c + "_id", "int32", conversationList.talk_procs[e].windows[q].options[c].id.ToString() });
                                }
                            }
                        }
                    }
                    if (scroll > -1)
                    {
                        dataGridView_item.FirstDisplayedScrollingRowIndex = scroll;
                    }
                }
                catch { }
                finally
                {
                    suppressValuesUiRefresh = false;
                }
                UpdateDescriptionTabForSelection();
                UpdatePickIconButtonState();
                PersistNavigationState();
			}
		}

		private void change_offset(object sender, EventArgs e)
		{
			eLC.SetOffset(comboBox_lists.SelectedIndex, textBox_offset.Text);
            hasUnsavedChanges = true;
		}

		private void change_value(object sender, DataGridViewCellEventArgs ea)
		{
            if (eLC == null || ea.ColumnIndex < 0 || ea.RowIndex < 0)
            {
                return;
            }

            int l = comboBox_lists.SelectedIndex;
            int gridRow = ea.RowIndex;
            int f = GetFieldIndexForValueRow(gridRow);
            if (f < 0)
            {
                return;
            }
            int currentGridRow = GetActiveGridRowIndex();
            if (currentGridRow < 0)
            {
                return;
            }
            if (dataGridView_elems.CurrentCell == null || dataGridView_elems.CurrentCell.RowIndex != currentGridRow)
            {
                dataGridView_elems.CurrentCell = dataGridView_elems.Rows[currentGridRow].Cells[0];
            }
            int r = ResolveElementIndexFromGridRow(l, currentGridRow);
            if (l < 0)
            {
                return;
            }
            if (r < 0)
            {
                return;
            }

            try
            {
                if (l != eLC.ConversationListIndex)
                {
                    EnableSelectionItem = false;
                    int[] selGridIndices = gridSelectedIndices(dataGridView_elems);
                    if (selGridIndices.Length == 0)
                    {
                        selGridIndices = new int[] { currentGridRow };
                    }

                    int[] selElementIndices = new int[selGridIndices.Length];
                    for (int i = 0; i < selGridIndices.Length; i++)
                    {
                        selElementIndices[i] = ResolveElementIndexFromGridRow(l, selGridIndices[i]);
                        if (selElementIndices[i] < 0)
                        {
                            selElementIndices[i] = selGridIndices[i];
                        }
                    }

                    string editedField = dataGridView_item.Rows[gridRow].Cells[0].Value.ToString();
                    string fieldType = eLC.Lists[l].elementTypes[f];
                    string valueToSet = Convert.ToString(dataGridView_item.Rows[gridRow].Cells[2].Value);
                    bool isIdEdit = string.Equals(editedField, "id", StringComparison.OrdinalIgnoreCase);
                    bool isModelField = IsModelFieldName(editedField);

                    if (l == 0 && IsAddonParamField(editedField))
                    {
                        string normalized;
                        if (TryNormalizeAddonParamValueForStorage(l, r, editedField, valueToSet, out normalized))
                        {
                            valueToSet = normalized;
                        }
                    }

                    if (isModelField)
                    {
                        int modelPathId;
                        if (!TryExtractPathId(valueToSet, out modelPathId))
                        {
                            MarkFieldInvalid(l, r, f);
                            dataGridView_item.Rows[gridRow].Cells[2].Style.ForeColor = Color.Red;
                            MessageBox.Show("Invalid model PathID value.");
                            return;
                        }
                        valueToSet = modelPathId.ToString();
                    }

                    if (isIdEdit)
                    {
                        int desiredId;
                        if (!int.TryParse(valueToSet, out desiredId))
                        {
                            MarkFieldInvalid(l, r, f);
                            dataGridView_item.Rows[gridRow].Cells[2].Style.ForeColor = Color.Red;
                            MessageBox.Show("Invalid ID value.");
                            return;
                        }

                        HashSet<int> usedIds = BuildUsedIds(l, f);
                        for (int i = 0; i < selElementIndices.Length; i++)
                        {
                            int curId;
                            int.TryParse(eLC.GetValue(l, selElementIndices[i], f), out curId);
                            usedIds.Remove(curId);
                        }
                        if (usedIds.Contains(desiredId))
                        {
                            MarkFieldInvalid(l, r, f);
                            dataGridView_item.Rows[gridRow].Cells[2].Style.ForeColor = Color.Red;
                            MessageBox.Show("ID already exists in this list.");
                            return;
                        }
                    }
                    else if (!IsValueCompatibleWithFieldType(fieldType, valueToSet))
                    {
                        MarkFieldInvalid(l, r, f);
                        dataGridView_item.Rows[gridRow].Cells[2].Style.ForeColor = Color.Red;
                        MessageBox.Show("Invalid value for field type " + fieldType + ".");
                        return;
                    }

                    if (l == 0 &&
                        (string.Equals(editedField, "id", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(editedField, "name", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(editedField, "type", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(editedField, "num_params", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(editedField, "param1", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(editedField, "param2", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(editedField, "param3", StringComparison.OrdinalIgnoreCase)))
                    {
                        ResetList0DisplayCache();
                    }

                    for (int e = 0; e < selElementIndices.Length; e++)
                    {
                        int oldId = 0;
                        int newId = 0;
                        if (isIdEdit)
                        {
                            int.TryParse(eLC.GetValue(l, selElementIndices[e], f), out oldId);
                            int.TryParse(valueToSet, out newId);
                        }
                        eLC.SetValue(l, selElementIndices[e], f, valueToSet);
                        if (isIdEdit && oldId > 0 && newId > 0 && oldId != newId)
                        {
                            RemapDescriptionIdIfNeeded(oldId, newId);
                        }
                        MarkRowDirty(l, selElementIndices[e]);
                        MarkFieldDirty(l, selElementIndices[e], f);
                        ClearFieldInvalid(l, selElementIndices[e], f);
                    }

                    dataGridView_item.Rows[gridRow].Cells[2].Style.ForeColor = Color.DeepSkyBlue;
                    if (isModelField)
                    {
                        dataGridView_item.Rows[gridRow].Cells[2].Value = FormatModelPathIdDisplay(valueToSet, editedField);
                    }
                    else if (l == 0 && IsAddonParamField(editedField))
                    {
                        dataGridView_item.Rows[gridRow].Cells[2].Value = FormatAddonParamValueForUi(l, r, editedField, valueToSet);
                    }

                    int namePosForStar = -1;
                    for (int i = 0; i < eLC.Lists[l].elementFields.Length; i++)
                    {
                        if (string.Equals(eLC.Lists[l].elementFields[i], "name", StringComparison.OrdinalIgnoreCase))
                        {
                            namePosForStar = i;
                            break;
                        }
                    }
                    if (namePosForStar < 0) { namePosForStar = 0; }
                    for (int e = 0; e < selGridIndices.Length; e++)
                    {
                        dataGridView_elems.Rows[selGridIndices[e]].Cells[2].Value = ComposeListDisplayName(l, selElementIndices[e], namePosForStar);
                    }

                    if (string.Equals(editedField, "id", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(editedField, "name", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(editedField, "file_icon", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(editedField, "file_icon1", StringComparison.OrdinalIgnoreCase))
                    {
                        int pos = -1;
                        int pos2 = -1;
                        for (int i = 0; i < eLC.Lists[l].elementFields.Length; i++)
                        {
                            if (string.Equals(eLC.Lists[l].elementFields[i], "name", StringComparison.OrdinalIgnoreCase))
                            {
                                pos = i;
                            }
                            if (string.Equals(eLC.Lists[l].elementFields[i], "file_icon", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(eLC.Lists[l].elementFields[i], "file_icon1", StringComparison.OrdinalIgnoreCase))
                            {
                                pos2 = i;
                            }
                        }
                        if (pos < 0) { pos = 0; }

                        for (int e = 0; e < selGridIndices.Length; e++)
                        {
                            Bitmap img = Properties.Resources.blank;
                            if (pos2 > -1)
                            {
                                string path = ResolveIconKeyForList(l, eLC.GetValue(l, selElementIndices[e], pos2));
                                if (database.sourceBitmap != null && database.ContainsKey(path))
                                {
                                    img = database.images(path);
                                }
                            }
                            dataGridView_elems.Rows[selGridIndices[e]].Cells[0].Value = eLC.GetValue(l, selElementIndices[e], 0);
                            dataGridView_elems.Rows[selGridIndices[e]].Cells[1].Value = img;
                            dataGridView_elems.Rows[selGridIndices[e]].Cells[2].Value = ComposeListDisplayName(l, selElementIndices[e], pos);
                        }
                    }

                    if (IsItemQualityFieldName(editedField))
                    {
                        for (int e = 0; e < selGridIndices.Length; e++)
                        {
                            DataGridViewRow row = dataGridView_elems.Rows[selGridIndices[e]];
                            ApplyItemQualityColorToRow(l, selElementIndices[e], row);
                        }
                    }
                }
                else
                {
                    if (conversationList == null)
                    {
                        return;
                    }
                    string fieldName = dataGridView_item[0, ea.RowIndex].Value.ToString();
                    if (fieldName == "id_talk")
                    {
                        conversationList.talk_procs[r].id_talk = Convert.ToInt32(dataGridView_item.CurrentCell.Value);
                        hasUnsavedChanges = true;
                        return;
                    }
                    if (fieldName == "text")
                    {
                        conversationList.talk_procs[r].SetText(dataGridView_item.CurrentCell.Value.ToString());
                        hasUnsavedChanges = true;
                        return;
                    }
                    if (fieldName.StartsWith("window_") && fieldName.EndsWith("_id"))
                    {
                        int q = Convert.ToInt32(fieldName.Replace("window_", "").Replace("_id", ""));
                        conversationList.talk_procs[r].windows[q].id = Convert.ToInt32(dataGridView_item.CurrentCell.Value);
                        hasUnsavedChanges = true;
                        return;
                    }
                    if (fieldName.StartsWith("window_") && fieldName.Contains("option_") && fieldName.EndsWith("_param"))
                    {
                        string[] s = fieldName.Replace("window_", "").Replace("_option_", ";").Replace("_param", "").Split(new char[] { ';' });
                        int q = Convert.ToInt32(s[0]);
                        int c = Convert.ToInt32(s[1]);
                        conversationList.talk_procs[r].windows[q].options[c].param = Convert.ToInt32(dataGridView_item.CurrentCell.Value);
                        hasUnsavedChanges = true;
                        return;
                    }
                    if (fieldName.StartsWith("window_") && fieldName.Contains("option_") && fieldName.EndsWith("_text"))
                    {
                        string[] s = fieldName.Replace("window_", "").Replace("_option_", ";").Replace("_text", "").Split(new char[] { ';' });
                        int q = Convert.ToInt32(s[0]);
                        int c = Convert.ToInt32(s[1]);
                        conversationList.talk_procs[r].windows[q].options[c].SetText(dataGridView_item.CurrentCell.Value.ToString());
                        hasUnsavedChanges = true;
                        return;
                    }
                    if (fieldName.StartsWith("window_") && fieldName.Contains("option_") && fieldName.EndsWith("_id"))
                    {
                        string[] s = fieldName.Replace("window_", "").Replace("_option_", ";").Replace("_id", "").Split(new char[] { ';' });
                        int q = Convert.ToInt32(s[0]);
                        int c = Convert.ToInt32(s[1]);
                        conversationList.talk_procs[r].windows[q].options[c].id = Convert.ToInt32(dataGridView_item.CurrentCell.Value);
                        hasUnsavedChanges = true;
                        return;
                    }
                    if (fieldName.StartsWith("window_") && fieldName.EndsWith("_id_parent"))
                    {
                        int q = Convert.ToInt32(fieldName.Replace("window_", "").Replace("_id_parent", ""));
                        conversationList.talk_procs[r].windows[q].id_parent = Convert.ToInt32(dataGridView_item.CurrentCell.Value);
                        hasUnsavedChanges = true;
                        return;
                    }
                    if (fieldName.StartsWith("window_") && fieldName.EndsWith("_talk_text"))
                    {
                        int q = Convert.ToInt32(fieldName.Replace("window_", "").Replace("_talk_text", ""));
                        conversationList.talk_procs[r].windows[q].SetText(dataGridView_item.CurrentCell.Value.ToString());
                        dataGridView_item[1, ea.RowIndex].Value = "wstring:" + conversationList.talk_procs[r].windows[q].talk_text_len;
                        hasUnsavedChanges = true;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("change_value", ex);
                MessageBox.Show("CHANGING ERROR!\nFailed changing value, this value seems to be invalid.");
            }
            finally
            {
                EnableSelectionItem = true;
            }
		}

		private void click_search(object sender, EventArgs ea)
		{
            HideSearchSuggestions();
			string id = textBox_search.Text;
			if (!checkBox_SearchMatchCase.Checked)
				id = id.ToLower();
			string value = "";
			int l = comboBox_lists.SelectedIndex;
			if (l < 0) { l = 0; }
			int f = 0;
			if (dataGridView_item.CurrentCell != null)
				f = dataGridView_item.CurrentCell.RowIndex + 1;
			if (f < 0) { f = 0; }
			if (eLC != null && eLC.Lists != null)
			{
				EnableSelectionItem = false;
				int ftmp = f;
				if (checkBox_SearchAll.Checked)
				{
                    int e = dataGridView_elems.CurrentCell.RowIndex;
					if (e < 0) { e = 0; }
					int etmp = e;
					for (int lf = l; lf < eLC.Lists.Length; lf++)
					{
						for (int ef = etmp; ef < eLC.Lists[lf].elementValues.Length; ef++)
						{
							for (int ff = ftmp; ff < eLC.Lists[lf].elementFields.Length; ff++)
							{
								if (checkBox_SearchExactMatching.Checked)
								{
									if (eLC.GetValue(lf, ef, ff) == id)
									{
										comboBox_lists.SelectedIndex = lf;
                                        dataGridView_elems.ClearSelection();
										EnableSelectionItem = true;
                                        dataGridView_elems.CurrentCell = dataGridView_elems[0, ef];
                                        dataGridView_elems.Rows[ef].Selected = true;
                                        dataGridView_elems.FirstDisplayedScrollingRowIndex = ef;
                                        EnsureEquipmentTabForField(lf, eLC.Lists[lf].elementFields[ff]);
                                        int valueRow = FindValueRowByFieldIndex(ff);
                                        if (valueRow > -1)
                                        {
                                            dataGridView_item.CurrentCell = dataGridView_item.Rows[valueRow].Cells[2];
                                        }
										return;
									}
								}
								else
								{
									value = eLC.GetValue(lf, ef, ff);
									if (!checkBox_SearchMatchCase.Checked)
										value = value.ToLower();
									if (value.Contains(id))
									{
										comboBox_lists.SelectedIndex = lf;
                                        dataGridView_elems.ClearSelection();
                                        EnableSelectionItem = true;
                                        dataGridView_elems.CurrentCell = dataGridView_elems[0, ef];
                                        dataGridView_elems.Rows[ef].Selected = true;
                                        dataGridView_elems.FirstDisplayedScrollingRowIndex = ef;
                                        EnsureEquipmentTabForField(lf, eLC.Lists[lf].elementFields[ff]);
                                        int valueRow = FindValueRowByFieldIndex(ff);
                                        if (valueRow > -1)
                                        {
                                            dataGridView_item.CurrentCell = dataGridView_item.Rows[valueRow].Cells[2];
                                        }
										return;
									}
								}
							}
							ftmp = 0;
						}
						etmp = 0;
					}
					etmp = e;
					ftmp = f;
					for (int lf = 0; lf < eLC.Lists.Length && lf <= l; lf++)
					{
						for (int ef = 0; ef < eLC.Lists[lf].elementValues.Length; ef++)
						{
							for (int ff = 0; ff < eLC.Lists[lf].elementFields.Length; ff++)
							{
								if (checkBox_SearchExactMatching.Checked)
								{
									if (eLC.GetValue(lf, ef, ff) == id)
									{
										comboBox_lists.SelectedIndex = lf;
                                        dataGridView_elems.ClearSelection();
                                        EnableSelectionItem = true;
                                        dataGridView_elems.CurrentCell = dataGridView_elems[0, ef];
                                        dataGridView_elems.Rows[ef].Selected = true;
                                        dataGridView_elems.FirstDisplayedScrollingRowIndex = ef;
                                        EnsureEquipmentTabForField(lf, eLC.Lists[lf].elementFields[ff]);
                                        int valueRow = FindValueRowByFieldIndex(ff);
                                        if (valueRow > -1)
                                        {
                                            dataGridView_item.CurrentCell = dataGridView_item.Rows[valueRow].Cells[2];
                                        }
										return;
									}
								}
								else
								{
									value = eLC.GetValue(lf, ef, ff);
									if (!checkBox_SearchMatchCase.Checked)
										value = value.ToLower();
									if (value.Contains(id))
									{
										comboBox_lists.SelectedIndex = lf;
                                        dataGridView_elems.ClearSelection();
                                        EnableSelectionItem = true;
                                        dataGridView_elems.CurrentCell = dataGridView_elems[0, ef];
                                        dataGridView_elems.Rows[ef].Selected = true;
                                        dataGridView_elems.FirstDisplayedScrollingRowIndex = ef;
                                        EnsureEquipmentTabForField(lf, eLC.Lists[lf].elementFields[ff]);
                                        int valueRow = FindValueRowByFieldIndex(ff);
                                        if (valueRow > -1)
                                        {
                                            dataGridView_item.CurrentCell = dataGridView_item.Rows[valueRow].Cells[2];
                                        }
										return;
									}
								}
							}
							ftmp = 0;
						}
						etmp = 0;
					}
				}
				else
				{
                    int e = dataGridView_elems.CurrentCell.RowIndex + 1;
                    if (e < 0) { e = 0; }
					int etmp = e;
					for (int lf = l; lf < eLC.Lists.Length; lf++)
					{
						int pos = 0;
						for (int i = 0; i < eLC.Lists[lf].elementFields.Length; i++)
						{
							if (string.Equals(eLC.Lists[lf].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
							{
								pos = i;
								break;
							}
						}
						for (int ef = etmp; ef < eLC.Lists[lf].elementValues.Length; ef++)
						{
							if (checkBox_SearchExactMatching.Checked)
							{
								if (id == eLC.GetValue(lf, ef, 0) || eLC.GetValue(lf, ef, pos) == id)
								{
									comboBox_lists.SelectedIndex = lf;
                                    dataGridView_elems.ClearSelection();
                                    EnableSelectionItem = true;
                                    dataGridView_elems.CurrentCell = dataGridView_elems[0, ef];
                                    dataGridView_elems.Rows[ef].Selected = true;
                                    dataGridView_elems.FirstDisplayedScrollingRowIndex = ef;
                                    return;
								}
							}
							else
							{
								value = eLC.GetValue(lf, ef, pos);
								if (!checkBox_SearchMatchCase.Checked)
									value = value.ToLower();
								if (id == eLC.GetValue(lf, ef, 0) || value.Contains(id))
								{
									comboBox_lists.SelectedIndex = lf;
                                    dataGridView_elems.ClearSelection();
                                    EnableSelectionItem = true;
                                    dataGridView_elems.CurrentCell = dataGridView_elems[0,ef];
                                    dataGridView_elems.Rows[ef].Selected = true;
                                    dataGridView_elems.FirstDisplayedScrollingRowIndex = ef;
                                    return;
								}
							}
						}
						etmp = 0;
					}
					etmp = e;
					for (int lf = 0; lf < eLC.Lists.Length && lf <= l; lf++)
					{
						int pos = 0;
						for (int i = 0; i < eLC.Lists[lf].elementFields.Length; i++)
						{
							if (string.Equals(eLC.Lists[lf].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
							{
								pos = i;
								break;
							}
						}
						for (int ef = 0; ef < eLC.Lists[lf].elementValues.Length; ef++)
						{
							if (checkBox_SearchExactMatching.Checked)
							{
								if (id == eLC.GetValue(lf, ef, 0) || eLC.GetValue(lf, ef, pos) == id)
								{
									comboBox_lists.SelectedIndex = lf;
                                    dataGridView_elems.ClearSelection();
                                    EnableSelectionItem = true;
                                    dataGridView_elems.CurrentCell = dataGridView_elems[0, ef];
                                    dataGridView_elems.Rows[ef].Selected = true;
                                    dataGridView_elems.FirstDisplayedScrollingRowIndex = ef;
                                    return;
								}
							}
							else
							{
								value = eLC.GetValue(lf, ef, pos);
								if (!checkBox_SearchMatchCase.Checked)
									value = value.ToLower();
								if (id == eLC.GetValue(lf, ef, 0) || value.Contains(id))
								{
									comboBox_lists.SelectedIndex = lf;
                                    dataGridView_elems.ClearSelection();
                                    EnableSelectionItem = true;
                                    dataGridView_elems.CurrentCell = dataGridView_elems[0, ef];
                                    dataGridView_elems.Rows[ef].Selected = true;
                                    dataGridView_elems.FirstDisplayedScrollingRowIndex = ef;
                                    return;
								}
							}
						}
						etmp = 0;
					}
				}
				EnableSelectionItem = true;
				MessageBox.Show("Search reached End without Result!");
			}
		}

		private void click_deleteItem(object sender, EventArgs ea)
		{
			int l = comboBox_lists.SelectedIndex;
            int[] selIndices = gridSelectedIndices(dataGridView_elems);
            if (dataGridView_elems.RowCount > 0 && selIndices.Length > 0)
			{
				if (selIndices.Length < dataGridView_elems.RowCount)
				{
					if (l != eLC.ConversationListIndex)
					{
						EnableSelectionList = false;
						EnableSelectionItem = false;
						for (int i = selIndices.Length - 1; i > -1; i--)
						{
							eLC.Lists[l].RemoveItem(selIndices[i]);
                            dataGridView_elems.Rows.RemoveAt(selIndices[i]);
						}
                        hasUnsavedChanges = true;
						comboBox_lists.Items[l] = "[" + l + "]: " + eLC.Lists[l].listName + " (" + eLC.Lists[l].elementValues.Length + ")";
						EnableSelectionList = true;
						EnableSelectionItem = true;
                        change_item(null, null);
					}
					else
					{
						MessageBox.Show("Operation not supported in List " + eLC.ConversationListIndex.ToString());
					}
				}
				else
				{
					MessageBox.Show("Cannot delete all items in list!");
				}
			}
		}

		private void click_cloneItem(object sender, EventArgs ea)
		{
			int l = comboBox_lists.SelectedIndex;
			if (dataGridView_elems.RowCount > 0)
			{
				if (l != eLC.ConversationListIndex)
				{
                    int[] selIndices = gridSelectedIndices(dataGridView_elems);
                    if (selIndices.Length == 0 && dataGridView_elems.CurrentCell != null)
                    {
                        selIndices = new int[] { dataGridView_elems.CurrentCell.RowIndex };
                    }
                    if (selIndices.Length == 0)
                    {
                        return;
                    }
                    EnableSelectionList = false;
					EnableSelectionItem = false;
                    int idFieldIndex = GetIdFieldIndex(l);
                    HashSet<int> usedIds = BuildUsedIds(l, idFieldIndex);
                    List<int> newRows = new List<int>();
					for (int i = 0; i < selIndices.Length; i++)
					{
                        int sourceRow = selIndices[i];
                        object[] o = CloneElementValuesDeep(eLC.Lists[l].elementValues[sourceRow]);
                        eLC.Lists[l].AddItem(o);
                        int newRow = eLC.Lists[l].elementValues.Length - 1;
                        newRows.Add(newRow);

                        if (idFieldIndex > -1)
                        {
                            int sourceId;
                            if (!int.TryParse(eLC.GetValue(l, sourceRow, idFieldIndex), out sourceId))
                            {
                                sourceId = 1;
                            }
                            int newId = GetNextUniqueId(usedIds, sourceId + 1);
                            eLC.SetValue(l, newRow, idFieldIndex, newId.ToString());
                        }
                        MarkRowDirty(l, newRow);
                    }
                    EnableSelectionList = true;
                    EnableSelectionItem = true;

                    change_list(null, null);
                    comboBox_lists.Items[l] = "[" + l + "] " + GetFriendlyListName(eLC.Lists[l].listName) + " (" + eLC.Lists[l].elementValues.Length + ")";
                    dataGridView_elems.ClearSelection();
                    for (int i = 0; i < newRows.Count; i++)
                    {
                        int rowIndex = newRows[i];
                        if (rowIndex > -1 && rowIndex < dataGridView_elems.Rows.Count)
                        {
                            dataGridView_elems.Rows[rowIndex].Selected = true;
                            dataGridView_elems.CurrentCell = dataGridView_elems[0, rowIndex];
                        }
                    }
                    if (dataGridView_elems.CurrentCell != null)
                    {
                        dataGridView_elems.FirstDisplayedScrollingRowIndex = dataGridView_elems.CurrentCell.RowIndex;
                    }
					change_item(null, null);
				}
				else
				{
					MessageBox.Show("Operation not supported in List " + eLC.ConversationListIndex.ToString());
				}
			}
		}

		private void click_logicReplace(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				(new ReplaceWindow(eLC)).ShowDialog();
				int itemIndex = dataGridView_elems.CurrentCell.RowIndex;
				change_list(null, null);
				dataGridView_elems.Rows[itemIndex].Selected = true;
			}
			else
			{
				MessageBox.Show("No File Loaded!");
			}
		}

		private void click_fieldReplace(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				(new FieldReplaceWindow(eLC, conversationList, ref cpb2)).ShowDialog();
			}
			else
			{
				MessageBox.Show("No File Loaded!");
			}
		}

		private void click_info(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				//(gcnew InfoWindow(eLC))->ShowDialog();
			}
			else
			{
				MessageBox.Show("No File Loaded!");
			}
		}

		private void click_version(object sender, EventArgs e)
		{
			OpenFileDialog eLoad = new OpenFileDialog();
			eLoad.Filter = "Elements File (*.data)|*.data|All Files (*.*)|*.*";
			if (eLoad.ShowDialog() == DialogResult.OK && File.Exists(eLoad.FileName))
			{
				FileStream fs = File.OpenRead(eLoad.FileName);
				BinaryReader br = new BinaryReader(fs);
				short version = br.ReadInt16();
				short signature = br.ReadInt16();
				int timestamp = 0;
				string stimestamp = "";
				if (version >= 10)
					timestamp = br.ReadInt32();
				if (timestamp != 0)
					stimestamp = "\nTimestamp: " + timestamp_to_string((uint)timestamp);
				br.Close();
				fs.Close();

				MessageBox.Show("File: " + eLoad.FileName + "\n\nVersion: " + version.ToString() + "\nSignature: " + signature.ToString() + stimestamp);
			}
			else
			{
				//MessageBox::Show("No File!");
			}
		}

		private void click_config(object sender, EventArgs e)
		{
			(new ConfigWindow()).Show();
		}

		private void click_exportItem(object sender, EventArgs ea)
		{
			if (dataGridView_elems.RowCount > 0)
			{
				int l = comboBox_lists.SelectedIndex;
                int[] selIndices = gridSelectedIndices(dataGridView_elems);
                if (l != eLC.ConversationListIndex)
				{
                    if (dataGridView_elems.RowCount > 0 && selIndices.Length > 0)
                    {
                        FolderBrowserDialog eSave = new FolderBrowserDialog();
                        if (eSave.ShowDialog() == DialogResult.OK && Directory.Exists(eSave.SelectedPath))
                        {
                            try
                            {
                                Cursor = Cursors.AppStarting;
                                //progressBar_progress.Style = ProgressBarStyle.Continuous;
                                cpb2.Maximum = selIndices.Length;
                                for (int i = 0; i < selIndices.Length; i++)
                                {
                                    eLC.Lists[l].ExportItem(eSave.SelectedPath + "\\" + selIndices[i], selIndices[i]);
                                    cpb2.Value++;
                                }
                                Cursor = Cursors.Default;
                                MessageBox.Show("Export complete!");
                            }
                            catch
                            {
                                MessageBox.Show("EXPORT ERROR!\nExporting item to unicode text file failed!");
                                Cursor = Cursors.Default;
                            }
                            cpb2.Value = 0;
                            //progressBar_progress.Style = ProgressBarStyle.Continuous;
                        }
                    }
				}
				else
				{
					MessageBox.Show("Operation not supported in List " + eLC.ConversationListIndex.ToString());
				}
			}
		}

		private void click_importItem(object sender, EventArgs ea)
		{
			int l = comboBox_lists.SelectedIndex;
			int e = dataGridView_elems.CurrentRow.Index;
			if (l != eLC.ConversationListIndex)
			{
				if (l > -1 && e > -1)
				{
					OpenFileDialog eLoad = new OpenFileDialog();
					eLoad.Filter = "All Files (*.*)|*.*";
					if (eLoad.ShowDialog() == DialogResult.OK && File.Exists(eLoad.FileName))
					{
						try
						{
							Cursor = Cursors.AppStarting;
							eLC.Lists[l].ImportItem(eLoad.FileName, e);
                            MarkRowDirty(l, e);
                            hasUnsavedChanges = true;
							change_list(null, null);
							dataGridView_elems.Rows[e].Selected = true;
							Cursor = Cursors.Default;
						}
						catch
						{
							MessageBox.Show("IMPORT ERROR!\nCheck if the item version matches the elements.data version and is imported to the correct list!");
							Cursor = Cursors.Default;
						}
					}
				}
			}
			else
			{
				MessageBox.Show("Operation not supported in List " + eLC.ConversationListIndex.ToString());
			}
		}

		private void click_addItems(object sender, EventArgs ea)
		{
			int l = comboBox_lists.SelectedIndex;
			if (dataGridView_elems.RowCount >= 1)
			{
				if (l != eLC.ConversationListIndex)
				{
					string[] fileNames = null;
					OpenFileDialog eLoad = new OpenFileDialog();
					eLoad.Filter = "All Files (*.*)|*.*";
					eLoad.Multiselect = true;
					if (eLoad.ShowDialog() == DialogResult.OK && File.Exists(eLoad.FileName))
					{
						EnableSelectionList = false;
						EnableSelectionItem = false;
						fileNames = eLoad.FileNames;
						try
						{
							Cursor = Cursors.AppStarting;
                            //progressBar_progress.Style = ProgressBarStyle.Continuous;
                            cpb2.Maximum = fileNames.Length;
							int pos = -1;
                            int pos2 = -1;
                            for (int i = 0; i < eLC.Lists[l].elementFields.Length; i++)
							{
                                if (string.Equals(eLC.Lists[l].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
                                {
                                    pos = i;
                                }
                                if (string.Equals(eLC.Lists[l].elementFields[i], "file_icon", StringComparison.OrdinalIgnoreCase) || string.Equals(eLC.Lists[l].elementFields[i], "file_icon1", StringComparison.OrdinalIgnoreCase))
                                {
                                    pos2 = i;
                                }
                                if (pos != -1 && pos2 != -1)
                                {
                                    break;
                                }
                            }
							for (int i = 0; i < fileNames.Length; i++)
							{
								int e = dataGridView_elems.RowCount - 1;
								object[] o = new object[eLC.Lists[l].elementValues[e].Length];
								eLC.Lists[l].elementValues[e].CopyTo(o, 0);
								eLC.Lists[l].AddItem(o);
								eLC.Lists[l].ImportItem(fileNames[i], e + 1);
                                MarkRowDirty(l, e + 1);
                                hasUnsavedChanges = true;
								if (string.Equals(eLC.Lists[l].elementFields[0], "ID", StringComparison.OrdinalIgnoreCase))
								{
                                    Bitmap img = Properties.Resources.blank;
                                    string path = ResolveIconKeyForList(l, eLC.GetValue(l, e, pos2));
                                    if (database.sourceBitmap != null && database.ContainsKey(path))
                                    {
                                        if (database.ContainsKey(path))
                                        {
                                            img = database.images(path);
                                        }
                                    }
                                    dataGridView_elems.Rows.Add(new object[] { eLC.GetValue(l, e, 0), img, eLC.GetValue(l, e, pos) });
                                }
								else
								{
                                    dataGridView_elems.Rows.Add(new object[] { 0, Properties.Resources.blank, eLC.GetValue(l, e, pos) });
                                }
                                cpb2.Value++;
							}
							Cursor = Cursors.Default;
						}
						catch
						{
							MessageBox.Show("IMPORT ERROR!\nCheck if the item version matches the elements.data version and is imported to the correct list!");
							Cursor = Cursors.Default;
						}
						comboBox_lists.Items[l] = "[" + l + "]: " + eLC.Lists[l].listName + " (" + eLC.Lists[l].elementValues.Length + ")";
                        cpb2.Value = 0;
						//progressBar_progress.Style = ProgressBarStyle.Continuous;
                        dataGridView_elems.ClearSelection();
						EnableSelectionList = true;
						EnableSelectionItem = true;
						dataGridView_elems.Rows[dataGridView_elems.RowCount - 1].Selected = true;
                        dataGridView_elems.FirstDisplayedScrollingRowIndex = dataGridView_elems.RowCount - 1;
                        change_item(null, null);
					}
				}
				else
				{
					MessageBox.Show("Operation not supported in List " + eLC.ConversationListIndex.ToString());
				}
			}
		}

		private void click_npcExport(object sender, EventArgs e)
		{
			SaveFileDialog save = new SaveFileDialog();
			save.InitialDirectory = Environment.CurrentDirectory;
			save.Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*";
			if (save.ShowDialog() == DialogResult.OK && save.FileName != "")
			{
				try
				{
					Cursor = Cursors.AppStarting;
					//progressBar_progress.Style = ProgressBarStyle.Marquee;

					StreamWriter sw = new StreamWriter(save.FileName, false, Encoding.Unicode);

					for (int i = 0; i < eLC.Lists[38].elementValues.Length; i++)
					{
						sw.WriteLine(eLC.GetValue(38, i, 0) + "\t" + eLC.GetValue(38, i, 2));
					}

					for (int i = 0; i < eLC.Lists[57].elementValues.Length; i++)
					{
						sw.WriteLine(eLC.GetValue(57, i, 0) + "\t" + eLC.GetValue(57, i, 1));
					}

					sw.Close();

					//progressBar_progress.Style = ProgressBarStyle.Continuous;
					Cursor = Cursors.Default;
				}
				catch
				{
					MessageBox.Show("SAVING ERROR!");
					//progressBar_progress.Style = ProgressBarStyle.Continuous;
					Cursor = Cursors.Default;
				}
			}
		}

		private void click_joinEL(object sender, EventArgs e)
		{
			JoinWindow eJoin = new JoinWindow();
			if (eJoin.ShowDialog() == DialogResult.OK)
			{
				if (File.Exists(eJoin.FileName))
				{
					if (eJoin.LogDirectory == "" || !Directory.Exists(eJoin.LogDirectory))
					{
						eJoin.LogDirectory = eJoin.FileName + ".JOIN";
						Directory.CreateDirectory(eJoin.LogDirectory);
					}

					if (eJoin.BackupNew)
					{
						Directory.CreateDirectory(eJoin.LogDirectory + "\\added.backup");
					}
					if (eJoin.BackupChanged)
					{
						Directory.CreateDirectory(eJoin.LogDirectory + "\\replaced.backup");
					}
					if (eJoin.BackupMissing)
					{
						Directory.CreateDirectory(eJoin.LogDirectory + "\\removed.backup");
					}

					try
					{
						Cursor = Cursors.WaitCursor;
						//progressBar_progress.Style = ProgressBarStyle.Continuous;
						eListCollection neLC = new eListCollection(eJoin.FileName, ref cpb2);
						if (eLC.ConfigFile != neLC.ConfigFile)
						{
							MessageBox.Show("You're going to join two different element.data versions. The merged file will become invalid!", " WARNING");
						}
						if (eLC.ConversationListIndex > -1 && neLC.Lists.Length > eLC.ConversationListIndex)
						{
                            try
                            {
							    conversationList = new eListConversation((byte[])neLC.Lists[eLC.ConversationListIndex].elementValues[0][0]);
                            }
                            catch
                            {
                                conversationList = null;
                            }
						}
						StreamWriter sw = new StreamWriter(eJoin.LogDirectory + "\\LOG.TXT", false, Encoding.Unicode);

						ArrayList report;
						for (int l = 0; l < eLC.Lists.Length; l++)
						{
							if (l != eLC.ConversationListIndex)
							{
								report = eLC.Lists[l].JoinElements(neLC.Lists[l], l, eJoin.AddNew, eJoin.BackupNew, eJoin.ReplaceChanged, eJoin.BackupChanged, eJoin.RemoveMissing, eJoin.BackupMissing, eJoin.LogDirectory + "\\added.backup", eJoin.LogDirectory + "\\replaced.backup", eJoin.LogDirectory + "\\removed.backup");
								report.Sort();
								if (report.Count > 0)
								{
									sw.WriteLine("List " + l + ": " + report.Count + " Item(s) Affected");
									sw.WriteLine();

									for (int n = 0; n < report.Count; n++)
									{
										sw.WriteLine((string)report[n]);
									}

									sw.WriteLine();
								}

								comboBox_lists.Items[l] = "[" + l + "]: " + eLC.Lists[l].listName + " (" + eLC.Lists[l].elementValues.Length + ")";
							}
						}
						
						sw.Close();

						if (comboBox_lists.SelectedIndex > -1)
						{
							change_list(null, null);
						}

                        cpb2.Value = 0;
                        //cpb2.Style = ProgressBarStyle.Continuous;
						Cursor = Cursors.Default;
					}
					catch
					{
						MessageBox.Show("LOADING ERROR!\nThis error mostly occurs of configuration and elements.data mismatch");
						//progressBar_progress.Style = ProgressBarStyle.Continuous;
						Cursor = Cursors.Default;
					}
				}
			}
		}

		private void click_npcAIexport(object sender, EventArgs e)
		{
			SaveFileDialog save = new SaveFileDialog();
			save.InitialDirectory = Environment.CurrentDirectory;
			save.Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*";
			if (save.ShowDialog() == DialogResult.OK && save.FileName != "")
			{
				try
				{
					Cursor = Cursors.AppStarting;
					//progressBar_progress.Style = ProgressBarStyle.Marquee;

					StreamWriter sw = new StreamWriter(save.FileName, false, Encoding.Unicode);

					for (int i = 0; i < eLC.Lists[38].elementValues.Length; i++)
					{
						sw.WriteLine(eLC.GetValue(38, i, 0) + "\t" + eLC.GetValue(38, i, 2) + "\t" + eLC.GetValue(38, i, 64));
					}

					sw.Close();

					//progressBar_progress.Style = ProgressBarStyle.Continuous;
					Cursor = Cursors.Default;
				}
				catch
				{
					MessageBox.Show("SAVING ERROR!");
					//progressBar_progress.Style = ProgressBarStyle.Continuous;
					Cursor = Cursors.Default;
				}
			}
		}

		private void click_skillValidate(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				ArrayList mobSkills = new ArrayList();

				string skill;

				// check all monster skills (list 38 fields 119, 121, 123, 125, 127, 129)
				for (int n = 0; n < eLC.Lists[38].elementValues.Length; n++)
				{
					for (int f = 119; f < 130; f += 2)
					{
						skill = eLC.GetValue(38, n, f);

						if (Convert.ToInt32(skill) > 846)
						{
							mobSkills.Add("Invalid Skill: " + skill + " (Monster: " + eLC.GetValue(38, n, 0) + ")");
						}
					}
				}

				if (mobSkills.Count == 0)
				{
					MessageBox.Show("OK, no invalid skills found!");
				}
				else
				{
					string message = "";
					for (int i = 0; i < mobSkills.Count; i++)
					{
						message += (string)mobSkills[i] + "\r\n";
					}
					new DebugWindow("Invalid Skills", message);
				}
			}
		}

		private void click_propertyValidate(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				ArrayList properties = new ArrayList();

				string attribute;

				// weapons (list 3, fields 43-201, +=2)
				for (int n = 0; n < eLC.Lists[3].elementValues.Length; n++)
				{
					for (int f = 43; f < 202; f += 2)
					{
						attribute = eLC.GetValue(3, n, f);

						if (Convert.ToInt32(attribute) > 1909)
						{
							properties.Add("Invalid Property: " + attribute + " (Weapon: " + eLC.GetValue(3, n, 0) + ")");
						}
					}
				}

				// armors (list 6, fields 55-179, +=2)
				for (int n = 0; n < eLC.Lists[6].elementValues.Length; n++)
				{
					for (int f = 55; f < 180; f += 2)
					{
						attribute = eLC.GetValue(6, n, f);

						if (Convert.ToInt32(attribute) > 1909)
						{
							properties.Add("Invalid Property: " + attribute + " (Armor: " + eLC.GetValue(6, n, 0) + ")");
						}
					}
				}

				// ornaments (list 9, fields 44-160, +=2)
				for (int n = 0; n < eLC.Lists[9].elementValues.Length; n++)
				{
					for (int f = 44; f < 161; f += 2)
					{
						attribute = eLC.GetValue(9, n, f);

						if (Convert.ToInt32(attribute) > 1909)
						{
							properties.Add("Invalid Property: " + attribute + " (Ornament: " + eLC.GetValue(9, n, 0) + ")");
						}
					}
				}

				// soulgems (list 35, fields 11-12, +=1)
				for (int n = 0; n < eLC.Lists[35].elementValues.Length; n++)
				{
					for (int f = 11; f < 13; f++)
					{
						attribute = eLC.GetValue(35, n, f);

						if (Convert.ToInt32(attribute) > 1909)
						{
							properties.Add("Invalid Property: " + attribute + " (Soulgem: " + eLC.GetValue(35, n, 0) + ")");
						}
					}
				}

				// complect boni (list 90, fields 15-19, +=1)
				for (int n = 0; n < eLC.Lists[90].elementValues.Length; n++)
				{
					for (int f = 15; f < 20; f++)
					{
						attribute = eLC.GetValue(90, n, f);

						if (Convert.ToInt32(attribute) > 1909)
						{
							properties.Add("Invalid Property: " + attribute + " (Complect Bonus: " + eLC.GetValue(90, n, 0) + ")");
						}
					}
				}

				if (properties.Count == 0)
				{
					MessageBox.Show("OK, no invalid properties found!");
				}
				else
				{
					string message = "";
					for (int i = 0; i < properties.Count; i++)
					{
						message += (string)properties[i] + "\r\n";
					}
					new DebugWindow("Invalid Properties", message);
				}
			}
		}

		private void click_tomeValidate(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				ArrayList properties = new ArrayList();

				string attribute;

				for (int n = 0; n < eLC.Lists[112].elementValues.Length; n++)
				{
					for (int f = 4; f < 14; f++)
					{
						attribute = eLC.GetValue(112, n, f);

						if (Convert.ToInt32(attribute) > 1909)
						{
							properties.Add("Invalid Property: " + attribute + " (Tome: " + eLC.GetValue(112, n, 0) + ")");
						}
					}
				}

				if (properties.Count == 0)
				{
					MessageBox.Show("OK, no invalid tome properties found!");
				}
				else
				{
					string message = "";
					for (int i = 0; i < properties.Count; i++)
					{
						message += (string)properties[i] + "\r\n";
					}
					new DebugWindow("Invalid Tome Properties", message);
				}
			}
		}

		private void click_skillReplace(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				OpenFileDialog load = new OpenFileDialog();
				load.InitialDirectory = Application.StartupPath + "\\replace";
				load.Filter = "Skill Replace File (*.txt)|*.txt|All Files (*.*)|*.*";
				if (load.ShowDialog() == DialogResult.OK && File.Exists(load.FileName))
				{
					SortedList skillTable = new SortedList();

					StreamReader sr = new StreamReader(load.FileName);

					string line;
					string[] pair;
					string[] seperator = new string[] { "=" };
					while (!sr.EndOfStream)
					{
						line = sr.ReadLine();
						if (!line.StartsWith("#") && line.Contains("="))
						{
							pair = line.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
							if (pair.Length == 2)
							{
								skillTable.Add(pair[0], pair[1]);
							}
						}
					}

					sr.Close();
					/*
					ArrayList^ mobSkills = gcnew ArrayList();
					*/
					string skill;

					// change all monster skills (list 38 fields 119, 121, 123, 125, 127, 129)
					for (int n = 0; n < eLC.Lists[38].elementValues.Length; n++)
					{
						for (int f = 119; f < 130; f += 2)
						{
							skill = eLC.GetValue(38, n, f);
							/*
							if(!mobSkills->Contains(skill))
							{
								mobSkills->Add(skill);
							}
							*/
							if (skillTable.ContainsKey(skill))
							{
								eLC.SetValue(38, n, f, (string)skillTable[skill]);
							}
						}
					}
					/*
					int debug = 1;
					*/
				}
			}
		}

		private void click_propertyReplace(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				OpenFileDialog load = new OpenFileDialog();
				load.InitialDirectory = Application.StartupPath + "\\replace";
				load.Filter = "Property Replace File (*.txt)|*.txt|All Files (*.*)|*.*";
				if (load.ShowDialog() == DialogResult.OK && File.Exists(load.FileName))
				{
					SortedList propertyTable = new SortedList();

					StreamReader sr = new StreamReader(load.FileName);

					string line;
					string[] pair;
					string[] seperator = new string[] { "=" };
					while (!sr.EndOfStream)
					{
						line = sr.ReadLine();
						if (!line.StartsWith("#") && line.Contains("="))
						{
							pair = line.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
							if (pair.Length == 2)
							{
								propertyTable.Add(pair[0], pair[1]);
							}
						}
					}

					sr.Close();
					/*
					ArrayList^ weaponProps = gcnew ArrayList();
					ArrayList^ armorProps = gcnew ArrayList();
					ArrayList^ ornamentProps = gcnew ArrayList();
					ArrayList^ gemProps = gcnew ArrayList();
					ArrayList^ complectProps = gcnew ArrayList();
					*/

					string attribute;

					// weapons (list 3, fields 43-201, +=2)
					for (int n = 0; n < eLC.Lists[3].elementValues.Length; n++)
					{
						for (int f = 43; f < 202; f += 2)
						{
							attribute = eLC.GetValue(3, n, f);
							/*
							if(!weaponProps->Contains(attribute))
							{
								weaponProps->Add(attribute);
							}
							*/
							if (propertyTable.ContainsKey(attribute))
							{
								eLC.SetValue(3, n, f, (string)propertyTable[attribute]);
							}
						}
					}

					// armors (list 6, fields 55-179, +=2)
					for (int n = 0; n < eLC.Lists[6].elementValues.Length; n++)
					{
						for (int f = 55; f < 180; f += 2)
						{
							attribute = eLC.GetValue(6, n, f);
							/*
							if(!armorProps->Contains(attribute))
							{
								armorProps->Add(attribute);
							}
							*/
							if (propertyTable.ContainsKey(attribute))
							{
								eLC.SetValue(6, n, f, (string)propertyTable[attribute]);
							}
						}
					}

					// ornaments (list 9, fields 44-160, +=2)
					for (int n = 0; n < eLC.Lists[9].elementValues.Length; n++)
					{
						for (int f = 44; f < 161; f += 2)
						{
							attribute = eLC.GetValue(9, n, f);
							/*
							if(!ornamentProps->Contains(attribute))
							{
								ornamentProps->Add(attribute);
							}
							*/
							if (propertyTable.ContainsKey(attribute))
							{
								eLC.SetValue(9, n, f, (string)propertyTable[attribute]);
							}
						}
					}

					// soulgems (list 35, fields 11-12, +=1)
					for (int n = 0; n < eLC.Lists[35].elementValues.Length; n++)
					{
						for (int f = 11; f < 13; f++)
						{
							attribute = eLC.GetValue(35, n, f);
							/*
							if(!gemProps->Contains(attribute))
							{
								gemProps->Add(attribute);
							}
							*/
							if (propertyTable.ContainsKey(attribute))
							{
								eLC.SetValue(35, n, f, (string)propertyTable[attribute]);

								if ((string)propertyTable[attribute] == "1515")
								{
									eLC.SetValue(35, n, f + 2, "Vit. +20");
								}
								if ((string)propertyTable[attribute] == "1517")
								{
									eLC.SetValue(35, n, f + 2, "Critical +2%");
								}
								if ((string)propertyTable[attribute] == "1518")
								{
									eLC.SetValue(35, n, f + 2, "Channel -6%");
								}
							}
						}
					}

					// complect boni (list 90, fields 15-19, +=1)
					for (int n = 0; n < eLC.Lists[90].elementValues.Length; n++)
					{
						for (int f = 15; f < 20; f++)
						{
							attribute = eLC.GetValue(90, n, f);
							/*
							if(!complectProps->Contains(attribute))
							{
								complectProps->Add(attribute);
							}
							*/
							if (propertyTable.ContainsKey(attribute))
							{
								eLC.SetValue(90, n, f, (string)propertyTable[attribute]);
							}
						}
					}
					/*
					int debug = 1;
					*/
				}
			}
		}

		private void click_tomeReplace(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				OpenFileDialog load = new OpenFileDialog();
				load.InitialDirectory = Application.StartupPath + "\\replace";
				load.Filter = "Tome Replace File (*.txt)|*.txt|All Files (*.*)|*.*";
				if (load.ShowDialog() == DialogResult.OK && File.Exists(load.FileName))
				{
					SortedList propertyTable = new SortedList();

					StreamReader sr = new StreamReader(load.FileName);

					string line;
					string[] pair;
					string[] seperator = new string[] { "=" };
					string[] divider = new string[] { "," };
					while (!sr.EndOfStream)
					{
						line = sr.ReadLine();
						if (!line.StartsWith("#") && line.Contains("="))
						{
							pair = line.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
							if (pair.Length == 2)
							{
								propertyTable.Add(pair[0], pair[1].Split(divider, StringSplitOptions.RemoveEmptyEntries));
							}
						}
					}

					sr.Close();
					/*
					ArrayList^ tomeProps = gcnew ArrayList();
					*/
					string attribute;
					string[] attributes;
					ArrayList attributesOrgiginal = new ArrayList();
					ArrayList attributesReplaced = new ArrayList();

					// weapons (list 3, fields 43-201, +=2)
					for (int n = 0; n < eLC.Lists[112].elementValues.Length; n++)
					{
						attributesOrgiginal.Clear();
						attributesReplaced.Clear();

						for (int f = 4; f < 14; f++)
						{
							attribute = eLC.GetValue(112, n, f);
							/*
							if(!tomeProps->Contains(attribute))
							{
								tomeProps->Add(attribute);
							}
							*/
							if (attribute != "0")
							{
								if (propertyTable.ContainsKey(attribute))
								{
									attributes = (string[])propertyTable[attribute];
									for (int a = 0; a < attributes.Length; a++)
									{
										attributesReplaced.Add(attributes[a]);
									}
								}
								else
								{
									// add the attribute without changes
									attributesReplaced.Add(attribute);
								}
							}
						}

						if (attributesReplaced.Count > 10)
						{
							MessageBox.Show("Tome Attribute Overflow: " + n + "\nAttributes Truncated");
						}

						// add the new attribute list to the current tome
						for (int f = 4; f < 14; f++)
						{
							if (f - 4 < attributesReplaced.Count)
							{
								// add the replaced attribute
								attribute = (string)attributesReplaced[f - 4];
								eLC.SetValue(112, n, f, attribute);
							}
							else
							{
								eLC.SetValue(112, n, f, "0");
							}
						}
					}
					/*
					int debug = 1;
					*/
				}
			}
		}

		private void click_probabilityValidate(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				ArrayList probabilities = new ArrayList();
				double attribute;

				// weapons (list 3)
				for (int n = 0; n < eLC.Lists[3].elementValues.Length; n++)
				{
					// weapon drop sockets count(fields 32-34, +=1)

					attribute = 0;

					for (int f = 32; f < 35; f++)
					{
						attribute += Convert.ToDouble(eLC.GetValue(3, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Socket Drop Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + eLC.GetValue(3, n, 0) + ")");
					}

					// weapon craft sockets count(fields 35-37, +=1)

					attribute = 0;

					for (int f = 35; f < 38; f++)
					{
						attribute += Convert.ToDouble(eLC.GetValue(3, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Socket Craft Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + eLC.GetValue(3, n, 0) + ")");
					}

					// weapon addons count(fields 38-41, +=1)

					attribute = 0;

					for (int f = 38; f < 42; f++)
					{
						attribute += Convert.ToDouble(eLC.GetValue(3, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Addon Count Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + eLC.GetValue(3, n, 0) + ")");
					}

					// weapon drop (fields 44-106, +=2)

					attribute = 0;

					for (int f = 44; f < 107; f += 2)
					{
						attribute += Convert.ToDouble(eLC.GetValue(3, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Drop Attriutes Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + eLC.GetValue(3, n, 0) + ")");
					}

					// weapon craft (fields 108-170, +=2)

					attribute = 0;

					for (int f = 108; f < 171; f += 2)
					{
						attribute += Convert.ToDouble(eLC.GetValue(3, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Craft Attributes Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + eLC.GetValue(3, n, 0) + ")");
					}

					// weapons unique (fields 172-202, +=2)

					attribute = 0;

					for (int f = 172; f < 203; f += 2)
					{
						attribute += Convert.ToDouble(eLC.GetValue(3, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Unique Attributes Probability (sum != 1.0): " + attribute.ToString() + " (Weapon: " + eLC.GetValue(3, n, 0) + ")");
					}
				}

				// armors (list 6)
				for (int n = 0; n < eLC.Lists[6].elementValues.Length; n++)
				{
					// armor drop sockets count(fields 41-45, +=1)

					attribute = 0;

					for (int f = 41; f < 46; f++)
					{
						attribute += Convert.ToDouble(eLC.GetValue(6, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Socket Drop Probability (sum != 1.0): " + attribute.ToString() + " (Armor: " + eLC.GetValue(6, n, 0) + ")");
					}

					// armor craft sockets count(fields 46-50, +=1)

					attribute = 0;

					for (int f = 46; f < 51; f++)
					{
						attribute += Convert.ToDouble(eLC.GetValue(6, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Socket Craft Probability (sum != 1.0): " + attribute.ToString() + " (Armor: " + eLC.GetValue(6, n, 0) + ")");
					}

					// armor addons count(fields 51-54, +=1)

					attribute = 0;

					for (int f = 51; f < 55; f++)
					{
						attribute += Convert.ToDouble(eLC.GetValue(6, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Addon Count Probability (sum != 1.0): " + attribute.ToString() + " (Armor: " + eLC.GetValue(6, n, 0) + ")");
					}

					// armor drop (fields 56-118, +=2)

					attribute = 0;

					for (int f = 56; f < 119; f += 2)
					{
						attribute += Convert.ToDouble(eLC.GetValue(6, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Drop Attriutes Probability (sum != 1.0): " + attribute.ToString() + " (Armor: " + eLC.GetValue(6, n, 0) + ")");
					}

					// armor craft (fields 120-180, +=2)

					attribute = 0;

					for (int f = 120; f < 181; f += 2)
					{
						attribute += Convert.ToDouble(eLC.GetValue(6, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Craft Attributes Probability (sum != 1.0): " + attribute.ToString() + " (Armor: " + eLC.GetValue(6, n, 0) + ")");
					}
				}

				// ornaments (list 9)
				for (int n = 0; n < eLC.Lists[9].elementValues.Length; n++)
				{
					// ornament addons count(fields 40-43, +=1)

					attribute = 0;

					for (int f = 40; f < 44; f++)
					{
						attribute += Convert.ToDouble(eLC.GetValue(9, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Addon Count Probability (sum != 1.0): " + attribute.ToString() + " (Ornament: " + eLC.GetValue(9, n, 0) + ")");
					}

					// ornament drop (fields 45-107, +=2)

					attribute = 0;

					for (int f = 45; f < 108; f += 2)
					{
						attribute += Convert.ToDouble(eLC.GetValue(9, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Drop Attriutes Probability (sum != 1.0): " + attribute.ToString() + " (Ornament: " + eLC.GetValue(9, n, 0) + ")");
					}

					// ornament craft (fields 109-161, +=2)

					attribute = 0;

					for (int f = 109; f < 162; f += 2)
					{
						attribute += Convert.ToDouble(eLC.GetValue(9, n, f));
					}

					if (Math.Round(attribute, 6) != 1)
					{
						probabilities.Add("Suspicious Craft Attributes Probability (sum != 1.0): " + attribute.ToString() + " (Ornament: " + eLC.GetValue(9, n, 0) + ")");
					}
				}

				if (probabilities.Count == 0)
				{
					MessageBox.Show("OK, no invalid probabilities found!");
				}
				else
				{
					string message = "";
					for (int i = 0; i < probabilities.Count; i++)
					{
						message += (string)probabilities[i] + "\r\n";
					}
					new DebugWindow("Invalid Probabilities", message);
				}
			}
		}

		private void click_TaskOverflowCheck(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				string value;
				bool isAddedElement;

				LoseQuestWindow questWindow = new LoseQuestWindow();



				// list 45 recive quests
				for (int n = 0; n < eLC.Lists[45].elementValues.Length; n++)
				{
					isAddedElement = false;
					for (int f = 34; f < eLC.Lists[45].elementFields.Length; f++)
					{
						value = eLC.GetValue(45, n, f);
						if (value != "0")
						{
							if (!isAddedElement)
							{
								questWindow.listBox_Receive.Items.Add("+++++ " + eLC.GetValue(45, n, 0) + " - " + eLC.GetValue(45, n, 1) + " +++++");
								isAddedElement = true;
							}
							questWindow.listBox_Receive.Items.Add(value);
						}
					}
				}

				// list 46 activate quests
				for (int n = 0; n < eLC.Lists[46].elementValues.Length; n++)
				{
					isAddedElement = false;
					for (int f = 34; f < eLC.Lists[46].elementFields.Length; f++)
					{
						value = eLC.GetValue(46, n, f);
						if (value != "0")
						{
							if (!isAddedElement)
							{
								questWindow.listBox_Activate.Items.Add("+++++ " + eLC.GetValue(46, n, 0) + " - " + eLC.GetValue(46, n, 1) + " +++++");
								isAddedElement = true;
							}
							questWindow.listBox_Activate.Items.Add(value);
						}
					}
				}

				questWindow.Show();
			}
		}

		private void click_classMask(object sender, EventArgs e)
		{
			ClassMaskWindow eClassMask = new ClassMaskWindow();
			eClassMask.Show();
		}

		private void cellMouseMove_ToolTip(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (comboBox_lists.SelectedIndex == 0 && dataGridView_elems.CurrentCell.RowIndex != -1 && e.ColumnIndex == 2 && e.RowIndex > 2 && e.RowIndex < 6)
			{
				dataGridView_item.ShowCellToolTips = false;
				string text = "Float: " + (BitConverter.ToSingle(BitConverter.GetBytes((int)(eLC.Lists[0].elementValues[dataGridView_elems.CurrentCell.RowIndex][e.RowIndex])), 0)).ToString("F6");

				// not working on first mouse over (still shows previous value on first mouse over)
				//dataGridView_item->Rows[e->RowIndex]->Cells[e->ColumnIndex]->ToolTipText = text;

				// only draw on real move to prevent flickering in windows 7
				if (mouseMoveCheck.X != e.X || mouseMoveCheck.Y != e.Y)
				{
					toolTip.SetToolTip((Control)sender, text);
					mouseMoveCheck.X = e.X;
					mouseMoveCheck.Y = e.Y;
				}
			}
			else if(e.RowIndex > -1 && dataGridView_item.Rows[e.RowIndex].Cells[0].Value.ToString() == "shop_price" && comboBox_lists.SelectedIndex > -1 && dataGridView_elems.CurrentCell.RowIndex  > -1)
			{
				dataGridView_item.ShowCellToolTips = false;
                int fieldIndex = GetFieldIndexForValueRow(e.RowIndex);
                if (fieldIndex < 0)
                {
                    return;
                }
				int shop_price = Convert.ToInt32(eLC.GetValue(comboBox_lists.SelectedIndex, dataGridView_elems.CurrentCell.RowIndex, fieldIndex));
				double tmp = 0;
				double tmp1 = 0;
				tmp1 = shop_price * 0.05;
				if (shop_price >= 10)
					tmp1 = Math.Round(tmp1, MidpointRounding.AwayFromZero);
				else
					tmp1 = Math.Round(tmp1);
				tmp = shop_price + tmp1;
				if (tmp >= 100 && tmp < 1000)
				{
					tmp = tmp * 0.1;
					tmp = Math.Ceiling(tmp);
					tmp = tmp * 10;
				}
				if (tmp >= 1000)
				{
					tmp = tmp * 0.01;
					tmp = Math.Ceiling(tmp);
					tmp = tmp * 100;
				}
				string text = "In Game Price: " + tmp;
				if (mouseMoveCheck.X != e.X || mouseMoveCheck.Y != e.Y)
				{
					toolTip.SetToolTip((Control)sender, text);
					mouseMoveCheck.X = e.X;
					mouseMoveCheck.Y = e.Y;
				}
			}
			else
			{
				toolTip.Hide((Control)sender);
				dataGridView_item.ShowCellToolTips = true;
			}
		}

		private void click_diffEL(object sender, EventArgs e)
		{
			RulesWindow eRules = new RulesWindow(ref cpb2);
			eRules.Show();
		}

		private void listBox_items_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			int l = comboBox_lists.SelectedIndex;
			int k = dataGridView_elems.CurrentCell.RowIndex;
			if (l != eLC.ConversationListIndex)
			{
				if (l > -1 && k > -1)
				{
					int pos = -1;
					for (int i = 0; i < eLC.Lists[l].elementFields.Length; i++)
					{
						if (string.Equals(eLC.Lists[l].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
						{
							pos = i;
							break;
						}
					}
					if (pos > -1)
					{
						Clipboard.SetDataObject(eLC.GetValue(l, k, 0) + "	" + eLC.GetValue(l, k, pos), true);
					}
					else
					{
						MessageBox.Show("Config Error: cannot find Name field");
					}
				}
				else
				{
					MessageBox.Show("Invalid List");
				}
			}
			else
			{
				MessageBox.Show("Operation not supported in List " + eLC.ConversationListIndex.ToString());
			}
		}

		private void click_xrefItem(object sender, EventArgs ea)
		{
			int l = comboBox_lists.SelectedIndex;
			int e = dataGridView_elems.CurrentCell.RowIndex;
			if (l != eLC.ConversationListIndex)
			{
				if (l > -1 && e > -1)
				{
                    if (xrefs == null || l >= xrefs.Length || xrefs[l] == null || xrefs[l].Length < 2)
                    {
                        MessageBox.Show("No cross-reference rules for this list.");
                        return;
                    }

					ReferencesWindow eXRef = new ReferencesWindow();
					char[] chars = { '-' };
					int results = 0;

					for (int j = 1; j < xrefs[l].Length; j++)
					{
						string[] x = xrefs[l][j].Split(chars);
						for (int m = 1; m < eLC.Lists[int.Parse(x[0])].elementValues.Length; m++)
						{
							for (int k = 1; k < x.Length; k++)
							{
								if (eLC.GetValue(int.Parse(x[0]), m, int.Parse(x[k])) == eLC.GetValue(l, e, 0))
								{
									results++;
									int pos = -1;
									for (int i = 0; i < eLC.Lists[int.Parse(x[0])].elementFields.Length; i++)
									{
										if (string.Equals(eLC.Lists[int.Parse(x[0])].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
										{
											pos = i;
											break;
										}
									}
                                    if (pos < 0)
                                    {
                                        pos = 0;
                                    }
									eXRef.dataGridView.Rows.Add(new string[] { x[0], eLC.Lists[int.Parse(x[0])].listName, eLC.GetValue(int.Parse(x[0]), m, 0), eLC.GetValue(int.Parse(x[0]), m, pos), x[k] + " - " + eLC.Lists[int.Parse(x[0])].elementFields[int.Parse(x[k])] });
								}
							}
						}
					}
					if (results > 0)
					{
						eXRef.Show();
					}
					else
					{
						eXRef.Close();
						MessageBox.Show("No results found");
					}
				}
			}
			else
			{
				MessageBox.Show("Operation not supported in List " + eLC.ConversationListIndex.ToString());
			}
		}

        private void CheckSearchCondition(object sender, EventArgs e)
        {
            if (checkBox_SearchAll.Checked && textBox_search.Text == "ID or NAME")
                textBox_search.Text = "VALUE";
            else if (textBox_search.Text == "VALUE")
                textBox_search.Text = "ID or NAME";
            HideSearchSuggestions();
        }

		private void click_SetValue(object sender, EventArgs e)
		{
            MessageBox.Show("Set Value is disabled in this mode. Edit directly in the Values grid.");
		}

		private void listBox_items_KeyDown(object sender, KeyEventArgs e)
		{
			//if (ModifierKeys == Keys.Control && listBox_items.SelectedIndices.Count > 0 && comboBox_lists.SelectedIndex != eLC.ConversationListIndex)
			//{
			//	if (e.KeyCode == Keys.Up)
			//	{
			//		if (listBox_items.SelectedIndices[0] > 0)
			//		{
			//			EnableSelectionItem = false;
			//			int[] SelectedIndices = new int[listBox_items.SelectedIndices.Count];
			//			for (int i = 0; i < listBox_items.SelectedIndices.Count; i++)
			//			{
			//				SelectedIndices[i] = listBox_items.SelectedIndices[i];
			//			}
			//			int pos = -1;
			//			for (int i = 0; i < eLC.Lists[comboBox_lists.SelectedIndex].elementFields.Length; i++)
			//			{
			//				if (string.Equals(eLC.Lists[comboBox_lists.SelectedIndex].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
			//				{
			//					pos = i;
			//					break;
			//				}
			//			}
			//			for (int i = 0; i < listBox_items.SelectedIndices.Count; i++)
			//			{
			//				object[][] temp = new object[eLC.Lists[comboBox_lists.SelectedIndex].elementValues.Length][];
			//				Array.Copy(eLC.Lists[comboBox_lists.SelectedIndex].elementValues, 0, temp, 0, listBox_items.SelectedIndices[i] - 1);
			//				Array.Copy(eLC.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i], temp, listBox_items.SelectedIndices[i] - 1, 1);
			//				Array.Copy(eLC.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i] - 1, temp, listBox_items.SelectedIndices[i], 1);
			//				if (listBox_items.SelectedIndices[i] < listBox_items.Items.Count - 1)
			//					Array.Copy(eLC.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i] + 1, temp, listBox_items.SelectedIndices[i] + 1, listBox_items.Items.Count - listBox_items.SelectedIndices[i] - 1);
			//				eLC.Lists[comboBox_lists.SelectedIndex].elementValues = temp;
			//				int ei = SelectedIndices[i] - 1;
			//				int ei2 = SelectedIndices[i];
			//				if (string.Equals(eLC.Lists[comboBox_lists.SelectedIndex].elementFields[0], "ID", StringComparison.OrdinalIgnoreCase))
			//				{
			//					listBox_items.Items[ei] = "[" + ei + "]: " + eLC.GetValue(comboBox_lists.SelectedIndex, ei, 0) + " - " + eLC.GetValue(comboBox_lists.SelectedIndex, ei, pos);
			//					listBox_items.Items[ei2] = "[" + ei2 + "]: " + eLC.GetValue(comboBox_lists.SelectedIndex, ei2, 0) + " - " + eLC.GetValue(comboBox_lists.SelectedIndex, ei2, pos);
			//				}
			//				else
			//				{
			//					listBox_items.Items[ei] = "[" + ei + "]: " + eLC.GetValue(comboBox_lists.SelectedIndex, ei, pos);
			//					listBox_items.Items[ei2] = "[" + ei2 + "]: " + eLC.GetValue(comboBox_lists.SelectedIndex, ei2, pos);
			//				}
			//			}
			//			listBox_items.SelectedIndex = -1;
			//			listBox_items.SelectionMode = SelectionMode.MultiSimple;
			//			for (int i = 0; i < SelectedIndices.Length; i++)
			//			{
			//				listBox_items.SelectedIndex = SelectedIndices[i] - 1;
			//			}
			//			listBox_items.SelectionMode = SelectionMode.MultiExtended;
			//			EnableSelectionItem = true;
			//			change_item(null, null);
			//		}
			//	}
			//	else if (e.KeyCode == Keys.Down)
			//	{
			//		if (listBox_items.SelectedIndices[listBox_items.SelectedIndices.Count - 1] < listBox_items.Items.Count - 1)
			//		{
			//			EnableSelectionItem = false;
			//			int[] SelectedIndices = new int[listBox_items.SelectedIndices.Count];
			//			for (int i = 0; i < listBox_items.SelectedIndices.Count; i++)
			//			{
			//				SelectedIndices[i] = listBox_items.SelectedIndices[i];
			//			}
			//			int pos = -1;
			//			for (int i = 0; i < eLC.Lists[comboBox_lists.SelectedIndex].elementFields.Length; i++)
			//			{
			//				if (string.Equals(eLC.Lists[comboBox_lists.SelectedIndex].elementFields[i], "Name", StringComparison.OrdinalIgnoreCase))
			//				{
			//					pos = i;
			//					break;
			//				}
			//			}
			//			for (int i = listBox_items.SelectedIndices.Count - 1; i > -1; i--)
			//			{
			//				object[][] temp = new object[eLC.Lists[comboBox_lists.SelectedIndex].elementValues.Length][];
			//				Array.Copy(eLC.Lists[comboBox_lists.SelectedIndex].elementValues, 0, temp, 0, listBox_items.SelectedIndices[i]);
			//				Array.Copy(eLC.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i] + 1, temp, listBox_items.SelectedIndices[i], 1);
			//				Array.Copy(eLC.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i], temp, listBox_items.SelectedIndices[i] + 1, 1);
			//				if (listBox_items.SelectedIndices[i] < listBox_items.Items.Count - 2)
			//					Array.Copy(eLC.Lists[comboBox_lists.SelectedIndex].elementValues, listBox_items.SelectedIndices[i] + 2, temp, listBox_items.SelectedIndices[i] + 2, listBox_items.Items.Count - listBox_items.SelectedIndices[i] - 2);
			//				eLC.Lists[comboBox_lists.SelectedIndex].elementValues = temp;
			//				int ei = SelectedIndices[i] + 1;
			//				int ei2 = SelectedIndices[i];
			//				if (string.Equals(eLC.Lists[comboBox_lists.SelectedIndex].elementFields[0], "ID", StringComparison.OrdinalIgnoreCase))
			//				{
			//					listBox_items.Items[ei] = "[" + ei + "]: " + eLC.GetValue(comboBox_lists.SelectedIndex, ei, 0) + " - " + eLC.GetValue(comboBox_lists.SelectedIndex, ei, pos);
			//					listBox_items.Items[ei2] = "[" + ei2 + "]: " + eLC.GetValue(comboBox_lists.SelectedIndex, ei2, 0) + " - " + eLC.GetValue(comboBox_lists.SelectedIndex, ei2, pos);
			//				}
			//				else
			//				{
			//					listBox_items.Items[ei] = "[" + ei + "]: " + eLC.GetValue(comboBox_lists.SelectedIndex, ei, pos);
			//					listBox_items.Items[ei2] = "[" + ei2 + "]: " + eLC.GetValue(comboBox_lists.SelectedIndex, ei2, pos);
			//				}
			//			}
			//			listBox_items.SelectedIndex = -1;
			//			listBox_items.SelectionMode = SelectionMode.MultiSimple;
			//			for (int i = 0; i < SelectedIndices.Length; i++)
			//			{
			//				listBox_items.SelectedIndex = SelectedIndices[i] + 1;
			//			}
			//			listBox_items.SelectionMode = SelectionMode.MultiExtended;
			//			EnableSelectionItem = true;
			//			change_item(null, null);
			//		}
			//	}
			//}
		}

        private void click_moveItemsToTop(object sender, EventArgs ea)
        {
            int l = comboBox_lists.SelectedIndex;
            int[] selIndices = gridSelectedIndices(dataGridView_elems);
            if (selIndices[0] > 0 && selIndices.Length > 0 && l != eLC.ConversationListIndex)
            {
                EnableSelectionItem = false;
                object[][] temp = new object[eLC.Lists[l].elementValues.Length][];
                for (int i = 0; i < selIndices.Length; i++)
                {
                    Array.Copy(eLC.Lists[l].elementValues, selIndices[i], temp, i, 1);
                }
                Array.Copy(eLC.Lists[l].elementValues, 0, temp, selIndices.Length, selIndices[0]);
                for (int i = selIndices.Length - 1; i > -1; i--)
                {
                    eLC.Lists[l].RemoveItem(selIndices[i]);
                }
                Array.Copy(eLC.Lists[l].elementValues, 0, temp, selIndices.Length, eLC.Lists[l].elementValues.Length);
                eLC.Lists[l].elementValues = temp;

                change_list(null, null);

                dataGridView_elems.ClearSelection();
                for (int i = 0; i < selIndices.Length; i++)
                {
                    dataGridView_elems.Rows[i].Selected = true;
                    dataGridView_elems.FirstDisplayedScrollingRowIndex = i;
                }
                EnableSelectionItem = true;
            }
        }

        private void click_moveItemsToEnd(object sender, EventArgs ea)
        {
            int l = comboBox_lists.SelectedIndex;
            int[] selIndices = gridSelectedIndices(dataGridView_elems);
            if (selIndices[0] < dataGridView_elems.RowCount-1 && selIndices.Length > 0 && l != eLC.ConversationListIndex)
            {
                EnableSelectionItem = false;
                object[][] temp = new object[eLC.Lists[l].elementValues.Length][];
                for (int i = 0; i < selIndices.Length; i++)
                {
                    Array.Copy(eLC.Lists[l].elementValues, selIndices[i], temp, dataGridView_elems.RowCount - selIndices.Length + i, 1);
                }
                Array.Copy(eLC.Lists[l].elementValues, 0, temp, selIndices.Length, selIndices[0]);
                for (int i = selIndices.Length - 1; i > -1; i--)
                {
                    eLC.Lists[l].RemoveItem(selIndices[i]);
                }
                Array.Copy(eLC.Lists[l].elementValues, 0, temp, 0, eLC.Lists[l].elementValues.Length);
                eLC.Lists[l].elementValues = temp;

                change_list(null, null);

                dataGridView_elems.ClearSelection();
                for (int i = dataGridView_elems.RowCount - selIndices.Length; i < dataGridView_elems.RowCount; i++)
                {
                    dataGridView_elems.Rows[i].Selected = true;
                    dataGridView_elems.FirstDisplayedScrollingRowIndex = i;
                }
                EnableSelectionItem = true;
            }
        }

        private void click_fieldCompare(object sender, EventArgs e)
		{
			if (eLC != null)
			{
				(new FieldCompare(eLC, conversationList, ref cpb2)).Show();
			}
			else
			{
				MessageBox.Show("No File Loaded!");
			}
		}

		string timestamp_to_string(uint timestamp)
		{
			DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			origin = origin.AddSeconds(timestamp);
			return origin.ToString("yyyy-MM-dd HH:mm:ss");
		}

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.Show();
        }

        //public Bitmap CropBitmap(Bitmap bitmap, int cropX, int cropY, int cropWidth, int cropHeight)
        //{
        //    Rectangle rect = new Rectangle(cropX, cropY, cropWidth, cropHeight);
        //    Bitmap cropped = bitmap.Clone(rect, bitmap.PixelFormat);
        //    return cropped;
        //}

        public Bitmap ddsIcon(Bitmap rawImg, string rawTxt, string icoName)
        {
            try
            { 
                int counter = 0;
                string line;
                int W = 0;
                int H = 0;
                double col = 0;
                double imgNum = -1;
                //X = horizonal
                double X;
                //Y = vertical
                double Y;
                Bitmap cImage = null;

                StreamReader file = new StreamReader(rawTxt, Encoding.GetEncoding("GB2312"));
                while ((line = file.ReadLine()) != null)
                {
                    if (counter == 0) { W = Int32.Parse(line); }
                    if (counter == 1) { H = Int32.Parse(line); }
                    if (counter == 3) { col = Int32.Parse(line); }

                    if (line == icoName)
                    {
                        imgNum = counter - 3;
                        break;
                    }
                    counter++;
                }

                if (imgNum != -1)
                {
                    X = Math.Floor(((imgNum * W) - W) / (col * W)) * W;
                    Y = ((imgNum * W) - W) - (((col * W) * X) / W);

                    Rectangle rect = new Rectangle(Convert.ToInt32(Y), Convert.ToInt32(X), W, H);
                    cImage = rawImg.Clone(rect, rawImg.PixelFormat);
                    //Bitmap cropped = rawImg.Clone(rect, rawImg.PixelFormat);

                    //cImage = CropBitmap(rawImg, Convert.ToInt32(Y), Convert.ToInt32(X), W, H);
                }
                return cImage;
            }
            catch
            {
                return Properties.Resources.NoIcon;
            }
        }

        public int[] gridSelectedIndices(DataGridView grd)
        {
            List<int> inx = new List<int>();
            Int32 selectedRowCount = grd.Rows.GetRowCount(DataGridViewElementStates.Selected);
            if (selectedRowCount > 0)
            {
                for (int i = 0; i < selectedRowCount; i++)
                {
                    inx.Add(grd.SelectedRows[i].Index);
                }
            }
            inx.Sort();
            int[] arr = inx.ToArray();
            return arr;
        }

        private void textBox_search_enter(object sender, EventArgs e)
        {
            if (textBox_search.Text == "ID or NAME")
            {
                textBox_search.Clear();
            }
        }

        private void textBox_search_leave(object sender, EventArgs e)
        {
            if (textBox_search.Text == "")
            {
                textBox_search.Text = "ID or NAME";
            }
            BeginInvoke((Action)(() =>
            {
                if (searchSuggestionList != null && !searchSuggestionList.Focused && !textBox_search.Focused)
                {
                    HideSearchSuggestions();
                }
            }));
        }

        private void textBox_search_TextChanged(object sender, EventArgs e)
        {
            UpdateSearchSuggestions();
        }

        private void textBox_search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (searchSuggestionList != null && searchSuggestionList.Visible && searchSuggestionList.SelectedItem != null)
                {
                    ApplySearchSuggestion(searchSuggestionList.SelectedItem as SearchSuggestion);
                }
                else
                {
                    click_search(sender, EventArgs.Empty);
                }
                return;
            }

            if (e.KeyCode == Keys.Down && searchSuggestionList != null && searchSuggestionList.Visible && searchSuggestionList.Items.Count > 0)
            {
                searchSuggestionList.Focus();
                if (searchSuggestionList.SelectedIndex < 0)
                {
                    searchSuggestionList.SelectedIndex = 0;
                }
                e.SuppressKeyPress = true;
            }

            if (e.KeyCode == Keys.Escape && searchSuggestionList != null && searchSuggestionList.Visible)
            {
                HideSearchSuggestions();
                e.SuppressKeyPress = true;
            }
        }

        private void searchSuggestionList_MouseClick(object sender, MouseEventArgs e)
        {
            if (searchSuggestionList == null)
            {
                return;
            }
            int index = searchSuggestionList.IndexFromPoint(e.Location);
            if (index >= 0 && index < searchSuggestionList.Items.Count)
            {
                ApplySearchSuggestion(searchSuggestionList.Items[index] as SearchSuggestion);
            }
        }

        private void searchSuggestionList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ApplySearchSuggestion(searchSuggestionList.SelectedItem as SearchSuggestion);
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                HideSearchSuggestions();
                textBox_search.Focus();
            }
        }

        private void ApplySearchSuggestion(SearchSuggestion suggestion)
        {
            if (suggestion == null)
            {
                return;
            }
            NavigateToListItem(suggestion.ListIndex, suggestion.ElementIndex);
            HideSearchSuggestions();
        }

        private void UpdateSearchSuggestions()
        {
            if (searchSuggestionList == null)
            {
                return;
            }

            string rawQuery = textBox_search.Text ?? string.Empty;
            string query = rawQuery.Trim();
            if (string.IsNullOrWhiteSpace(query) || IsSearchPlaceholder(query))
            {
                HideSearchSuggestions();
                return;
            }

            if (eLC == null || eLC.Lists == null || eLC.Lists.Length == 0)
            {
                HideSearchSuggestions();
                return;
            }

            bool matchCase = checkBox_SearchMatchCase.Checked;
            string queryCompare = matchCase ? query : query.ToLowerInvariant();
            bool queryIsNumeric = int.TryParse(query, out _);

            searchSuggestionList.BeginUpdate();
            searchSuggestionList.Items.Clear();

            int added = 0;
            for (int listIndex = 0; listIndex < eLC.Lists.Length; listIndex++)
            {
                int idFieldIndex = GetIdFieldIndex(listIndex);
                if (idFieldIndex < 0)
                {
                    idFieldIndex = 0;
                }
                int nameFieldIndex = GetNameFieldIndex(listIndex);

                for (int i = 0; i < eLC.Lists[listIndex].elementValues.Length; i++)
                {
                    string idText = eLC.GetValue(listIndex, i, idFieldIndex) ?? string.Empty;
                    string nameText = nameFieldIndex >= 0 ? (eLC.GetValue(listIndex, i, nameFieldIndex) ?? string.Empty) : string.Empty;

                    string idCompare = matchCase ? idText : idText.ToLowerInvariant();
                    string nameCompare = matchCase ? nameText : nameText.ToLowerInvariant();

                    bool match = false;
                    if (queryIsNumeric)
                    {
                        if (!string.IsNullOrEmpty(idCompare) && idCompare.StartsWith(queryCompare))
                        {
                            match = true;
                        }
                        else if (!string.IsNullOrEmpty(nameCompare) && nameCompare.Contains(queryCompare))
                        {
                            match = true;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(nameCompare) && nameCompare.Contains(queryCompare))
                        {
                            match = true;
                        }
                        else if (!string.IsNullOrEmpty(idCompare) && (idCompare == queryCompare || idCompare.StartsWith(queryCompare)))
                        {
                            match = true;
                        }
                    }

                    if (match)
                    {
                        searchSuggestionList.Items.Add(new SearchSuggestion
                        {
                            ListIndex = listIndex,
                            ElementIndex = i,
                            IdText = idText,
                            NameText = nameText
                        });
                        added++;
                        if (added >= SearchSuggestionMax)
                        {
                            break;
                        }
                    }
                }

                if (added >= SearchSuggestionMax)
                {
                    break;
                }
            }

            searchSuggestionList.EndUpdate();

            if (searchSuggestionList.Items.Count == 0)
            {
                HideSearchSuggestions();
                return;
            }

            int desiredHeight = Math.Min(SearchSuggestionMaxHeight, (searchSuggestionList.ItemHeight + 2) * searchSuggestionList.Items.Count + 4);
            searchSuggestionList.Height = Math.Max(60, desiredHeight);
            searchSuggestionList.Visible = true;
            searchSuggestionList.BringToFront();
        }

        private void HideSearchSuggestions()
        {
            if (searchSuggestionList == null)
            {
                return;
            }
            searchSuggestionList.Visible = false;
            searchSuggestionList.Items.Clear();
        }

        private bool IsSearchPlaceholder(string text)
        {
            return string.Equals(text, "ID or NAME", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "VALUE", StringComparison.OrdinalIgnoreCase);
        }

        private int GetNameFieldIndex(int listIndex)
        {
            if (eLC == null || listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return -1;
            }

            for (int i = 0; i < eLC.Lists[listIndex].elementFields.Length; i++)
            {
                string field = eLC.Lists[listIndex].elementFields[i];
                if (string.Equals(field, "Name", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private void NavigateToListItem(int listIndex, int rowIndex)
        {
            if (eLC == null || eLC.Lists == null)
            {
                return;
            }
            if (listIndex < 0 || listIndex >= eLC.Lists.Length)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= eLC.Lists[listIndex].elementValues.Length)
            {
                return;
            }

            EnableSelectionItem = false;
            comboBox_lists.SelectedIndex = listIndex;
            EnableSelectionItem = true;

            if (dataGridView_elems.Rows.Count > rowIndex)
            {
                dataGridView_elems.ClearSelection();
                dataGridView_elems.CurrentCell = dataGridView_elems[0, rowIndex];
                dataGridView_elems.Rows[rowIndex].Selected = true;
                dataGridView_elems.FirstDisplayedScrollingRowIndex = rowIndex;
            }
        }

        private void textBox_value_enter(object sender, EventArgs e)
        {
            if (textBox_SetValue.Text == "Set Value")
            {
                textBox_SetValue.Clear();
            }
        }

        private void textBox_value_leave(object sender, EventArgs e)
        {
            if (textBox_SetValue.Text == "")
            {
                textBox_SetValue.Text = "Set Value";
            }
        }

        private void listBox_items_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (MainWindow.database == null || eLC == null)
            {
                return;
            }
            try
            {
                if (customTooltype != null)
                    customTooltype.Close();
            }
            catch { }
            if (e.ColumnIndex >= 0 && e.ColumnIndex == 1 && e.RowIndex > -1)
            {
                InfoTool ift = null;
                try
                {
                    int l = comboBox_lists.SelectedIndex;
                    int xe = e.RowIndex;
                    int Id = Convert.ToInt32(this.dataGridView_elems.Rows[e.RowIndex].Cells[0].Value);
                    if (Id > 0)
                    {
                        ift = Extensions.GetItemProps2(Id, 0, l, xe);
                    }
                    if (ift == null)
                    {
                        string text = Extensions.GetItemProps(Id, 0);
                        text += Extensions.ItemDesc(Id);
                        this.dataGridView_elems.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = text;
                    }
                    else
                    {
                        ift.description = Extensions.ColorClean(Extensions.ItemDesc(Id));
                        customTooltype = new IToolType(ift);
                        customTooltype.Show();
                    }
                }
                catch
                {
                }
            }
        }

        private void createListWithCountsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.FileName = "elements.list.count";
            saveFileDialog1.Title = "Save List Count File";
            saveFileDialog1.ShowDialog();

            // If the file name is not an empty string open it for saving.
            if (saveFileDialog1.FileName != "")
            {
                if (File.Exists(saveFileDialog1.FileName)) { File.Delete(saveFileDialog1.FileName); }
                using (StreamWriter file = new StreamWriter(saveFileDialog1.FileName))
                {
                    file.WriteLine("ver=" + eLC.Version);
                    for (int l = 0; l < eLC.Lists.Length; l++)
                    {
                        file.WriteLine(l + "=" + eLC.Lists[l].elementValues.Length);
                    }
                }
            }
        }
    }
}


