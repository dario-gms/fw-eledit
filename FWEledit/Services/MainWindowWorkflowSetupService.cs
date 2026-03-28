namespace FWEledit
{
    public sealed class MainWindowWorkflowSetupService
    {
        public MainWindowWorkflowSetupResult Build(
            ElementsImportExportService elementsImportExportService,
            ElementsLoadService elementsLoadService,
            ElementsFileInfoService elementsFileInfoService,
            NavigationStateService navigationStateService,
            IdGenerationService idGenerationService,
            IconResolutionService iconResolutionService)
        {
            return new MainWindowWorkflowSetupResult
            {
                ElementImportExportWorkflowService = new ElementImportExportWorkflowService(elementsImportExportService),
                ElementsRulesExportWorkflowService = new ElementsRulesExportWorkflowService(elementsImportExportService),
                ElementListMutationService = new ElementListMutationService(idGenerationService),
                ElementsLoadWorkflowService = new ElementsLoadWorkflowService(
                    elementsLoadService,
                    navigationStateService,
                    elementsFileInfoService),
                ListRowBuilderService = new ListRowBuilderService(iconResolutionService)
            };
        }
    }
}
