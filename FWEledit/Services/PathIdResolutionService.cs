using System;
using System.Windows.Forms;

namespace FWEledit
{
    public delegate bool TryParsePathIdDelegate(string rawValue, out int pathId);

    public sealed class PathIdResolutionService
    {
        public int TryGetCurrentPathId(DataGridView grid, int rowIndex, TryParsePathIdDelegate tryExtractPathId)
        {
            if (grid == null || rowIndex < 0 || rowIndex >= grid.Rows.Count)
            {
                return 0;
            }

            object valueObj = grid.Rows[rowIndex].Cells[2].Value;
            int value;
            if (tryExtractPathId != null && tryExtractPathId(Convert.ToString(valueObj), out value))
            {
                return value;
            }
            return 0;
        }
    }
}
