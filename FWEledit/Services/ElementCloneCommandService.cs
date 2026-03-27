using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementCloneCommandService
    {
        public void CloneSelected(
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService selectionService,
            ElementListMutationService mutationService,
            ElementCloneUiService cloneUiService,
            ComboBox listComboBox,
            ref bool enableSelectionList,
            ref bool enableSelectionItem,
            System.Action<int> markRowDirty,
            System.Action refreshListAction,
            System.Action refreshItemAction,
            System.Func<int, string> getFriendlyListName)
        {
            if (listCollection == null || elementGrid == null || selectionService == null || mutationService == null || cloneUiService == null)
            {
                return;
            }

            if (elementGrid.RowCount <= 0)
            {
                return;
            }

            int[] selIndices = selectionService.GetSelectedIndices(elementGrid);
            if (selIndices.Length == 0 && elementGrid.CurrentCell != null)
            {
                selIndices = new int[] { elementGrid.CurrentCell.RowIndex };
            }
            if (selIndices.Length == 0)
            {
                return;
            }

            enableSelectionList = false;
            enableSelectionItem = false;

            ElementCloneResult result = mutationService.CloneItems(listCollection, listIndex, selIndices);
            cloneUiService.ApplyCloneResult(
                result,
                listCollection,
                listIndex,
                elementGrid,
                listComboBox,
                ref enableSelectionList,
                ref enableSelectionItem,
                markRowDirty,
                refreshListAction,
                refreshItemAction,
                getFriendlyListName);
        }

        public void CloneSelected(
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService selectionService,
            ElementListMutationService mutationService,
            ElementCloneUiService cloneUiService,
            ComboBox listComboBox,
            MainWindowViewModel viewModel,
            System.Action<int> markRowDirty,
            System.Action refreshListAction,
            System.Action refreshItemAction,
            System.Func<int, string> getFriendlyListName)
        {
            if (listCollection == null || elementGrid == null || selectionService == null || mutationService == null || cloneUiService == null)
            {
                return;
            }

            if (elementGrid.RowCount <= 0)
            {
                return;
            }

            int[] selIndices = selectionService.GetSelectedIndices(elementGrid);
            if (selIndices.Length == 0 && elementGrid.CurrentCell != null)
            {
                selIndices = new int[] { elementGrid.CurrentCell.RowIndex };
            }
            if (selIndices.Length == 0)
            {
                return;
            }

            if (viewModel != null)
            {
                viewModel.EnableSelectionList = false;
                viewModel.EnableSelectionItem = false;
            }

            ElementCloneResult result = mutationService.CloneItems(listCollection, listIndex, selIndices);
            cloneUiService.ApplyCloneResult(
                result,
                listCollection,
                listIndex,
                elementGrid,
                listComboBox,
                viewModel,
                markRowDirty,
                refreshListAction,
                refreshItemAction,
                getFriendlyListName);
        }
    }
}
