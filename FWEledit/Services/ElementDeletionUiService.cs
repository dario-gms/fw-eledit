using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementDeletionUiService
    {
        public void ApplyDeletionResult(
            ElementDeleteResult result,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            ComboBox listComboBox,
            ref bool enableSelectionList,
            ref bool enableSelectionItem,
            ref bool hasUnsavedChanges,
            System.Action refreshItemSelection)
        {
            if (result == null || elementGrid == null || listCollection == null || listComboBox == null)
            {
                return;
            }

            if (!result.Success)
            {
                if (result.IsConversationList)
                {
                    MessageBox.Show("Operation not supported in List " + listCollection.ConversationListIndex.ToString());
                }
                else if (result.DeleteAllBlocked)
                {
                    MessageBox.Show("Cannot delete all items in list!");
                }
                return;
            }

            enableSelectionList = false;
            enableSelectionItem = false;

            for (int i = result.DeletedIndices.Length - 1; i > -1; i--)
            {
                int rowIndex = result.DeletedIndices[i];
                if (rowIndex > -1 && rowIndex < elementGrid.Rows.Count)
                {
                    elementGrid.Rows.RemoveAt(rowIndex);
                }
            }

            hasUnsavedChanges = true;
            listComboBox.Items[listIndex] = "[" + listIndex + "]: " + listCollection.Lists[listIndex].listName + " (" + listCollection.Lists[listIndex].elementValues.Length + ")";
            enableSelectionList = true;
            enableSelectionItem = true;

            if (refreshItemSelection != null)
            {
                refreshItemSelection();
            }
        }

        public void ApplyDeletionResult(
            ElementDeleteResult result,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            ComboBox listComboBox,
            MainWindowViewModel viewModel,
            System.Action refreshItemSelection)
        {
            if (result == null || elementGrid == null || listCollection == null || viewModel == null || listComboBox == null)
            {
                return;
            }

            if (!result.Success)
            {
                if (result.IsConversationList)
                {
                    MessageBox.Show("Operation not supported in List " + listCollection.ConversationListIndex.ToString());
                }
                else if (result.DeleteAllBlocked)
                {
                    MessageBox.Show("Cannot delete all items in list!");
                }
                return;
            }

            viewModel.EnableSelectionList = false;
            viewModel.EnableSelectionItem = false;

            for (int i = result.DeletedIndices.Length - 1; i > -1; i--)
            {
                int rowIndex = result.DeletedIndices[i];
                if (rowIndex > -1 && rowIndex < elementGrid.Rows.Count)
                {
                    elementGrid.Rows.RemoveAt(rowIndex);
                }
            }

            viewModel.HasUnsavedChanges = true;
            listComboBox.Items[listIndex] = "[" + listIndex + "]: " + listCollection.Lists[listIndex].listName + " (" + listCollection.Lists[listIndex].elementValues.Length + ")";
            viewModel.EnableSelectionList = true;
            viewModel.EnableSelectionItem = true;

            if (refreshItemSelection != null)
            {
                refreshItemSelection();
            }
        }
    }
}
