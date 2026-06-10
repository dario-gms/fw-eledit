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

            DataGridViewRow[] rows = new DataGridViewRow[selection.Rows.Count];
            for (int i = 0; i < selection.Rows.Count; i++)
            {
                ValueRowDisplay rowDisplay = selection.Rows[i];
                DataGridViewRow row = (DataGridViewRow)itemGrid.RowTemplate.Clone();
                row.CreateCells(itemGrid, new string[] { rowDisplay.FieldName, rowDisplay.FieldType, rowDisplay.DisplayValue });
                row.Tag = rowDisplay.FieldIndex;
                row.Cells[2].Tag = rowDisplay.ResolvedReferenceOption != null
                    ? new ValueCellState
                    {
                        RawValue = rowDisplay.RawValue ?? string.Empty,
                        ReferenceOption = rowDisplay.ResolvedReferenceOption
                    }
                    : (object)(rowDisplay.RawValue ?? string.Empty);
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
                rows[i] = row;
            }

            if (rows.Length > 0)
            {
                itemGrid.Rows.AddRange(rows);
            }

            if (scrollIndex > -1 && scrollIndex < itemGrid.Rows.Count)
            {
                itemGrid.FirstDisplayedScrollingRowIndex = scrollIndex;
            }
        }
    }
}
