using System.Collections.Generic;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ListSelectionUiService
    {
        public void ApplySelection(
            ListSelectionWorkflowService workflowService,
            ListSelectionRequest request,
            int listIndex,
            int conversationListIndex,
            DataGridView elementGrid,
            DataGridView itemGrid,
            TextBox offsetBox,
            ToolStripMenuItem xrefMenuItem,
            System.Action<int, int, DataGridViewRow> applyQualityColor,
            System.Action updateDescription,
            System.Action updatePickIcon,
            System.Action persistNavigation)
        {
            if (workflowService == null || elementGrid == null || itemGrid == null)
            {
                return;
            }

            ListSelectionResult selection = workflowService.BuildSelection(request);

            elementGrid.SuspendLayout();
            elementGrid.Rows.Clear();
            itemGrid.Rows.Clear();

            if (!selection.Success)
            {
                elementGrid.ResumeLayout();
                return;
            }

            if (offsetBox != null)
            {
                offsetBox.Text = selection.OffsetText ?? string.Empty;
            }
            if (xrefMenuItem != null)
            {
                xrefMenuItem.Enabled = selection.HasXref;
            }

            List<object[]> rows = selection.Rows ?? new List<object[]>();
            if (rows.Count > 0)
            {
                DataGridViewRow[] dgRows = new DataGridViewRow[rows.Count];
                for (int i = 0; i < rows.Count; i++)
                {
                    DataGridViewRow row = (DataGridViewRow)elementGrid.RowTemplate.Clone();
                    row.CreateCells(elementGrid, rows[i]);
                    if (listIndex != conversationListIndex && applyQualityColor != null)
                    {
                        applyQualityColor(listIndex, i, row);
                    }
                    dgRows[i] = row;
                }
                elementGrid.Rows.AddRange(dgRows);
            }

            elementGrid.ResumeLayout();
            if (elementGrid.Rows.Count > 0 && elementGrid.CurrentCell == null)
            {
                elementGrid.CurrentCell = elementGrid.Rows[0].Cells[0];
            }

            if (updateDescription != null)
            {
                updateDescription();
            }
            if (updatePickIcon != null)
            {
                updatePickIcon();
            }
            if (persistNavigation != null)
            {
                persistNavigation();
            }
        }
    }
}
