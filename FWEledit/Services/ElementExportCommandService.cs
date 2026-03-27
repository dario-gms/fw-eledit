using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementExportCommandService
    {
        public void ExportSelected(
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService selectionService,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            ColorProgressBar.ColorProgressBar progressBar)
        {
            if (listCollection == null || elementGrid == null || selectionService == null || importExportUiService == null || workflowService == null)
            {
                return;
            }

            if (elementGrid.RowCount <= 0)
            {
                return;
            }

            if (!importExportUiService.ValidateNotConversationList(listCollection, listIndex))
            {
                return;
            }

            int[] selIndices = selectionService.GetSelectedIndices(elementGrid);
            if (selIndices.Length == 0)
            {
                return;
            }

            importExportUiService.ExportSelectedItems(workflowService, listCollection, listIndex, selIndices, progressBar);
        }
    }
}
