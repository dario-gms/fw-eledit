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
            modelPickerService = assetSetup.ModelPickerService;
            itemValueRowBuilderService = assetSetup.ItemValueRowBuilderService;
            itemSelectionWorkflowService = assetSetup.ItemSelectionWorkflowService;
            valueCompatibilityService = assetSetup.ValueCompatibilityService;
            valueChangeService = assetSetup.ValueChangeService;
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

            Assembly assembly = Assembly.GetExecutingAssembly();
            mainWindowVersionUiService.InitializeVersion(
                assembly,
                label_Version,
                navigationStateService,
                "0.4.9");

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
                button_search,
                button_SetValue,
                fwInlinePickIconButton,
                fwDescriptionSaveButton,
                fwDescriptionEditor,
                fwDescriptionPreview,
                fwDescriptionStatusLabel,
                () => new ThemeMenuRenderer(() => sessionService.Database != null ? sessionService.Database.arrTheme : null),
                itemListThemeService);
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


