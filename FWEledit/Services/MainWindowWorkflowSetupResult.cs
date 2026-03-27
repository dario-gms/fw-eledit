namespace FWEledit
{
    public sealed class MainWindowWorkflowSetupResult
    {
        public ElementImportExportWorkflowService ElementImportExportWorkflowService { get; set; }
        public ElementsRulesExportWorkflowService ElementsRulesExportWorkflowService { get; set; }
        public ElementListMutationService ElementListMutationService { get; set; }
        public ElementsLoadWorkflowService ElementsLoadWorkflowService { get; set; }
        public ListRowBuilderService ListRowBuilderService { get; set; }
    }
}
