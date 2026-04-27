using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void BeginSaveProgress()
        {
            mainWindowSaveUiService.BeginSaveProgress(saveProgressUiService, saveProgressService, cpb2);
        }

        private void SetSaveProgress(int value)
        {
            mainWindowSaveUiService.SetSaveProgress(saveProgressUiService, saveProgressService, cpb2, value);
        }

        private void EndSaveProgress()
        {
            mainWindowSaveUiService.EndSaveProgress(saveProgressUiService, saveProgressService, cpb2);
        }

        private void ShowSaveConfirmation(string details)
        {
            mainWindowSaveUiService.ShowSaveConfirmation(saveConfirmationUiService, saveConfirmationService, details);
        }

        private NavigationSnapshot CaptureNavigationSnapshot()
        {
            return mainWindowNavigationUiService.CaptureSnapshot(
                navigationSnapshotUiService,
                navigationSnapshotService,
                comboBox_lists,
                dataGridView_elems);
        }

        private void RestoreNavigationSnapshot(NavigationSnapshot snapshot)
        {
            mainWindowNavigationUiService.RestoreSnapshot(
                navigationSnapshotUiService,
                navigationSnapshotService,
                snapshot,
                comboBox_lists,
                dataGridView_elems,
                () => isRestoringSessionState,
                value => isRestoringSessionState = value);
        }

        private bool ShouldIncludeFieldInValuesTab(int listIndex, string fieldName)
        {
            EquipmentValuesTab tab = equipmentTabService.GetSelectedTab(fwEquipmentTabs);
            return equipmentTabService.ShouldIncludeField(equipmentFieldService, eLC, listIndex, fieldName, tab);
        }

        private void EnsureEquipmentTabForField(int listIndex, string fieldName)
        {
            equipmentTabService.EnsureTabForField(fwEquipmentTabs, equipmentFieldService, eLC, listIndex, fieldName);
        }

        private void UpdateEquipmentTabsVisibility(int listIndex)
        {
            equipmentTabService.UpdateVisibility(fwEquipmentTabs, equipmentFieldService.IsEquipmentEssenceList(eLC, listIndex));
        }

        private void ClearDirtyTrackingAfterSave()
        {
            mainWindowDirtyTrackingService.ClearAfterSave(
                dirtyTrackingUiService,
                dirtyStateTracker,
                viewModel.DescriptionViewModel,
                listDisplayService,
                comboBox_lists.SelectedIndex,
                () => change_list(null, null),
                ref hasUnsavedChanges);
        }

        private void PersistNavigationState()
        {
            mainWindowNavigationUiService.PersistNavigationState(
                navigationPersistenceUiService,
                navigationPersistenceService,
                selectionStateService,
                viewModel,
                comboBox_lists,
                dataGridView_elems,
                navigationStateService,
                () => navigationPersistenceService.RestartTimer(navigationPersistTimer),
                value => hasPendingNavigationStateWrite = value,
                () => isRestoringSessionState);
        }

        private void FlushNavigationStateToDisk()
        {
            mainWindowNavigationUiService.FlushNavigationState(
                navigationPersistenceUiService,
                navigationPersistenceService,
                viewModel,
                navigationStateService,
                value => hasPendingNavigationStateWrite = value);
        }

        private bool SaveCurrentSessionNoDialog()
        {
            return mainWindowSaveUiService.SaveCurrentSessionNoDialog(
                eLC,
                saveSessionUiService,
                elementsSessionService,
                saveContextBuilderService,
                conversationList,
                ElementsPath,
                asm,
                saveProgressUiService,
                saveProgressService,
                cpb2,
                ValidateUniqueIdsBeforeSave,
                FlushPendingDescriptionsToDisk,
                ClearDirtyTrackingAfterSave,
                message => MessageBox.Show(message),
                message => MessageBox.Show(message),
                LogError,
                CaptureNavigationSnapshot,
                RestoreNavigationSnapshot,
                () => savePathService.PromptElementsSavePath(Environment.CurrentDirectory, this),
                path =>
                {
                    ElementsPath = path ?? ElementsPath;
                    viewModel.ElementsPath = ElementsPath;
                },
                summary =>
                {
                    if (fwDescriptionStatusLabel != null)
                    {
                        fwDescriptionStatusLabel.Text = summary;
                    }
                });
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            closePromptUiService.HandleClosing(
                suppressClosePrompt,
                hasUnsavedChanges,
                viewModel.DescriptionViewModel,
                closePromptService,
                SaveCurrentSessionNoDialog,
                PersistNavigationState,
                FlushNavigationStateToDisk,
                e);
        }

        private void OpenModelPickerForValueRow(int rowIndex)
        {
            mainWindowValueRowPickerUiService.OpenModelPickerForValueRow(
                valueRowPickerUiService,
                eLC,
                database,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemFieldClassifierService,
                modelPickerService,
                modelPickerCacheService,
                modelPackageNotificationService,
                dialogService,
                pathIdResolutionService,
                this);
        }

        private void OpenAddonTypePickerForValueRow(int rowIndex)
        {
            mainWindowValueRowPickerUiService.OpenAddonTypePickerForValueRow(
                valueRowPickerUiService,
                eLC,
                dataGridView_item,
                dataGridView_elems,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemFieldClassifierService,
                addonTypeOptionService,
                Application.StartupPath,
                AssetManager.GameRootPath,
                valueRowIndex => valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, valueRowIndex),
                message => MessageBox.Show(message, "Added Attribute", MessageBoxButtons.OK, MessageBoxIcon.Information),
                this);
        }

        private void OpenItemQualityPickerForValueRow(int rowIndex)
        {
            mainWindowValueRowPickerUiService.OpenItemQualityPickerForValueRow(
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                ItemQualityOptions,
                this);
        }

        private void OpenIconPickerForValueRow(int rowIndex)
        {
            mainWindowValueRowPickerUiService.OpenIconPickerForValueRow(
                valueRowPickerUiService,
                eLC,
                database,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemFieldClassifierService,
                pathIdResolutionService,
                modelPickerService,
                valueRowIndexService,
                iconUsageLookupService,
                this);
        }
        private void OpenModelPreviewForValueRow(int rowIndex)
        {
            mainWindowValueRowPickerUiService.OpenModelPreviewForValueRow(
                valueRowPickerUiService,
                asm,
                database,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemFieldClassifierService,
                pathIdResolutionService,
                modelPickerService,
                modelPreviewService,
                this,
                message => MessageBox.Show(message));
        }

        private void UpdatePickIconButtonState()
        {
            mainWindowValueRowPickerUiService.UpdatePickIconButtonState(
                valueRowPickerUiService,
                inlineIconButtonService,
                fwInlinePickIconButton,
                dataGridView_item,
                fwRightTabs,
                fwValuesTab,
                itemFieldClassifierService,
                ref fwInlinePickIconRowIndex,
                suppressValuesUiRefresh);
        }

        private void click_pick_icon(object sender, EventArgs e)
        {
            mainWindowValueRowPickerUiService.HandleInlinePickIconClick(
                valueRowPickerUiService,
                dataGridView_item,
                fwInlinePickIconRowIndex,
                OpenIconPickerForValueRow);
        }

        private void dataGridView_item_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            mainWindowValueRowPickerUiService.HandleCellDoubleClick(
                valueRowPickerUiService,
                dataGridView_item,
                eLC,
                comboBox_lists.SelectedIndex,
                e,
                itemFieldClassifierService,
                OpenIconPickerForValueRow,
                OpenAddonTypePickerForValueRow,
                OpenItemQualityPickerForValueRow,
                OpenModelPickerForValueRow,
                UpdatePickIconButtonState,
                message => MessageBox.Show(message));
        }

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainWindowViewModel(new ItemDescriptionStore(), searchSuggestionService);
            MainWindowWorkflowSetupResult workflowSetup = mainWindowWorkflowSetupService.Build(
                elementsImportExportService,
                elementsLoadService,
                elementsFileInfoService,
                navigationStateService,
                idGenerationService,
                iconResolutionService);
            elementImportExportWorkflowService = workflowSetup.ElementImportExportWorkflowService;
            elementsRulesExportWorkflowService = workflowSetup.ElementsRulesExportWorkflowService;
            elementListMutationService = workflowSetup.ElementListMutationService;
            elementsLoadWorkflowService = workflowSetup.ElementsLoadWorkflowService;
            listRowBuilderService = workflowSetup.ListRowBuilderService;

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

            Assembly assembly = Assembly.GetExecutingAssembly();
            mainWindowVersionUiService.InitializeVersion(
                assembly,
                label_Version,
                navigationStateService,
                "0.9.3.1");

            MainWindowAssetSetupResult assetSetup = mainWindowAssetSetupService.Build(
                addonTypeHintService,
                modelPickerCacheService,
                iconResolutionService,
                idGenerationService,
                itemFieldClassifierService,
                addonTypeDisplayService,
                addonParamService,
                fieldValueValidationService);
            asm = assetSetup.AssetManager;
            addonTypeOptionService = assetSetup.AddonTypeOptionService;
            modelPickerService = assetSetup.ModelPickerService;
            itemValueRowBuilderService = assetSetup.ItemValueRowBuilderService;
            itemSelectionWorkflowService = assetSetup.ItemSelectionWorkflowService;
            valueCompatibilityService = assetSetup.ValueCompatibilityService;
            valueChangeService = assetSetup.ValueChangeService;
            cpb2.Value = 0;
            colorTheme();

            mouseMoveCheck = new Point(0, 0);
            navigationPersistTimer = mainWindowNavigationTimerService.CreateTimer(700, FlushNavigationStateToDisk);
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
                () => EnableSelectionItem,
                searchSuggestionList_MouseClick,
                searchSuggestionList_KeyDown,
                click_pick_icon,
                fw_description_changed,
                click_save_description);

            fwMainSplit = layout.MainSplit;
            fwRightTabs = layout.RightTabs;
            fwValuesTab = layout.ValuesTab;
            fwEquipmentTabs = layout.EquipmentTabs;
            fwEquipmentTabMain = layout.EquipmentTabMain;
            fwEquipmentTabRefine = layout.EquipmentTabRefine;
            fwEquipmentTabModels = layout.EquipmentTabModels;
            fwEquipmentTabOther = layout.EquipmentTabOther;
            fwDescriptionTab = layout.DescriptionTab;
            fwDescriptionEditor = layout.DescriptionEditor;
            fwDescriptionPreview = layout.DescriptionPreview;
            fwDescriptionSaveButton = layout.DescriptionSaveButton;
            fwDescriptionStatusLabel = layout.DescriptionStatusLabel;
            fwInlinePickIconButton = layout.InlinePickIconButton;
            searchSuggestionList = layout.SearchSuggestionList;
            fwLayoutInitialized = true;
        }

        private void EnsureMainSplitSizing()
        {
            mainWindowLayoutUiService.EnsureMainSplitSizing(fwMainSplit, splitContainerSizingService);
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            mainWindowLayoutUiService.HandleShown(
                fwMainSplit,
                splitContainerSizingService,
                startupSessionService,
                navigationStateService,
                LoadGameFolder,
                ref startupSessionRestoreDone);
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            mainWindowLayoutUiService.HandleResize(fwMainSplit, splitContainerSizingService);
        }

        private void colorTheme()
        {
            mainWindowThemeUiService.ApplyTheme(
                database,
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
                button_search,
                button_SetValue,
                fwInlinePickIconButton,
                fwDescriptionSaveButton,
                fwDescriptionEditor,
                fwDescriptionPreview,
                fwDescriptionStatusLabel,
                () => new ThemeMenuRenderer(() => database != null ? database.arrTheme : null),
                itemListThemeService);
        }

        private void comboBoxDb_DrawItem(object sender, DrawItemEventArgs e)
        {
            mainWindowThemeUiService.DrawComboBoxItem(
                database,
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
                    viewModel.DescriptionViewModel.ResetPendingChanges();
                    hasUnsavedChanges = false;
                },
                IconPickerWindow.CancelBackgroundWarmup,
                () =>
                {
                    IconPickerWindow.BeginBackgroundWarmup(database);
                },
                colorTheme,
                LoadItemDescriptionsFromConfigs,
                PersistNavigationState,
                result =>
                {
                    eLC = result.ListCollection;
                    conversationList = result.ConversationList;
                    xrefs = result.Xrefs;
                    ElementsPath = result.ElementsPath ?? string.Empty;
                    viewModel.ElementsPath = ElementsPath;
                },
                listDisplayService,
                listComboPopulationService,
                exportRulesMenuService,
                xrefMenuService,
                navigationSelectionService,
                navigationStateService,
                windowTitleService,
                iconListAvailabilityService,
                asm,
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

        private void LogError(string context, Exception ex)
        {
            errorLoggingService.Log(context, ex);
        }

        private bool ValidateUniqueIdsBeforeSave()
        {
            return mainWindowSaveUiService.ValidateUniqueIdsBeforeSave(
                uniqueIdValidationService,
                eLC,
                listIndex => idGenerationService.GetIdFieldIndex(eLC, listIndex),
                elementsValidationService,
                message => MessageBox.Show(message));
        }

        private void RenderDescriptionPreview(string rawText)
        {
            mainWindowDescriptionUiService.RenderDescriptionPreview(
                descriptionPreviewUiService,
                descriptionPreviewService,
                fwDescriptionPreview,
                rawText);
        }

        private void click_save_description(object sender, EventArgs e)
        {
            mainWindowDescriptionUiService.TrySaveCurrentDescription(
                descriptionUiService,
                viewModel,
                () => StageCurrentDescriptionChange(true),
                message => MessageBox.Show(message));
        }
    }
}
