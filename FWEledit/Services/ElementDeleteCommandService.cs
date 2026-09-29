using System.Collections.Generic;
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

            int[] elementIndices = ResolveSelectedElementIndices(listCollection, listIndex, elementGrid, selIndices);
            ElementDeleteResult result = mutationService.DeleteItems(listCollection, listIndex, elementIndices);
            result.DeletedGridIndices = selIndices;
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

            int[] elementIndices = ResolveSelectedElementIndices(listCollection, listIndex, elementGrid, selIndices);
            ElementDeleteResult result = mutationService.DeleteItems(listCollection, listIndex, elementIndices);
            result.DeletedGridIndices = selIndices;
            deletionUiService.ApplyDeletionResult(
                result,
                listCollection,
                listIndex,
                elementGrid,
                listComboBox,
                viewModel,
                refreshItemAction);
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
