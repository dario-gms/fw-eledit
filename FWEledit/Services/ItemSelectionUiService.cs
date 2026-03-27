using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemSelectionUiService
    {
        public void ApplySelection(
            ItemSelectionWorkflowService workflowService,
            ItemSelectionRequest request,
            DataGridView itemGrid,
            int scrollIndex)
        {
            if (workflowService == null || itemGrid == null)
            {
                return;
            }

            ItemSelectionResult selection = workflowService.BuildSelection(request);
            if (!selection.Success)
            {
                return;
            }

            itemGrid.Enabled = false;
            for (int i = 0; i < selection.Rows.Count; i++)
            {
                ValueRowDisplay rowDisplay = selection.Rows[i];
                int rowIndex = itemGrid.Rows.Add(new string[] { rowDisplay.FieldName, rowDisplay.FieldType, rowDisplay.DisplayValue });
                DataGridViewRow row = itemGrid.Rows[rowIndex];
                row.Tag = rowDisplay.FieldIndex;
                if (rowDisplay.FieldIndex >= 0)
                {
                    row.HeaderCell.Value = rowDisplay.FieldIndex.ToString();
                }
                if (rowDisplay.IsInvalid)
                {
                    row.Cells[2].Style.ForeColor = Color.Red;
                }
                else if (rowDisplay.IsDirty)
                {
                    row.Cells[2].Style.ForeColor = Color.DeepSkyBlue;
                }
            }
            itemGrid.Enabled = true;

            if (scrollIndex > -1)
            {
                itemGrid.FirstDisplayedScrollingRowIndex = scrollIndex;
            }
        }
    }
}
