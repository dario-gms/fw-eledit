using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class SelectionStateService
    {
        public SelectionState BuildSelectionState(ComboBox listCombo, DataGridView grid)
        {
            SelectionState state = new SelectionState
            {
                ListIndex = listCombo != null ? listCombo.SelectedIndex : -1,
                ItemId = 0
            };

            if (grid == null)
            {
                return state;
            }

            int rowIndex = -1;
            if (grid.CurrentRow != null && grid.CurrentRow.Index > -1)
            {
                rowIndex = grid.CurrentRow.Index;
            }
            else if (grid.CurrentCell != null && grid.CurrentCell.RowIndex > -1)
            {
                rowIndex = grid.CurrentCell.RowIndex;
            }
            else if (grid.SelectedRows != null && grid.SelectedRows.Count > 0)
            {
                rowIndex = grid.SelectedRows[0].Index;
            }

            if (rowIndex > -1 && rowIndex < grid.Rows.Count)
            {
                int id;
                if (int.TryParse(Convert.ToString(grid.Rows[rowIndex].Cells[0].Value), out id))
                {
                    state.ItemId = id;
                }
            }

            return state;
        }
    }
}
