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

            if (grid != null && grid.CurrentCell != null && grid.CurrentCell.RowIndex > -1)
            {
                int id;
                if (int.TryParse(Convert.ToString(grid.Rows[grid.CurrentCell.RowIndex].Cells[0].Value), out id))
                {
                    state.ItemId = id;
                }
            }

            return state;
        }
    }
}
