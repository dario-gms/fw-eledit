using System.Collections.Generic;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigRowCopyService
    {
        public void CopySelection(
            ConfigData data,
            int listIndex,
            DataGridView grid,
            GridCellSelectionService selectionService,
            out string[] copyNames,
            out string[] copyTypes)
        {
            copyNames = new string[0];
            copyTypes = new string[0];

            if (data == null || grid == null || selectionService == null || listIndex < 0)
            {
                return;
            }
            if (data.FieldNames == null || data.FieldTypes == null)
            {
                return;
            }
            if (listIndex >= data.FieldNames.Length || listIndex >= data.FieldTypes.Length)
            {
                return;
            }

            int[] selectedRows = selectionService.GetSelectedRowIndices(grid);
            if (selectedRows.Length == 0)
            {
                return;
            }

            List<string> names = new List<string>();
            List<string> types = new List<string>();
            for (int i = 0; i < selectedRows.Length; i++)
            {
                int rowIndex = selectedRows[i];
                if (rowIndex < 0 || rowIndex >= data.FieldNames[listIndex].Length)
                {
                    continue;
                }

                names.Add(data.FieldNames[listIndex][rowIndex]);
                types.Add(data.FieldTypes[listIndex][rowIndex]);
            }

            copyNames = names.ToArray();
            copyTypes = types.ToArray();
        }
    }
}
