using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NpcGenEditorWindow : Form
    {
        private static readonly bool NpcGenMapViewEnabled = false;

        private readonly NpcGenFileService fileService = new NpcGenFileService();
        private readonly NpcGenEntityLookupService entityLookupService = new NpcGenEntityLookupService();
        private readonly NpcGenMapNameResolverService mapNameResolverService = new NpcGenMapNameResolverService();
        private readonly NpcGenMapPreviewService mapPreviewService = new NpcGenMapPreviewService();
        private readonly GameFolderDialogService folderDialogService = new GameFolderDialogService();
        private readonly ISessionService sessionService;
        private NpcGenData currentData;
        private string currentMapDisplayName = string.Empty;
        private bool suppressChanges;
        private bool isDirty;

        private TextBox pathTextBox;
        private Label mapNameLabel;
        private Button openButton;
        private Button openFolderButton;
        private Button saveButton;
        private Button mapViewButton;
        private TextBox searchTextBox;
        private Button searchButton;
        private TabControl tabs;
        private DataGridView areaGrid;
        private DataGridView entryGrid;
        private DataGridView attachGrid;
        private DataGridView controllerGrid;
        private DataGridView controllerLinkedGrid;
        private Label controllerLinkIdLabel;
        private Label controllerUsageLabel;
        private Label controllerEffectLabel;
        private Label controllerLinkHelpLabel;
        private Label controllerRealmTranslationLabel;
        private Button spawnEditorButton;
        private TextBox controllerNameTextBox;
        private NumericUpDown controllerTriggerIdBox;
        private NumericUpDown controllerWaitTimeBox;
        private NumericUpDown controllerStopTimeBox;
        private NumericUpDown controllerActiveTimeRangeBox;
        private TextBox controllerZoneMaskTextBox;
        private CheckBox controllerAllRealmsCheckBox;
        private CheckedListBox controllerRealmList;
        private Label controllerRealmSummaryLabel;
        private CheckBox controllerActiveCheckBox;
        private CheckBox controllerRepeatActiveCheckBox;
        private CheckBox controllerActiveTimeInvalidCheckBox;
        private CheckBox controllerStopTimeInvalidCheckBox;
        private ComboBox groupTypeCombo;
        private ComboBox groupBehaviorCombo;
        private Label groupControllerSummaryLabel;
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
        private Button goToControllerButton;
        private Label statusLabel;

        public NpcGenEditorWindow(ISessionService sessionService)
        {
            this.sessionService = sessionService;
            Text = "FWEledit - NPCGen Editor";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1280, 860);
            Size = new Size(1360, 900);
            BuildUi();
            ApplyDarkTheme(this);
        }

        public void OpenFile(string filePath)
        {
            currentData = fileService.Load(filePath);
            pathTextBox.Text = filePath;
            UpdateMapNameDisplay(filePath);
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
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            Controls.Add(root);

            TableLayoutPanel top = new TableLayoutPanel();
            top.Dock = DockStyle.Fill;
            top.ColumnCount = 5;
            top.RowCount = 2;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.Controls.Add(top, 0, 0);
            top.Controls.Add(new Label { Text = "npcgen.data:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            pathTextBox = new TextBox { Dock = DockStyle.Fill };
            top.Controls.Add(pathTextBox, 1, 0);
            openButton = new Button { Text = "Open", Dock = DockStyle.Fill };
            openFolderButton = new Button { Text = "Folder", Dock = DockStyle.Fill };
            saveButton = new Button { Text = "Save", Dock = DockStyle.Fill };
            openButton.Click += openButton_Click;
            openFolderButton.Click += openFolderButton_Click;
            saveButton.Click += saveButton_Click;
            top.Controls.Add(openButton, 2, 0);
            top.Controls.Add(openFolderButton, 3, 0);
            top.Controls.Add(saveButton, 4, 0);
            mapNameLabel = new Label
            {
                Text = "Map: (no file loaded)",
                Dock = DockStyle.Fill,
                Font = new Font(Font, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            top.Controls.Add(mapNameLabel, 0, 1);
            top.SetColumnSpan(mapNameLabel, 3);
            mapViewButton = new Button { Text = "Map View", Dock = DockStyle.Fill, Enabled = false, Margin = new Padding(3, 4, 3, 4) };
            mapViewButton.Click += mapViewButton_Click;
            top.Controls.Add(mapViewButton, 3, 1);
            top.SetColumnSpan(mapViewButton, 2);

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
            right.RowCount = 3;
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 370));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            main.Controls.Add(right, 1, 0);

            TableLayoutPanel upper = new TableLayoutPanel();
            upper.Dock = DockStyle.Fill;
            upper.ColumnCount = 2;
            upper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 74));
            upper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26));
            right.Controls.Add(upper, 0, 0);
            upper.Controls.Add(BuildParametersGroup(), 0, 0);
            upper.Controls.Add(BuildAttachGroup(), 1, 0);

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

            TableLayoutPanel vectorPanel = new TableLayoutPanel();
            vectorPanel.Dock = DockStyle.Fill;
            vectorPanel.ColumnCount = 3;
            vectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            vectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            vectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
            right.Controls.Add(vectorPanel, 0, 2);
            vectorPanel.Controls.Add(BuildVectorGroup("Position", out posX, out posY, out posZ), 0, 0);
            vectorPanel.Controls.Add(BuildVectorGroup("Direction", out dirX, out dirY, out dirZ), 1, 0);
            vectorPanel.Controls.Add(BuildVectorGroup("Spawn Area", out extX, out extY, out extZ), 2, 0);

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
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 13 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int row = 0; row < 13; row++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
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
            groupControllerSummaryLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            goToControllerButton = new Button { Text = "Go", Dock = DockStyle.Fill };
            goToControllerButton.Click += goToControllerButton_Click;
            initGenCheckBox = new CheckBox { Text = "Init Gen", Dock = DockStyle.Fill };
            autoReviveCheckBox = new CheckBox { Text = "Auto Revive", Dock = DockStyle.Fill };
            validOnceCheckBox = new CheckBox { Text = "Valid Once", Dock = DockStyle.Fill };

            TableLayoutPanel controllerPicker = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
            controllerPicker.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            controllerPicker.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
            controllerPicker.Controls.Add(groupControllerId, 0, 0);
            controllerPicker.Controls.Add(goToControllerButton, 1, 0);
            groupControllerId.ValueChanged += (s, e) => SaveSelectedArea();

            AddLabeledControl(layout, "Type:", groupTypeCombo, 0);
            AddLabeledControl(layout, "Behavior:", groupBehaviorCombo, 1);
            AddLabeledControl(layout, "Raw type:", groupRawType, 2);
            AddLabeledControl(layout, "Gen ID:", groupGenId, 3);
            AddLabeledControl(layout, "ID Ctrl:", controllerPicker, 4);
            AddLabeledControl(layout, "Controller:", groupControllerSummaryLabel, 5);
            AddLabeledControl(layout, "Lifetime:", groupLifeTime, 6);
            AddLabeledControl(layout, "Max Num:", groupMaxNum, 7);
            AddLabeledControl(layout, "Export ID:", groupExportId, 8);
            AddLabeledControl(layout, "Buf Region:", groupBufferRegionId, 9);
            layout.Controls.Add(initGenCheckBox, 1, 10);
            layout.Controls.Add(autoReviveCheckBox, 1, 11);
            layout.Controls.Add(validOnceCheckBox, 1, 12);
            WireAreaEditorEvents(layout);
            group.Controls.Add(layout);
            return group;
        }

        private GroupBox BuildAttachGroup()
        {
            GroupBox group = new GroupBox { Text = "Attaches", Dock = DockStyle.Fill };
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            attachGrid = CreateGrid();
            attachGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Attach ID", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            layout.Controls.Add(attachGrid, 0, 0);
            TableLayoutPanel buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            addAttachButton = new Button { Text = "Add", Dock = DockStyle.Fill };
            deleteAttachButton = new Button { Text = "Delete", Dock = DockStyle.Fill };
            addAttachButton.Click += addAttachButton_Click;
            deleteAttachButton.Click += deleteAttachButton_Click;
            buttons.Controls.Add(addAttachButton, 0, 0);
            buttons.Controls.Add(deleteAttachButton, 0, 1);
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
            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Horizontal;
            split.SizeChanged += (s, e) => SetControllerSplitterDistance(split);
            page.Controls.Add(split);

            controllerGrid = CreateGrid();
            controllerGrid.Dock = DockStyle.Fill;
            controllerGrid.ReadOnly = true;
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Link ID", Width = 72 });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trigger", Width = 76 });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Starts", Width = 62 });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Delay", Width = 64 });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Life", Width = 64 });
            controllerGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Uses", Width = 58 });
            controllerGrid.SelectionChanged += controllerGrid_SelectionChanged;
            split.Panel1.Controls.Add(controllerGrid);

            TableLayoutPanel bottom = new TableLayoutPanel();
            bottom.Dock = DockStyle.Fill;
            bottom.ColumnCount = 2;
            bottom.RowCount = 1;
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 560));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            split.Panel2.Controls.Add(bottom);

            TableLayoutPanel details = new TableLayoutPanel();
            details.Dock = DockStyle.Fill;
            details.RowCount = 3;
            details.RowStyles.Add(new RowStyle(SizeType.Absolute, 102));
            details.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
            details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            bottom.Controls.Add(details, 0, 0);

            TableLayoutPanel summary = new TableLayoutPanel();
            summary.Dock = DockStyle.Fill;
            summary.ColumnCount = 2;
            summary.RowCount = 4;
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
            summary.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            summary.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            summary.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            summary.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            controllerUsageLabel = new Label { Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            controllerEffectLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            controllerRealmTranslationLabel = new Label { Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            controllerLinkHelpLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            spawnEditorButton = new Button { Text = "Spawn Editor", Dock = DockStyle.Fill };
            spawnEditorButton.Click += spawnEditorButton_Click;
            summary.Controls.Add(controllerUsageLabel, 0, 0);
            summary.SetColumnSpan(controllerUsageLabel, 2);
            summary.Controls.Add(controllerEffectLabel, 0, 1);
            summary.SetColumnSpan(controllerEffectLabel, 2);
            summary.Controls.Add(controllerRealmTranslationLabel, 0, 2);
            summary.Controls.Add(spawnEditorButton, 1, 2);
            summary.Controls.Add(controllerLinkHelpLabel, 0, 3);
            summary.SetColumnSpan(controllerLinkHelpLabel, 2);
            details.Controls.Add(summary, 0, 0);

            details.Controls.Add(BuildControllerDetailsGroup(), 0, 1);
            details.Controls.Add(BuildControllerRealmGroup(), 0, 2);

            GroupBox linkedGroup = new GroupBox { Text = "Linked spawns", Dock = DockStyle.Fill };
            controllerLinkedGrid = CreateGrid();
            controllerLinkedGrid.ReadOnly = true;
            controllerLinkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kind", Width = 92 });
            controllerLinkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "No", Width = 58 });
            controllerLinkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 78 });
            controllerLinkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            controllerLinkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Other Filter", Width = 190 });
            controllerLinkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Count", Width = 58 });
            controllerLinkedGrid.CellDoubleClick += controllerLinkedGrid_CellDoubleClick;
            linkedGroup.Controls.Add(controllerLinkedGrid);
            bottom.Controls.Add(linkedGroup, 1, 0);

            return page;
        }

        private GroupBox BuildControllerDetailsGroup()
        {
            GroupBox group = new GroupBox { Text = "Controller details", Dock = DockStyle.Fill };
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 4;
            layout.RowCount = 7;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < 7; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            }

            controllerLinkIdLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            controllerNameTextBox = new TextBox { Dock = DockStyle.Fill };
            controllerTriggerIdBox = CreateIntBox();
            controllerWaitTimeBox = CreateIntBox();
            controllerStopTimeBox = CreateIntBox();
            controllerActiveTimeRangeBox = CreateIntBox();
            controllerZoneMaskTextBox = new TextBox { Dock = DockStyle.Fill };
            controllerActiveCheckBox = new CheckBox { Text = "Starts active", Dock = DockStyle.Fill };
            controllerRepeatActiveCheckBox = new CheckBox { Text = "Repeat", Dock = DockStyle.Fill };
            controllerActiveTimeInvalidCheckBox = new CheckBox { Text = "No start time", Dock = DockStyle.Fill };
            controllerStopTimeInvalidCheckBox = new CheckBox { Text = "No stop time", Dock = DockStyle.Fill };

            AddLabeledControl(layout, "Link ID:", controllerLinkIdLabel, 0, 0);
            AddLabeledControl(layout, "Trigger ID:", controllerTriggerIdBox, 0, 1);
            AddLabeledControl(layout, "Name:", controllerNameTextBox, 1, 0);
            layout.SetColumnSpan(controllerNameTextBox, 3);
            AddLabeledControl(layout, "Delay:", controllerWaitTimeBox, 2, 0);
            AddLabeledControl(layout, "Lifetime:", controllerStopTimeBox, 2, 1);
            AddLabeledControl(layout, "Time range:", controllerActiveTimeRangeBox, 3, 0);
            AddLabeledControl(layout, "Zone mask:", controllerZoneMaskTextBox, 3, 1);
            layout.Controls.Add(controllerActiveCheckBox, 1, 4);
            layout.Controls.Add(controllerRepeatActiveCheckBox, 3, 4);
            layout.Controls.Add(controllerActiveTimeInvalidCheckBox, 1, 5);
            layout.Controls.Add(controllerStopTimeInvalidCheckBox, 3, 5);

            WireControllerEditorEvents(layout);
            group.Controls.Add(layout);
            return group;
        }

        private GroupBox BuildControllerRealmGroup()
        {
            GroupBox group = new GroupBox { Text = "Controller realm filter", Dock = DockStyle.Fill };
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

            controllerAllRealmsCheckBox = new CheckBox { Text = "All realms", Dock = DockStyle.Fill };
            controllerAllRealmsCheckBox.CheckedChanged += controllerAllRealmsCheckBox_CheckedChanged;
            layout.Controls.Add(controllerAllRealmsCheckBox, 0, 0);

            controllerRealmList = new CheckedListBox();
            controllerRealmList.CheckOnClick = true;
            controllerRealmList.Dock = DockStyle.Fill;
            controllerRealmList.IntegralHeight = false;
            for (int realm = 1; realm <= 32; realm++)
            {
                controllerRealmList.Items.Add(GetRealmDisplayName(realm));
            }
            controllerRealmList.ItemCheck += controllerRealmList_ItemCheck;
            layout.Controls.Add(controllerRealmList, 0, 1);

            controllerRealmSummaryLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(controllerRealmSummaryLabel, 0, 2);

            group.Controls.Add(layout);
            return group;
        }

        private void PopulateAll()
        {
            PopulateAreas();
            PopulateControllers();
            if (mapViewButton != null)
            {
                mapViewButton.Enabled = NpcGenMapViewEnabled && currentData != null;
            }
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
            int oldIndex = controllerGrid.CurrentRow != null ? controllerGrid.CurrentRow.Index : 0;
            controllerGrid.Rows.Clear();
            if (currentData == null)
            {
                LoadSelectedController();
                return;
            }
            for (int i = 0; i < currentData.Controllers.Count; i++)
            {
                NpcGenController controller = currentData.Controllers[i];
                int row = controllerGrid.Rows.Add(
                    controller.Id,
                    controller.ControllerId,
                    controller.Name,
                    controller.Active != 0 ? "Yes" : "No",
                    controller.WaitTime,
                    controller.StopTime,
                    CountControllerUses(controller.Id));
                controllerGrid.Rows[row].Tag = i;
            }

            if (controllerGrid.Rows.Count > 0)
            {
                controllerGrid.Rows[Math.Min(oldIndex, controllerGrid.Rows.Count - 1)].Selected = true;
            }
            LoadSelectedController();
        }

        private void LoadSelectedArea()
        {
            NpcGenArea area = GetSelectedArea();
            suppressChanges = true;
            entryGrid.Rows.Clear();
            attachGrid.Rows.Clear();
            if (area == null)
            {
                RefreshSelectedAreaControllerSummary(null);
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
            RefreshSelectedAreaControllerSummary(area);

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

        private void LoadSelectedController()
        {
            NpcGenController controller = GetSelectedController();
            suppressChanges = true;
            if (controllerLinkedGrid != null)
            {
                controllerLinkedGrid.Rows.Clear();
            }

            if (controller == null)
            {
                controllerLinkIdLabel.Text = string.Empty;
                controllerUsageLabel.Text = "No controller selected";
                controllerEffectLabel.Text = string.Empty;
                controllerRealmTranslationLabel.Text = string.Empty;
                controllerLinkHelpLabel.Text = string.Empty;
                controllerNameTextBox.Text = string.Empty;
                controllerTriggerIdBox.Value = 0;
                controllerWaitTimeBox.Value = 0;
                controllerStopTimeBox.Value = 0;
                controllerActiveTimeRangeBox.Value = 0;
                controllerZoneMaskTextBox.Text = string.Empty;
                LoadControllerRealmMask(0);
                controllerActiveCheckBox.Checked = false;
                controllerRepeatActiveCheckBox.Checked = false;
                controllerActiveTimeInvalidCheckBox.Checked = false;
                controllerStopTimeInvalidCheckBox.Checked = false;
                suppressChanges = false;
                return;
            }

            controllerLinkIdLabel.Text = controller.Id.ToString(CultureInfo.InvariantCulture);
            controllerUsageLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "Controller {0} is used by {1:N0} spawn group(s)",
                controller.Id,
                CountControllerUses(controller.Id));
            controllerEffectLabel.Text = BuildControllerEffectText(controller);
            controllerRealmTranslationLabel.Text = "Use Spawn Editor to inspect or change realm rules.";
            controllerLinkHelpLabel.Text = "Groups link by ID Ctrl. Scripts/templates may still restrict the final realm.";
            controllerNameTextBox.Text = controller.Name ?? string.Empty;
            controllerTriggerIdBox.Value = Clamp(controller.ControllerId, controllerTriggerIdBox);
            controllerWaitTimeBox.Value = Clamp(controller.WaitTime, controllerWaitTimeBox);
            controllerStopTimeBox.Value = Clamp(controller.StopTime, controllerStopTimeBox);
            controllerActiveTimeRangeBox.Value = Clamp(controller.ActiveTimeRange, controllerActiveTimeRangeBox);
            controllerZoneMaskTextBox.Text = controller.ZoneMask.ToString(CultureInfo.InvariantCulture);
            LoadControllerRealmMask(controller.ZoneMask);
            controllerActiveCheckBox.Checked = controller.Active != 0;
            controllerRepeatActiveCheckBox.Checked = controller.RepeatActive != 0;
            controllerActiveTimeInvalidCheckBox.Checked = controller.ActiveTimeInvalid != 0;
            controllerStopTimeInvalidCheckBox.Checked = controller.StopTimeInvalid != 0;
            PopulateControllerLinks(controller.Id);
            suppressChanges = false;
        }

        private void SaveSelectedController()
        {
            if (suppressChanges)
            {
                return;
            }
            NpcGenController controller = GetSelectedController();
            if (controller == null)
            {
                return;
            }

            long zoneMask;
            if (!long.TryParse(controllerZoneMaskTextBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out zoneMask))
            {
                controllerZoneMaskTextBox.BackColor = Color.FromArgb(70, 31, 31);
                return;
            }

            controllerZoneMaskTextBox.BackColor = Color.FromArgb(13, 17, 22);
            controller.Name = controllerNameTextBox.Text;
            controller.ControllerId = (int)controllerTriggerIdBox.Value;
            controller.WaitTime = (int)controllerWaitTimeBox.Value;
            controller.StopTime = (int)controllerStopTimeBox.Value;
            controller.ActiveTimeRange = (int)controllerActiveTimeRangeBox.Value;
            controller.ZoneMask = zoneMask;
            controller.Active = (byte)(controllerActiveCheckBox.Checked ? 1 : 0);
            controller.RepeatActive = (byte)(controllerRepeatActiveCheckBox.Checked ? 1 : 0);
            controller.ActiveTimeInvalid = (byte)(controllerActiveTimeInvalidCheckBox.Checked ? 1 : 0);
            controller.StopTimeInvalid = (byte)(controllerStopTimeInvalidCheckBox.Checked ? 1 : 0);
            LoadControllerRealmMask(controller.ZoneMask);
            MarkDirty();
            RefreshSelectedControllerRow(controller);
            controllerEffectLabel.Text = BuildControllerEffectText(controller);
        }

        private void RefreshSelectedControllerRow(NpcGenController controller)
        {
            if (controllerGrid.CurrentRow == null || controller == null)
            {
                return;
            }

            controllerGrid.CurrentRow.Cells[0].Value = controller.Id;
            controllerGrid.CurrentRow.Cells[1].Value = controller.ControllerId;
            controllerGrid.CurrentRow.Cells[2].Value = controller.Name;
            controllerGrid.CurrentRow.Cells[3].Value = controller.Active != 0 ? "Yes" : "No";
            controllerGrid.CurrentRow.Cells[4].Value = controller.WaitTime;
            controllerGrid.CurrentRow.Cells[5].Value = controller.StopTime;
            controllerGrid.CurrentRow.Cells[6].Value = CountControllerUses(controller.Id);
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
            area.Position = new PointF3(ReadFloatBox(posX), ReadFloatBox(posY), ReadFloatBox(posZ));
            area.Direction = new PointF3(ReadFloatBox(dirX), ReadFloatBox(dirY), ReadFloatBox(dirZ));
            area.Extents = new PointF3(ReadFloatBox(extX), ReadFloatBox(extY), ReadFloatBox(extZ));
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
            RefreshSelectedAreaControllerSummary(area);
            MarkDirty();
            RefreshControllerUsageState();
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
            entry.OffsetWater = ReadFloatBox(entryOffsetWater);
            entry.OffsetTerrain = ReadFloatBox(entryOffsetTerrain);
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
            int index = GetSelectedAreaIndex();
            return index >= 0 && index < currentData.Areas.Count ? currentData.Areas[index] : null;
        }

        private int GetSelectedAreaIndex()
        {
            if (currentData == null || areaGrid.CurrentRow == null || areaGrid.CurrentRow.Tag == null)
            {
                return -1;
            }

            int index = (int)areaGrid.CurrentRow.Tag;
            return index >= 0 && index < currentData.Areas.Count ? index : -1;
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

        private NpcGenController GetSelectedController()
        {
            if (currentData == null || controllerGrid.CurrentRow == null || controllerGrid.CurrentRow.Tag == null)
            {
                return null;
            }
            int index = (int)controllerGrid.CurrentRow.Tag;
            return index >= 0 && index < currentData.Controllers.Count ? currentData.Controllers[index] : null;
        }

        private NpcGenController FindControllerByLinkId(int linkId)
        {
            return currentData == null ? null : currentData.Controllers.FirstOrDefault(controller => controller.Id == linkId);
        }

        private void RefreshSelectedAreaControllerSummary(NpcGenArea area)
        {
            if (groupControllerSummaryLabel == null || goToControllerButton == null)
            {
                return;
            }

            if (area == null)
            {
                groupControllerSummaryLabel.Text = string.Empty;
                goToControllerButton.Enabled = false;
                return;
            }

            NpcGenController controller = FindControllerByLinkId(area.ControllerId);
            if (controller == null)
            {
                groupControllerSummaryLabel.Text = area.ControllerId == 0
                    ? "Default controller (0)"
                    : "No matching controller";
                goToControllerButton.Enabled = false;
                return;
            }

            string controllerName = string.IsNullOrWhiteSpace(controller.Name) ? "(no name)" : controller.Name;
            groupControllerSummaryLabel.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0} | Trigger {1} | {2}",
                controllerName,
                controller.ControllerId,
                BuildControllerEffectText(controller));
            goToControllerButton.Enabled = true;
        }

        private void SelectControllerByLinkId(int linkId)
        {
            if (currentData == null)
            {
                return;
            }

            for (int i = 0; i < controllerGrid.Rows.Count; i++)
            {
                DataGridViewRow row = controllerGrid.Rows[i];
                if (row.Tag == null)
                {
                    continue;
                }

                int controllerIndex = (int)row.Tag;
                if (controllerIndex >= 0
                    && controllerIndex < currentData.Controllers.Count
                    && currentData.Controllers[controllerIndex].Id == linkId)
                {
                    tabs.SelectedIndex = 1;
                    row.Selected = true;
                    controllerGrid.CurrentCell = row.Cells[0];
                    LoadSelectedController();
                    return;
                }
            }
        }

        private int CountControllerUses(int controllerId)
        {
            if (currentData == null)
            {
                return 0;
            }

            int npcCount = currentData.Areas.Count(area => area.ControllerId == controllerId);
            int resourceCount = currentData.ResourceAreas.Count(area => area.ControllerId == controllerId);
            int dynamicObjectCount = currentData.DynamicObjects.Count(item => item.ControllerId == (uint)controllerId);
            return npcCount + resourceCount + dynamicObjectCount;
        }

        private string BuildControllerEffectText(NpcGenController controller)
        {
            if (controller == null)
            {
                return string.Empty;
            }

            string startMode = controller.Active != 0 ? "starts active" : "waits for trigger";
            string delay = controller.WaitTime > 0
                ? string.Format(CultureInfo.InvariantCulture, "delay {0}s", controller.WaitTime)
                : "no delay";
            string lifetime = controller.StopTime > 0
                ? string.Format(CultureInfo.InvariantCulture, "lifetime {0}s", controller.StopTime)
                : "stays until stopped";
            return string.Format(CultureInfo.InvariantCulture, "{0}; {1}; {2}; {3}", startMode, delay, lifetime, BuildRealmMaskSummary(controller.ZoneMask));
        }

        private void LoadControllerRealmMask(long zoneMask)
        {
            if (controllerRealmList == null || controllerAllRealmsCheckBox == null || controllerRealmSummaryLabel == null)
            {
                return;
            }

            bool previousSuppress = suppressChanges;
            suppressChanges = true;
            controllerAllRealmsCheckBox.Checked = zoneMask == -1;
            for (int i = 0; i < controllerRealmList.Items.Count; i++)
            {
                long bit = 1L << i;
                controllerRealmList.SetItemChecked(i, zoneMask == -1 || (zoneMask & bit) != 0);
            }
            controllerRealmList.Enabled = zoneMask != -1;
            controllerRealmSummaryLabel.Text = BuildRealmMaskSummary(zoneMask);
            suppressChanges = previousSuppress;
        }

        private long BuildMaskFromRealmChecks()
        {
            long mask = 0;
            if (controllerRealmList == null)
            {
                return mask;
            }

            for (int i = 0; i < controllerRealmList.Items.Count; i++)
            {
                if (controllerRealmList.GetItemChecked(i))
                {
                    mask |= 1L << i;
                }
            }
            return mask;
        }

        private static string BuildRealmMaskSummary(long zoneMask)
        {
            if (zoneMask == -1)
            {
                return "controller allows all realms";
            }
            if (zoneMask == 0)
            {
                return "controller allows no realm";
            }

            string realms = string.Join(
                ", ",
                Enumerable.Range(1, 32)
                    .Where(realm => (zoneMask & (1L << (realm - 1))) != 0)
                    .Select(realm => realm.ToString(CultureInfo.InvariantCulture))
                    .ToArray());
            return "controller allows realm(s): " + realms;
        }

        private static string BuildEntityRealmFilterSummary(NpcGenEntityInfo info)
        {
            if (info == null || !info.ZoneMask.HasValue)
            {
                return "not found in template";
            }

            return BuildGenericRealmMaskSummary(info.ZoneMask.Value);
        }

        private static string BuildGenericRealmMaskSummary(long zoneMask)
        {
            if (zoneMask == -1)
            {
                return "all realms";
            }
            if (zoneMask == 0)
            {
                return "no realm";
            }

            string realms = string.Join(
                ", ",
                Enumerable.Range(1, 32)
                    .Where(realm => (zoneMask & (1L << (realm - 1))) != 0)
                    .Select(realm => realm.ToString(CultureInfo.InvariantCulture))
                    .ToArray());
            return "realm(s): " + realms;
        }

        private static string GetRealmDisplayName(int realm)
        {
            return string.Format(CultureInfo.InvariantCulture, "Realm {0}", realm);
        }

        private void PopulateControllerLinks(int controllerId)
        {
            if (controllerLinkedGrid == null || currentData == null)
            {
                return;
            }

            controllerLinkedGrid.Rows.Clear();
            for (int i = 0; i < currentData.Areas.Count; i++)
            {
                NpcGenArea area = currentData.Areas[i];
                if (area.ControllerId != controllerId)
                {
                    continue;
                }

                NpcGenEntry first = area.Entries.FirstOrDefault();
                int id = first != null ? first.Id : 0;
                NpcGenEntityInfo info = entityLookupService.Resolve(sessionService != null ? sessionService.ListCollection : null, sessionService != null ? sessionService.Database : null, id);
                int row = controllerLinkedGrid.Rows.Add(
                    "NPC group",
                    i,
                    id,
                    string.IsNullOrWhiteSpace(info.Name) ? "NONE!" : info.Name,
                    BuildEntityRealmFilterSummary(info),
                    area.Entries.Count);
                controllerLinkedGrid.Rows[row].Tag = new ControllerLinkInfo("npc", i);
                ApplyEntityRowStyle(controllerLinkedGrid.Rows[row], info, 3);
            }

            for (int i = 0; i < currentData.ResourceAreas.Count; i++)
            {
                NpcGenResourceArea area = currentData.ResourceAreas[i];
                if (area.ControllerId != controllerId)
                {
                    continue;
                }

                NpcGenResourceEntry first = area.Entries.FirstOrDefault();
                int id = first != null ? first.TemplateId : 0;
                int row = controllerLinkedGrid.Rows.Add("Resource", i, id, "Resource area", "not found in template", area.Entries.Count);
                controllerLinkedGrid.Rows[row].Tag = new ControllerLinkInfo("resource", i);
            }

            for (int i = 0; i < currentData.DynamicObjects.Count; i++)
            {
                NpcGenDynamicObject item = currentData.DynamicObjects[i];
                if (item.ControllerId != (uint)controllerId)
                {
                    continue;
                }

                int row = controllerLinkedGrid.Rows.Add("Dyn object", i, item.DynamicObjectId, "Dynamic object", "not found in template", 1);
                controllerLinkedGrid.Rows[row].Tag = new ControllerLinkInfo("dynamic", i);
            }
        }

        private IEnumerable<int> GetEntityIdsForController(int controllerId)
        {
            if (currentData == null)
            {
                return Enumerable.Empty<int>();
            }

            return currentData.Areas
                .Where(area => area.ControllerId == controllerId)
                .SelectMany(area => area.Entries)
                .Select(entry => entry.Id)
                .Where(id => id > 0)
                .Distinct()
                .ToArray();
        }

        private void RefreshControllerUsageState()
        {
            if (currentData == null || controllerGrid == null)
            {
                return;
            }

            foreach (DataGridViewRow row in controllerGrid.Rows)
            {
                if (row.Tag == null)
                {
                    continue;
                }

                int index = (int)row.Tag;
                if (index >= 0 && index < currentData.Controllers.Count)
                {
                    NpcGenController controller = currentData.Controllers[index];
                    row.Cells[6].Value = CountControllerUses(controller.Id);
                }
            }

            NpcGenController selected = GetSelectedController();
            if (selected != null)
            {
                controllerUsageLabel.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Controller {0} is used by {1:N0} spawn group(s)",
                    selected.Id,
                    CountControllerUses(selected.Id));
                PopulateControllerLinks(selected.Id);
            }
        }

        private static void SetControllerSplitterDistance(SplitContainer split)
        {
            const int panel1MinSize = 120;
            const int panel2MinSize = 160;
            int available = split.Height - split.SplitterWidth;
            if (available <= panel1MinSize + panel2MinSize)
            {
                return;
            }

            int desired = Math.Max(panel1MinSize, Math.Min(300, available - panel2MinSize));
            if (split.SplitterDistance != desired)
            {
                split.SplitterDistance = desired;
            }
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

        private void openFolderButton_Click(object sender, EventArgs e)
        {
            string currentPath = !string.IsNullOrWhiteSpace(pathTextBox.Text)
                ? Path.GetDirectoryName(pathTextBox.Text)
                : string.Empty;
            string selectedPath = folderDialogService.PromptForGameFolder(
                "Select the map folder that contains npcgen.data",
                currentPath,
                this);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            string npcGenPath = Path.Combine(selectedPath, "npcgen.data");
            if (!File.Exists(npcGenPath))
            {
                MessageBox.Show(this, "npcgen.data was not found in this folder.", "NPCGen Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OpenFile(npcGenPath);
        }

        private void mapViewButton_Click(object sender, EventArgs e)
        {
            if (!NpcGenMapViewEnabled)
            {
                return;
            }

            if (currentData == null)
            {
                return;
            }

            if (!EnsureMapPreviewClientRoot())
            {
                return;
            }

            SaveSelectedArea();
            SaveSelectedEntry();
            int initialAreaIndex = GetSelectedAreaIndex();
            int initialEntryIndex = entryGrid.CurrentRow != null ? entryGrid.CurrentRow.Index : -1;
            NpcGenMapPreviewData preview = mapPreviewService.BuildPreview(
                currentData,
                sessionService,
                entityLookupService,
                currentMapDisplayName,
                initialAreaIndex,
                initialEntryIndex);
            NpcGenMapPreviewWindow window = new NpcGenMapPreviewWindow(preview, SelectNpcEntryByIndex);
            window.Show();
        }

        private bool EnsureMapPreviewClientRoot()
        {
            string inferredRoot = TryInferClientRootFromNpcGenPath(currentData != null ? currentData.FilePath : string.Empty);
            string currentRoot = AssetManager.GameRootPath ?? string.Empty;
            if (IsUsableMapPreviewClientRoot(inferredRoot))
            {
                ApplyMapPreviewClientRoot(inferredRoot);
                return true;
            }

            if (IsUsableMapPreviewClientRoot(currentRoot))
            {
                return true;
            }

            string selectedRoot = folderDialogService.PromptForGameFolder(
                "Select the client root folder that contains resources\\script.pck and resources\\surfaces.pck",
                !string.IsNullOrWhiteSpace(inferredRoot) ? inferredRoot : currentRoot,
                this);
            if (string.IsNullOrWhiteSpace(selectedRoot))
            {
                return false;
            }

            if (!IsUsableMapPreviewClientRoot(selectedRoot))
            {
                MessageBox.Show(
                    this,
                    "This folder does not look like a client root. Select the folder that contains resources\\script.pck and resources\\surfaces.pck.",
                    "NPCGen Map View",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            ApplyMapPreviewClientRoot(selectedRoot);
            return true;
        }

        private void ApplyMapPreviewClientRoot(string root)
        {
            string normalized = Path.GetFullPath(root ?? string.Empty);
            string currentRoot = AssetManager.GameRootPath ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(currentRoot)
                && string.Equals(Path.GetFullPath(currentRoot), normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            AssetManager.GameRootPath = normalized;
            AssetManager.WorkspaceRootPath = string.Empty;
            if (sessionService != null)
            {
                sessionService.AssetManager = new AssetManager(sessionService);
            }
        }

        private static bool IsUsableMapPreviewClientRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return false;
            }

            string resources = Path.Combine(root, "resources");
            return File.Exists(Path.Combine(resources, "script.pck"))
                && File.Exists(Path.Combine(resources, "surfaces.pck"));
        }

        private static string TryInferClientRootFromNpcGenPath(string npcGenPath)
        {
            if (string.IsNullOrWhiteSpace(npcGenPath))
            {
                return string.Empty;
            }

            try
            {
                DirectoryInfo directory = new DirectoryInfo(Path.GetDirectoryName(npcGenPath) ?? string.Empty);
                while (directory != null)
                {
                    if (string.Equals(directory.Name, "maps", StringComparison.OrdinalIgnoreCase))
                    {
                        return directory.Parent != null ? directory.Parent.FullName : string.Empty;
                    }

                    if (Directory.Exists(Path.Combine(directory.FullName, "maps"))
                        && Directory.Exists(Path.Combine(directory.FullName, "resources")))
                    {
                        return directory.FullName;
                    }

                    directory = directory.Parent;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (currentData == null)
            {
                return;
            }
            SaveSelectedArea();
            SaveSelectedEntry();
            SaveSelectedController();
            string savePath = string.IsNullOrWhiteSpace(pathTextBox.Text) ? currentData.FilePath : pathTextBox.Text;
            fileService.Save(currentData, savePath);
            pathTextBox.Text = savePath;
            UpdateMapNameDisplay(savePath);
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

        private void controllerGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (!suppressChanges)
            {
                LoadSelectedController();
            }
        }

        private void controllerLinkedGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (currentData == null || e.RowIndex < 0 || e.RowIndex >= controllerLinkedGrid.Rows.Count)
            {
                return;
            }

            ControllerLinkInfo link = controllerLinkedGrid.Rows[e.RowIndex].Tag as ControllerLinkInfo;
            if (link == null || link.Kind != "npc" || link.Index < 0 || link.Index >= currentData.Areas.Count)
            {
                return;
            }

            tabs.SelectedIndex = 0;
            foreach (DataGridViewRow row in areaGrid.Rows)
            {
                if (row.Tag is int && (int)row.Tag == link.Index)
                {
                    row.Selected = true;
                    areaGrid.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }

        private void goToControllerButton_Click(object sender, EventArgs e)
        {
            NpcGenArea area = GetSelectedArea();
            if (area == null || FindControllerByLinkId(area.ControllerId) == null)
            {
                return;
            }

            SelectControllerByLinkId(area.ControllerId);
        }

        private void SelectNpcEntryByIndex(int areaIndex, int entryIndex)
        {
            if (currentData == null || areaIndex < 0 || areaIndex >= currentData.Areas.Count)
            {
                return;
            }

            tabs.SelectedIndex = 0;
            foreach (DataGridViewRow row in areaGrid.Rows)
            {
                if (row.Tag is int && (int)row.Tag == areaIndex)
                {
                    row.Selected = true;
                    areaGrid.CurrentCell = row.Cells[0];
                    LoadSelectedArea();
                    SelectEntryByIndex(entryIndex);
                    return;
                }
            }
        }

        private void SelectEntryByIndex(int entryIndex)
        {
            if (entryIndex < 0 || entryIndex >= entryGrid.Rows.Count)
            {
                return;
            }

            entryGrid.Rows[entryIndex].Selected = true;
            entryGrid.CurrentCell = entryGrid.Rows[entryIndex].Cells[0];
            LoadSelectedEntry();
        }

        private void spawnEditorButton_Click(object sender, EventArgs e)
        {
            NpcGenController controller = GetSelectedController();
            if (currentData == null || controller == null)
            {
                return;
            }

            using (NpcGenSpawnEditorWindow window = new NpcGenSpawnEditorWindow(
                currentData,
                controller,
                sessionService,
                entityLookupService))
            {
                window.ShowDialog(this);
            }
        }

        private void controllerAllRealmsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressChanges)
            {
                return;
            }

            controllerZoneMaskTextBox.Text = controllerAllRealmsCheckBox.Checked
                ? "-1"
                : BuildMaskFromRealmChecks().ToString(CultureInfo.InvariantCulture);
            controllerRealmList.Enabled = !controllerAllRealmsCheckBox.Checked;
        }

        private void controllerRealmList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (suppressChanges)
            {
                return;
            }

            BeginInvoke(new Action(delegate
            {
                if (suppressChanges || controllerAllRealmsCheckBox.Checked)
                {
                    return;
                }

                controllerZoneMaskTextBox.Text = BuildMaskFromRealmChecks().ToString(CultureInfo.InvariantCulture);
            }));
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
            string mapSuffix = string.IsNullOrWhiteSpace(currentMapDisplayName) ? string.Empty : " - " + currentMapDisplayName;
            Text = (isDirty ? "*" : string.Empty) + "FWEledit - NPCGen Editor" + mapSuffix;
        }

        private void UpdateMapNameDisplay(string filePath)
        {
            if (mapNameLabel == null)
            {
                return;
            }

            string technicalMapName = GetTechnicalMapNameFromNpcGenPath(filePath);
            currentMapDisplayName = mapNameResolverService.ResolveDisplayName(filePath, technicalMapName);
            mapNameLabel.Text = string.IsNullOrWhiteSpace(currentMapDisplayName)
                ? "Map: (unknown)"
                : "Map: " + currentMapDisplayName;
        }

        private static string GetTechnicalMapNameFromNpcGenPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return string.Empty;
            }

            try
            {
                string directory = Path.GetDirectoryName(filePath.Trim());
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    return Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                }

                return Path.GetFileNameWithoutExtension(filePath.Trim());
            }
            catch
            {
                return string.Empty;
            }
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
            box.ThousandsSeparator = false;
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

        private void WireControllerEditorEvents(Control root)
        {
            foreach (Control control in root.Controls)
            {
                NumericUpDown number = control as NumericUpDown;
                if (number != null)
                {
                    number.ValueChanged += (s, e) => SaveSelectedController();
                }
                CheckBox checkBox = control as CheckBox;
                if (checkBox != null)
                {
                    checkBox.CheckedChanged += (s, e) => SaveSelectedController();
                }
                TextBox textBox = control as TextBox;
                if (textBox != null)
                {
                    textBox.TextChanged += (s, e) => SaveSelectedController();
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

        private static float ReadFloatBox(NumericUpDown box)
        {
            string text = (box.Text ?? string.Empty).Trim();
            float parsed;
            if (text.IndexOf('.') >= 0
                && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
            {
                return parsed;
            }
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }
            return (float)box.Value;
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

        private sealed class ControllerLinkInfo
        {
            public ControllerLinkInfo(string kind, int index)
            {
                Kind = kind;
                Index = index;
            }

            public string Kind { get; private set; }
            public int Index { get; private set; }
        }
    }
}
