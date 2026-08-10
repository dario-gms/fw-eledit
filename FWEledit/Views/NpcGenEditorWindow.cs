using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NpcGenEditorWindow : Form
    {
        private readonly NpcGenFileService fileService = new NpcGenFileService();
        private readonly NpcGenEntityLookupService entityLookupService = new NpcGenEntityLookupService();
        private readonly ISessionService sessionService;
        private NpcGenData currentData;
        private bool suppressChanges;
        private bool isDirty;

        private TextBox pathTextBox;
        private Button openButton;
        private Button saveButton;
        private TextBox searchTextBox;
        private Button searchButton;
        private TabControl tabs;
        private DataGridView areaGrid;
        private DataGridView entryGrid;
        private DataGridView attachGrid;
        private DataGridView controllerGrid;
        private ComboBox groupTypeCombo;
        private ComboBox groupBehaviorCombo;
        private NumericUpDown groupRawType;
        private NumericUpDown groupGenId;
        private NumericUpDown groupControllerId;
        private NumericUpDown groupLifeTime;
        private NumericUpDown groupMaxNum;
        private NumericUpDown groupExportId;
        private NumericUpDown groupBufferRegionId;
        private CheckBox initGenCheckBox;
        private CheckBox autoReviveCheckBox;
        private CheckBox validOnceCheckBox;
        private NumericUpDown posX;
        private NumericUpDown posY;
        private NumericUpDown posZ;
        private NumericUpDown dirX;
        private NumericUpDown dirY;
        private NumericUpDown dirZ;
        private NumericUpDown extX;
        private NumericUpDown extY;
        private NumericUpDown extZ;
        private NumericUpDown entryId;
        private NumericUpDown entryNum;
        private NumericUpDown entryRefresh;
        private NumericUpDown entryDiedTimes;
        private NumericUpDown entryAggressive;
        private NumericUpDown entryOffsetWater;
        private NumericUpDown entryOffsetTerrain;
        private NumericUpDown entryPathId;
        private NumericUpDown entryLoopType;
        private NumericUpDown entrySpeedFlag;
        private NumericUpDown entryDeadTime;
        private NumericUpDown entryFaction;
        private NumericUpDown entryFactionHelper;
        private NumericUpDown entryFactionAccept;
        private CheckBox entryNeedHelp;
        private CheckBox entryDefaultFaction;
        private CheckBox entryDefaultFactionHelper;
        private CheckBox entryDefaultFactionAccept;
        private Button addAreaButton;
        private Button deleteAreaButton;
        private Button addEntryButton;
        private Button deleteEntryButton;
        private Button addAttachButton;
        private Button deleteAttachButton;
        private Label statusLabel;

        public NpcGenEditorWindow(ISessionService sessionService)
        {
            this.sessionService = sessionService;
            Text = "FWEledit - NPCGen Editor";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1020, 720);
            Size = new Size(1120, 760);
            BuildUi();
            ApplyDarkTheme(this);
        }

        public void OpenFile(string filePath)
        {
            currentData = fileService.Load(filePath);
            pathTextBox.Text = filePath;
            isDirty = false;
            PopulateAll();
            UpdateTitle();
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            Controls.Add(root);

            TableLayoutPanel top = new TableLayoutPanel();
            top.Dock = DockStyle.Fill;
            top.ColumnCount = 4;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            root.Controls.Add(top, 0, 0);
            top.Controls.Add(new Label { Text = "npcgen.data:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            pathTextBox = new TextBox { Dock = DockStyle.Fill };
            top.Controls.Add(pathTextBox, 1, 0);
            openButton = new Button { Text = "Open", Dock = DockStyle.Fill };
            saveButton = new Button { Text = "Save", Dock = DockStyle.Fill };
            openButton.Click += openButton_Click;
            saveButton.Click += saveButton_Click;
            top.Controls.Add(openButton, 2, 0);
            top.Controls.Add(saveButton, 3, 0);

            tabs = new TabControl { Dock = DockStyle.Fill };
            root.Controls.Add(tabs, 0, 1);
            tabs.TabPages.Add(BuildNpcPage());
            tabs.TabPages.Add(BuildControllersPage());

            statusLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            root.Controls.Add(statusLabel, 0, 2);
        }

        private TabPage BuildNpcPage()
        {
            TabPage page = new TabPage("NPCs and Monsters");
            TableLayoutPanel main = new TableLayoutPanel();
            main.Dock = DockStyle.Fill;
            main.ColumnCount = 2;
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            page.Controls.Add(main);

            TableLayoutPanel left = new TableLayoutPanel();
            left.Dock = DockStyle.Fill;
            left.RowCount = 3;
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            main.Controls.Add(left, 0, 0);

            TableLayoutPanel search = new TableLayoutPanel();
            search.Dock = DockStyle.Fill;
            search.ColumnCount = 2;
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            search.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            searchTextBox = new TextBox { Dock = DockStyle.Fill };
            searchButton = new Button { Text = "...", Dock = DockStyle.Fill };
            searchButton.Click += searchButton_Click;
            searchTextBox.KeyDown += searchTextBox_KeyDown;
            search.Controls.Add(searchTextBox, 0, 0);
            search.Controls.Add(searchButton, 1, 0);
            left.Controls.Add(search, 0, 0);

            areaGrid = CreateGrid();
            areaGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "No", Width = 52 });
            areaGrid.Columns.Add(new DataGridViewImageColumn { HeaderText = "", Width = 32, ImageLayout = DataGridViewImageCellLayout.Zoom });
            areaGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 70 });
            areaGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            areaGrid.SelectionChanged += areaGrid_SelectionChanged;
            left.Controls.Add(areaGrid, 0, 1);

            TableLayoutPanel areaButtons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            areaButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            areaButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            addAreaButton = new Button { Text = "Add", Dock = DockStyle.Fill };
            deleteAreaButton = new Button { Text = "Delete", Dock = DockStyle.Fill };
            addAreaButton.Click += addAreaButton_Click;
            deleteAreaButton.Click += deleteAreaButton_Click;
            areaButtons.Controls.Add(addAreaButton, 0, 0);
            areaButtons.Controls.Add(deleteAreaButton, 1, 0);
            left.Controls.Add(areaButtons, 0, 2);

            TableLayoutPanel right = new TableLayoutPanel();
            right.Dock = DockStyle.Fill;
            right.RowCount = 2;
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 330));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            main.Controls.Add(right, 1, 0);

            TableLayoutPanel upper = new TableLayoutPanel();
            upper.Dock = DockStyle.Fill;
            upper.ColumnCount = 5;
            upper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            upper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            upper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            upper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            upper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
            right.Controls.Add(upper, 0, 0);
            upper.Controls.Add(BuildVectorGroup("Position", out posX, out posY, out posZ), 0, 0);
            upper.Controls.Add(BuildVectorGroup("Direction", out dirX, out dirY, out dirZ), 1, 0);
            upper.Controls.Add(BuildVectorGroup("Exts", out extX, out extY, out extZ), 2, 0);
            upper.Controls.Add(BuildParametersGroup(), 3, 0);
            upper.Controls.Add(BuildAttachGroup(), 4, 0);

            GroupBox mobsGroup = new GroupBox { Text = "Mobs and NPCs", Dock = DockStyle.Fill };
            right.Controls.Add(mobsGroup, 0, 1);
            TableLayoutPanel mobsLayout = new TableLayoutPanel();
            mobsLayout.Dock = DockStyle.Fill;
            mobsLayout.ColumnCount = 2;
            mobsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            mobsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            mobsGroup.Controls.Add(mobsLayout);

            TableLayoutPanel entryListPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            entryListPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            entryListPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            entryGrid = CreateGrid();
            entryGrid.Columns.Add(new DataGridViewImageColumn { HeaderText = "", Width = 32, ImageLayout = DataGridViewImageCellLayout.Zoom });
            entryGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 74 });
            entryGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            entryGrid.SelectionChanged += entryGrid_SelectionChanged;
            entryListPanel.Controls.Add(entryGrid, 0, 0);
            TableLayoutPanel entryButtons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            entryButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            entryButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            addEntryButton = new Button { Text = "Add", Dock = DockStyle.Fill };
            deleteEntryButton = new Button { Text = "Delete", Dock = DockStyle.Fill };
            addEntryButton.Click += addEntryButton_Click;
            deleteEntryButton.Click += deleteEntryButton_Click;
            entryButtons.Controls.Add(addEntryButton, 0, 0);
            entryButtons.Controls.Add(deleteEntryButton, 1, 0);
            entryListPanel.Controls.Add(entryButtons, 0, 1);
            mobsLayout.Controls.Add(entryListPanel, 0, 0);
            mobsLayout.Controls.Add(BuildEntryEditor(), 1, 0);

            return page;
        }

        private GroupBox BuildVectorGroup(string title, out NumericUpDown x, out NumericUpDown y, out NumericUpDown z)
        {
            GroupBox group = new GroupBox { Text = title, Dock = DockStyle.Fill };
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 3, Padding = new Padding(0, 4, 0, 0), Height = 92 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            x = CreateFloatBox();
            y = CreateFloatBox();
            z = CreateFloatBox();
            AddLabeledControl(layout, "X:", x, 0);
            AddLabeledControl(layout, "Y:", y, 1);
            AddLabeledControl(layout, "Z:", z, 2);
            WireAreaEditorEvents(layout);
            group.Controls.Add(layout);
            return group;
        }

        private GroupBox BuildParametersGroup()
        {
            GroupBox group = new GroupBox { Text = "Parameters", Dock = DockStyle.Fill };
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 12 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int row = 0; row < 12; row++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            }
            groupBehaviorCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            groupBehaviorCombo.Items.AddRange(new object[] { "Ground", "Air", "Water" });
            groupTypeCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            groupTypeCombo.Items.AddRange(new object[] { "Monster", "NPC" });
            groupRawType = CreateIntBox();
            groupGenId = CreateIntBox();
            groupControllerId = CreateIntBox();
            groupLifeTime = CreateIntBox();
            groupMaxNum = CreateIntBox();
            groupExportId = CreateIntBox();
            groupBufferRegionId = CreateIntBox();
            initGenCheckBox = new CheckBox { Text = "Init Gen", Dock = DockStyle.Fill };
            autoReviveCheckBox = new CheckBox { Text = "Auto Revive", Dock = DockStyle.Fill };
            validOnceCheckBox = new CheckBox { Text = "Valid Once", Dock = DockStyle.Fill };
            AddLabeledControl(layout, "Type:", groupTypeCombo, 0);
            AddLabeledControl(layout, "Behavior:", groupBehaviorCombo, 1);
            AddLabeledControl(layout, "Raw type:", groupRawType, 2);
            AddLabeledControl(layout, "Gen ID:", groupGenId, 3);
            AddLabeledControl(layout, "ID Ctrl:", groupControllerId, 4);
            AddLabeledControl(layout, "Lifetime:", groupLifeTime, 5);
            AddLabeledControl(layout, "Max Num:", groupMaxNum, 6);
            AddLabeledControl(layout, "Export ID:", groupExportId, 7);
            AddLabeledControl(layout, "Buf Region:", groupBufferRegionId, 8);
            layout.Controls.Add(initGenCheckBox, 1, 9);
            layout.Controls.Add(autoReviveCheckBox, 1, 10);
            layout.Controls.Add(validOnceCheckBox, 1, 11);
            WireAreaEditorEvents(layout);
            group.Controls.Add(layout);
            return group;
        }

        private GroupBox BuildAttachGroup()
        {
            GroupBox group = new GroupBox { Text = "Attaches", Dock = DockStyle.Fill };
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            attachGrid = CreateGrid();
            attachGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Attach ID", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            layout.Controls.Add(attachGrid, 0, 0);
            TableLayoutPanel buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            addAttachButton = new Button { Text = "Add", Dock = DockStyle.Fill };
            deleteAttachButton = new Button { Text = "Delete", Dock = DockStyle.Fill };
            addAttachButton.Click += addAttachButton_Click;
            deleteAttachButton.Click += deleteAttachButton_Click;
            buttons.Controls.Add(addAttachButton, 0, 0);
            buttons.Controls.Add(deleteAttachButton, 1, 0);
            layout.Controls.Add(buttons, 0, 1);
            group.Controls.Add(layout);
            return group;
        }

        private Control BuildEntryEditor()
        {
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 10 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            entryId = CreateIntBox();
            entryNum = CreateIntBox();
            entryRefresh = CreateIntBox();
            entryDiedTimes = CreateIntBox();
            entryAggressive = CreateIntBox();
            entryOffsetWater = CreateFloatBox();
            entryOffsetTerrain = CreateFloatBox();
            entryPathId = CreateIntBox();
            entryLoopType = CreateIntBox();
            entrySpeedFlag = CreateIntBox();
            entryDeadTime = CreateIntBox();
            entryFaction = CreateIntBox();
            entryFactionHelper = CreateIntBox();
            entryFactionAccept = CreateIntBox();
            entryNeedHelp = new CheckBox { Text = "Need Help", Dock = DockStyle.Fill };
            entryDefaultFaction = new CheckBox { Text = "Def Faction", Dock = DockStyle.Fill };
            entryDefaultFactionHelper = new CheckBox { Text = "Def Fac Helper", Dock = DockStyle.Fill };
            entryDefaultFactionAccept = new CheckBox { Text = "Def Fac Accept", Dock = DockStyle.Fill };
            AddLabeledControl(layout, "Mob/NPC ID:", entryId, 0, 0);
            AddLabeledControl(layout, "Num:", entryNum, 0, 1);
            AddLabeledControl(layout, "Refresh:", entryRefresh, 1, 0);
            AddLabeledControl(layout, "Died Times:", entryDiedTimes, 1, 1);
            AddLabeledControl(layout, "Aggressive:", entryAggressive, 2, 0);
            AddLabeledControl(layout, "Offset Water:", entryOffsetWater, 2, 1);
            AddLabeledControl(layout, "Offset Trn:", entryOffsetTerrain, 3, 0);
            AddLabeledControl(layout, "Path ID:", entryPathId, 3, 1);
            AddLabeledControl(layout, "Loop Type:", entryLoopType, 4, 0);
            AddLabeledControl(layout, "Speed Flag:", entrySpeedFlag, 4, 1);
            AddLabeledControl(layout, "Dead Time:", entryDeadTime, 5, 0);
            AddLabeledControl(layout, "Faction:", entryFaction, 5, 1);
            AddLabeledControl(layout, "Def Helper:", entryFactionHelper, 6, 0);
            AddLabeledControl(layout, "Def Accept:", entryFactionAccept, 6, 1);
            layout.Controls.Add(entryNeedHelp, 1, 7);
            layout.Controls.Add(entryDefaultFaction, 3, 7);
            layout.Controls.Add(entryDefaultFactionHelper, 1, 8);
            layout.Controls.Add(entryDefaultFactionAccept, 3, 8);
            WireEntryEditorEvents(layout);
            return layout;
        }

        private TabPage BuildControllersPage()
        {
            TabPage page = new TabPage("Controllers");
            controllerGrid = CreateGrid();
            controllerGrid.Dock = DockStyle.Fill;
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 70 });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ctrl ID", Width = 80 });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Active", Width = 60 });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Wait", Width = 70 });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Stop", Width = 70 });
            page.Controls.Add(controllerGrid);
            return page;
        }

        private void PopulateAll()
        {
            PopulateAreas();
            PopulateControllers();
            statusLabel.Text = currentData == null
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, "Version {0} | NPC groups: {1:N0} | Resources: {2:N0} | Controllers: {3:N0}", currentData.Version, currentData.Areas.Count, currentData.ResourceAreas.Count, currentData.Controllers.Count);
        }

        private void PopulateAreas()
        {
            int oldIndex = areaGrid.CurrentRow != null ? areaGrid.CurrentRow.Index : 0;
            areaGrid.Rows.Clear();
            if (currentData == null)
            {
                return;
            }

            string query = (searchTextBox.Text ?? string.Empty).Trim();
            for (int i = 0; i < currentData.Areas.Count; i++)
            {
                NpcGenArea area = currentData.Areas[i];
                NpcGenEntry first = area.Entries.FirstOrDefault();
                int id = first != null ? first.Id : 0;
                NpcGenEntityInfo info = entityLookupService.Resolve(sessionService != null ? sessionService.ListCollection : null, sessionService != null ? sessionService.Database : null, id);
                string name = string.IsNullOrWhiteSpace(info.Name) ? "NONE!" : info.Name;
                if (!string.IsNullOrWhiteSpace(query)
                    && !id.ToString(CultureInfo.InvariantCulture).Contains(query)
                    && name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }
                int row = areaGrid.Rows.Add(i, info.Icon ?? Properties.Resources.blank, id, name);
                areaGrid.Rows[row].Tag = i;
                ApplyEntityRowStyle(areaGrid.Rows[row], info, 3);
            }

            if (areaGrid.Rows.Count > 0)
            {
                areaGrid.Rows[Math.Min(oldIndex, areaGrid.Rows.Count - 1)].Selected = true;
            }
            LoadSelectedArea();
        }

        private void PopulateControllers()
        {
            controllerGrid.Rows.Clear();
            if (currentData == null)
            {
                return;
            }
            foreach (NpcGenController controller in currentData.Controllers)
            {
                controllerGrid.Rows.Add(controller.Id, controller.ControllerId, controller.Name, controller.Active != 0 ? "Yes" : "No", controller.WaitTime, controller.StopTime);
            }
        }

        private void LoadSelectedArea()
        {
            NpcGenArea area = GetSelectedArea();
            suppressChanges = true;
            entryGrid.Rows.Clear();
            attachGrid.Rows.Clear();
            if (area == null)
            {
                suppressChanges = false;
                return;
            }

            SetFloatBox(posX, area.Position.X);
            SetFloatBox(posY, area.Position.Y);
            SetFloatBox(posZ, area.Position.Z);
            SetFloatBox(dirX, area.Direction.X);
            SetFloatBox(dirY, area.Direction.Y);
            SetFloatBox(dirZ, area.Direction.Z);
            SetFloatBox(extX, area.Extents.X);
            SetFloatBox(extY, area.Extents.Y);
            SetFloatBox(extZ, area.Extents.Z);
            groupTypeCombo.SelectedIndex = area.AreaType == 1 ? 1 : 0;
            groupBehaviorCombo.SelectedIndex = Math.Max(0, Math.Min(groupBehaviorCombo.Items.Count - 1, area.GroupType));
            groupRawType.Value = Clamp(area.NpcType, groupRawType);
            groupGenId.Value = Clamp(area.GenId, groupGenId);
            groupControllerId.Value = Clamp(area.ControllerId, groupControllerId);
            groupLifeTime.Value = Clamp(area.LifeTime, groupLifeTime);
            groupMaxNum.Value = Clamp(area.MaxNum, groupMaxNum);
            groupExportId.Value = Clamp(area.ExportId, groupExportId);
            groupBufferRegionId.Value = Clamp(area.BufferRegionId, groupBufferRegionId);
            initGenCheckBox.Checked = area.InitGen != 0;
            autoReviveCheckBox.Checked = area.AutoRevive != 0;
            validOnceCheckBox.Checked = area.ValidOnce != 0;

            for (int i = 0; i < area.Entries.Count; i++)
            {
                NpcGenEntry entry = area.Entries[i];
                NpcGenEntityInfo info = entityLookupService.Resolve(sessionService != null ? sessionService.ListCollection : null, sessionService != null ? sessionService.Database : null, entry.Id);
                int row = entryGrid.Rows.Add(info.Icon ?? Properties.Resources.blank, entry.Id, string.IsNullOrWhiteSpace(info.Name) ? "NONE!" : info.Name);
                ApplyEntityRowStyle(entryGrid.Rows[row], info, 2);
            }
            foreach (int attachId in area.AttachIds)
            {
                attachGrid.Rows.Add(attachId);
            }
            if (entryGrid.Rows.Count > 0)
            {
                entryGrid.Rows[0].Selected = true;
            }
            suppressChanges = false;
            LoadSelectedEntry();
        }

        private void LoadSelectedEntry()
        {
            NpcGenEntry entry = GetSelectedEntry();
            suppressChanges = true;
            if (entry != null)
            {
                entryId.Value = Clamp(entry.Id, entryId);
                entryNum.Value = Clamp(entry.Num, entryNum);
                entryRefresh.Value = Clamp(entry.Refresh, entryRefresh);
                entryDiedTimes.Value = Clamp(entry.DiedTimes, entryDiedTimes);
                entryAggressive.Value = Clamp(entry.Aggressive, entryAggressive);
                SetFloatBox(entryOffsetWater, entry.OffsetWater);
                SetFloatBox(entryOffsetTerrain, entry.OffsetTerrain);
                entryPathId.Value = Clamp(entry.PathId, entryPathId);
                entryLoopType.Value = Clamp(entry.LoopType, entryLoopType);
                entrySpeedFlag.Value = Clamp(entry.SpeedFlag, entrySpeedFlag);
                entryDeadTime.Value = Clamp(entry.DeadTime, entryDeadTime);
                entryFaction.Value = Clamp(entry.Faction, entryFaction);
                entryFactionHelper.Value = Clamp(entry.FactionHelper, entryFactionHelper);
                entryFactionAccept.Value = Clamp(entry.FactionAccept, entryFactionAccept);
                entryNeedHelp.Checked = entry.NeedHelp != 0;
                entryDefaultFaction.Checked = entry.DefaultFaction != 0;
                entryDefaultFactionHelper.Checked = entry.DefaultFactionHelper != 0;
                entryDefaultFactionAccept.Checked = entry.DefaultFactionAccept != 0;
            }
            suppressChanges = false;
        }

        private void SaveSelectedArea()
        {
            if (suppressChanges)
            {
                return;
            }
            NpcGenArea area = GetSelectedArea();
            if (area == null)
            {
                return;
            }
            area.Position = new PointF3((float)posX.Value, (float)posY.Value, (float)posZ.Value);
            area.Direction = new PointF3((float)dirX.Value, (float)dirY.Value, (float)dirZ.Value);
            area.Extents = new PointF3((float)extX.Value, (float)extY.Value, (float)extZ.Value);
            area.AreaType = groupTypeCombo.SelectedIndex == 1 ? 1 : 0;
            area.GroupType = Math.Max(0, groupBehaviorCombo.SelectedIndex);
            area.NpcType = (int)groupRawType.Value;
            area.GenId = (int)groupGenId.Value;
            area.ControllerId = (int)groupControllerId.Value;
            area.LifeTime = (int)groupLifeTime.Value;
            area.MaxNum = (int)groupMaxNum.Value;
            area.ExportId = (int)groupExportId.Value;
            area.BufferRegionId = (int)groupBufferRegionId.Value;
            area.InitGen = (byte)(initGenCheckBox.Checked ? 1 : 0);
            area.AutoRevive = (byte)(autoReviveCheckBox.Checked ? 1 : 0);
            area.ValidOnce = (byte)(validOnceCheckBox.Checked ? 1 : 0);
            MarkDirty();
        }

        private void SaveSelectedEntry()
        {
            if (suppressChanges)
            {
                return;
            }
            NpcGenEntry entry = GetSelectedEntry();
            if (entry == null)
            {
                return;
            }
            entry.Id = (int)entryId.Value;
            entry.Num = (int)entryNum.Value;
            entry.Refresh = (int)entryRefresh.Value;
            entry.DiedTimes = (int)entryDiedTimes.Value;
            entry.Aggressive = (int)entryAggressive.Value;
            entry.OffsetWater = (float)entryOffsetWater.Value;
            entry.OffsetTerrain = (float)entryOffsetTerrain.Value;
            entry.PathId = (int)entryPathId.Value;
            entry.LoopType = (int)entryLoopType.Value;
            entry.SpeedFlag = (int)entrySpeedFlag.Value;
            entry.DeadTime = (int)entryDeadTime.Value;
            entry.Faction = (int)entryFaction.Value;
            entry.FactionHelper = (int)entryFactionHelper.Value;
            entry.FactionAccept = (int)entryFactionAccept.Value;
            entry.NeedHelp = (byte)(entryNeedHelp.Checked ? 1 : 0);
            entry.DefaultFaction = (byte)(entryDefaultFaction.Checked ? 1 : 0);
            entry.DefaultFactionHelper = (byte)(entryDefaultFactionHelper.Checked ? 1 : 0);
            entry.DefaultFactionAccept = (byte)(entryDefaultFactionAccept.Checked ? 1 : 0);
            MarkDirty();
            RefreshSelectedEntryRow(entry);
        }

        private void RefreshSelectedEntryRow(NpcGenEntry entry)
        {
            if (entryGrid.CurrentRow == null || entry == null)
            {
                return;
            }
            NpcGenEntityInfo info = entityLookupService.Resolve(sessionService != null ? sessionService.ListCollection : null, sessionService != null ? sessionService.Database : null, entry.Id);
            entryGrid.CurrentRow.Cells[0].Value = info.Icon ?? Properties.Resources.blank;
            entryGrid.CurrentRow.Cells[1].Value = entry.Id;
            entryGrid.CurrentRow.Cells[2].Value = string.IsNullOrWhiteSpace(info.Name) ? "NONE!" : info.Name;
            ApplyEntityRowStyle(entryGrid.CurrentRow, info, 2);
        }

        private NpcGenArea GetSelectedArea()
        {
            if (currentData == null || areaGrid.CurrentRow == null || areaGrid.CurrentRow.Tag == null)
            {
                return null;
            }
            int index = (int)areaGrid.CurrentRow.Tag;
            return index >= 0 && index < currentData.Areas.Count ? currentData.Areas[index] : null;
        }

        private NpcGenEntry GetSelectedEntry()
        {
            NpcGenArea area = GetSelectedArea();
            if (area == null || entryGrid.CurrentRow == null)
            {
                return null;
            }
            int index = entryGrid.CurrentRow.Index;
            return index >= 0 && index < area.Entries.Count ? area.Entries[index] : null;
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "npcgen.data|npcgen.data|Data files (*.data)|*.data|All files (*.*)|*.*";
                dialog.FileName = string.IsNullOrWhiteSpace(pathTextBox.Text) ? "npcgen.data" : pathTextBox.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    OpenFile(dialog.FileName);
                }
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (currentData == null)
            {
                return;
            }
            SaveSelectedArea();
            SaveSelectedEntry();
            fileService.Save(currentData, string.IsNullOrWhiteSpace(pathTextBox.Text) ? currentData.FilePath : pathTextBox.Text);
            isDirty = false;
            UpdateTitle();
            MessageBox.Show(this, "npcgen.data saved.", "NPCGen Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void areaGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (!suppressChanges)
            {
                LoadSelectedArea();
            }
        }

        private void entryGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (!suppressChanges)
            {
                LoadSelectedEntry();
            }
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            PopulateAreas();
        }

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                PopulateAreas();
                e.Handled = true;
            }
        }

        private void addAreaButton_Click(object sender, EventArgs e)
        {
            if (currentData == null)
            {
                return;
            }
            NpcGenArea area = new NpcGenArea();
            area.AreaType = 1;
            area.GroupType = 0;
            area.InitGen = 1;
            area.AutoRevive = 1;
            area.ValidOnce = 1;
            area.Entries.Add(new NpcGenEntry { Num = 1 });
            currentData.Areas.Add(area);
            MarkDirty();
            PopulateAreas();
        }

        private void deleteAreaButton_Click(object sender, EventArgs e)
        {
            if (currentData == null || areaGrid.CurrentRow == null || areaGrid.CurrentRow.Tag == null)
            {
                return;
            }
            int index = (int)areaGrid.CurrentRow.Tag;
            if (index >= 0 && index < currentData.Areas.Count)
            {
                currentData.Areas.RemoveAt(index);
                MarkDirty();
                PopulateAreas();
            }
        }

        private void addEntryButton_Click(object sender, EventArgs e)
        {
            NpcGenArea area = GetSelectedArea();
            if (area == null)
            {
                return;
            }
            area.Entries.Add(new NpcGenEntry { Num = 1 });
            MarkDirty();
            LoadSelectedArea();
        }

        private void deleteEntryButton_Click(object sender, EventArgs e)
        {
            NpcGenArea area = GetSelectedArea();
            if (area == null || entryGrid.CurrentRow == null)
            {
                return;
            }
            int index = entryGrid.CurrentRow.Index;
            if (index >= 0 && index < area.Entries.Count)
            {
                area.Entries.RemoveAt(index);
                MarkDirty();
                LoadSelectedArea();
            }
        }

        private void addAttachButton_Click(object sender, EventArgs e)
        {
            NpcGenArea area = GetSelectedArea();
            if (area == null)
            {
                return;
            }
            area.AttachIds.Add(0);
            attachGrid.Rows.Add(0);
            MarkDirty();
        }

        private void deleteAttachButton_Click(object sender, EventArgs e)
        {
            NpcGenArea area = GetSelectedArea();
            if (area == null || attachGrid.CurrentRow == null)
            {
                return;
            }
            int index = attachGrid.CurrentRow.Index;
            if (index >= 0 && index < area.AttachIds.Count)
            {
                area.AttachIds.RemoveAt(index);
                attachGrid.Rows.RemoveAt(index);
                MarkDirty();
            }
        }

        private void MarkDirty()
        {
            isDirty = true;
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            Text = (isDirty ? "*" : string.Empty) + "FWEledit - NPCGen Editor";
        }

        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.BackgroundColor = Color.FromArgb(17, 21, 26);
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.Dock = DockStyle.Fill;
            grid.EditMode = DataGridViewEditMode.EditOnEnter;
            grid.GridColor = Color.FromArgb(45, 52, 61);
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.RowTemplate.Height = 28;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            return grid;
        }

        private static void ApplyEntityRowStyle(DataGridViewRow row, NpcGenEntityInfo info, int nameColumnIndex)
        {
            if (row == null || info == null || nameColumnIndex < 0 || nameColumnIndex >= row.Cells.Count)
            {
                return;
            }

            if (info.NameColor.HasValue)
            {
                row.Cells[nameColumnIndex].Style.ForeColor = info.NameColor.Value;
                row.Cells[nameColumnIndex].Style.SelectionForeColor = info.NameColor.Value;
            }
            else
            {
                row.Cells[nameColumnIndex].Style.ForeColor = Color.White;
                row.Cells[nameColumnIndex].Style.SelectionForeColor = Color.White;
            }
        }

        private NumericUpDown CreateIntBox()
        {
            NumericUpDown box = new NumericUpDown();
            box.Dock = DockStyle.Fill;
            box.Maximum = int.MaxValue;
            box.Minimum = int.MinValue;
            box.ThousandsSeparator = true;
            return box;
        }

        private NumericUpDown CreateFloatBox()
        {
            NumericUpDown box = CreateIntBox();
            box.DecimalPlaces = 6;
            box.Increment = 0.1M;
            return box;
        }

        private void AddLabeledControl(TableLayoutPanel layout, string label, Control control, int row)
        {
            AddLabeledControl(layout, label, control, row, 0);
        }

        private void AddLabeledControl(TableLayoutPanel layout, string label, Control control, int row, int pair)
        {
            int column = pair * 2;
            layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, column, row);
            layout.Controls.Add(control, column + 1, row);
        }

        private void WireAreaEditorEvents(Control root)
        {
            foreach (Control control in root.Controls)
            {
                NumericUpDown number = control as NumericUpDown;
                if (number != null)
                {
                    number.ValueChanged += (s, e) => SaveSelectedArea();
                }
                CheckBox checkBox = control as CheckBox;
                if (checkBox != null)
                {
                    checkBox.CheckedChanged += (s, e) => SaveSelectedArea();
                }
                ComboBox comboBox = control as ComboBox;
                if (comboBox != null)
                {
                    comboBox.SelectedIndexChanged += (s, e) => SaveSelectedArea();
                }
            }
        }

        private void WireEntryEditorEvents(Control root)
        {
            foreach (Control control in root.Controls)
            {
                NumericUpDown number = control as NumericUpDown;
                if (number != null)
                {
                    number.ValueChanged += (s, e) => SaveSelectedEntry();
                }
                CheckBox checkBox = control as CheckBox;
                if (checkBox != null)
                {
                    checkBox.CheckedChanged += (s, e) => SaveSelectedEntry();
                }
            }
        }

        private static decimal Clamp(int value, NumericUpDown box)
        {
            if (value < box.Minimum)
            {
                return box.Minimum;
            }
            if (value > box.Maximum)
            {
                return box.Maximum;
            }
            return value;
        }

        private static void SetFloatBox(NumericUpDown box, float value)
        {
            decimal decimalValue = (decimal)value;
            if (decimalValue < box.Minimum)
            {
                decimalValue = box.Minimum;
            }
            if (decimalValue > box.Maximum)
            {
                decimalValue = box.Maximum;
            }
            box.Value = decimalValue;
        }

        private void ApplyDarkTheme(Control root)
        {
            root.BackColor = Color.FromArgb(17, 21, 26);
            root.ForeColor = Color.White;
            foreach (Control control in root.Controls)
            {
                if (control is TextBox || control is NumericUpDown || control is ComboBox)
                {
                    control.BackColor = Color.FromArgb(13, 17, 22);
                    control.ForeColor = Color.White;
                }
                else if (control is Button)
                {
                    control.BackColor = Color.FromArgb(37, 47, 62);
                    control.ForeColor = Color.White;
                }
                else if (control is DataGridView)
                {
                    StyleGrid((DataGridView)control);
                }
                ApplyDarkTheme(control);
            }
        }

        private void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Color.FromArgb(17, 21, 26);
            grid.ForeColor = Color.White;
            grid.DefaultCellStyle.BackColor = Color.FromArgb(17, 21, 26);
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(63, 111, 154);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(36, 43, 52);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.EnableHeadersVisualStyles = false;
        }
    }
}
