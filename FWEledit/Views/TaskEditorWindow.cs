using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class TaskEditorWindow : Form
    {
        private readonly TaskEditorFileService fileService = new TaskEditorFileService();
        private readonly NpcGenEntityLookupService entityLookupService = new NpcGenEntityLookupService();
        private readonly ItemReferenceService itemReferenceService = new ItemReferenceService();
        private readonly IconResolutionService iconResolutionService = new IconResolutionService();
        private readonly ToolTip previewToolTip = new ToolTip();
        private const int MaxDisplayedTasks = 20000;
        private readonly ISessionService sessionService;
        private TaskEditorData currentData;
        private List<TaskEditorEntry> filteredEntries = new List<TaskEditorEntry>();
        private Dictionary<int, ItemReferenceOption> itemOptionsById;
        private Dictionary<int, long> levelExperienceByLevel;
        private int loadRequestId;
        private bool isLoading;

        private TextBox rootPathBox;
        private Button openFolderButton;
        private Button reloadButton;
        private Button searchButton;
        private TextBox searchBox;
        private DataGridView taskGrid;
        private Label statusLabel;
        private Label summaryLabel;
        private SplitContainer mainSplit;
        private FlowLayoutPanel overviewPanel;
        private FlowLayoutPanel questPreviewPanel;
        private DataGridView itemGrid;
        private DataGridView generalGrid;
        private DataGridView fieldGrid;
        private DataGridView textGrid;
        private DataGridView rawGrid;
        private TextBox hexBox;
        private Panel loadingPanel;
        private Label loadingLabel;
        private ProgressBar loadingProgress;

        public TaskEditorWindow(ISessionService sessionService)
        {
            this.sessionService = sessionService;
            Text = "FWEledit - Task Viewer";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1180, 760);
            Size = new Size(1320, 860);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BuildUi();
            ApplyTheme(this);
            Shown += (s, e) => EnsureInitialSplitterLayout();
        }

        public void OpenDefaultGameRoot()
        {
            string root = AssetManager.GameRootPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(root) && sessionService != null && sessionService.AssetManager != null)
            {
                root = AssetManager.GameRootPath ?? string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
            {
                LoadGameRoot(root);
            }
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            root.Padding = new Padding(10);
            Controls.Add(root);

            TableLayoutPanel header = new TableLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.ColumnCount = 5;
            header.RowCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            root.Controls.Add(header, 0, 0);

            rootPathBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            openFolderButton = new Button { Text = "Open", Dock = DockStyle.Fill };
            reloadButton = new Button { Text = "Reload", Dock = DockStyle.Fill };
            searchButton = new Button { Text = "Search", Dock = DockStyle.Fill };
            searchBox = new TextBox { Dock = DockStyle.Fill };
            openFolderButton.Click += openFolderButton_Click;
            reloadButton.Click += reloadButton_Click;
            searchButton.Click += (s, e) => ApplyFilter(false);
            searchBox.KeyDown += searchBox_KeyDown;

            header.Controls.Add(rootPathBox, 0, 0);
            header.Controls.Add(openFolderButton, 1, 0);
            header.Controls.Add(reloadButton, 2, 0);
            header.Controls.Add(new Label { Text = "Read-only viewer", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 4, 0);
            header.Controls.Add(searchBox, 0, 1);
            header.SetColumnSpan(searchBox, 4);
            header.Controls.Add(searchButton, 4, 1);

            Panel contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            root.Controls.Add(contentPanel, 0, 1);

            mainSplit = new SplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.FixedPanel = FixedPanel.Panel1;
            mainSplit.SplitterWidth = 6;
            contentPanel.Controls.Add(mainSplit);

            taskGrid = CreateGrid();
            taskGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 72 });
            taskGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            taskGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Shard", Width = 58 });
            taskGrid.SelectionChanged += taskGrid_SelectionChanged;
            mainSplit.Panel1.Controls.Add(taskGrid);

            TableLayoutPanel right = new TableLayoutPanel();
            right.Dock = DockStyle.Fill;
            right.RowCount = 2;
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainSplit.Panel2.Controls.Add(right);

            summaryLabel = new Label();
            summaryLabel.Dock = DockStyle.Fill;
            summaryLabel.Padding = new Padding(8);
            summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            right.Controls.Add(summaryLabel, 0, 0);

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            right.Controls.Add(tabs, 0, 1);

            TabPage overviewTab = new TabPage("Overview");
            overviewPanel = new FlowLayoutPanel();
            overviewPanel.Dock = DockStyle.Fill;
            overviewPanel.AutoScroll = true;
            overviewPanel.FlowDirection = FlowDirection.TopDown;
            overviewPanel.WrapContents = false;
            overviewPanel.Padding = new Padding(10);
            overviewPanel.Resize += (s, e) => AdjustOverviewCardWidths();
            overviewTab.Controls.Add(overviewPanel);
            tabs.TabPages.Add(overviewTab);

            TabPage questPreviewTab = new TabPage("Quest Preview");
            questPreviewPanel = new FlowLayoutPanel();
            questPreviewPanel.Tag = "quest-preview";
            questPreviewPanel.Dock = DockStyle.Fill;
            questPreviewPanel.AutoScroll = true;
            questPreviewPanel.FlowDirection = FlowDirection.TopDown;
            questPreviewPanel.WrapContents = false;
            questPreviewPanel.Padding = new Padding(14);
            questPreviewPanel.BackColor = Color.FromArgb(17, 21, 26);
            questPreviewPanel.Resize += (s, e) => AdjustQuestPreviewWidth();
            questPreviewTab.Controls.Add(questPreviewPanel);
            tabs.TabPages.Add(questPreviewTab);

            TabPage itemTab = new TabPage("Items");
            itemGrid = CreateGrid();
            itemGrid.RowTemplate.Height = 30;
            itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kind", Width = 150 });
            DataGridViewImageColumn itemIconColumn = new DataGridViewImageColumn();
            itemIconColumn.HeaderText = "Icon";
            itemIconColumn.Width = 42;
            itemIconColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
            itemIconColumn.DefaultCellStyle.NullValue = Properties.Resources.NoIcon;
            itemGrid.Columns.Add(itemIconColumn);
            itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 86 });
            itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Count", Width = 72 });
            itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Source", Width = 220 });
            itemGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Offset", Width = 82 });
            itemTab.Controls.Add(itemGrid);
            tabs.TabPages.Add(itemTab);

            TabPage generalTab = new TabPage("General");
            generalGrid = CreateGrid();
            generalGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Field", Width = 190 });
            generalGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            generalTab.Controls.Add(generalGrid);
            tabs.TabPages.Add(generalTab);

            TabPage fieldsTab = new TabPage("Decoded Fields");
            fieldGrid = CreateGrid();
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Group", Width = 112 });
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Field", Width = 180 });
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", Width = 190 });
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Meaning", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Source", Width = 150 });
            fieldGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Offset", Width = 82 });
            fieldsTab.Controls.Add(fieldGrid);
            tabs.TabPages.Add(fieldsTab);

            TabPage textTab = new TabPage("Texts");
            textGrid = CreateGrid();
            textGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Offset", Width = 82 });
            textGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kind", Width = 130 });
            textGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Text", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            textTab.Controls.Add(textGrid);
            tabs.TabPages.Add(textTab);

            TabPage rawTab = new TabPage("All Values");
            rawGrid = CreateGrid();
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Group", Width = 112 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Field", Width = 190 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Source", Width = 190 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Offset", Width = 82 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", Width = 112 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", Width = 260 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Meaning", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            rawTab.Controls.Add(rawGrid);
            tabs.TabPages.Add(rawTab);

            TabPage hexTab = new TabPage("Hex");
            hexBox = new TextBox();
            hexBox.Dock = DockStyle.Fill;
            hexBox.Multiline = true;
            hexBox.ReadOnly = true;
            hexBox.ScrollBars = ScrollBars.Both;
            hexBox.WordWrap = false;
            hexBox.Font = new Font("Consolas", 9F);
            hexTab.Controls.Add(hexBox);
            tabs.TabPages.Add(hexTab);

            statusLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            root.Controls.Add(statusLabel, 0, 2);

            loadingPanel = new Panel();
            loadingPanel.Dock = DockStyle.Fill;
            loadingPanel.Visible = false;
            loadingPanel.BackColor = Color.FromArgb(17, 21, 26);

            TableLayoutPanel loadingLayout = new TableLayoutPanel();
            loadingLayout.Dock = DockStyle.Fill;
            loadingLayout.ColumnCount = 3;
            loadingLayout.RowCount = 3;
            loadingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            loadingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420));
            loadingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            loadingLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            loadingLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            loadingLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            Panel loadingCard = new Panel();
            loadingCard.Dock = DockStyle.Fill;
            loadingCard.Padding = new Padding(18);
            loadingCard.BackColor = Color.FromArgb(24, 28, 34);

            loadingLabel = new Label();
            loadingLabel.Dock = DockStyle.Top;
            loadingLabel.Height = 38;
            loadingLabel.Text = "Loading tasks.data...";
            loadingLabel.TextAlign = ContentAlignment.MiddleLeft;
            loadingLabel.ForeColor = Color.FromArgb(229, 234, 242);

            loadingProgress = new ProgressBar();
            loadingProgress.Dock = DockStyle.Top;
            loadingProgress.Height = 22;
            loadingProgress.Style = ProgressBarStyle.Marquee;
            loadingProgress.MarqueeAnimationSpeed = 30;

            loadingCard.Controls.Add(loadingProgress);
            loadingCard.Controls.Add(loadingLabel);
            loadingLayout.Controls.Add(loadingCard, 1, 1);
            loadingPanel.Controls.Add(loadingLayout);
            contentPanel.Controls.Add(loadingPanel);
            loadingPanel.BringToFront();
        }

        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.BackgroundColor = Color.FromArgb(18, 21, 26);
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 26;
            grid.Dock = DockStyle.Fill;
            grid.EnableHeadersVisualStyles = false;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.RowTemplate.Height = 24;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            return grid;
        }

        private async void LoadGameRoot(string gameRootPath)
        {
            int requestId = ++loadRequestId;
            try
            {
                SetLoadingState(true, "Loading tasks.data from " + gameRootPath);
                TaskEditorData loadedData = await Task.Run(() => fileService.LoadFromGameRoot(gameRootPath));
                if (requestId != loadRequestId)
                {
                    return;
                }

                currentData = loadedData;
                rootPathBox.Text = gameRootPath;
                ApplyFilter(true);
                EnsureInitialSplitterLayout();
                statusLabel.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Loaded {0:N0} task entries from {1:N0} shard(s). Showing {2:N0}.",
                    currentData.Entries.Count,
                    currentData.Shards.Count,
                    filteredEntries.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load tasks.data files.\n\n" + ex.Message, "Task Viewer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                if (requestId == loadRequestId)
                {
                    SetLoadingState(false, string.Empty);
                }
            }
        }

        private void ApplyFilter(bool force)
        {
            if (isLoading && !force)
            {
                return;
            }

            taskGrid.Rows.Clear();
            ClearDetails();

            if (currentData == null)
            {
                statusLabel.Text = "Open a game folder that contains data\\tasks.data0.";
                return;
            }

            string query = (searchBox.Text ?? string.Empty).Trim();
            filteredEntries = BuildVisibleTaskEntries(query)
                .Take(MaxDisplayedTasks)
                .ToList();

            foreach (TaskEditorEntry entry in filteredEntries)
            {
                int rowIndex = taskGrid.Rows.Add(entry.Id, FormatTaskTreeName(entry), entry.ShardIndex);
                DataGridViewRow row = taskGrid.Rows[rowIndex];
                row.Tag = entry;
                if (entry.TreeDepth > 0)
                {
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(195, 217, 238);
                }
            }

            if (taskGrid.Rows.Count > 0)
            {
                DataGridViewRow selectedRow = FindPreferredInitialRow(query) ?? taskGrid.Rows[0];
                selectedRow.Selected = true;
                taskGrid.CurrentCell = selectedRow.Cells[0];
                LoadEntry(selectedRow.Tag as TaskEditorEntry);
            }

            taskGrid.Invalidate();
            taskGrid.Refresh();

            statusLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "Showing {0:N0} of {1:N0} task entries.",
                filteredEntries.Count,
                currentData.Entries.Count);
        }

        private DataGridViewRow FindPreferredInitialRow(string query)
        {
            if (taskGrid == null || taskGrid.Rows.Count == 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return taskGrid.Rows[0];
            }

            foreach (DataGridViewRow row in taskGrid.Rows)
            {
                TaskEditorEntry entry = row.Tag as TaskEditorEntry;
                if (entry != null
                    && (string.Equals(entry.Name, query, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(entry.Id.ToString(CultureInfo.InvariantCulture), query, StringComparison.OrdinalIgnoreCase)))
                {
                    return row;
                }
            }

            foreach (DataGridViewRow row in taskGrid.Rows)
            {
                TaskEditorEntry entry = row.Tag as TaskEditorEntry;
                if (Matches(entry, query))
                {
                    return row;
                }
            }

            return taskGrid.Rows[0];
        }

        private List<TaskEditorEntry> BuildVisibleTaskEntries(string query)
        {
            if (currentData == null || currentData.Entries == null)
            {
                return new List<TaskEditorEntry>();
            }

            Dictionary<int, TaskEditorEntry> byId = currentData.Entries
                .GroupBy(entry => entry.Id)
                .ToDictionary(group => group.Key, group => group.First());
            Dictionary<int, List<TaskEditorEntry>> childrenByParent = currentData.Entries
                .Where(entry => entry.ParentId > 0)
                .GroupBy(entry => entry.ParentId)
                .ToDictionary(group => group.Key, group => SortTaskEntries(group.ToList()));

            HashSet<int> included = new HashSet<int>();
            bool hasQuery = !string.IsNullOrWhiteSpace(query);
            if (hasQuery)
            {
                foreach (TaskEditorEntry entry in currentData.Entries.Where(entry => Matches(entry, query)))
                {
                    IncludeEntryWithContext(entry, byId, childrenByParent, included);
                }
            }

            List<TaskEditorEntry> roots = currentData.Entries
                .Where(entry => entry.ParentId <= 0 || !byId.ContainsKey(entry.ParentId))
                .ToList();
            roots = SortTaskEntries(roots);

            List<TaskEditorEntry> result = new List<TaskEditorEntry>();
            HashSet<int> emitted = new HashSet<int>();
            foreach (TaskEditorEntry root in roots)
            {
                AppendTaskTree(root, childrenByParent, included, hasQuery, 0, result, emitted);
            }

            foreach (TaskEditorEntry entry in SortTaskEntries(currentData.Entries))
            {
                if (emitted.Contains(entry.Id))
                {
                    continue;
                }

                AppendTaskTree(entry, childrenByParent, included, hasQuery, 0, result, emitted);
            }

            return result;
        }

        private void IncludeEntryWithContext(
            TaskEditorEntry entry,
            Dictionary<int, TaskEditorEntry> byId,
            Dictionary<int, List<TaskEditorEntry>> childrenByParent,
            HashSet<int> included)
        {
            if (entry == null || included == null)
            {
                return;
            }

            bool wasAlreadyIncluded = included.Contains(entry.Id);
            included.Add(entry.Id);
            if (wasAlreadyIncluded)
            {
                return;
            }

            TaskEditorEntry cursor = entry;
            HashSet<int> guard = new HashSet<int>();
            while (cursor != null && cursor.ParentId > 0 && guard.Add(cursor.Id))
            {
                TaskEditorEntry parent;
                if (!byId.TryGetValue(cursor.ParentId, out parent) || parent == null)
                {
                    break;
                }

                included.Add(parent.Id);
                cursor = parent;
            }

            List<TaskEditorEntry> children;
            if (childrenByParent.TryGetValue(entry.Id, out children))
            {
                foreach (TaskEditorEntry child in children)
                {
                    IncludeEntryWithContext(child, byId, childrenByParent, included);
                }
            }
        }

        private static void AppendTaskTree(
            TaskEditorEntry entry,
            Dictionary<int, List<TaskEditorEntry>> childrenByParent,
            HashSet<int> included,
            bool hasQuery,
            int depth,
            List<TaskEditorEntry> result,
            HashSet<int> emitted)
        {
            if (entry == null || result == null || emitted == null || emitted.Contains(entry.Id))
            {
                return;
            }

            bool include = !hasQuery || (included != null && included.Contains(entry.Id));
            if (include)
            {
                entry.TreeDepth = Math.Min(depth, 12);
                entry.HasChildren = entry.HasChildren || (childrenByParent != null && childrenByParent.ContainsKey(entry.Id));
                result.Add(entry);
                emitted.Add(entry.Id);
            }

            List<TaskEditorEntry> children;
            if (childrenByParent == null || !childrenByParent.TryGetValue(entry.Id, out children))
            {
                return;
            }

            foreach (TaskEditorEntry child in children)
            {
                AppendTaskTree(child, childrenByParent, included, hasQuery, depth + 1, result, emitted);
            }
        }

        private static List<TaskEditorEntry> SortTaskEntries(List<TaskEditorEntry> entries)
        {
            return (entries ?? new List<TaskEditorEntry>())
                .OrderBy(entry => entry.ShardIndex)
                .ThenBy(entry => entry.ChunkIndex)
                .ThenBy(entry => entry.LocalOffset)
                .ThenBy(entry => entry.Id)
                .ToList();
        }

        private static string FormatTaskTreeName(TaskEditorEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            string prefix = new string(' ', Math.Max(0, entry.TreeDepth) * 3);
            if (entry.HasChildren)
            {
                prefix += "+ ";
            }
            else if (entry.TreeDepth > 0)
            {
                prefix += "- ";
            }

            return prefix + (entry.Name ?? string.Empty);
        }

        private bool Matches(TaskEditorEntry entry, string query)
        {
            if (entry == null)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            return entry.Id.ToString(CultureInfo.InvariantCulture).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || (entry.Name ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void taskGrid_SelectionChanged(object sender, EventArgs e)
        {
            TaskEditorEntry entry = taskGrid.CurrentRow != null ? taskGrid.CurrentRow.Tag as TaskEditorEntry : null;
            LoadEntry(entry);
        }

        private void LoadEntry(TaskEditorEntry entry)
        {
            ClearDetails();
            if (entry == null)
            {
                return;
            }

            List<TaskEditorTextValue> texts = TaskEditorFileService.ExtractUnicodeTexts(entry.Bytes);
            List<TaskEditorFieldValue> knownFields = TaskEditorFileService.BuildKnownFields(entry.Bytes);
            List<TaskEditorFieldValue> allMappedFields = TaskEditorFileService.BuildAllMappedFields(entry.Bytes);
            List<TaskEditorItemValue> itemValues = TaskEditorFileService.BuildItemValues(entry.Bytes);
            List<TaskEditorMonsterValue> monsterValues = TaskEditorFileService.BuildMonsterWantedValues(entry.Bytes);
            ResolveTaskItems(itemValues);
            itemValues = FilterResolvedTaskItems(itemValues);
            ResolveTaskMonsters(monsterValues);

            summaryLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0} - {1}\r\n{2} | chunk {3} | offset 0x{4:X} | size {5:N0} bytes",
                entry.Id,
                entry.Name,
                entry.ShardName,
                entry.ChunkIndex,
                entry.AbsoluteOffset,
                entry.Size);

            AddGeneral("ID", entry.Id.ToString(CultureInfo.InvariantCulture));
            AddGeneral("Name", entry.Name);
            AddGeneral("Type", GetKnownFieldValue(knownFields, "m_ulType"));
            AddGeneral("Delivery NPC", FormatNpc(GetKnownFieldValue(knownFields, "m_ulDelvNPC")));
            AddGeneral("Award NPC", FormatNpc(GetKnownFieldValue(knownFields, "m_ulAwardNPC")));
            AddGeneral("Cooldown", GetKnownFieldValue(knownFields, "m_lTimeInterval"));
            AddGeneral("Avail Frequency", GetKnownFieldValue(knownFields, "m_lAvailFrequency"));
            AddGeneral("Shard", entry.ShardName);
            AddGeneral("Shard index", entry.ShardIndex.ToString(CultureInfo.InvariantCulture));
            AddGeneral("Chunk index", entry.ChunkIndex.ToString(CultureInfo.InvariantCulture));
            AddGeneral("Chunk start", "0x" + entry.ChunkStartOffset.ToString("X", CultureInfo.InvariantCulture));
            AddGeneral("Local task offset", "0x" + entry.LocalOffset.ToString("X", CultureInfo.InvariantCulture));
            AddGeneral("Absolute task offset", "0x" + entry.AbsoluteOffset.ToString("X", CultureInfo.InvariantCulture));
            AddGeneral("Size", entry.Size.ToString(CultureInfo.InvariantCulture));
            AddGeneral("Detected text blocks", texts.Count.ToString(CultureInfo.InvariantCulture));
            AddGeneral("Detected task items", itemValues.Count.ToString(CultureInfo.InvariantCulture));

            SetOverviewCards(entry, knownFields, texts, itemValues);
            SetQuestPreview(entry, knownFields, texts, itemValues, monsterValues);

            foreach (TaskEditorFieldValue field in knownFields)
            {
                int rowIndex = fieldGrid.Rows.Add(field.Section, field.DisplayName, field.Value, field.Meaning, field.Field, field.HexOffset);
                StyleFieldRow(fieldGrid.Rows[rowIndex], field.Section);
            }

            foreach (TaskEditorItemValue item in itemValues)
            {
                int rowIndex = itemGrid.Rows.Add(
                    item.Kind,
                    ResolveTaskItemIcon(item),
                    item.ItemId.ToString(CultureInfo.InvariantCulture),
                    string.IsNullOrWhiteSpace(item.Name) ? "Item " + item.ItemId.ToString(CultureInfo.InvariantCulture) : item.Name,
                    item.Count.ToString(CultureInfo.InvariantCulture),
                    item.Source,
                    item.HexOffset);
                StyleItemRow(itemGrid.Rows[rowIndex], item);
            }

            foreach (TaskEditorTextValue text in texts)
            {
                textGrid.Rows.Add("0x" + text.Offset.ToString("X4", CultureInfo.InvariantCulture), ClassifyText(text), text.Text);
            }

            foreach (TaskEditorFieldValue field in allMappedFields)
            {
                int rowIndex = rawGrid.Rows.Add(field.Section, field.DisplayName, field.Field, field.HexOffset, field.Type, field.Value, field.Meaning);
                StyleFieldRow(rawGrid.Rows[rowIndex], field.Section);
            }

            hexBox.Text = BuildHexPreview(entry.Bytes, 4096);
        }

        private void AddGeneral(string name, string value)
        {
            generalGrid.Rows.Add(name ?? string.Empty, value ?? string.Empty);
        }

        private string GetKnownFieldValue(List<TaskEditorFieldValue> fields, string fieldName)
        {
            if (fields == null)
            {
                return string.Empty;
            }

            TaskEditorFieldValue field = fields.FirstOrDefault(value => string.Equals(value.Field, fieldName, StringComparison.Ordinal));
            return field != null ? field.Value : string.Empty;
        }

        private string BuildOverview(TaskEditorEntry entry, List<TaskEditorFieldValue> fields, List<TaskEditorTextValue> texts, List<TaskEditorItemValue> itemValues)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(entry.Id.ToString(CultureInfo.InvariantCulture) + " - " + (entry.Name ?? string.Empty));
            builder.AppendLine();

            AppendSection(builder, "Player-facing summary");
            AppendItem(builder, "Task type", DescribeTaskType(GetKnownFieldValue(fields, "m_ulType")));
            AppendItem(builder, "Completion method", DescribeCompletionMethod(GetKnownFieldValue(fields, "m_enumMethod")));
            AppendItem(builder, "Finish handoff", DescribeFinishType(GetKnownFieldValue(fields, "m_enumFinishType")));
            AppendItem(builder, "Recommended level", ZeroAsNone(GetKnownFieldValue(fields, "m_ulSuitableLevel")));
            AppendItem(builder, "Start NPC", FormatNpc(GetKnownFieldValue(fields, "m_ulDelvNPC")));
            AppendItem(builder, "Finish NPC", FormatNpc(GetKnownFieldValue(fields, "m_ulAwardNPC")));
            AppendItem(builder, "Visibility", IsYes(fields, "m_bHidden") ? "Hidden until unlocked or triggered" : "Visible when conditions allow it");
            AppendItem(builder, "Tracking", BuildTrackingSummary(fields));
            AppendItem(builder, "Importance", IsYes(fields, "m_bKeyTask") ? "Marked as a key task" : "Normal task");
            AppendItem(builder, "Task tree", BuildTaskTreeSummary(entry));

            builder.AppendLine();
            AppendSection(builder, "Availability and repeat rules");
            AppendItem(builder, "Can give up", YesNoMeaning(fields, "m_bCanGiveUp"));
            AppendItem(builder, "Repeatable", YesNoMeaning(fields, "m_bCanRedo"));
            AppendItem(builder, "Retry after failure", YesNoMeaning(fields, "m_bCanRedoAfterFailure"));
            AppendItem(builder, "Available times", ZeroAsNone(GetKnownFieldValue(fields, "m_lAvailFrequency")));
            AppendItem(builder, "Cooldown", FormatSeconds(GetKnownFieldValue(fields, "m_lTimeInterval")));
            AppendItem(builder, "Time limit", FormatSeconds(GetKnownFieldValue(fields, "m_ulTimeLimit")));

            builder.AppendLine();
            AppendSection(builder, "Game flow");
            AppendItem(builder, "Auto delivery", YesNoMeaning(fields, "m_bAutoDeliver"));
            AppendItem(builder, "Death behavior", IsYes(fields, "m_bFailAsPlayerDie") ? "Fails when the player dies" : "Does not fail from death flag");
            AppendItem(builder, "Cleanup", IsYes(fields, "m_bClearAcquired") ? "Removes acquired task items during cleanup" : "No acquired-item cleanup flag");
            AppendItem(builder, "Lua logic", IsYes(fields, "m_bLuaTask") ? "Uses Lua-side behavior" : "No Lua task flag");

            List<TaskEditorItemValue> requiredItems = GetItemsByKind(itemValues, "Required item", "Completion item", "Submit item");
            if (requiredItems.Count > 0)
            {
                builder.AppendLine();
                AppendSection(builder, "Required items");
                foreach (TaskEditorItemValue item in requiredItems)
                {
                    AppendItem(builder, item.Kind, FormatTaskItemValue(item));
                }
            }

            List<TaskEditorFieldValue> rewards = GetRewardFields(fields);
            List<TaskEditorItemValue> rewardItems = GetItemsByKind(itemValues, "Success reward item", "Given item");
            if (rewards.Count > 0 || rewardItems.Count > 0)
            {
                builder.AppendLine();
                AppendSection(builder, "Rewards");
                AppendItem(builder, "Success reward mode", DescribeAwardType(GetKnownFieldValue(fields, "m_ulAwardType_S")));
                AppendItem(builder, "Failure reward mode", DescribeAwardType(GetKnownFieldValue(fields, "m_ulAwardType_F")));
                foreach (TaskEditorItemValue item in rewardItems)
                {
                    AppendItem(builder, item.Kind, FormatTaskItemValue(item));
                }
                foreach (TaskEditorFieldValue reward in rewards)
                {
                    AppendItem(builder, reward.DisplayName, FormatRewardValue(reward, fields));
                }
            }

            string description = GetDescriptionText(texts, entry.Name);
            if (!string.IsNullOrWhiteSpace(description))
            {
                builder.AppendLine();
                AppendSection(builder, "Description");
                builder.AppendLine(WrapLongText(description));
            }

            List<string> dialogLines = GetDialogPreview(texts, entry.Name, description);
            if (dialogLines.Count > 0)
            {
                builder.AppendLine();
                AppendSection(builder, "Dialog preview");
                foreach (string line in dialogLines)
                {
                    builder.AppendLine("- " + line);
                }
            }

            builder.AppendLine();
            AppendSection(builder, "Technical source");
            AppendItem(builder, "Shard", entry.ShardName + " / chunk " + entry.ChunkIndex.ToString(CultureInfo.InvariantCulture));
            AppendItem(builder, "Offset", "0x" + entry.AbsoluteOffset.ToString("X", CultureInfo.InvariantCulture));
            AppendItem(builder, "Size", entry.Size.ToString("N0", CultureInfo.InvariantCulture) + " bytes");
            return builder.ToString();
        }

        private void SetOverviewCards(TaskEditorEntry entry, List<TaskEditorFieldValue> fields, List<TaskEditorTextValue> texts, List<TaskEditorItemValue> itemValues)
        {
            if (overviewPanel == null)
            {
                return;
            }

            overviewPanel.SuspendLayout();
            overviewPanel.Controls.Clear();

            AddOverviewHeader(entry);

            AddOverviewCard(
                "Player-facing summary",
                GetOverviewSectionColor("PLAYER-FACING SUMMARY"),
                new[]
                {
                    new OverviewRow("Task type", DescribeTaskType(GetKnownFieldValue(fields, "m_ulType"))),
                    new OverviewRow("Completion method", DescribeCompletionMethod(GetKnownFieldValue(fields, "m_enumMethod"))),
                    new OverviewRow("Finish handoff", DescribeFinishType(GetKnownFieldValue(fields, "m_enumFinishType"))),
                    new OverviewRow("Recommended level", ZeroAsNone(GetKnownFieldValue(fields, "m_ulSuitableLevel"))),
                    new OverviewRow("Start NPC", FormatNpc(GetKnownFieldValue(fields, "m_ulDelvNPC"))),
                    new OverviewRow("Finish NPC", FormatNpc(GetKnownFieldValue(fields, "m_ulAwardNPC"))),
                    new OverviewRow("Visibility", IsYes(fields, "m_bHidden") ? "Hidden until unlocked or triggered" : "Visible when conditions allow it"),
                    new OverviewRow("Tracking", BuildTrackingSummary(fields)),
                    new OverviewRow("Importance", IsYes(fields, "m_bKeyTask") ? "Marked as a key task" : "Normal task"),
                    new OverviewRow("Task tree", BuildTaskTreeSummary(entry))
                });

            AddOverviewCard(
                "Availability and repeat rules",
                GetOverviewSectionColor("AVAILABILITY AND REPEAT RULES"),
                new[]
                {
                    new OverviewRow("Can give up", YesNoMeaning(fields, "m_bCanGiveUp")),
                    new OverviewRow("Repeatable", YesNoMeaning(fields, "m_bCanRedo")),
                    new OverviewRow("Retry after failure", YesNoMeaning(fields, "m_bCanRedoAfterFailure")),
                    new OverviewRow("Available times", ZeroAsNone(GetKnownFieldValue(fields, "m_lAvailFrequency"))),
                    new OverviewRow("Cooldown", FormatSeconds(GetKnownFieldValue(fields, "m_lTimeInterval"))),
                    new OverviewRow("Time limit", FormatSeconds(GetKnownFieldValue(fields, "m_ulTimeLimit")))
                });

            AddOverviewCard(
                "Game flow",
                GetOverviewSectionColor("GAME FLOW"),
                new[]
                {
                    new OverviewRow("Auto delivery", YesNoMeaning(fields, "m_bAutoDeliver")),
                    new OverviewRow("Death behavior", IsYes(fields, "m_bFailAsPlayerDie") ? "Fails when the player dies" : "Does not fail from death flag"),
                    new OverviewRow("Cleanup", IsYes(fields, "m_bClearAcquired") ? "Removes acquired task items during cleanup" : "No acquired-item cleanup flag"),
                    new OverviewRow("Lua logic", IsYes(fields, "m_bLuaTask") ? "Uses Lua-side behavior" : "No Lua task flag")
                });

            List<TaskEditorItemValue> requiredItems = GetItemsByKind(itemValues, "Required item", "Completion item", "Submit item");
            if (requiredItems.Count > 0)
            {
                List<OverviewRow> rows = new List<OverviewRow>();
                foreach (TaskEditorItemValue item in requiredItems)
                {
                    rows.Add(new OverviewRow(item.Kind, FormatTaskItemValue(item), item));
                }
                AddOverviewCard("Required items", GetOverviewSectionColor("REQUIRED ITEMS"), rows);
            }

            List<TaskEditorFieldValue> rewards = GetRewardFields(fields);
            List<TaskEditorItemValue> rewardItems = GetItemsByKind(itemValues, "Success reward item", "Given item");
            if (rewards.Count > 0 || rewardItems.Count > 0)
            {
                List<OverviewRow> rows = new List<OverviewRow>
                {
                    new OverviewRow("Success reward mode", DescribeAwardType(GetKnownFieldValue(fields, "m_ulAwardType_S"))),
                    new OverviewRow("Failure reward mode", DescribeAwardType(GetKnownFieldValue(fields, "m_ulAwardType_F")))
                };

                foreach (TaskEditorItemValue item in rewardItems)
                {
                    rows.Add(new OverviewRow(item.Kind, FormatTaskItemValue(item), item));
                }
                foreach (TaskEditorFieldValue reward in rewards)
                {
                    rows.Add(new OverviewRow(reward.DisplayName, FormatRewardValue(reward, fields)));
                }
                AddOverviewCard("Rewards", GetOverviewSectionColor("REWARDS"), rows);
            }

            string description = GetDescriptionText(texts, entry != null ? entry.Name : string.Empty);
            if (!string.IsNullOrWhiteSpace(description))
            {
                AddOverviewTextCard("Description", GetOverviewSectionColor("DESCRIPTION"), WrapLongText(description));
            }

            List<string> dialogLines = GetDialogPreview(texts, entry != null ? entry.Name : string.Empty, description);
            if (dialogLines.Count > 0)
            {
                AddOverviewTextCard("Dialog preview", GetOverviewSectionColor("DIALOG PREVIEW"), string.Join(Environment.NewLine, dialogLines.Select(line => "- " + line).ToArray()));
            }

            AddOverviewCard(
                "Technical source",
                GetOverviewSectionColor("TECHNICAL SOURCE"),
                new[]
                {
                    new OverviewRow("Shard", entry != null ? entry.ShardName + " / chunk " + entry.ChunkIndex.ToString(CultureInfo.InvariantCulture) : "-"),
                    new OverviewRow("Offset", entry != null ? "0x" + entry.AbsoluteOffset.ToString("X", CultureInfo.InvariantCulture) : "-"),
                    new OverviewRow("Size", entry != null ? entry.Size.ToString("N0", CultureInfo.InvariantCulture) + " bytes" : "-")
                });

            overviewPanel.ResumeLayout();
            AdjustOverviewCardWidths();
        }

        private void AddOverviewHeader(TaskEditorEntry entry)
        {
            Panel card = CreateOverviewCard(Color.FromArgb(255, 235, 170));
            Label title = new Label();
            title.AutoSize = false;
            title.Location = new Point(16, 10);
            title.Size = new Size(Math.Max(260, card.Width - 32), 30);
            title.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point, 0);
            title.ForeColor = Color.FromArgb(255, 235, 170);
            title.Text = entry == null ? string.Empty : entry.Id.ToString(CultureInfo.InvariantCulture) + " - " + (entry.Name ?? string.Empty);
            card.Controls.Add(title);
            SetOverviewCardHeight(card, title.Bottom + 10);
            overviewPanel.Controls.Add(card);
        }

        private void AddOverviewTextCard(string title, Color accent, string text)
        {
            AddOverviewCard(title, accent, new[] { new OverviewRow(string.Empty, text) });
        }

        private void AddOverviewCard(string title, Color accent, IEnumerable<OverviewRow> rows)
        {
            Panel card = CreateOverviewCard(accent);
            Label titleLabel = new Label();
            titleLabel.AutoSize = false;
            titleLabel.Location = new Point(16, 10);
            titleLabel.Size = new Size(Math.Max(260, card.Width - 32), 22);
            titleLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
            titleLabel.ForeColor = accent;
            titleLabel.Text = title ?? string.Empty;
            card.Controls.Add(titleLabel);

            TableLayoutPanel table = new TableLayoutPanel();
            table.AutoSize = true;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            table.Location = new Point(16, titleLabel.Bottom + 8);
            table.Width = Math.Max(260, card.Width - 32);
            table.ColumnCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Math.Max(120, table.Width - 150)));
            card.Controls.Add(table);

            int rowIndex = 0;
            foreach (OverviewRow row in rows ?? Enumerable.Empty<OverviewRow>())
            {
                table.RowCount = rowIndex + 1;
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                Label label = CreateOverviewLabel(row.Label, Color.FromArgb(146, 158, 174), FontStyle.Regular);
                table.Controls.Add(label, 0, rowIndex);
                Control valueControl = CreateOverviewValueControl(row);
                table.Controls.Add(valueControl, 1, rowIndex);
                rowIndex++;
            }

            ResizeOverviewCardContent(card);
            overviewPanel.Controls.Add(card);
        }

        private Panel CreateOverviewCard(Color accent)
        {
            Panel card = new Panel();
            card.Tag = "overview-card";
            card.AutoSize = false;
            card.Width = GetOverviewCardWidth();
            card.Margin = new Padding(0, 0, 0, 10);
            card.BackColor = Color.FromArgb(22, 27, 34);

            Panel stripe = new Panel();
            stripe.Location = new Point(0, 0);
            stripe.Width = 3;
            stripe.Height = 1;
            stripe.BackColor = accent;
            stripe.Tag = "overview-stripe";
            card.Controls.Add(stripe);

            return card;
        }

        private Control CreateOverviewValueControl(OverviewRow row)
        {
            if (row != null && row.Item != null)
            {
                FlowLayoutPanel itemPanel = new FlowLayoutPanel();
                itemPanel.AutoSize = true;
                itemPanel.WrapContents = false;
                itemPanel.Margin = new Padding(0, 0, 0, 4);
                itemPanel.Padding = new Padding(0);

                PictureBox icon = new PictureBox();
                icon.Size = new Size(22, 22);
                icon.SizeMode = PictureBoxSizeMode.Zoom;
                icon.Margin = new Padding(0, 0, 6, 0);
                icon.Image = ResolveTaskItemIcon(row.Item);
                itemPanel.Controls.Add(icon);

                Label text = CreateOverviewLabel(row.Value, ResolveTaskItemTextColor(row.Item, false), FontStyle.Bold);
                itemPanel.Controls.Add(text);
                return itemPanel;
            }

            return CreateOverviewLabel(row != null ? row.Value : string.Empty, Color.FromArgb(223, 230, 238), FontStyle.Regular);
        }

        private Label CreateOverviewLabel(string text, Color color, FontStyle style)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Margin = new Padding(0, 0, 0, 5);
            label.Font = new Font("Segoe UI", 9F, style, GraphicsUnit.Point, 0);
            label.ForeColor = color;
            label.Text = string.IsNullOrWhiteSpace(text) ? "-" : text;
            return label;
        }

        private int GetOverviewCardWidth()
        {
            if (overviewPanel == null)
            {
                return 640;
            }

            return Math.Max(320, overviewPanel.ClientSize.Width - overviewPanel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 6);
        }

        private void ResizeOverviewCardContent(Panel card)
        {
            if (card == null)
            {
                return;
            }

            int contentWidth = Math.Max(260, card.Width - 32);
            int bottom = 10;
            foreach (Control control in card.Controls)
            {
                if (string.Equals(control.Tag as string, "overview-stripe", StringComparison.Ordinal))
                {
                    continue;
                }

                control.Width = contentWidth;
                TableLayoutPanel table = control as TableLayoutPanel;
                if (table != null)
                {
                    table.Width = contentWidth;
                    table.ColumnStyles[1].Width = Math.Max(120, contentWidth - 150);
                    foreach (Control child in table.Controls)
                    {
                        int column = table.GetColumn(child);
                        int maxWidth = column == 0 ? 145 : Math.Max(120, contentWidth - 150);
                        child.MaximumSize = new Size(maxWidth, 0);
                    }
                    table.PerformLayout();
                }

                bottom = Math.Max(bottom, control.Bottom);
            }

            SetOverviewCardHeight(card, bottom + 12);
        }

        private static void SetOverviewCardHeight(Panel card, int height)
        {
            card.Height = Math.Max(44, height);
            foreach (Control control in card.Controls)
            {
                if (string.Equals(control.Tag as string, "overview-stripe", StringComparison.Ordinal))
                {
                    control.Height = card.Height;
                }
            }
        }

        private void AdjustOverviewCardWidths()
        {
            if (overviewPanel == null)
            {
                return;
            }

            int width = GetOverviewCardWidth();
            foreach (Control control in overviewPanel.Controls)
            {
                if (control != null && string.Equals(control.Tag as string, "overview-card", StringComparison.Ordinal))
                {
                    control.Width = width;
                    ResizeOverviewCardContent(control as Panel);
                }
            }
        }

        private sealed class OverviewRow
        {
            public OverviewRow(string label, string value)
                : this(label, value, null)
            {
            }

            public OverviewRow(string label, string value, TaskEditorItemValue item)
            {
                Label = label ?? string.Empty;
                Value = value ?? string.Empty;
                Item = item;
            }

            public string Label { get; private set; }
            public string Value { get; private set; }
            public TaskEditorItemValue Item { get; private set; }
        }

        private void SetQuestPreview(TaskEditorEntry entry, List<TaskEditorFieldValue> fields, List<TaskEditorTextValue> texts, List<TaskEditorItemValue> itemValues, List<TaskEditorMonsterValue> monsterValues)
        {
            if (questPreviewPanel == null)
            {
                return;
            }

            questPreviewPanel.SuspendLayout();
            questPreviewPanel.Controls.Clear();

            QuestPreviewCard card = new QuestPreviewCard();
            card.Tag = "quest-preview";
            card.Width = GetQuestPreviewCardWidth();
            card.Margin = new Padding(0, 0, 0, 14);
            card.Padding = new Padding(16, 12, 16, 16);

            int y = 14;
            string taskName = entry != null ? entry.Name : string.Empty;
            string description = GetDescriptionText(texts, taskName);

            Label title = CreateQuestPreviewLabel(taskName, 10F, FontStyle.Regular, Color.FromArgb(72, 40, 20), ContentAlignment.MiddleCenter);
            title.Location = new Point(18, y);
            title.Size = new Size(card.Width - 48, 22);
            card.Controls.Add(title);
            title.BringToFront();
            y = title.Bottom + 4;

            string objective = BuildQuestPreviewObjective(entry, fields, monsterValues);
            if (!string.IsNullOrWhiteSpace(objective))
            {
                Label objectiveLabel = CreateQuestPreviewLabel(objective, 9F, FontStyle.Regular, Color.FromArgb(60, 34, 20), ContentAlignment.TopLeft);
                objectiveLabel.Location = new Point(18, y);
                objectiveLabel.MaximumSize = new Size(card.Width - 36, 0);
                objectiveLabel.AutoSize = true;
                card.Controls.Add(objectiveLabel);
                y = objectiveLabel.Bottom + 2;
            }

            string finishNpc = GetNpcDisplayName(FormatNpc(GetKnownFieldValue(fields, "m_ulAwardNPC")));
            if (!string.IsNullOrWhiteSpace(finishNpc) && !string.Equals(finishNpc, "None", StringComparison.OrdinalIgnoreCase))
            {
                Label npcLabel = CreateQuestPreviewLabel(finishNpc, 9F, FontStyle.Regular, Color.FromArgb(0, 150, 42), ContentAlignment.TopLeft);
                npcLabel.Location = new Point(18, y);
                npcLabel.MaximumSize = new Size(card.Width - 36, 0);
                npcLabel.AutoSize = true;
                card.Controls.Add(npcLabel);
                y = npcLabel.Bottom + 10;
            }

            List<TaskEditorFieldValue> rewards = GetRewardFields(fields);
            List<TaskEditorItemValue> rewardItems = GetItemsByKind(itemValues, "Success reward item", "Given item");
            if (rewards.Count > 0 || rewardItems.Count > 0)
            {
                string rewardSummary = BuildQuestPreviewRewardText(rewards, fields);
                if (!string.IsNullOrWhiteSpace(rewardSummary))
                {
                    Label rewardText = CreateQuestPreviewLabel(rewardSummary, 9F, FontStyle.Regular, Color.FromArgb(30, 73, 66), ContentAlignment.TopLeft);
                    rewardText.Location = new Point(18, y);
                    rewardText.MaximumSize = new Size(card.Width - 36, 0);
                    rewardText.AutoSize = true;
                    card.Controls.Add(rewardText);
                    y = rewardText.Bottom + 6;
                }

                if (rewardItems.Count > 0)
                {
                    FlowLayoutPanel rewardsPanel = CreateQuestPreviewItemStrip(rewardItems);
                    rewardsPanel.Location = new Point(18, y);
                    card.Controls.Add(rewardsPanel);
                    y = rewardsPanel.Bottom + 8;
                }
            }

            List<TaskEditorItemValue> requiredItems = GetItemsByKind(itemValues, "Required item", "Completion item", "Submit item");
            if (requiredItems.Count > 0)
            {
                FlowLayoutPanel requiredPanel = CreateQuestPreviewItemStrip(requiredItems);
                requiredPanel.Location = new Point(18, y);
                card.Controls.Add(requiredPanel);
                y = requiredPanel.Bottom + 8;
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                Label body = CreateQuestPreviewLabel(description, 9F, FontStyle.Regular, Color.FromArgb(62, 38, 24), ContentAlignment.TopLeft);
                body.Location = new Point(18, y);
                body.MaximumSize = new Size(card.Width - 36, 0);
                body.AutoSize = true;
                card.Controls.Add(body);
                y = body.Bottom + 8;
            }

            string hint = BuildQuestPreviewHint(fields);
            if (!string.IsNullOrWhiteSpace(hint))
            {
                Label hintLabel = CreateQuestPreviewLabel(hint, 9F, FontStyle.Bold, Color.FromArgb(0, 138, 204), ContentAlignment.TopLeft);
                hintLabel.Location = new Point(18, y);
                hintLabel.MaximumSize = new Size(card.Width - 36, 0);
                hintLabel.AutoSize = true;
                card.Controls.Add(hintLabel);
                y = hintLabel.Bottom + 8;
            }

            card.Height = Math.Max(210, y + 16);
            questPreviewPanel.Controls.Add(card);
            questPreviewPanel.ResumeLayout();
            AdjustQuestPreviewWidth();
        }

        private string BuildQuestPreviewObjective(TaskEditorEntry entry, List<TaskEditorFieldValue> fields, List<TaskEditorMonsterValue> monsterValues)
        {
            string taskName = entry != null ? entry.Name : string.Empty;
            if (string.IsNullOrWhiteSpace(taskName))
            {
                return string.Empty;
            }

            int method;
            if (int.TryParse(GetKnownFieldValue(fields, "m_enumMethod"), NumberStyles.Integer, CultureInfo.InvariantCulture, out method))
            {
                switch (method)
                {
                    case 1:
                        string monsterObjective = BuildMonsterObjective(monsterValues);
                        return string.IsNullOrWhiteSpace(monsterObjective) ? "Defeat targets: " + taskName : monsterObjective;
                    case 2: return "Collect items: " + taskName;
                    case 3: return "Complete quest: " + taskName;
                    case 17: return "Hand in items: " + taskName;
                }
            }

            return "Complete quest: " + taskName;
        }

        private static string BuildMonsterObjective(List<TaskEditorMonsterValue> monsterValues)
        {
            if (monsterValues == null || monsterValues.Count == 0)
            {
                return string.Empty;
            }

            List<string> lines = new List<string>();
            foreach (TaskEditorMonsterValue monster in monsterValues)
            {
                if (monster == null || monster.MonsterId <= 0 || monster.Count <= 0)
                {
                    continue;
                }

                string name = string.IsNullOrWhiteSpace(monster.Name)
                    ? "Monster " + monster.MonsterId.ToString(CultureInfo.InvariantCulture)
                    : monster.Name;
                lines.Add(name + "(0/" + monster.Count.ToString(CultureInfo.InvariantCulture) + ")");
            }

            return string.Join(Environment.NewLine, lines.ToArray());
        }

        private static string GetNpcDisplayName(string formattedNpc)
        {
            if (string.IsNullOrWhiteSpace(formattedNpc))
            {
                return string.Empty;
            }

            int separator = formattedNpc.IndexOf(" - ", StringComparison.Ordinal);
            return separator >= 0 ? formattedNpc.Substring(separator + 3) : formattedNpc;
        }

        private string BuildQuestPreviewRewardText(List<TaskEditorFieldValue> rewards, List<TaskEditorFieldValue> allFields)
        {
            List<string> lines = new List<string>();
            foreach (TaskEditorFieldValue reward in rewards ?? new List<TaskEditorFieldValue>())
            {
                if (reward == null || string.IsNullOrWhiteSpace(reward.Value) || string.Equals(reward.Value, "0", StringComparison.Ordinal))
                {
                    continue;
                }
                if (IsAuxiliaryRewardField(reward))
                {
                    continue;
                }

                if (reward.DisplayName.IndexOf("EXP", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lines.Add("Exp: " + FormatExperienceReward(reward, allFields));
                }
                else if (reward.DisplayName.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lines.Add("Money: " + FormatMoneyReward(reward.Value));
                }
                else if (reward.DisplayName.IndexOf("Soul power", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lines.Add("Soul Power: " + reward.Value);
                }
                else if (IsPointRewardName(reward.DisplayName))
                {
                    lines.Add(reward.DisplayName + ": " + reward.Value);
                }
            }

            return lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines.ToArray());
        }

        private string BuildQuestPreviewHint(List<TaskEditorFieldValue> fields)
        {
            if (IsYes(fields, "m_bCanRedo") && !string.Equals(GetKnownFieldValue(fields, "m_lAvailFrequency"), "0", StringComparison.Ordinal))
            {
                return "Only can complete this task once per availability cycle.";
            }

            if (IsYes(fields, "m_bShowPrompt"))
            {
                return "Tips: the quest can be tracked in the quest list.";
            }

            return string.Empty;
        }

        private FlowLayoutPanel CreateQuestPreviewItemStrip(List<TaskEditorItemValue> items)
        {
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Tag = "quest-preview";
            panel.AutoSize = true;
            panel.WrapContents = true;
            panel.MaximumSize = new Size(GetQuestPreviewCardWidth() - 36, 0);
            panel.Padding = new Padding(0);
            panel.Margin = new Padding(0);
            panel.BackColor = Color.FromArgb(235, 226, 166);

            foreach (TaskEditorItemValue item in items ?? new List<TaskEditorItemValue>())
            {
                Panel slot = new Panel();
                slot.Tag = "quest-preview";
                slot.Size = new Size(42, 42);
                slot.Margin = new Padding(0, 0, 6, 6);
                slot.BackColor = Color.FromArgb(235, 226, 166);
                previewToolTip.SetToolTip(slot, FormatTaskItemValue(item));

                PictureBox icon = new PictureBox();
                icon.Tag = "quest-preview";
                icon.Image = ResolveTaskItemIcon(item);
                icon.SizeMode = PictureBoxSizeMode.Zoom;
                icon.Location = new Point(4, 4);
                icon.Size = new Size(34, 34);
                slot.Controls.Add(icon);
                previewToolTip.SetToolTip(icon, FormatTaskItemValue(item));

                if (item.Count > 1)
                {
                    Label count = CreateQuestPreviewLabel(item.Count.ToString(CultureInfo.InvariantCulture), 7F, FontStyle.Bold, Color.White, ContentAlignment.BottomLeft);
                    count.Tag = "quest-preview";
                    count.BackColor = Color.Transparent;
                    count.Location = new Point(3, 24);
                    count.Size = new Size(34, 14);
                    slot.Controls.Add(count);
                    count.BringToFront();
                }

                panel.Controls.Add(slot);
            }

            return panel;
        }

        private Label CreateQuestPreviewLabel(string text, float size, FontStyle style, Color color, ContentAlignment alignment)
        {
            Label label = new Label();
            label.Tag = "quest-preview";
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            label.Font = new Font("Segoe UI", size, style, GraphicsUnit.Point, 0);
            label.ForeColor = color;
            label.TextAlign = alignment;
            label.Text = text ?? string.Empty;
            return label;
        }

        private int GetQuestPreviewCardWidth()
        {
            if (questPreviewPanel == null)
            {
                return 320;
            }

            return Math.Min(360, Math.Max(280, questPreviewPanel.ClientSize.Width - questPreviewPanel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 8));
        }

        private void AdjustQuestPreviewWidth()
        {
            if (questPreviewPanel == null)
            {
                return;
            }

            int width = GetQuestPreviewCardWidth();
            foreach (Control control in questPreviewPanel.Controls)
            {
                QuestPreviewCard card = control as QuestPreviewCard;
                if (card == null)
                {
                    continue;
                }

                card.Width = width;
                foreach (Control child in card.Controls)
                {
                    Label label = child as Label;
                    if (label != null)
                    {
                        int labelWidth = label.TextAlign == ContentAlignment.MiddleCenter ? width - 48 : width - 36;
                        child.Width = labelWidth;
                        child.MaximumSize = new Size(labelWidth, 0);
                    }
                    FlowLayoutPanel strip = child as FlowLayoutPanel;
                    if (strip != null)
                    {
                        strip.MaximumSize = new Size(width - 36, 0);
                    }
                }
            }
        }

        private sealed class QuestPreviewCard : Panel
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(224, 213, 148)))
                {
                    e.Graphics.FillRectangle(brush, bounds);
                }

                using (SolidBrush center = new SolidBrush(Color.FromArgb(235, 226, 166)))
                {
                    e.Graphics.FillRectangle(center, new Rectangle(6, 6, Math.Max(1, Width - 13), Math.Max(1, Height - 13)));
                }

                using (Pen border = new Pen(Color.FromArgb(94, 71, 42), 2F))
                {
                    e.Graphics.DrawRectangle(border, bounds);
                }

                using (Pen inner = new Pen(Color.FromArgb(190, 174, 107), 1F))
                {
                    e.Graphics.DrawRectangle(inner, new Rectangle(5, 5, Math.Max(1, Width - 11), Math.Max(1, Height - 11)));
                }

                using (Font font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point, 0))
                using (SolidBrush closeBrush = new SolidBrush(Color.FromArgb(104, 95, 67)))
                {
                    e.Graphics.DrawString("x", font, closeBrush, Width - 28, 7);
                }
            }
        }

        private static Color GetOverviewSectionColor(string sectionTitle)
        {
            switch (sectionTitle)
            {
                case "PLAYER-FACING SUMMARY": return Color.FromArgb(111, 202, 255);
                case "AVAILABILITY AND REPEAT RULES": return Color.FromArgb(125, 214, 157);
                case "GAME FLOW": return Color.FromArgb(255, 196, 116);
                case "REQUIRED ITEMS": return Color.FromArgb(255, 155, 116);
                case "REWARDS": return Color.FromArgb(255, 213, 105);
                case "DESCRIPTION": return Color.FromArgb(218, 188, 255);
                case "DIALOG PREVIEW": return Color.FromArgb(255, 145, 165);
                case "TECHNICAL SOURCE": return Color.FromArgb(160, 169, 181);
                default: return Color.FromArgb(223, 230, 238);
            }
        }

        private static void AppendSection(StringBuilder builder, string title)
        {
            builder.AppendLine(title.ToUpperInvariant());
        }

        private static void AppendItem(StringBuilder builder, string label, string value)
        {
            builder.AppendLine(label + ": " + (string.IsNullOrWhiteSpace(value) ? "-" : value));
        }

        private string BuildTrackingSummary(List<TaskEditorFieldValue> fields)
        {
            bool seek = IsYes(fields, "m_bCanSeekOut");
            bool direction = IsYes(fields, "m_bShowDirection");
            bool prompt = IsYes(fields, "m_bShowPrompt");
            List<string> parts = new List<string>();
            if (seek) parts.Add("searchable");
            if (direction) parts.Add("direction marker");
            if (prompt) parts.Add("client prompt");
            return parts.Count == 0 ? "No tracking flags enabled" : string.Join(", ", parts);
        }

        private string BuildTaskTreeSummary(TaskEditorEntry entry)
        {
            if (entry == null)
            {
                return "No subtask relationship detected";
            }

            List<string> parts = new List<string>();
            TaskEditorEntry parent = FindTaskById(entry.ParentId);
            if (parent != null)
            {
                parts.Add("Child of " + parent.Id.ToString(CultureInfo.InvariantCulture) + " - " + parent.Name);
            }
            else if (entry.ParentId > 0)
            {
                parts.Add("Child of " + entry.ParentId.ToString(CultureInfo.InvariantCulture));
            }

            if (entry.HasChildren || entry.FirstChildId > 0)
            {
                TaskEditorEntry firstChild = FindTaskById(entry.FirstChildId);
                if (firstChild != null)
                {
                    parts.Add("First child " + firstChild.Id.ToString(CultureInfo.InvariantCulture) + " - " + firstChild.Name);
                }
                else
                {
                    parts.Add("Has child tasks");
                }
            }

            return parts.Count == 0 ? "No subtask relationship detected" : string.Join("; ", parts);
        }

        private TaskEditorEntry FindTaskById(int id)
        {
            if (id <= 0 || currentData == null || currentData.Entries == null)
            {
                return null;
            }

            return currentData.Entries.FirstOrDefault(entry => entry.Id == id);
        }

        private List<TaskEditorFieldValue> GetRewardFields(List<TaskEditorFieldValue> fields)
        {
            if (fields == null)
            {
                return new List<TaskEditorFieldValue>();
            }

            return fields
                .Where(field => field != null)
                .Where(field => string.Equals(field.Section, "Success reward", StringComparison.Ordinal))
                .Where(field => !string.Equals(field.DisplayName, "Item reward groups", StringComparison.OrdinalIgnoreCase))
                .Where(field => !IsAuxiliaryRewardField(field))
                .ToList();
        }

        private static List<TaskEditorItemValue> GetItemsByKind(List<TaskEditorItemValue> items, params string[] kinds)
        {
            if (items == null || kinds == null || kinds.Length == 0)
            {
                return new List<TaskEditorItemValue>();
            }

            return items
                .Where(item => item != null)
                .Where(item => kinds.Any(kind => item.Kind != null && item.Kind.StartsWith(kind, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private static string FormatTaskItemValue(TaskEditorItemValue item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            string name = string.IsNullOrWhiteSpace(item.Name)
                ? "Item " + item.ItemId.ToString(CultureInfo.InvariantCulture)
                : item.Name;
            string flags = item.Bind ? ", bound" : string.Empty;
            return item.Count.ToString(CultureInfo.InvariantCulture)
                + "x "
                + name
                + " (ID "
                + item.ItemId.ToString(CultureInfo.InvariantCulture)
                + flags
                + ")";
        }

        private string FormatRewardValue(TaskEditorFieldValue reward)
        {
            return FormatRewardValue(reward, null);
        }

        private string FormatRewardValue(TaskEditorFieldValue reward, List<TaskEditorFieldValue> allFields)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            if (reward.DisplayName.IndexOf("EXP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return FormatExperienceReward(reward, allFields);
            }

            if (string.Equals(reward.DisplayName, "Item reward groups", StringComparison.OrdinalIgnoreCase))
            {
                return reward.Value + " group(s)";
            }

            if (IsPointRewardName(reward.DisplayName))
            {
                return reward.Value + " point(s)";
            }

            return reward.Value;
        }

        private string FormatExperienceReward(TaskEditorFieldValue reward, List<TaskEditorFieldValue> allFields)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            long rawValue;
            if (!long.TryParse(reward.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out rawValue))
            {
                return reward.Value ?? string.Empty;
            }

            int reviseLevel;
            if (IsYesValue(GetSiblingFieldValue(allFields, reward, "m_bExpRevise"))
                && int.TryParse(GetSiblingFieldValue(allFields, reward, "m_lExpReviseLev"), NumberStyles.Integer, CultureInfo.InvariantCulture, out reviseLevel)
                && TryGetLevelExperience(reviseLevel, out long levelExperience))
            {
                long adjusted = rawValue * levelExperience / 10000L;
                return adjusted.ToString("N0", CultureInfo.InvariantCulture);
            }

            return rawValue.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string GetSiblingFieldValue(List<TaskEditorFieldValue> fields, TaskEditorFieldValue origin, string siblingName)
        {
            if (fields == null || origin == null || string.IsNullOrWhiteSpace(origin.Field) || string.IsNullOrWhiteSpace(siblingName))
            {
                return string.Empty;
            }

            int separator = origin.Field.LastIndexOf('.');
            string fieldName = separator >= 0 ? origin.Field.Substring(0, separator + 1) + siblingName : siblingName;
            TaskEditorFieldValue sibling = fields.FirstOrDefault(field => string.Equals(field.Field, fieldName, StringComparison.Ordinal));
            return sibling != null ? sibling.Value : string.Empty;
        }

        private static bool IsYesValue(string value)
        {
            return string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase);
        }

        private bool TryGetLevelExperience(int level, out long experience)
        {
            if (TryGetSessionLevelExperience(level, out experience))
            {
                return true;
            }

            return TryGetKnownLevelExperience(level, out experience);
        }

        private bool TryGetSessionLevelExperience(int level, out long experience)
        {
            EnsureLevelExperienceCache();
            if (levelExperienceByLevel != null && levelExperienceByLevel.TryGetValue(level, out experience))
            {
                return true;
            }

            experience = 0L;
            return false;
        }

        private void EnsureLevelExperienceCache()
        {
            if (levelExperienceByLevel != null)
            {
                return;
            }

            levelExperienceByLevel = new Dictionary<int, long>();
            if (sessionService == null || sessionService.ListCollection == null || sessionService.ListCollection.Lists == null)
            {
                return;
            }

            for (int listIndex = 0; listIndex < sessionService.ListCollection.Lists.Length; listIndex++)
            {
                eList list = sessionService.ListCollection.Lists[listIndex];
                if (list == null
                    || eListCollection.IsRawTailList(list)
                    || list.elementFields == null
                    || list.elementValues == null)
                {
                    continue;
                }

                string listName = list.listName ?? string.Empty;
                bool likelyLevelExpList = listName.IndexOf("LEVEL", StringComparison.OrdinalIgnoreCase) >= 0
                    && listName.IndexOf("EXP", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!likelyLevelExpList)
                {
                    continue;
                }

                int levelFieldIndex = FindFirstFieldIndex(list, "level", "id", "ID");
                int expFieldIndex = FindBestExperienceFieldIndex(list);
                if (levelFieldIndex < 0 || expFieldIndex < 0)
                {
                    continue;
                }

                foreach (object[] row in list.elementValues)
                {
                    if (row == null || row.Length <= Math.Max(levelFieldIndex, expFieldIndex))
                    {
                        continue;
                    }

                    int rowLevel;
                    long rowExperience;
                    if (TryConvertToInt(row[levelFieldIndex], out rowLevel)
                        && TryConvertToLong(row[expFieldIndex], out rowExperience)
                        && rowLevel > 0
                        && rowExperience > 0
                        && !levelExperienceByLevel.ContainsKey(rowLevel))
                    {
                        levelExperienceByLevel.Add(rowLevel, rowExperience);
                    }
                }
            }
        }

        private static int FindFirstFieldIndex(eList list, params string[] names)
        {
            if (list == null || list.elementFields == null || names == null)
            {
                return -1;
            }

            for (int i = 0; i < list.elementFields.Length; i++)
            {
                foreach (string name in names)
                {
                    if (string.Equals(list.elementFields[i], name, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static int FindBestExperienceFieldIndex(eList list)
        {
            if (list == null || list.elementFields == null)
            {
                return -1;
            }

            for (int i = 0; i < list.elementFields.Length; i++)
            {
                string field = list.elementFields[i] ?? string.Empty;
                if (field.IndexOf("exp", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryConvertToInt(object value, out int result)
        {
            long longValue;
            if (TryConvertToLong(value, out longValue) && longValue >= int.MinValue && longValue <= int.MaxValue)
            {
                result = (int)longValue;
                return true;
            }

            result = 0;
            return false;
        }

        private static bool TryConvertToLong(object value, out long result)
        {
            if (value == null)
            {
                result = 0L;
                return false;
            }

            if (value is long)
            {
                result = (long)value;
                return true;
            }

            if (value is int)
            {
                result = (int)value;
                return true;
            }

            if (value is uint)
            {
                result = (uint)value;
                return true;
            }

            return long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryGetKnownLevelExperience(int level, out long experience)
        {
            switch (level)
            {
                case 90:
                    experience = 246340000L;
                    return true;
                default:
                    experience = 0L;
                    return false;
            }
        }

        private static bool IsAuxiliaryRewardField(TaskEditorFieldValue reward)
        {
            if (reward == null || string.IsNullOrWhiteSpace(reward.Field))
            {
                return false;
            }

            string field = reward.Field;
            return field.EndsWith(".m_bGoldRevise", StringComparison.Ordinal)
                || field.EndsWith(".m_lGoldReviseLev", StringComparison.Ordinal)
                || field.EndsWith(".m_bExpRevise", StringComparison.Ordinal)
                || field.EndsWith(".m_lExpReviseLev", StringComparison.Ordinal)
                || field.EndsWith(".m_bExpFix", StringComparison.Ordinal)
                || field.EndsWith(".m_ulExpAlgo", StringComparison.Ordinal)
                || field.EndsWith(".m_ulFriendshipAlgo", StringComparison.Ordinal)
                || field.EndsWith(".m_ulBindMoneyAlgo", StringComparison.Ordinal);
        }

        private static string FormatMoneyReward(string value)
        {
            long amount;
            if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out amount))
            {
                return value ?? string.Empty;
            }

            if (amount >= 1000000L && amount % 1000000L == 0L)
            {
                return (amount / 1000000L).ToString("N0", CultureInfo.InvariantCulture);
            }

            return amount.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static bool IsPointRewardName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return false;
            }

            return displayName.IndexOf("point", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("score", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("reputation", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("fealty", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("valor", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("acclaim", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("contribution", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("kindness", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("courage", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("cruelty", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("luck", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("mentor", StringComparison.OrdinalIgnoreCase) >= 0
                || displayName.IndexOf("rose", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string DescribeTaskType(string value)
        {
            int type;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out type))
            {
                return value;
            }

            switch (type)
            {
                case 0: return "0 - Basic/normal";
                case 4: return "4 - Daily style";
                case 9: return "9 - NPC/story task";
                default: return type.ToString(CultureInfo.InvariantCulture);
            }
        }

        private string DescribeCompletionMethod(string value)
        {
            int method;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out method))
            {
                return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
            }

            switch (method)
            {
                case 0: return "0 - None / script-driven";
                case 1: return "1 - Kill monsters";
                case 2: return "2 - Collect required items";
                case 3: return "3 - Talk to NPC";
                case 4: return "4 - Reach location";
                case 5: return "5 - Wait timer";
                case 6: return "6 - Answer question";
                case 7: return "7 - Mini game";
                case 8: return "8 - Protect NPC";
                case 9: return "9 - Escort NPC to location";
                case 10: return "10 - Own title";
                case 11: return "11 - Finish task count";
                case 12: return "12 - Mining count";
                case 13: return "13 - Use item count";
                case 14: return "14 - Kill players";
                case 15: return "15 - Achievement count";
                case 16: return "16 - Perform emotion";
                case 17: return "17 - Submit items";
                case 18: return "18 - PVP wins";
                case 19: return "19 - PVP failures";
                case 20: return "20 - PVP completions";
                default: return method.ToString(CultureInfo.InvariantCulture);
            }
        }

        private string DescribeFinishType(string value)
        {
            int finishType;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out finishType))
            {
                return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
            }

            switch (finishType)
            {
                case 0: return "0 - Direct completion";
                case 1: return "1 - Return to finish NPC";
                default: return finishType.ToString(CultureInfo.InvariantCulture);
            }
        }

        private string DescribeAwardType(string value)
        {
            int awardType;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out awardType))
            {
                return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
            }

            switch (awardType)
            {
                case 0: return "0 - Normal reward";
                case 1: return "1 - Per-condition reward";
                case 2: return "2 - Ratio reward";
                case 3: return "3 - Item-count reward";
                case 4: return "4 - Finish-count reward";
                case 5: return "5 - Submit-item reward";
                default: return awardType.ToString(CultureInfo.InvariantCulture);
            }
        }

        private string FormatNpc(string value)
        {
            uint id;
            if (!uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id) || id == 0)
            {
                return "None";
            }

            if (id > int.MaxValue || sessionService == null)
            {
                return value;
            }

            NpcGenEntityInfo info = entityLookupService.Resolve(sessionService.ListCollection, sessionService.Database, (int)id);
            if (info != null && !string.IsNullOrWhiteSpace(info.Name))
            {
                return value + " - " + info.Name;
            }

            return value;
        }

        private static string ZeroAsNone(string value)
        {
            int number;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number == 0)
            {
                return "None / not limited";
            }

            return value;
        }

        private static string FormatSeconds(string value)
        {
            int seconds;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds) || seconds <= 0)
            {
                return "None";
            }

            if (seconds % 3600 == 0)
            {
                return seconds.ToString(CultureInfo.InvariantCulture) + " sec (" + (seconds / 3600).ToString(CultureInfo.InvariantCulture) + " h)";
            }
            if (seconds % 60 == 0)
            {
                return seconds.ToString(CultureInfo.InvariantCulture) + " sec (" + (seconds / 60).ToString(CultureInfo.InvariantCulture) + " min)";
            }

            return seconds.ToString(CultureInfo.InvariantCulture) + " sec";
        }

        private bool IsYes(List<TaskEditorFieldValue> fields, string fieldName)
        {
            return string.Equals(GetKnownFieldValue(fields, fieldName), "Yes", StringComparison.OrdinalIgnoreCase);
        }

        private string YesNoMeaning(List<TaskEditorFieldValue> fields, string fieldName)
        {
            return IsYes(fields, fieldName) ? "Yes" : "No";
        }

        private string GetDescriptionText(List<TaskEditorTextValue> texts, string taskName)
        {
            if (texts == null)
            {
                return string.Empty;
            }

            int rootNodeOffset = GetRootNodeOffset(texts);
            List<string> candidates = texts
                .Where(text => IsPlayerFacingText(text, taskName))
                .Where(text => rootNodeOffset < 0 || text.Offset < rootNodeOffset)
                .Select(text => CleanTaskText(text.Text))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Take(12)
                .ToList();
            if (candidates.Count == 0)
            {
                return string.Empty;
            }

            return JoinTextFragments(candidates);
        }

        private List<string> GetDialogPreview(List<TaskEditorTextValue> texts, string taskName, string description)
        {
            if (texts == null)
            {
                return new List<string>();
            }

            int rootNodeOffset = GetRootNodeOffset(texts);
            if (rootNodeOffset < 0)
            {
                return new List<string>();
            }

            return texts
                .Where(text => IsPlayerFacingText(text, taskName))
                .Where(text => text.Offset > rootNodeOffset)
                .Select(text => CleanTaskText(text.Text))
                .Where(text => !string.Equals(text, taskName, StringComparison.OrdinalIgnoreCase))
                .Where(text => !string.Equals(text, "RootNode", StringComparison.OrdinalIgnoreCase))
                .Where(text => !string.Equals(text, description, StringComparison.Ordinal))
                .Where(text => text.Length >= 8)
                .Take(6)
                .ToList();
        }

        private static int GetRootNodeOffset(List<TaskEditorTextValue> texts)
        {
            TaskEditorTextValue root = (texts ?? new List<TaskEditorTextValue>())
                .FirstOrDefault(text => text != null && string.Equals(text.Text, "RootNode", StringComparison.OrdinalIgnoreCase));
            return root != null ? root.Offset : -1;
        }

        private static bool IsPlayerFacingText(TaskEditorTextValue text, string taskName)
        {
            if (text == null || string.IsNullOrWhiteSpace(text.Text))
            {
                return false;
            }

            string value = CleanTaskText(text.Text);
            if ((value.Length < 4 && !IsShortCommandText(value))
                || string.Equals(value, taskName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "RootNode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "pos:", StringComparison.OrdinalIgnoreCase)
                || LooksLikeCoordinate(value)
                || (!IsShortCommandText(value) && !HasReadableLatinText(value)))
            {
                return false;
            }

            return true;
        }

        private static bool IsShortCommandText(string value)
        {
            value = (value ?? string.Empty).Trim();
            if (value.Length != 1)
            {
                return false;
            }

            char c = value[0];
            return c == 'O';
        }

        private static string CleanTaskText(string text)
        {
            string value = (text ?? string.Empty).Trim();
            while (value.Length >= 7 && value[0] == '^' && IsHexColor(value.Substring(1, 6)))
            {
                value = value.Substring(7).TrimStart();
            }

            return value;
        }

        private static bool IsHexColor(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 6)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (!Uri.IsHexDigit(value[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool LooksLikeCoordinate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            int digits = value.Count(char.IsDigit);
            int separators = value.Count(ch => ch == ',' || ch == '-' || ch == '.');
            return digits >= 3 && separators >= 2 && digits + separators >= value.Length - 1;
        }

        private static bool HasReadableLatinText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            int latin = value.Count(ch => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'));
            int suspiciousCjk = value.Count(ch => (ch >= 0x2E80 && ch <= 0x9FFF) || (ch >= 0xF900 && ch <= 0xFAFF));
            return latin >= 2 && suspiciousCjk <= Math.Max(1, value.Length / 4);
        }

        private static string JoinTextFragments(List<string> fragments)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string fragment in fragments ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(fragment))
                {
                    continue;
                }

                if (builder.Length > 0 && !builder.ToString().EndsWith(" ", StringComparison.Ordinal))
                {
                    builder.Append(' ');
                }

                builder.Append(fragment.Trim());
            }

            return builder.ToString().Trim();
        }

        private static string WrapLongText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= 360)
            {
                return text ?? string.Empty;
            }

            return text.Substring(0, 360) + "...";
        }

        private string ClassifyText(TaskEditorTextValue text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            if (text.Offset == 0x0004)
            {
                return "Name";
            }
            if (text.Text != null && text.Text.Length > 80)
            {
                return "Description";
            }
            if (string.Equals(text.Text, "RootNode", StringComparison.OrdinalIgnoreCase))
            {
                return "Talk node";
            }

            return "Text";
        }

        private void ClearDetails()
        {
            summaryLabel.Text = string.Empty;
            if (overviewPanel != null)
            {
                overviewPanel.Controls.Clear();
            }
            if (questPreviewPanel != null)
            {
                questPreviewPanel.Controls.Clear();
            }
            itemGrid.Rows.Clear();
            generalGrid.Rows.Clear();
            fieldGrid.Rows.Clear();
            textGrid.Rows.Clear();
            rawGrid.Rows.Clear();
            hexBox.Text = string.Empty;
        }

        private string BuildHexPreview(byte[] bytes, int maxBytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            int count = Math.Min(bytes.Length, maxBytes);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int offset = 0; offset < count; offset += 16)
            {
                builder.Append(offset.ToString("X6", CultureInfo.InvariantCulture)).Append("  ");
                int rowCount = Math.Min(16, count - offset);
                for (int i = 0; i < rowCount; i++)
                {
                    builder.Append(bytes[offset + i].ToString("X2", CultureInfo.InvariantCulture)).Append(' ');
                }
                builder.AppendLine();
            }

            if (bytes.Length > count)
            {
                builder.AppendLine("... truncated ...");
            }

            return builder.ToString();
        }

        private void StyleFieldRow(DataGridViewRow row, string section)
        {
            if (row == null)
            {
                return;
            }

            Color color;
            switch (section ?? string.Empty)
            {
                case "General": color = Color.FromArgb(21, 27, 35); break;
                case "Flags": color = Color.FromArgb(18, 33, 29); break;
                case "Delivery": color = Color.FromArgb(20, 31, 37); break;
                case "Transport": color = Color.FromArgb(34, 28, 42); break;
                case "Flow": color = Color.FromArgb(31, 29, 23); break;
                case "NPC": color = Color.FromArgb(39, 29, 22); break;
                case "Success reward": color = Color.FromArgb(42, 34, 19); break;
                case "Finish Count": color = Color.FromArgb(29, 25, 38); break;
                case "Trade": color = Color.FromArgb(24, 34, 31); break;
                case "Message": color = Color.FromArgb(35, 26, 30); break;
                default: color = Color.FromArgb(18, 21, 26); break;
            }

            row.DefaultCellStyle.BackColor = color;
        }

        private void StyleItemRow(DataGridViewRow row, TaskEditorItemValue item)
        {
            if (row == null)
            {
                return;
            }

            string kind = item != null ? item.Kind : string.Empty;
            string text = kind ?? string.Empty;
            if (text.IndexOf("reward", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(42, 34, 19);
            }
            else if (text.IndexOf("required", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("submit", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(42, 25, 20);
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(20, 29, 34);
            }

            if (row.Cells.Count > 3)
            {
                row.Cells[3].Style.ForeColor = ResolveTaskItemTextColor(item, false);
                row.Cells[3].Style.SelectionForeColor = ResolveTaskItemTextColor(item, true);
            }
        }

        private void ResolveTaskItems(List<TaskEditorItemValue> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            EnsureItemOptions();
            if (itemOptionsById == null)
            {
                return;
            }

            foreach (TaskEditorItemValue item in items)
            {
                ItemReferenceOption option;
                if (item != null && itemOptionsById.TryGetValue(item.ItemId, out option) && option != null)
                {
                    item.Name = option.Name;
                    item.NameForeColor = option.NameForeColor;
                    item.IconKey = option.IconKey;
                    item.Quality = option.Quality;
                    item.AccentHex = option.AccentHex;
                }
            }
        }

        private List<TaskEditorItemValue> FilterResolvedTaskItems(List<TaskEditorItemValue> items)
        {
            if (items == null || items.Count == 0 || itemOptionsById == null)
            {
                return items ?? new List<TaskEditorItemValue>();
            }

            return items
                .Where(item => item != null)
                .Where(item => itemOptionsById.ContainsKey(item.ItemId))
                .Where(item => !string.IsNullOrWhiteSpace(item.Name))
                .ToList();
        }

        private void ResolveTaskMonsters(List<TaskEditorMonsterValue> monsters)
        {
            if (monsters == null || monsters.Count == 0 || sessionService == null)
            {
                return;
            }

            foreach (TaskEditorMonsterValue monster in monsters)
            {
                if (monster == null || monster.MonsterId <= 0)
                {
                    continue;
                }

                NpcGenEntityInfo info = entityLookupService.Resolve(sessionService.ListCollection, sessionService.Database, monster.MonsterId);
                if (info == null)
                {
                    continue;
                }

                monster.Name = info.Name;
                monster.NameForeColor = info.NameColor;
                monster.Icon = info.Icon;
            }
        }

        private void EnsureItemOptions()
        {
            if (itemOptionsById != null || sessionService == null || sessionService.ListCollection == null)
            {
                return;
            }

            List<ItemReferenceOption> options = itemReferenceService.BuildSearchableItemOptions(
                sessionService.ListCollection,
                sessionService.Database,
                iconResolutionService);
            itemOptionsById = options
                .Where(option => option != null && option.Id > 0)
                .GroupBy(option => option.Id)
                .ToDictionary(group => group.Key, group => group
                    .OrderByDescending(ScoreItemReferenceOption)
                    .First());
        }

        private static int ScoreItemReferenceOption(ItemReferenceOption option)
        {
            if (option == null)
            {
                return 0;
            }

            int score = 0;
            if (!string.IsNullOrWhiteSpace(option.IconKey)) score += 8;
            if (option.NameForeColor.HasValue) score += 4;
            if (!string.IsNullOrWhiteSpace(option.AccentHex)) score += 2;
            if (option.Quality >= 0) score += 1;
            return score;
        }

        private Image ResolveTaskItemIcon(TaskEditorItemValue item)
        {
            Bitmap baseIcon = Properties.Resources.NoIcon;
            bool hasRealIcon = false;

            if (item != null
                && sessionService != null
                && sessionService.Database != null
                && !string.IsNullOrWhiteSpace(item.IconKey))
            {
                try
                {
                    if (sessionService.Database.ContainsKey(item.IconKey))
                    {
                        baseIcon = sessionService.Database.images(item.IconKey);
                        hasRealIcon = true;
                    }
                }
                catch
                {
                }
            }

            Color accentColor;
            if (!hasRealIcon && TryParseAccentColor(item != null ? item.AccentHex : string.Empty, out accentColor))
            {
                return CreateTaskItemSwatch(accentColor);
            }

            return CreateTaskItemIconPreview(baseIcon, ResolveTaskItemTextColor(item, false));
        }

        private static Image CreateTaskItemIconPreview(Image icon, Color borderColor)
        {
            Bitmap preview = new Bitmap(32, 32);
            using (Graphics graphics = Graphics.FromImage(preview))
            {
                graphics.Clear(Color.FromArgb(13, 16, 20));
                if (icon != null)
                {
                    graphics.DrawImage(icon, new Rectangle(3, 3, 26, 26));
                }

                using (Pen border = new Pen(borderColor, 2F))
                {
                    graphics.DrawRectangle(border, 1, 1, 29, 29);
                }
            }

            return preview;
        }

        private static Image CreateTaskItemSwatch(Color accentColor)
        {
            Bitmap preview = new Bitmap(32, 32);
            using (Graphics graphics = Graphics.FromImage(preview))
            {
                using (SolidBrush brush = new SolidBrush(accentColor))
                {
                    graphics.FillRectangle(brush, new Rectangle(3, 3, 26, 26));
                }

                using (SolidBrush gloss = new SolidBrush(Color.FromArgb(54, Color.White)))
                {
                    graphics.FillRectangle(gloss, new Rectangle(3, 3, 26, 8));
                }

                using (Pen border = new Pen(accentColor, 2F))
                {
                    graphics.DrawRectangle(border, 1, 1, 29, 29);
                }
            }

            return preview;
        }

        private static Color ResolveTaskItemTextColor(TaskEditorItemValue item, bool selected)
        {
            if (item != null && item.NameForeColor.HasValue)
            {
                return item.NameForeColor.Value;
            }

            Color accentColor;
            if (TryParseAccentColor(item != null ? item.AccentHex : string.Empty, out accentColor))
            {
                return accentColor;
            }

            Color qualityColor;
            int quality = item != null ? item.Quality : -1;
            if (ItemQualityCatalog.TryGetColor(quality, out qualityColor))
            {
                if (quality == 1 && !selected)
                {
                    return Color.FromArgb(235, 238, 244);
                }

                return qualityColor;
            }

            return selected ? Color.White : Color.FromArgb(226, 231, 239);
        }

        private static bool TryParseAccentColor(string accentHex, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(accentHex))
            {
                return false;
            }

            string normalized = accentHex.Trim().TrimStart('#');
            if (normalized.Length != 6)
            {
                return false;
            }

            int rgb;
            if (!int.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
            {
                return false;
            }

            color = Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            return true;
        }

        private void openFolderButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose the game folder that contains the data directory.";
                if (!string.IsNullOrWhiteSpace(rootPathBox.Text) && Directory.Exists(rootPathBox.Text))
                {
                    dialog.SelectedPath = rootPathBox.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadGameRoot(dialog.SelectedPath);
                }
            }
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            if (!isLoading && !string.IsNullOrWhiteSpace(rootPathBox.Text) && Directory.Exists(rootPathBox.Text))
            {
                LoadGameRoot(rootPathBox.Text);
            }
        }

        private void searchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.SuppressKeyPress = true;
            ApplyFilter(false);
        }

        private void SetLoadingState(bool loading, string message)
        {
            isLoading = loading;
            Cursor = loading ? Cursors.WaitCursor : Cursors.Default;
            openFolderButton.Enabled = !loading;
            reloadButton.Enabled = !loading;
            searchBox.Enabled = !loading;
            searchButton.Enabled = !loading;
            taskGrid.Enabled = !loading;
            if (loading)
            {
                statusLabel.Text = message ?? "Loading tasks.data...";
                loadingLabel.Text = message ?? "Loading tasks.data...";
                loadingPanel.Visible = true;
                loadingPanel.BringToFront();
            }
            else
            {
                loadingPanel.Visible = false;
            }
        }

        private void EnsureInitialSplitterLayout()
        {
            if (mainSplit == null || mainSplit.Width <= 0)
            {
                return;
            }

            mainSplit.Panel1MinSize = 25;
            mainSplit.Panel2MinSize = 25;

            int desired = 390;
            int preferredLeftMin = 320;
            int preferredRightMin = 620;
            int max = mainSplit.Width - preferredRightMin - mainSplit.SplitterWidth;
            int min = preferredLeftMin;
            if (max < min)
            {
                min = mainSplit.Panel1MinSize;
                max = mainSplit.Width - mainSplit.Panel2MinSize - mainSplit.SplitterWidth;
            }

            if (max < min)
            {
                return;
            }

            int distance = Math.Min(Math.Max(desired, min), max);
            mainSplit.SplitterDistance = distance;

            if (distance >= preferredLeftMin && mainSplit.Width - distance - mainSplit.SplitterWidth >= preferredRightMin)
            {
                mainSplit.Panel1MinSize = preferredLeftMin;
                mainSplit.Panel2MinSize = preferredRightMin;
            }
        }

        private void ApplyTheme(Control root)
        {
            if (string.Equals(root.Tag as string, "quest-preview", StringComparison.Ordinal))
            {
                foreach (Control child in root.Controls)
                {
                    if (!string.Equals(child.Tag as string, "quest-preview", StringComparison.Ordinal))
                    {
                        ApplyTheme(child);
                    }
                }
                return;
            }

            Color back = Color.FromArgb(17, 21, 26);
            Color panel = Color.FromArgb(24, 28, 34);
            Color text = Color.FromArgb(229, 234, 242);
            root.BackColor = back;
            root.ForeColor = text;

            foreach (Control control in root.Controls)
            {
                ApplyTheme(control);
            }

            DataGridView grid = root as DataGridView;
            if (grid != null)
            {
                grid.BackgroundColor = back;
                grid.DefaultCellStyle.BackColor = Color.FromArgb(18, 21, 26);
                grid.DefaultCellStyle.ForeColor = text;
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 99, 135);
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(22, 26, 32);
                grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(38, 44, 53);
                grid.ColumnHeadersDefaultCellStyle.ForeColor = text;
                grid.GridColor = Color.FromArgb(50, 58, 70);
            }

            TextBox textBox = root as TextBox;
            if (textBox != null)
            {
                textBox.BackColor = panel;
                textBox.ForeColor = text;
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }

            RichTextBox richTextBox = root as RichTextBox;
            if (richTextBox != null)
            {
                richTextBox.BackColor = Color.FromArgb(18, 21, 26);
                richTextBox.ForeColor = text;
                richTextBox.BorderStyle = BorderStyle.None;
            }

            Button button = root as Button;
            if (button != null)
            {
                button.BackColor = Color.FromArgb(41, 48, 59);
                button.ForeColor = text;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Color.FromArgb(58, 67, 80);
            }

            TabControl tabControl = root as TabControl;
            if (tabControl != null)
            {
                tabControl.BackColor = back;
                tabControl.ForeColor = text;
            }

            TabPage tabPage = root as TabPage;
            if (tabPage != null)
            {
                tabPage.BackColor = back;
                tabPage.ForeColor = text;
            }
        }
    }
}
