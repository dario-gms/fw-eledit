using System.Linq;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class GridSelectionService
    {
        public int[] GetSelectedIndices(DataGridView grid)
        {
            if (grid == null)
            {
                return new int[0];
            }

            System.Collections.Generic.List<int> selectedIndices = new System.Collections.Generic.List<int>();

            if (grid.Rows != null && grid.Rows.Count > 0)
            {
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row != null && row.Selected && row.Index >= 0)
                    {
                        selectedIndices.Add(row.Index);
                    }
                }
            }

            if (grid.SelectedRows != null && grid.SelectedRows.Count > 0)
            {
                foreach (DataGridViewRow row in grid.SelectedRows)
                {
                    if (row != null && row.Index >= 0)
                    {
                        selectedIndices.Add(row.Index);
                    }
                }
            }

            if (selectedIndices.Count == 0 && grid.SelectedCells != null && grid.SelectedCells.Count > 0)
            {
                foreach (DataGridViewCell cell in grid.SelectedCells)
                {
                    if (cell != null && cell.RowIndex >= 0)
                    {
                        selectedIndices.Add(cell.RowIndex);
                    }
                }
            }

            if (selectedIndices.Count == 0)
            {
                return new int[0];
            }

            int[] selected = selectedIndices.Distinct().ToArray();
            System.Array.Sort(selected);
            return selected;
        }
    }
}
