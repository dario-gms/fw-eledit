using System.Windows.Forms;

namespace FWEledit
{
    public sealed class GridSelectionService
    {
        public int[] GetSelectedIndices(DataGridView grid)
        {
            if (grid == null || grid.SelectedRows == null || grid.SelectedRows.Count == 0)
            {
                return new int[0];
            }

            int[] selected = new int[grid.SelectedRows.Count];
            int i = 0;
            foreach (DataGridViewRow row in grid.SelectedRows)
            {
                selected[i++] = row.Index;
            }
            System.Array.Sort(selected);
            return selected;
        }
    }
}
