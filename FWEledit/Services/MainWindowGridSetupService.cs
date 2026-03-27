using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowGridSetupService
    {
        public void ApplyNotSortable(DataGridView grid)
        {
            if (grid == null || grid.Columns == null)
            {
                return;
            }

            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }
    }
}
