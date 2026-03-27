using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ReplaceWindowUiService
    {
        public void OpenReplaceWithSelectionRestore(
            ReplaceWindowCommandService replaceWindowCommandService,
            EditorWindowService editorWindowService,
            eListCollection listCollection,
            DataGridView elementGrid,
            System.Action reloadList,
            System.Action<string> showMessage)
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

        public ReplaceParameters BuildParameters(
            TextBox list,
            TextBox item,
            TextBox field,
            TextBox oldValue,
            TextBox newValue,
            ComboBox operation,
            NumericUpDown operand,
            RadioButton replaceMode,
            RadioButton recalcMode)
        {
            return new ReplaceParameters
            {
                ListIndexText = list != null ? list.Text : string.Empty,
                ItemIndexText = item != null ? item.Text : string.Empty,
                FieldIndexText = field != null ? field.Text : string.Empty,
                OldValue = oldValue != null ? oldValue.Text : string.Empty,
                NewValue = newValue != null ? newValue.Text : string.Empty,
                Operation = operation != null && operation.SelectedItem != null ? operation.SelectedItem.ToString() : string.Empty,
                Operand = operand != null ? operand.Value : 0,
                ReplaceMode = replaceMode != null && replaceMode.Checked,
                RecalculateMode = recalcMode != null && recalcMode.Checked
            };
        }

        public ReplaceResult ExecuteReplace(ReplaceWindowViewModel viewModel, ReplaceParameters parameters)
        {
            if (viewModel == null)
            {
                return new ReplaceResult { Success = false, Error = "Missing view model." };
            }

            return viewModel.Execute(parameters);
        }

        private int[] CaptureSelectedRows(DataGridView grid)
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
            System.Array.Sort(selected);
            return selected;
        }

        private void RestoreSelection(DataGridView grid, int[] selectedRows, int currentRow)
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
