using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ValueRowIndexService
    {
        public int GetFieldIndexForValueRow(DataGridView grid, int rowIndex)
        {
            if (grid == null || rowIndex < 0 || rowIndex >= grid.Rows.Count)
            {
                return -1;
            }
            object tag = grid.Rows[rowIndex].Tag;
            if (tag is int)
            {
                return (int)tag;
            }
            if (tag != null)
            {
                int parsed;
                if (int.TryParse(tag.ToString(), out parsed))
                {
                    return parsed;
                }
            }
            return rowIndex;
        }

        public int FindRowByFieldIndex(DataGridView grid, int fieldIndex)
        {
            if (grid == null || fieldIndex < 0)
            {
                return -1;
            }
            for (int row = 0; row < grid.Rows.Count; row++)
            {
                if (GetFieldIndexForValueRow(grid, row) == fieldIndex)
                {
                    return row;
                }
            }
            return -1;
        }
    }
}
