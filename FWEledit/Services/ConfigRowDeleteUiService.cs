using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigRowDeleteUiService
    {
        public int DeleteSelection(ConfigData data, int listIndex, DataGridView grid, GridCellSelectionService selectionService)
        {
            if (data == null || grid == null || selectionService == null || listIndex < 0)
            {
                return -1;
            }
            if (data.FieldNames == null || data.FieldTypes == null)
            {
                return -1;
            }
            if (listIndex >= data.FieldNames.Length || listIndex >= data.FieldTypes.Length)
            {
                return -1;
            }

            int[] selectedRows = selectionService.GetSelectedRowIndices(grid);
            if (selectedRows.Length == 0)
            {
                return -1;
            }

            for (int i = selectedRows.Length - 1; i > -1; i--)
            {
                int rowIndex = selectedRows[i];
                if (rowIndex < 0 || rowIndex >= data.FieldNames[listIndex].Length)
                {
                    continue;
                }

                string[] temp = new string[data.FieldNames[listIndex].Length - 1];
                System.Array.Copy(data.FieldNames[listIndex], 0, temp, 0, rowIndex);
                System.Array.Copy(data.FieldNames[listIndex], rowIndex + 1, temp, rowIndex, data.FieldNames[listIndex].Length - 1 - rowIndex);
                data.FieldNames[listIndex] = temp;

                temp = new string[data.FieldTypes[listIndex].Length - 1];
                System.Array.Copy(data.FieldTypes[listIndex], 0, temp, 0, rowIndex);
                System.Array.Copy(data.FieldTypes[listIndex], rowIndex + 1, temp, rowIndex, data.FieldTypes[listIndex].Length - 1 - rowIndex);
                data.FieldTypes[listIndex] = temp;
            }

            int currentRowIndex = selectedRows[0] - 1;
            return currentRowIndex;
        }
    }
}
