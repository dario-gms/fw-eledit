using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowBootstrapContext
    {
        public ISessionService SessionService { get; set; }
        public SearchSuggestionService SearchSuggestionService { get; set; }
        public MainWindowWorkflowSetupService WorkflowSetupService { get; set; }
        public MainWindowAssetSetupService AssetSetupService { get; set; }
        public MainWindowNavigationTimerService NavigationTimerService { get; set; }
        public ElementsImportExportService ElementsImportExportService { get; set; }
        public ElementsLoadService ElementsLoadService { get; set; }
        public ElementsFileInfoService ElementsFileInfoService { get; set; }
        public NavigationStateService NavigationStateService { get; set; }
        public IdGenerationService IdGenerationService { get; set; }
        public IconResolutionService IconResolutionService { get; set; }
        public AddonTypeHintService AddonTypeHintService { get; set; }
        public ModelPickerCacheService ModelPickerCacheService { get; set; }
        public ItemFieldClassifierService ItemFieldClassifierService { get; set; }
        public AddonTypeDisplayService AddonTypeDisplayService { get; set; }
        public AddonParamService AddonParamService { get; set; }
        public FieldValueValidationService FieldValueValidationService { get; set; }
        public Action FlushNavigationState { get; set; }
    }

    public sealed class MainWindowBootstrapResult
    {
        public MainWindowViewModel ViewModel { get; set; }
        public EditorWindowService EditorWindowService { get; set; }
        public MainWindowWorkflowSetupResult WorkflowSetup { get; set; }
        public MainWindowAssetSetupResult AssetSetup { get; set; }
        public Timer NavigationPersistTimer { get; set; }
    }

    public sealed class MainWindowBootstrapService
    {
        public MainWindowBootstrapResult Initialize(MainWindowBootstrapContext context)
        {
            if (context == null)
            {
                return new MainWindowBootstrapResult();
            }

            ISessionService sessionService = context.SessionService ?? new SessionService();
            SearchSuggestionService suggestionService = context.SearchSuggestionService ?? new SearchSuggestionService();

            MainWindowViewModel viewModel = new MainWindowViewModel(
                sessionService,
                new ItemDescriptionStore(),
                suggestionService);

            EditorWindowService editorWindowService = new EditorWindowService(sessionService);

            MainWindowWorkflowSetupResult workflowSetup = context.WorkflowSetupService != null
                ? context.WorkflowSetupService.Build(
                    context.ElementsImportExportService,
                    context.ElementsLoadService,
                    context.ElementsFileInfoService,
                    context.NavigationStateService,
                    context.IdGenerationService,
                    context.IconResolutionService)
                : new MainWindowWorkflowSetupResult();

            MainWindowAssetSetupResult assetSetup = context.AssetSetupService != null
                ? context.AssetSetupService.Build(
                    sessionService,
                    context.AddonTypeHintService,
                    context.ModelPickerCacheService,
                    context.IconResolutionService,
                    context.IdGenerationService,
                    context.ItemFieldClassifierService,
                    context.AddonTypeDisplayService,
                    context.AddonParamService,
                    context.FieldValueValidationService)
                : new MainWindowAssetSetupResult();

            viewModel.Session.AssetManager = assetSetup.AssetManager;
            viewModel.Session.Database = sessionService.Database;
            viewModel.MouseMoveCheck = new Point(0, 0);

            Timer navigationTimer = null;
            if (context.NavigationTimerService != null && context.FlushNavigationState != null)
            {
                navigationTimer = context.NavigationTimerService.CreateTimer(700, context.FlushNavigationState);
            }

            return new MainWindowBootstrapResult
            {
                ViewModel = viewModel,
                EditorWindowService = editorWindowService,
                WorkflowSetup = workflowSetup,
                AssetSetup = assetSetup,
                NavigationPersistTimer = navigationTimer
            };
        }
    }
}
