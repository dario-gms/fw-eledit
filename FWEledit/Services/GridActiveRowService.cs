using System.Windows.Forms;

namespace FWEledit
{
    public sealed class GridActiveRowService
    {
        public int GetActiveRowIndex(DataGridView grid, GridSelectionService selectionService)
        {
            if (grid == null || grid.Rows.Count == 0)
            {
                return -1;
            }

            int[] selected = selectionService != null ? selectionService.GetSelectedIndices(grid) : new int[0];
            if (selected != null && selected.Length > 0)
            {
                if (grid.CurrentCell != null)
                {
                    int current = grid.CurrentCell.RowIndex;
                    for (int i = 0; i < selected.Length; i++)
                    {
                        if (selected[i] == current)
                        {
                            return current;
                        }
                    }
                }
                return selected[0];
            }

            if (grid.CurrentCell != null)
            {
                return grid.CurrentCell.RowIndex;
            }

            return 0;
        }
    }
}
