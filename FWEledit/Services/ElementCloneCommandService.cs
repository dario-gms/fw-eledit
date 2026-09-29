using System.Collections.Generic;
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

            int[] elementIndices = ResolveSelectedElementIndices(listCollection, listIndex, elementGrid, selIndices);
            if (elementIndices.Length == 0)
            {
                return;
            }

            enableSelectionList = false;
            enableSelectionItem = false;

            ElementCloneResult result = mutationService.CloneItems(listCollection, listIndex, elementIndices);
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

            int[] elementIndices = ResolveSelectedElementIndices(listCollection, listIndex, elementGrid, selIndices);
            if (elementIndices.Length == 0)
            {
                return;
            }

            if (viewModel != null)
            {
                viewModel.EnableSelectionList = false;
                viewModel.EnableSelectionItem = false;
            }

            ElementCloneResult result = mutationService.CloneItems(listCollection, listIndex, elementIndices);
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

        private static int[] ResolveSelectedElementIndices(eListCollection listCollection, int listIndex, DataGridView elementGrid, int[] selectedGridIndices)
        {
            if (selectedGridIndices == null || selectedGridIndices.Length == 0)
            {
                return new int[0];
            }

            ElementIndexResolverService resolver = new ElementIndexResolverService();
            List<int> elementIndices = new List<int>();
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < selectedGridIndices.Length; i++)
            {
                int elementIndex = resolver.ResolveElementIndexFromGridRow(listCollection, listIndex, selectedGridIndices[i], elementGrid);
                if (elementIndex >= 0 && seen.Add(elementIndex))
                {
                    elementIndices.Add(elementIndex);
                }
            }

            return elementIndices.ToArray();
        }
    }
}
