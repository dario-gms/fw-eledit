using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigRowInsertUiService
    {
        public int InsertRow(ConfigData data, int listIndex, DataGridView grid)
        {
            if (data == null || grid == null || listIndex < 0)
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

            int insertIndex = 0;
            if (data.FieldNames[listIndex].Length > 0)
            {
                int currentIndex = grid.CurrentCell != null ? grid.CurrentCell.RowIndex : data.FieldNames[listIndex].Length - 1;
                if (currentIndex < 0)
                {
                    currentIndex = data.FieldNames[listIndex].Length - 1;
                }
                insertIndex = currentIndex + 1;
                if (insertIndex < 0)
                {
                    insertIndex = 0;
                }
                if (insertIndex > data.FieldNames[listIndex].Length)
                {
                    insertIndex = data.FieldNames[listIndex].Length;
                }

                string[] temp = new string[data.FieldNames[listIndex].Length + 1];
                System.Array.Copy(data.FieldNames[listIndex], 0, temp, 0, insertIndex);
                temp[insertIndex] = "< NAME >";
                System.Array.Copy(data.FieldNames[listIndex], insertIndex, temp, insertIndex + 1, data.FieldNames[listIndex].Length - insertIndex);
                data.FieldNames[listIndex] = temp;

                temp = new string[data.FieldTypes[listIndex].Length + 1];
                System.Array.Copy(data.FieldTypes[listIndex], 0, temp, 0, insertIndex);
                temp[insertIndex] = "< TYPE >";
                System.Array.Copy(data.FieldTypes[listIndex], insertIndex, temp, insertIndex + 1, data.FieldTypes[listIndex].Length - insertIndex);
                data.FieldTypes[listIndex] = temp;
            }
            else
            {
                data.FieldNames[listIndex] = new string[] { "< NAME >" };
                data.FieldTypes[listIndex] = new string[] { "< TYPE >" };
                insertIndex = 0;
            }

            return insertIndex;
        }
    }
}
