using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementMoveUiService
    {
        public void ApplyMoveResult(
            ElementMoveResult result,
            eListCollection listCollection,
            DataGridView elementGrid,
            ref bool enableSelectionItem,
            System.Action refreshList)
        {
            if (result == null || listCollection == null || elementGrid == null)
            {
                return;
            }

            if (!result.Success)
            {
                if (result.IsConversationList)
                {
                    MessageBox.Show("Operation not supported in List " + listCollection.ConversationListIndex.ToString());
                }
                return;
            }

            enableSelectionItem = false;
            if (refreshList != null)
            {
                refreshList();
            }

            elementGrid.ClearSelection();
            for (int i = 0; i < result.NewSelectedIndices.Length; i++)
            {
                int rowIndex = result.NewSelectedIndices[i];
                if (rowIndex > -1 && rowIndex < elementGrid.Rows.Count)
                {
                    elementGrid.Rows[rowIndex].Selected = true;
                    elementGrid.FirstDisplayedScrollingRowIndex = rowIndex;
                }
            }

            enableSelectionItem = true;
        }

        public void ApplyMoveResult(
            ElementMoveResult result,
            eListCollection listCollection,
            DataGridView elementGrid,
            MainWindowViewModel viewModel,
            System.Action refreshList)
        {
            if (result == null || listCollection == null || elementGrid == null || viewModel == null)
            {
                return;
            }

            if (!result.Success)
            {
                if (result.IsConversationList)
                {
                    MessageBox.Show("Operation not supported in List " + listCollection.ConversationListIndex.ToString());
                }
                return;
            }

            viewModel.EnableSelectionItem = false;
            if (refreshList != null)
            {
                refreshList();
            }

            elementGrid.ClearSelection();
            for (int i = 0; i < result.NewSelectedIndices.Length; i++)
            {
                int rowIndex = result.NewSelectedIndices[i];
                if (rowIndex > -1 && rowIndex < elementGrid.Rows.Count)
                {
                    elementGrid.Rows[rowIndex].Selected = true;
                    elementGrid.FirstDisplayedScrollingRowIndex = rowIndex;
                }
            }

            viewModel.EnableSelectionItem = true;
        }
    }
}
