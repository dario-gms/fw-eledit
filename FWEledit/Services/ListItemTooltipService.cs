using System;
using System.Drawing;
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
                    if (listIndex != 0)
                    {
                        text += Extensions.ItemDesc(sessionService, itemId);
                    }
                    tooltipText = text;
                    return true;
                }

                infoTool.description = listIndex == 0
                    ? string.Empty
                    : Extensions.ItemDesc(sessionService, itemId);
                if (infoTool.img == null)
                {
                    infoTool.img = TryGetGridIcon(grid, rowIndex);
                }
                return true;
            }
            catch
            {
                tooltipText = string.Empty;
                infoTool = null;
                return false;
            }
        }

        private static Bitmap TryGetGridIcon(DataGridView grid, int rowIndex)
        {
            if (grid == null || rowIndex < 0 || rowIndex >= grid.Rows.Count || grid.Columns.Count <= 1)
            {
                return null;
            }

            object value = grid.Rows[rowIndex].Cells[1].Value;
            Image image = value as Image;
            if (image == null)
            {
                return null;
            }

            Bitmap bitmap = image as Bitmap;
            if (bitmap != null)
            {
                return bitmap;
            }

            return new Bitmap(image);
        }
    }
}
