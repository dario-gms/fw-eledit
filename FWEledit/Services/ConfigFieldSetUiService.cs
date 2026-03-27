using System.Collections.Generic;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigFieldSetUiService
    {
        public void SetSelectedFieldNames(DataGridView itemGrid, string value)
        {
            SetSelectedColumnValue(itemGrid, 0, value);
        }

        public void SetSelectedFieldTypes(DataGridView itemGrid, string value)
        {
            SetSelectedColumnValue(itemGrid, 1, value);
        }

        private static void SetSelectedColumnValue(DataGridView itemGrid, int columnIndex, string value)
        {
            if (itemGrid == null || columnIndex < 0)
            {
                return;
            }

            HashSet<int> selectedRows = new HashSet<int>();
            for (int i = 0; i < itemGrid.SelectedCells.Count; i++)
            {
                selectedRows.Add(itemGrid.SelectedCells[i].RowIndex);
            }

            foreach (int rowIndex in selectedRows)
            {
                if (rowIndex >= 0 && rowIndex < itemGrid.Rows.Count)
                {
                    itemGrid.Rows[rowIndex].Cells[columnIndex].Value = value;
                }
            }
        }
    }
}
