using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementDeleteCommandService
    {
        public void DeleteSelected(
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService selectionService,
            ElementListMutationService mutationService,
            ElementDeletionUiService deletionUiService,
            ComboBox listComboBox,
            ref bool enableSelectionList,
            ref bool enableSelectionItem,
            ref bool hasUnsavedChanges,
            System.Action refreshItemAction)
        {
            if (listCollection == null || elementGrid == null || selectionService == null || mutationService == null || deletionUiService == null)
            {
                return;
            }

            if (elementGrid.RowCount <= 0)
            {
                return;
            }

            int[] selIndices = selectionService.GetSelectedIndices(elementGrid);
            if (selIndices.Length == 0)
            {
                return;
            }

            ElementDeleteResult result = mutationService.DeleteItems(listCollection, listIndex, selIndices);
            deletionUiService.ApplyDeletionResult(
                result,
                listCollection,
                listIndex,
                elementGrid,
                listComboBox,
                ref enableSelectionList,
                ref enableSelectionItem,
                ref hasUnsavedChanges,
                refreshItemAction);
        }

        public void DeleteSelected(
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService selectionService,
            ElementListMutationService mutationService,
            ElementDeletionUiService deletionUiService,
            ComboBox listComboBox,
            MainWindowViewModel viewModel,
            System.Action refreshItemAction)
        {
            if (listCollection == null || elementGrid == null || selectionService == null || mutationService == null || deletionUiService == null)
            {
                return;
            }

            if (elementGrid.RowCount <= 0)
            {
                return;
            }

            int[] selIndices = selectionService.GetSelectedIndices(elementGrid);
            if (selIndices.Length == 0)
            {
                return;
            }

            ElementDeleteResult result = mutationService.DeleteItems(listCollection, listIndex, selIndices);
            deletionUiService.ApplyDeletionResult(
                result,
                listCollection,
                listIndex,
                elementGrid,
                listComboBox,
                viewModel,
                refreshItemAction);
        }
    }
}
