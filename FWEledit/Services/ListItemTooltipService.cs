using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ListItemTooltipService
    {
        public bool TryBuildTooltip(
            ISessionService sessionService,
            int listIndex,
            DataGridView grid,
            int rowIndex,
            int columnIndex,
            out string tooltipText,
            out InfoTool infoTool)
        {
            tooltipText = string.Empty;
            infoTool = null;

            if (sessionService == null || sessionService.Database == null || sessionService.ListCollection == null)
            {
                return false;
            }

            if (grid == null || columnIndex != 1 || rowIndex < 0)
            {
                return false;
            }

            int itemId;
            if (!int.TryParse(Convert.ToString(grid.Rows[rowIndex].Cells[0].Value), out itemId))
            {
                return false;
            }

            if (itemId <= 0)
            {
                return false;
            }

            try
            {
                infoTool = Extensions.GetItemProps2(sessionService, itemId, 0, listIndex, rowIndex);
                if (infoTool == null)
                {
                    string text = Extensions.GetItemProps(sessionService, itemId, 0);
                    text += Extensions.ItemDesc(sessionService, itemId);
                    tooltipText = text;
                    return true;
                }

                infoTool.description = Extensions.ColorClean(Extensions.ItemDesc(sessionService, itemId));
                return true;
            }
            catch
            {
                tooltipText = string.Empty;
                infoTool = null;
                return false;
            }
        }
    }
}
