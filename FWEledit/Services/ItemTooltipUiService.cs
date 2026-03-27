using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemTooltipUiService
    {
        public void HandleCellMouseMove(
            eListCollection listCollection,
            int listIndex,
            int currentRowIndex,
            DataGridView itemGrid,
            DataGridView elementGrid,
            DataGridViewCellMouseEventArgs e,
            Control tooltipTarget,
            ToolTip tooltip,
            ItemTooltipService tooltipService,
            System.Func<int, int> getFieldIndex,
            ref Point mouseMoveCheck)
        {
            if (itemGrid == null || elementGrid == null || e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            string fieldName = itemGrid.Rows[e.RowIndex].Cells[0].Value != null
                ? itemGrid.Rows[e.RowIndex].Cells[0].Value.ToString()
                : string.Empty;

            ItemTooltipResult result = tooltipService.BuildTooltip(
                listCollection,
                listIndex,
                currentRowIndex,
                e.ColumnIndex,
                e.RowIndex,
                fieldName,
                getFieldIndex);

            if (!result.ShowTooltip)
            {
                if (tooltip != null && tooltipTarget != null)
                {
                    tooltip.Hide(tooltipTarget);
                }
                itemGrid.ShowCellToolTips = true;
                return;
            }

            itemGrid.ShowCellToolTips = result.ShowCellTooltips;
            if (mouseMoveCheck.X != e.X || mouseMoveCheck.Y != e.Y)
            {
                if (tooltip != null && tooltipTarget != null)
                {
                    tooltip.SetToolTip(tooltipTarget, result.Text ?? string.Empty);
                }
                mouseMoveCheck.X = e.X;
                mouseMoveCheck.Y = e.Y;
            }
        }
    }
}
