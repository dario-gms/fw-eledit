using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemSelectionContextService
    {
        public ItemSelectionContext BuildContext(
            bool enableSelectionItem,
            int listIndex,
            DataGridView elementGrid,
            DataGridView itemGrid,
            GridActiveRowService gridActiveRowService,
            GridSelectionService gridSelectionService,
            System.Func<int, int, int> resolveElementIndexFromGridRow)
        {
            if (!enableSelectionItem || elementGrid == null || itemGrid == null || gridActiveRowService == null || gridSelectionService == null)
            {
                return null;
            }

            int selectedGridRow = gridActiveRowService.GetActiveRowIndex(elementGrid, gridSelectionService);
            if (selectedGridRow < 0)
            {
                return null;
            }

            if (elementGrid.CurrentCell == null || elementGrid.CurrentCell.RowIndex != selectedGridRow)
            {
                elementGrid.CurrentCell = elementGrid.Rows[selectedGridRow].Cells[0];
            }

            int elementIndex = resolveElementIndexFromGridRow(listIndex, selectedGridRow);
            if (elementIndex < 0)
            {
                return null;
            }

            int scrollIndex = itemGrid.FirstDisplayedScrollingRowIndex;
            return new ItemSelectionContext
            {
                ListIndex = listIndex,
                SelectedGridRow = selectedGridRow,
                ElementIndex = elementIndex,
                ScrollIndex = scrollIndex
            };
        }
    }
}
