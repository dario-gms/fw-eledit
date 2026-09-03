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
        private readonly ISessionService sessionService;
        private TaskEditorData currentData;
        private List<TaskEditorEntry> filteredEntries = new List<TaskEditorEntry>();
        private int loadRequestId;
        private bool isLoading;

        private TextBox rootPathBox;
        private Button openFolderButton;
        private Button reloadButton;
        private TextBox searchBox;
        private DataGridView taskGrid;
        private Label statusLabel;
        private Label summaryLabel;
        private SplitContainer mainSplit;
        private RichTextBox overviewBox;
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
            header.ColumnCount = 4;
            header.RowCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            root.Controls.Add(header, 0, 0);

            rootPathBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            openFolderButton = new Button { Text = "Open", Dock = DockStyle.Fill };
            reloadButton = new Button { Text = "Reload", Dock = DockStyle.Fill };
            searchBox = new TextBox { Dock = DockStyle.Fill };
            openFolderButton.Click += openFolderButton_Click;
            reloadButton.Click += reloadButton_Click;
            searchBox.TextChanged += (s, e) => ApplyFilter(false);

            header.Controls.Add(rootPathBox, 0, 0);
            header.Controls.Add(openFolderButton, 1, 0);
            header.Controls.Add(reloadButton, 2, 0);
            header.Controls.Add(new Label { Text = "Read-only viewer", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }, 3, 0);
            header.Controls.Add(searchBox, 0, 1);
            header.SetColumnSpan(searchBox, 4);

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
            overviewBox = new RichTextBox();
            overviewBox.Dock = DockStyle.Fill;
            overviewBox.ReadOnly = true;
            overviewBox.BorderStyle = BorderStyle.None;
            overviewBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            overviewBox.DetectUrls = false;
            overviewTab.Controls.Add(overviewBox);
            tabs.TabPages.Add(overviewTab);

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

            TabPage rawTab = new TabPage("Technical Values");
            rawGrid = CreateGrid();
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Offset", Width = 78 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hint", Width = 110 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Int32", Width = 112 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "UInt32", Width = 112 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Float", Width = 112 });
            rawGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Bytes", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
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
            filteredEntries = currentData.Entries
                .Where(entry => Matches(entry, query))
                .Take(5000)
                .ToList();

            foreach (TaskEditorEntry entry in filteredEntries)
            {
                int rowIndex = taskGrid.Rows.Add(entry.Id, entry.Name, entry.ShardIndex);
                taskGrid.Rows[rowIndex].Tag = entry;
            }

            if (taskGrid.Rows.Count > 0)
            {
                taskGrid.Rows[0].Selected = true;
                taskGrid.CurrentCell = taskGrid.Rows[0].Cells[0];
            }

            taskGrid.Invalidate();
            taskGrid.Refresh();

            statusLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "Showing {0:N0} of {1:N0} task entries.",
                filteredEntries.Count,
                currentData.Entries.Count);
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
            List<TaskEditorRawValue> rawValues = TaskEditorFileService.BuildRawValues(entry.Bytes);

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

            SetOverviewText(BuildOverview(entry, knownFields, texts));

            foreach (TaskEditorFieldValue field in knownFields)
            {
                int rowIndex = fieldGrid.Rows.Add(field.Section, field.DisplayName, field.Value, field.Meaning, field.Field, field.HexOffset);
                StyleFieldRow(fieldGrid.Rows[rowIndex], field.Section);
            }

            foreach (TaskEditorTextValue text in texts)
            {
                textGrid.Rows.Add("0x" + text.Offset.ToString("X4", CultureInfo.InvariantCulture), ClassifyText(text), text.Text);
            }

            foreach (TaskEditorRawValue value in rawValues)
            {
                rawGrid.Rows.Add(value.HexOffset, value.Hint, value.Int32Value, value.UInt32Value, value.FloatValue, value.HexBytes);
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

        private string BuildOverview(TaskEditorEntry entry, List<TaskEditorFieldValue> fields, List<TaskEditorTextValue> texts)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(entry.Id.ToString(CultureInfo.InvariantCulture) + " - " + (entry.Name ?? string.Empty));
            builder.AppendLine();

            AppendSection(builder, "Player-facing summary");
            AppendItem(builder, "Task type", DescribeTaskType(GetKnownFieldValue(fields, "m_ulType")));
            AppendItem(builder, "Recommended level", ZeroAsNone(GetKnownFieldValue(fields, "m_ulSuitableLevel")));
            AppendItem(builder, "Start NPC", FormatNpc(GetKnownFieldValue(fields, "m_ulDelvNPC")));
            AppendItem(builder, "Finish NPC", FormatNpc(GetKnownFieldValue(fields, "m_ulAwardNPC")));
            AppendItem(builder, "Visibility", IsYes(fields, "m_bHidden") ? "Hidden until unlocked or triggered" : "Visible when conditions allow it");
            AppendItem(builder, "Tracking", BuildTrackingSummary(fields));
            AppendItem(builder, "Importance", IsYes(fields, "m_bKeyTask") ? "Marked as a key task" : "Normal task");

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

            List<TaskEditorFieldValue> rewards = GetRewardFields(fields);
            if (rewards.Count > 0)
            {
                builder.AppendLine();
                AppendSection(builder, "Rewards");
                foreach (TaskEditorFieldValue reward in rewards)
                {
                    AppendItem(builder, reward.DisplayName, FormatRewardValue(reward));
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

        private void SetOverviewText(string text)
        {
            overviewBox.Clear();
            overviewBox.Text = text ?? string.Empty;

            if (string.IsNullOrEmpty(overviewBox.Text))
            {
                return;
            }

            overviewBox.SelectAll();
            overviewBox.SelectionFont = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            overviewBox.SelectionColor = Color.FromArgb(223, 230, 238);

            int firstLineEnd = overviewBox.Text.IndexOf(Environment.NewLine, StringComparison.Ordinal);
            if (firstLineEnd > 0)
            {
                overviewBox.Select(0, firstLineEnd);
                overviewBox.SelectionFont = new Font("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point, 0);
                overviewBox.SelectionColor = Color.FromArgb(255, 235, 170);
            }

            string[] sectionTitles =
            {
                "PLAYER-FACING SUMMARY",
                "AVAILABILITY AND REPEAT RULES",
                "GAME FLOW",
                "REWARDS",
                "DESCRIPTION",
                "DIALOG PREVIEW",
                "TECHNICAL SOURCE"
            };

            foreach (string sectionTitle in sectionTitles)
            {
                int index = overviewBox.Text.IndexOf(sectionTitle, StringComparison.Ordinal);
                if (index < 0)
                {
                    continue;
                }

                overviewBox.Select(index, sectionTitle.Length);
                overviewBox.SelectionFont = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 0);
                overviewBox.SelectionColor = GetOverviewSectionColor(sectionTitle);
            }

            overviewBox.Select(0, 0);
        }

        private static Color GetOverviewSectionColor(string sectionTitle)
        {
            switch (sectionTitle)
            {
                case "PLAYER-FACING SUMMARY": return Color.FromArgb(111, 202, 255);
                case "AVAILABILITY AND REPEAT RULES": return Color.FromArgb(125, 214, 157);
                case "GAME FLOW": return Color.FromArgb(255, 196, 116);
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

        private List<TaskEditorFieldValue> GetRewardFields(List<TaskEditorFieldValue> fields)
        {
            if (fields == null)
            {
                return new List<TaskEditorFieldValue>();
            }

            return fields
                .Where(field => field != null)
                .Where(field => string.Equals(field.Section, "Success reward", StringComparison.Ordinal))
                .ToList();
        }

        private string FormatRewardValue(TaskEditorFieldValue reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            if (string.Equals(reward.DisplayName, "Item reward groups", StringComparison.OrdinalIgnoreCase))
            {
                return reward.Value + " group(s)";
            }

            if (reward.DisplayName != null
                && reward.DisplayName.IndexOf("points", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return reward.Value + " point(s)";
            }

            return reward.Value;
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

            return texts
                .Where(text => text != null && !string.IsNullOrWhiteSpace(text.Text))
                .Where(text => !string.Equals(text.Text, taskName, StringComparison.OrdinalIgnoreCase))
                .Where(text => !string.Equals(text.Text, "RootNode", StringComparison.OrdinalIgnoreCase))
                .Where(text => text.Text.Length >= 70)
                .OrderByDescending(text => text.Text.Length)
                .Select(text => text.Text)
                .FirstOrDefault() ?? string.Empty;
        }

        private List<string> GetDialogPreview(List<TaskEditorTextValue> texts, string taskName, string description)
        {
            if (texts == null)
            {
                return new List<string>();
            }

            return texts
                .Where(text => text != null && !string.IsNullOrWhiteSpace(text.Text))
                .Select(text => text.Text.Trim())
                .Where(text => !string.Equals(text, taskName, StringComparison.OrdinalIgnoreCase))
                .Where(text => !string.Equals(text, "RootNode", StringComparison.OrdinalIgnoreCase))
                .Where(text => !string.Equals(text, description, StringComparison.Ordinal))
                .Where(text => text.Length >= 8)
                .Take(6)
                .ToList();
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
            overviewBox.Text = string.Empty;
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

        private void SetLoadingState(bool loading, string message)
        {
            isLoading = loading;
            Cursor = loading ? Cursors.WaitCursor : Cursors.Default;
            openFolderButton.Enabled = !loading;
            reloadButton.Enabled = !loading;
            searchBox.Enabled = !loading;
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
