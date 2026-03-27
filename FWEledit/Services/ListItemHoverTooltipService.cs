using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ListItemHoverTooltipService
    {
        public void HandleCellMouseEnter(
            ISessionService sessionService,
            int listIndex,
            DataGridView elementGrid,
            DataGridViewCellEventArgs e,
            ListItemTooltipService tooltipService,
            ref IToolType customToolType)
        {
            if (sessionService == null || sessionService.Database == null || sessionService.ListCollection == null || elementGrid == null)
            {
                return;
            }

            try
            {
                if (customToolType != null)
                {
                    customToolType.Close();
                }
            }
            catch { }

            if (e.ColumnIndex < 0 || e.RowIndex < 0)
            {
                return;
            }
            if (e.ColumnIndex != 1)
            {
                return;
            }

            string tooltipText;
            InfoTool infoTool;
            if (!tooltipService.TryBuildTooltip(
                    sessionService,
                    listIndex,
                    elementGrid,
                    e.RowIndex,
                    e.ColumnIndex,
                    out tooltipText,
                    out infoTool))
            {
                return;
            }

            if (infoTool != null)
            {
                customToolType = new IToolType(sessionService, infoTool);
                customToolType.Show();
                return;
            }

            if (!string.IsNullOrWhiteSpace(tooltipText))
            {
                elementGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText = tooltipText;
            }
        }
    }
}
