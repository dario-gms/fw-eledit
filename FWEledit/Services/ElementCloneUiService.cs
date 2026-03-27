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
            listComboBox.Items[listIndex] = "[" + listIndex + "] " + friendlyListName + " (" + listCollection.Lists[listIndex].elementValues.Length + ")";

            elementGrid.ClearSelection();
            for (int i = 0; i < result.NewIndices.Length; i++)
            {
                int rowIndex = result.NewIndices[i];
                if (rowIndex > -1 && rowIndex < elementGrid.Rows.Count)
                {
                    elementGrid.Rows[rowIndex].Selected = true;
                    elementGrid.CurrentCell = elementGrid[0, rowIndex];
                }
            }
            if (elementGrid.CurrentCell != null)
            {
                elementGrid.FirstDisplayedScrollingRowIndex = elementGrid.CurrentCell.RowIndex;
            }

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
            listComboBox.Items[listIndex] = "[" + listIndex + "] " + friendlyListName + " (" + listCollection.Lists[listIndex].elementValues.Length + ")";

            elementGrid.ClearSelection();
            for (int i = 0; i < result.NewIndices.Length; i++)
            {
                int rowIndex = result.NewIndices[i];
                if (rowIndex > -1 && rowIndex < elementGrid.Rows.Count)
                {
                    elementGrid.Rows[rowIndex].Selected = true;
                    elementGrid.CurrentCell = elementGrid[0, rowIndex];
                }
            }
            if (elementGrid.CurrentCell != null)
            {
                elementGrid.FirstDisplayedScrollingRowIndex = elementGrid.CurrentCell.RowIndex;
            }

            if (refreshItemSelection != null)
            {
                refreshItemSelection();
            }
        }
    }
}
