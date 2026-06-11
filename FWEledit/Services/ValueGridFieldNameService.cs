using System.Windows.Forms;

namespace FWEledit
{
    public static class ValueGridFieldNameService
    {
        public static string GetFieldName(DataGridView grid, int rowIndex)
        {
            if (grid == null || rowIndex < 0 || rowIndex >= grid.Rows.Count)
            {
                return string.Empty;
            }

            return GetFieldName(grid.Rows[rowIndex]);
        }

        public static string GetFieldName(DataGridViewRow row)
        {
            if (row == null || row.Cells == null || row.Cells.Count <= 0)
            {
                return string.Empty;
            }

            object taggedFieldName = row.Cells[0].Tag;
            if (taggedFieldName is string taggedText && !string.IsNullOrWhiteSpace(taggedText))
            {
                return taggedText;
            }

            object displayedFieldName = row.Cells[0].Value;
            return displayedFieldName != null
                ? displayedFieldName.ToString() ?? string.Empty
                : string.Empty;
        }
    }
}
