using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementMoveCommandService
    {
        public void MoveToTop(
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService selectionService,
            ElementListMutationService mutationService,
            ElementMoveUiService moveUiService,
            ref bool enableSelectionItem,
            System.Action refreshListAction)
        {
            if (listCollection == null || elementGrid == null || selectionService == null || mutationService == null || moveUiService == null)
            {
                return;
            }

            int[] selIndices = selectionService.GetSelectedIndices(elementGrid);
            ElementMoveResult result = mutationService.MoveItemsToTop(listCollection, listIndex, selIndices);
            moveUiService.ApplyMoveResult(result, listCollection, elementGrid, ref enableSelectionItem, refreshListAction);
        }

        public void MoveToTop(
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService selectionService,
            ElementListMutationService mutationService,
            ElementMoveUiService moveUiService,
            MainWindowViewModel viewModel,
            System.Action refreshListAction)
        {
            if (listCollection == null || elementGrid == null || selectionService == null || mutationService == null || moveUiService == null)
            {
                return;
            }

            int[] selIndices = selectionService.GetSelectedIndices(elementGrid);
            ElementMoveResult result = mutationService.MoveItemsToTop(listCollection, listIndex, selIndices);
            moveUiService.ApplyMoveResult(result, listCollection, elementGrid, viewModel, refreshListAction);
        }

        public void MoveToEnd(
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService selectionService,
            ElementListMutationService mutationService,
            ElementMoveUiService moveUiService,
            ref bool enableSelectionItem,
            System.Action refreshListAction)
        {
            if (listCollection == null || elementGrid == null || selectionService == null || mutationService == null || moveUiService == null)
            {
                return;
            }

            int[] selIndices = selectionService.GetSelectedIndices(elementGrid);
            ElementMoveResult result = mutationService.MoveItemsToEnd(listCollection, listIndex, selIndices);
            moveUiService.ApplyMoveResult(result, listCollection, elementGrid, ref enableSelectionItem, refreshListAction);
        }

        public void MoveToEnd(
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService selectionService,
            ElementListMutationService mutationService,
            ElementMoveUiService moveUiService,
            MainWindowViewModel viewModel,
            System.Action refreshListAction)
        {
            if (listCollection == null || elementGrid == null || selectionService == null || mutationService == null || moveUiService == null)
            {
                return;
            }

            int[] selIndices = selectionService.GetSelectedIndices(elementGrid);
            ElementMoveResult result = mutationService.MoveItemsToEnd(listCollection, listIndex, selIndices);
            moveUiService.ApplyMoveResult(result, listCollection, elementGrid, viewModel, refreshListAction);
        }
    }
}
