using System;
using System.Windows.Forms;

namespace FWEledit
{
    public static class ReplaceWindowUiServiceExtensions
    {
        public static void OpenReplaceWithSelectionRestore(
            this ReplaceWindowUiService service,
            ReplaceWindowCommandService replaceWindowCommandService,
            EditorWindowService editorWindowService,
            eListCollection listCollection,
            DataGridView elementGrid,
            Action reloadList,
            Action<string> showMessage)
        {
            if (replaceWindowCommandService == null)
            {
                return;
            }

            int[] selectedRows = CaptureSelectedRows(elementGrid);
            int currentRow = elementGrid != null && elementGrid.CurrentCell != null
                ? elementGrid.CurrentCell.RowIndex
                : -1;

            replaceWindowCommandService.OpenReplaceWindow(
                editorWindowService,
                listCollection,
                listIndex =>
                {
                    if (reloadList != null)
                    {
                        reloadList();
                    }
                    RestoreSelection(elementGrid, selectedRows, currentRow);
                },
                showMessage);
        }

        private static int[] CaptureSelectedRows(DataGridView grid)
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
            Array.Sort(selected);
            return selected;
        }

        private static void RestoreSelection(DataGridView grid, int[] selectedRows, int currentRow)
        {
            if (grid == null || grid.Rows == null || grid.Rows.Count == 0)
            {
                return;
            }

            if (selectedRows != null && selectedRows.Length > 0)
            {
                for (int i = 0; i < selectedRows.Length; i++)
                {
                    int rowIndex = selectedRows[i];
                    if (rowIndex >= 0 && rowIndex < grid.Rows.Count)
                    {
                        grid.Rows[rowIndex].Selected = true;
                    }
                }
            }

            if (currentRow >= 0 && currentRow < grid.Rows.Count)
            {
                grid.CurrentCell = grid.Rows[currentRow].Cells[0];
            }
        }
    }
}
