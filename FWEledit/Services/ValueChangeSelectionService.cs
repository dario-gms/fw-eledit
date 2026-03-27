using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ValueChangeSelectionService
    {
        public ValueChangeSelectionContext BuildSelection(
            eListCollection listCollection,
            ComboBox listComboBox,
            DataGridView itemGrid,
            DataGridView elementGrid,
            DataGridViewCellEventArgs ea,
            GridActiveRowService gridActiveRowService,
            GridSelectionService gridSelectionService,
            System.Func<int, int> getFieldIndexForValueRow,
            System.Func<int, int, int> resolveElementIndexFromGridRow)
        {
            if (listCollection == null || listComboBox == null || itemGrid == null || elementGrid == null || ea == null)
            {
                return null;
            }
            if (ea.ColumnIndex < 0 || ea.RowIndex < 0)
            {
                return null;
            }

            int listIndex = listComboBox.SelectedIndex;
            int gridRow = ea.RowIndex;
            int fieldIndex = getFieldIndexForValueRow != null ? getFieldIndexForValueRow(gridRow) : -1;
            if (fieldIndex < 0 || listIndex < 0)
            {
                return null;
            }

            int currentGridRow = gridActiveRowService.GetActiveRowIndex(elementGrid, gridSelectionService);
            if (currentGridRow < 0)
            {
                return null;
            }
            if (elementGrid.CurrentCell == null || elementGrid.CurrentCell.RowIndex != currentGridRow)
            {
                elementGrid.CurrentCell = elementGrid.Rows[currentGridRow].Cells[0];
            }

            int elementIndex = resolveElementIndexFromGridRow(listIndex, currentGridRow);
            if (elementIndex < 0)
            {
                return null;
            }

            return new ValueChangeSelectionContext
            {
                ListIndex = listIndex,
                GridRow = gridRow,
                FieldIndex = fieldIndex,
                CurrentGridRow = currentGridRow,
                ElementIndex = elementIndex
            };
        }
    }
}
