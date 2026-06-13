using System;
using System.Reflection;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        public MainWindow()
            : this(new SessionService())
        {
        }

        public MainWindow(ISessionService sessionService)
        {
            this.sessionService = sessionService ?? new SessionService();

            MainWindowBootstrapResult bootstrap = mainWindowBootstrapService.Initialize(
                new MainWindowBootstrapContext
                {
                    SessionService = this.sessionService,
                    SearchSuggestionService = searchSuggestionService,
                    WorkflowSetupService = mainWindowWorkflowSetupService,
                    AssetSetupService = mainWindowAssetSetupService,
                    NavigationTimerService = mainWindowNavigationTimerService,
                    ElementsImportExportService = elementsImportExportService,
                    ElementsLoadService = elementsLoadService,
                    ElementsFileInfoService = elementsFileInfoService,
                    NavigationStateService = navigationStateService,
                    IdGenerationService = idGenerationService,
                    IconResolutionService = iconResolutionService,
                    AddonTypeHintService = addonTypeHintService,
                    ModelPickerCacheService = modelPickerCacheService,
                    ItemFieldClassifierService = itemFieldClassifierService,
                    AddonTypeDisplayService = addonTypeDisplayService,
                    AddonParamService = addonParamService,
                    FieldValueValidationService = fieldValueValidationService,
                    FlushNavigationState = FlushNavigationStateToDisk
                });

            viewModel = bootstrap.ViewModel ?? new MainWindowViewModel(
                this.sessionService,
                new ItemDescriptionStore(),
                searchSuggestionService);
            editorWindowService = bootstrap.EditorWindowService ?? new EditorWindowService(this.sessionService);

            MainWindowWorkflowSetupResult workflowSetup = bootstrap.WorkflowSetup ?? new MainWindowWorkflowSetupResult();
            elementImportExportWorkflowService = workflowSetup.ElementImportExportWorkflowService;
            elementsRulesExportWorkflowService = workflowSetup.ElementsRulesExportWorkflowService;
            elementListMutationService = workflowSetup.ElementListMutationService;
            elementsLoadWorkflowService = workflowSetup.ElementsLoadWorkflowService;
            listRowBuilderService = workflowSetup.ListRowBuilderService;

            MainWindowAssetSetupResult assetSetup = bootstrap.AssetSetup ?? new MainWindowAssetSetupResult();
            addonTypeOptionService = assetSetup.AddonTypeOptionService ?? new AddonTypeOptionService(addonTypeHintService);
            itemReferenceService = assetSetup.ItemReferenceService ?? new ItemReferenceService();
            modelPickerService = assetSetup.ModelPickerService;
            itemValueRowBuilderService = assetSetup.ItemValueRowBuilderService;
            itemSelectionWorkflowService = assetSetup.ItemSelectionWorkflowService;
            valueCompatibilityService = assetSetup.ValueCompatibilityService;
            valueChangeService = assetSetup.ValueChangeService;
            if (listRowBuilderService != null)
            {
                listRowBuilderService.ReferenceCountResolver = GetReferenceCountForElement;
            }
            navigationPersistTimer = bootstrap.NavigationPersistTimer
                ?? mainWindowNavigationTimerService.CreateTimer(700, FlushNavigationStateToDisk);

            InitializeComponent();

            mainWindowGridSetupService.ApplyNotSortable(dataGridView_elems);
            InitializeFwEditorLayout();
            gridDoubleBufferService.EnableDoubleBuffer(dataGridView_elems);
            gridDoubleBufferService.EnableDoubleBuffer(dataGridView_item);
            mainWindowEventWiringService.WireBasicEvents(
                this,
                MainWindow_Shown,
                MainWindow_Resize,
                MainWindow_FormClosing,
                textBox_search,
                textBox_search_TextChanged,
                textBox_search_KeyDown);
            dataGridView_item.CellMouseDown += dataGridView_item_CellMouseDown;
            dataGridView_item.CurrentCellChanged += dataGridView_item_CurrentCellChanged;
            dataGridView_item.CellPainting += dataGridView_item_CellPainting;
            dataGridView_elems.CellMouseDown += dataGridView_elems_CellMouseDown;

            Assembly assembly = Assembly.GetExecutingAssembly();
            mainWindowVersionUiService.InitializeVersion(
                assembly,
                label_Version,
                navigationStateService,
                "0.9.5.10");

            fwDarkMode = Properties.Settings.Default.UseDarkMode;
            cpb2.Value = 0;
            colorTheme();
        }
        private void InitializeFwEditorLayout()
        {
            if (fwLayoutInitialized)
            {
                return;
            }

            MainWindowLayoutResult layout = mainWindowLayoutBuilderService.BuildLayout(
                this,
                menuStrip_mainMenu,
                label_Version,
                comboBox_lists,
                label1,
                textBox_offset,
                dataGridView_elems,
                checkBox_SearchAll,
                checkBox_SearchExactMatching,
                checkBox_SearchMatchCase,
                textBox_search,
                button_search,
                dataGridView_item,
                textBox_SetValue,
                button_SetValue,
                cpb2,
                listBox_items,
                progressBar_progress,
                itemListThemeService,
                UpdatePickIconButtonState,
                () => change_item(null, null),
                () => viewModel.EnableSelectionItem,
                searchSuggestionList_MouseClick,
                searchSuggestionList_KeyDown,
                click_pick_icon,
                fw_description_changed,
                click_save_description,
                click_navigation_back,
                click_navigation_forward,
                click_toggle_theme);

            fwMainSplit = layout.MainSplit;
            fwRightTabs = layout.RightTabs;
            fwValuesTab = layout.ValuesTab;
            fwEquipmentTabs = layout.EquipmentTabs;
            fwEquipmentTabMain = layout.EquipmentTabMain;
            fwEquipmentTabModels = layout.EquipmentTabModels;
            fwEquipmentTabRefine = layout.EquipmentTabRefine;
            fwEquipmentTabDecompose = layout.EquipmentTabDecompose;
            fwEquipmentTabOther = layout.EquipmentTabOther;
            fwReferencesTab = layout.ReferencesTab;
            fwReferencesGrid = layout.ReferencesGrid;
            fwDescriptionTab = layout.DescriptionTab;
            fwDescriptionEditor = layout.DescriptionEditor;
            fwDescriptionPreview = layout.DescriptionPreview;
            fwDescriptionSaveButton = layout.DescriptionSaveButton;
            fwDescriptionStatusLabel = layout.DescriptionStatusLabel;
            fwDescriptionColorButton = layout.DescriptionColorButton;
            fwDescriptionLineBreakButton = layout.DescriptionLineBreakButton;
            fwDescriptionNormalFontButton = layout.DescriptionNormalFontButton;
            fwDescriptionSmallFontButton = layout.DescriptionSmallFontButton;
            fwDescriptionTitleFontButton = layout.DescriptionTitleFontButton;
            fwInlinePickIconButton = layout.InlinePickIconButton;
            fwRawValueUpButton = layout.RawValueUpButton;
            fwRawValueDownButton = layout.RawValueDownButton;
            fwBackButton = layout.BackButton;
            fwForwardButton = layout.ForwardButton;
            fwThemeToggleButton = layout.ThemeToggleButton;
            fwNpcSellEditorHostPanel = layout.NpcSellEditorHostPanel;
            searchSuggestionList = layout.SearchSuggestionList;
            if (searchSuggestionList != null)
            {
                searchSuggestionList.DrawItem -= searchSuggestionList_DrawItem;
                searchSuggestionList.DrawItem += searchSuggestionList_DrawItem;
            }
            if (referenceCountRefreshTimer == null)
            {
                referenceCountRefreshTimer = new System.Windows.Forms.Timer();
                referenceCountRefreshTimer.Interval = 140;
                referenceCountRefreshTimer.Tick += (s, e) =>
                {
                    referenceCountRefreshTimer.Stop();
                    RefreshVisibleReferenceCounts();
                };
            }
            if (dataGridView_elems != null)
            {
                dataGridView_elems.Scroll += (s, e) => ScheduleVisibleReferenceCountRefresh();
                dataGridView_elems.SizeChanged += (s, e) => ScheduleVisibleReferenceCountRefresh();
            }
            if (fwRightTabs != null)
            {
                fwRightTabs.SelectedIndexChanged += (s, e) =>
                {
                    if (IsReferencesTabActive())
                    {
                        UpdateReferencesTabForSelection();
                    }
                };
            }
            RemoveReferencesTabFromMainView();
            InitializeElementContextActions();
            InitializeDescriptionFormattingActions();
            InitializeRawValueEditor();
            InitializeNpcSellServicePageUi();
            UpdateThemeToggleButton();
            fwLayoutInitialized = true;
        }

        private void InitializeRawValueEditor()
        {
            if (textBox_SetValue != null)
            {
                textBox_SetValue.TextChanged += raw_value_editor_changed;
                textBox_SetValue.KeyDown += raw_value_editor_key_down;
            }
            if (fwRawValueUpButton != null)
            {
                fwRawValueUpButton.Click += (s, e) => AdjustRawValueEditor(1);
            }
            if (fwRawValueDownButton != null)
            {
                fwRawValueDownButton.Click += (s, e) => AdjustRawValueEditor(-1);
            }
            UpdateRawValueEditorFromCurrentCell();
        }

        private void EnsureMainSplitSizing()
        {
            mainWindowLayoutUiService.EnsureMainSplitSizing(fwMainSplit, splitContainerSizingService);
        }

        private void InitializeElementContextActions()
        {
            if (contextMenuStrip_items == null)
            {
                return;
            }

            if (!contextMenuStrip_items.Items.ContainsKey("searchElementToolStripMenuItem"))
            {
                ToolStripMenuItem searchItem = new ToolStripMenuItem();
                searchItem.Name = "searchElementToolStripMenuItem";
                searchItem.Text = "Search";
                searchItem.Click += click_context_search;
                contextMenuStrip_items.Items.Insert(0, searchItem);
            }

            if (!contextMenuStrip_items.Items.ContainsKey("previewElementToolStripMenuItem"))
            {
                ToolStripMenuItem previewItem = new ToolStripMenuItem();
                previewItem.Name = "previewElementToolStripMenuItem";
                previewItem.Text = "Preview";
                previewItem.Click += click_context_preview;
                int insertIndex = contextMenuStrip_items.Items.ContainsKey("searchElementToolStripMenuItem") ? 1 : 0;
                contextMenuStrip_items.Items.Insert(insertIndex, previewItem);
            }

            if (!contextMenuStrip_items.Items.ContainsKey("showReferencesToolStripMenuItem"))
            {
                ToolStripMenuItem referencesItem = new ToolStripMenuItem();
                referencesItem.Name = "showReferencesToolStripMenuItem";
                referencesItem.Text = "Show references";
                referencesItem.Click += click_context_show_references;
                int insertIndex = contextMenuStrip_items.Items.ContainsKey("previewElementToolStripMenuItem") ? 2 : 1;
                contextMenuStrip_items.Items.Insert(insertIndex, referencesItem);
            }

            if (!contextMenuStrip_items.Items.ContainsKey("elementContextSeparator"))
            {
                ToolStripSeparator separator = new ToolStripSeparator();
                separator.Name = "elementContextSeparator";
                int insertIndex = contextMenuStrip_items.Items.ContainsKey("showReferencesToolStripMenuItem") ? 3 : 0;
                contextMenuStrip_items.Items.Insert(insertIndex, separator);
            }
        }

        private void RemoveReferencesTabFromMainView()
        {
            if (fwRightTabs == null || fwReferencesTab == null)
            {
                return;
            }

            if (fwRightTabs.TabPages.Contains(fwReferencesTab))
            {
                fwRightTabs.TabPages.Remove(fwReferencesTab);
            }
        }

        private void dataGridView_elems_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e == null || e.Button != MouseButtons.Right || dataGridView_elems == null || e.RowIndex < 0)
            {
                return;
            }
            if (e.RowIndex >= dataGridView_elems.Rows.Count)
            {
                return;
            }

            int columnIndex = e.ColumnIndex >= 0 ? e.ColumnIndex : 0;
            if (columnIndex >= dataGridView_elems.Columns.Count)
            {
                columnIndex = 0;
            }

            int[] selectedRows = gridSelectionService != null
                ? gridSelectionService.GetSelectedIndices(dataGridView_elems)
                : new int[0];
            bool hasMultiSelection = selectedRows.Length > 1;
            bool clickedRowWasAlreadySelected = dataGridView_elems.Rows[e.RowIndex].Selected;

            if (hasMultiSelection)
            {
                return;
            }

            if (!clickedRowWasAlreadySelected)
            {
                dataGridView_elems.ClearSelection();
                dataGridView_elems.Rows[e.RowIndex].Selected = true;
            }

            dataGridView_elems.CurrentCell = dataGridView_elems.Rows[e.RowIndex].Cells[columnIndex];
        }

        private void click_context_search(object sender, EventArgs e)
        {
            if (textBox_search == null)
            {
                return;
            }

            textBox_search.Focus();
            textBox_search.SelectAll();
        }

        private void click_context_preview(object sender, EventArgs e)
        {
            int modelRow = FindFirstModelFieldRow();
            if (modelRow < 0)
            {
                OpenModelPreviewForCurrentItem();
                return;
            }

            OpenModelPreviewForValueRow(modelRow);
        }

        private void click_context_show_references(object sender, EventArgs e)
        {
            ShowReferencesViewerForCurrentSelection();
        }

        private void click_navigation_back(object sender, EventArgs e)
        {
            NavigateSelectionHistory(-1);
        }

        private void click_navigation_forward(object sender, EventArgs e)
        {
            NavigateSelectionHistory(1);
        }

        private void RecordSelectionHistory()
        {
            if (suppressSelectionHistory || comboBox_lists == null || dataGridView_elems == null || dataGridView_elems.CurrentCell == null)
            {
                return;
            }

            NavigationSnapshot snapshot = navigationSnapshotService.CaptureSnapshot(comboBox_lists.SelectedIndex, dataGridView_elems);
            if (snapshot == null || snapshot.ListIndex < 0 || snapshot.GridRowIndex < 0)
            {
                return;
            }

            if (selectionHistoryIndex >= 0 && selectionHistoryIndex < selectionHistory.Count)
            {
                NavigationSnapshot current = selectionHistory[selectionHistoryIndex];
                if (current.ListIndex == snapshot.ListIndex && current.ItemId == snapshot.ItemId && current.GridRowIndex == snapshot.GridRowIndex)
                {
                    UpdateSelectionHistoryButtons();
                    return;
                }
            }

            while (selectionHistory.Count > selectionHistoryIndex + 1)
            {
                selectionHistory.RemoveAt(selectionHistory.Count - 1);
            }

            selectionHistory.Add(snapshot);
            selectionHistoryIndex = selectionHistory.Count - 1;
            UpdateSelectionHistoryButtons();
        }

        private void NavigateSelectionHistory(int direction)
        {
            int targetIndex = selectionHistoryIndex + direction;
            if (targetIndex < 0 || targetIndex >= selectionHistory.Count)
            {
                return;
            }

            suppressSelectionHistory = true;
            try
            {
                RestoreSelectionSnapshot(selectionHistory[targetIndex]);
                selectionHistoryIndex = targetIndex;
            }
            finally
            {
                suppressSelectionHistory = false;
                UpdateSelectionHistoryButtons();
            }
        }

        private void RestoreSelectionSnapshot(NavigationSnapshot snapshot)
        {
            if (snapshot == null || comboBox_lists == null || dataGridView_elems == null)
            {
                return;
            }
            if (snapshot.ListIndex < 0 || snapshot.ListIndex >= comboBox_lists.Items.Count)
            {
                return;
            }

            viewModel.EnableSelectionItem = false;
            try
            {
                if (comboBox_lists.SelectedIndex != snapshot.ListIndex)
                {
                    comboBox_lists.SelectedIndex = snapshot.ListIndex;
                }
            }
            finally
            {
                viewModel.EnableSelectionItem = true;
            }

            int targetRow = FindElementRow(snapshot);
            if (targetRow < 0)
            {
                return;
            }

            dataGridView_elems.ClearSelection();
            dataGridView_elems.CurrentCell = dataGridView_elems.Rows[targetRow].Cells[0];
            dataGridView_elems.Rows[targetRow].Selected = true;
            try
            {
                dataGridView_elems.FirstDisplayedScrollingRowIndex =
                    snapshot.FirstDisplayedRow >= 0 && snapshot.FirstDisplayedRow < dataGridView_elems.Rows.Count
                        ? snapshot.FirstDisplayedRow
                        : targetRow;
            }
            catch
            {
            }

            change_item(null, null);
        }

        private int FindElementRow(NavigationSnapshot snapshot)
        {
            if (snapshot == null || dataGridView_elems == null)
            {
                return -1;
            }

            if (snapshot.ItemId > 0)
            {
                for (int row = 0; row < dataGridView_elems.Rows.Count; row++)
                {
                    int rowId;
                    if (int.TryParse(Convert.ToString(dataGridView_elems.Rows[row].Cells[0].Value), out rowId) && rowId == snapshot.ItemId)
                    {
                        return row;
                    }
                }
            }

            return snapshot.GridRowIndex >= 0 && snapshot.GridRowIndex < dataGridView_elems.Rows.Count
                ? snapshot.GridRowIndex
                : -1;
        }

        private void UpdateSelectionHistoryButtons()
        {
            if (fwBackButton != null)
            {
                fwBackButton.Enabled = selectionHistoryIndex > 0;
            }
            if (fwForwardButton != null)
            {
                fwForwardButton.Enabled = selectionHistoryIndex >= 0 && selectionHistoryIndex < selectionHistory.Count - 1;
            }
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            mainWindowLayoutUiService.HandleShown(
                fwMainSplit,
                splitContainerSizingService,
                startupSessionService,
                navigationStateService,
                LoadGameFolder,
                ref viewModel.StartupSessionRestoreDone);
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            mainWindowLayoutUiService.HandleResize(fwMainSplit, splitContainerSizingService);
        }

        private void colorTheme()
        {
            mainWindowThemeUiService.ApplyTheme(
                sessionService.Database,
                themeUiService,
                this,
                comboBox_lists,
                menuStrip_mainMenu,
                contextMenuStrip_items,
                cpb2,
                label1,
                checkBox_SearchAll,
                checkBox_SearchExactMatching,
                checkBox_SearchMatchCase,
                textBox_offset,
                textBox_search,
                textBox_SetValue,
                searchSuggestionList,
                dataGridView_elems,
                dataGridView_item,
                fwReferencesGrid,
                button_search,
                button_SetValue,
                fwInlinePickIconButton,
                fwThemeToggleButton,
                fwDescriptionSaveButton,
                fwDescriptionEditor,
                fwDescriptionPreview,
                fwDescriptionStatusLabel,
                () => new ThemeMenuRenderer(() => sessionService.Database != null ? sessionService.Database.arrTheme : null),
                itemListThemeService,
                fwDarkMode);
            UpdateThemeToggleButton();
            ApplyReferencesViewerTheme();
            if (fwReferencesGrid != null)
            {
                fwReferencesGrid.Invalidate();
            }
            if (fwNpcSellPageTabs != null)
            {
                fwNpcSellPageTabs.Invalidate();
            }
            if (fwNpcSellPriceEditorButton != null)
            {
                fwNpcSellPriceEditorButton.Invalidate();
            }
        }

        private void click_toggle_theme(object sender, EventArgs e)
        {
            fwDarkMode = !fwDarkMode;
            Properties.Settings.Default.UseDarkMode = fwDarkMode;
            Properties.Settings.Default.Save();
            colorTheme();
            dataGridView_item.Invalidate();
            dataGridView_elems.Invalidate();
            if (fwReferencesGrid != null)
            {
                fwReferencesGrid.Invalidate();
            }
            ApplyReferencesViewerTheme();
        }

        private void UpdateThemeToggleButton()
        {
            if (fwThemeToggleButton != null)
            {
                fwThemeToggleButton.Text = fwDarkMode ? "Light mode" : "Dark mode";
            }
        }

        private void comboBoxDb_DrawItem(object sender, DrawItemEventArgs e)
        {
            mainWindowThemeUiService.DrawComboBoxItem(
                sessionService.Database,
                themeComboBoxDrawService,
                comboBoxThemeRendererService,
                sender,
                e);
        }

        private void click_load(object sender, EventArgs e)
		{
            mainWindowElementsLoadUiService.LoadFromDialog(
                gameFolderLoadUiService,
                gameFolderDialogService,
                navigationStateService,
                LoadGameFolder,
                message => MessageBox.Show(message));
		}

        private void click_load_last_folder(object sender, EventArgs e)
        {
            mainWindowElementsLoadUiService.LoadLastFolder(
                gameFolderLoadUiService,
                gameFolderDialogService,
                navigationStateService,
                LoadGameFolder,
                message => MessageBox.Show(message));
        }

        private void LoadGameFolder(string gameFolderPath)
        {
            mainWindowElementsLoadUiService.LoadGameFolder(
                gameFolderPath,
                elementsLoadUiService,
                elementsLoadWorkflowService,
                cpb2,
                () =>
                {
                    dirtyStateTracker.Clear();
                    ResetReferenceCaches();
                    listDisplayService.ClearListDisplayCache();
                    selectionHistory.Clear();
                    selectionHistoryIndex = -1;
                    UpdateSelectionHistoryButtons();
                    viewModel.DescriptionViewModel.ResetPendingChanges();
                    viewModel.HasUnsavedChanges = false;
                },
                IconPickerWindow.CancelBackgroundWarmup,
                () =>
                {
                    IconPickerWindow.BeginBackgroundWarmup(sessionService.Database);
                },
                colorTheme,
                LoadItemDescriptionsFromConfigs,
                PersistNavigationState,
                result =>
                {
                    sessionService.ListCollection = result.ListCollection;
                    sessionService.ConversationList = result.ConversationList;
                    sessionService.Xrefs = result.Xrefs;
                    viewModel.ElementsPath = result.ElementsPath ?? string.Empty;
                    ConfigureReferenceCachePersistence(viewModel.ElementsPath);
                    StartReferenceCacheWarmup();
                },
                listDisplayService,
                listComboPopulationService,
                exportRulesMenuService,
                xrefMenuService,
                navigationSelectionService,
                navigationStateService,
                windowTitleService,
                iconListAvailabilityService,
                sessionService.AssetManager,
                viewModel,
                comboBox_lists,
                dataGridView_item,
                dataGridView_elems,
                exportContainerToolStripMenuItem,
                toolStripSeparator6,
                xrefItemToolStripMenuItem,
                click_export,
                this,
                cursor => Cursor = cursor,
                message => MessageBox.Show(message));
        }

    }
}





