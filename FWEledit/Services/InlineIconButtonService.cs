using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class InlineIconButtonService
    {
        public void UpdateInlineButton(
            Button button,
            DataGridView grid,
            TabControl rightTabs,
            TabPage valuesTab,
            eListCollection listCollection,
            int listIndex,
            ItemFieldClassifierService fieldClassifier,
            ItemReferenceService itemReferenceService,
            ref int rowIndex,
            bool suppressValuesUiRefresh)
        {
            if (button == null || grid == null || fieldClassifier == null)
            {
                return;
            }
            if (suppressValuesUiRefresh)
            {
                button.Visible = false;
                rowIndex = -1;
                return;
            }

            bool enabled = false;
            Rectangle rect = Rectangle.Empty;
            rowIndex = -1;

            if (grid.Rows != null && grid.Rows.Count > 0)
            {
                if (grid.CurrentCell != null && grid.CurrentCell.RowIndex >= 0)
                {
                    int currentRow = grid.CurrentCell.RowIndex;
                    if (currentRow < grid.Rows.Count)
                    {
                        string currentField = ValueGridFieldNameService.GetFieldName(grid, currentRow);
                        if (fieldClassifier.IsPickerField(listCollection, listIndex, currentField, itemReferenceService))
                        {
                            enabled = true;
                            rowIndex = currentRow;
                            rect = grid.GetCellDisplayRectangle(2, currentRow, true);
                        }
                    }
                }
            }

            bool valuesTabActive = rightTabs != null && valuesTab != null && rightTabs.SelectedTab == valuesTab;
            if (!enabled || rect.Width < 80 || rect.Height < 18 || !valuesTabActive)
            {
                button.Visible = false;
                return;
            }

            button.Enabled = true;
            int width = Math.Min(96, Math.Max(74, rect.Width / 3));
            int height = Math.Max(18, rect.Height - 2);
            button.SetBounds(rect.Right - width - 1, rect.Top + 1, width, height);
            button.BringToFront();
            button.Visible = true;
        }
    }
}
