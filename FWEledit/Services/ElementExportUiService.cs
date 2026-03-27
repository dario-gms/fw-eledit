namespace FWEledit
{
    public sealed class ElementExportUiService
    {
        public void ExportSelected(
            ElementExportCommandService exportCommandService,
            eListCollection listCollection,
            int listIndex,
            System.Windows.Forms.DataGridView elementGrid,
            GridSelectionService gridSelectionService,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            ColorProgressBar.ColorProgressBar progressBar)
        {
            if (exportCommandService == null)
            {
                return;
            }

            exportCommandService.ExportSelected(
                listCollection,
                listIndex,
                elementGrid,
                gridSelectionService,
                importExportUiService,
                workflowService,
                progressBar);
        }
    }
}
