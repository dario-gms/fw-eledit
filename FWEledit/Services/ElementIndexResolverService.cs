using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementIndexResolverService
    {
        public int ResolveElementIndexFromGridRow(eListCollection listCollection, int listIndex, int gridRowIndex, DataGridView grid)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return -1;
            }
            if (grid == null || gridRowIndex < 0 || gridRowIndex >= grid.Rows.Count)
            {
                return -1;
            }
            if (listIndex == listCollection.ConversationListIndex)
            {
                return gridRowIndex;
            }

            if (listCollection.Lists[listIndex].elementFields == null || listCollection.Lists[listIndex].elementFields.Length == 0)
            {
                return gridRowIndex;
            }

            string firstField = listCollection.Lists[listIndex].elementFields[0];
            bool hasIdField = string.Equals(firstField, "id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(firstField, "ID", StringComparison.OrdinalIgnoreCase);
            if (!hasIdField)
            {
                return gridRowIndex;
            }

            int selectedId;
            if (!int.TryParse(Convert.ToString(grid.Rows[gridRowIndex].Cells[0].Value), out selectedId))
            {
                return gridRowIndex;
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementValues.Length; i++)
            {
                int itemId;
                if (!int.TryParse(listCollection.GetValue(listIndex, i, 0), out itemId))
                {
                    continue;
                }
                if (itemId == selectedId)
                {
                    return i;
                }
            }

            return gridRowIndex;
        }
    }
}
