using System.Collections.Generic;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementCloneUiService
    {
        public void ApplyCloneResult(
            ElementCloneResult result,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            ComboBox listComboBox,
            ref bool enableSelectionList,
            ref bool enableSelectionItem,
            System.Action<int> markRowDirty,
            System.Action refreshList,
            System.Action refreshItemSelection,
            System.Func<int, string> getFriendlyListName)
        {
            if (result == null || listCollection == null || elementGrid == null || listComboBox == null)
            {
                return;
            }

            if (!result.Success)
            {
                if (result.IsConversationList)
                {
                    MessageBox.Show("Operation not supported in List " + listCollection.ConversationListIndex.ToString());
                }
                enableSelectionList = true;
                enableSelectionItem = true;
                return;
            }

            for (int i = 0; i < result.NewIndices.Length; i++)
            {
                if (markRowDirty != null)
                {
                    markRowDirty(result.NewIndices[i]);
                }
            }

            enableSelectionList = true;
            enableSelectionItem = true;

            if (refreshList != null)
            {
                refreshList();
            }

            string friendlyListName = getFriendlyListName != null
                ? getFriendlyListName(listIndex)
                : listCollection.Lists[listIndex].listName;
            ListComboPopulationService.SetItemTextPreservingIcon(
                listComboBox,
                listIndex,
                "[" + listIndex + "] " + friendlyListName + " (" + listCollection.Lists[listIndex].elementValues.Length + ")");

            SelectNewRows(elementGrid, result.NewIndices);

            if (refreshItemSelection != null)
            {
                refreshItemSelection();
            }
        }

        public void ApplyCloneResult(
            ElementCloneResult result,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            ComboBox listComboBox,
            MainWindowViewModel viewModel,
            System.Action<int> markRowDirty,
            System.Action refreshList,
            System.Action refreshItemSelection,
            System.Func<int, string> getFriendlyListName)
        {
            if (result == null || listCollection == null || elementGrid == null || listComboBox == null || viewModel == null)
            {
                return;
            }

            if (!result.Success)
            {
                if (result.IsConversationList)
                {
                    MessageBox.Show("Operation not supported in List " + listCollection.ConversationListIndex.ToString());
                }
                viewModel.EnableSelectionList = true;
                viewModel.EnableSelectionItem = true;
                return;
            }

            for (int i = 0; i < result.NewIndices.Length; i++)
            {
                if (markRowDirty != null)
                {
                    markRowDirty(result.NewIndices[i]);
                }
            }

            viewModel.EnableSelectionList = true;
            viewModel.EnableSelectionItem = true;

            if (refreshList != null)
            {
                refreshList();
            }

            string friendlyListName = getFriendlyListName != null
                ? getFriendlyListName(listIndex)
                : listCollection.Lists[listIndex].listName;
            ListComboPopulationService.SetItemTextPreservingIcon(
                listComboBox,
                listIndex,
                "[" + listIndex + "] " + friendlyListName + " (" + listCollection.Lists[listIndex].elementValues.Length + ")");

            SelectNewRows(elementGrid, result.NewIndices);

            if (refreshItemSelection != null)
            {
                refreshItemSelection();
            }
        }

        private static void SelectNewRows(DataGridView elementGrid, int[] newElementIndices)
        {
            if (elementGrid == null)
            {
                return;
            }

            elementGrid.ClearSelection();
            if (newElementIndices == null || newElementIndices.Length == 0)
            {
                return;
            }

            HashSet<int> pendingElementIndices = new HashSet<int>(newElementIndices);
            bool currentCellSet = false;
            for (int rowIndex = 0; rowIndex < elementGrid.Rows.Count; rowIndex++)
            {
                DataGridViewRow row = elementGrid.Rows[rowIndex];
                if (!(row.Tag is int))
                {
                    continue;
                }

                int elementIndex = (int)row.Tag;
                if (!pendingElementIndices.Contains(elementIndex))
                {
                    continue;
                }

                row.Selected = true;
                if (!currentCellSet)
                {
                    elementGrid.CurrentCell = elementGrid[0, rowIndex];
                    currentCellSet = true;
                }
            }

            if (elementGrid.CurrentCell != null)
            {
                elementGrid.FirstDisplayedScrollingRowIndex = elementGrid.CurrentCell.RowIndex;
            }
        }
    }
}
