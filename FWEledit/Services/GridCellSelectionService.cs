using System.Collections.Generic;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class GridCellSelectionService
    {
        public int[] GetSelectedRowIndices(DataGridView grid)
        {
            if (grid == null || grid.SelectedCells == null || grid.SelectedCells.Count == 0)
            {
                return new int[0];
            }

            HashSet<int> rows = new HashSet<int>();
            for (int i = 0; i < grid.SelectedCells.Count; i++)
            {
                rows.Add(grid.SelectedCells[i].RowIndex);
            }

            int[] selected = new int[rows.Count];
            rows.CopyTo(selected);
            System.Array.Sort(selected);
            return selected;
        }
    }
}
