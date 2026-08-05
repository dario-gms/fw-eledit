using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemQualityRowStyleService
    {
        public void ApplyQualityStyle(
            eListCollection listCollection,
            int listIndex,
            int entryIndex,
            DataGridViewRow row,
            DataGridView elementGrid,
            Func<int, int> getQualityFieldIndex,
            Func<int, Color?> getQualityColor,
            Func<int, int, Color?> getEntityNameColor,
            Func<Color, float, Color> darkenColor)
        {
            if (row == null || row.Cells == null || row.Cells.Count < 3 || elementGrid == null || listCollection == null)
            {
                return;
            }

            Color? entityNameColor = getEntityNameColor != null ? getEntityNameColor(listIndex, entryIndex) : null;
            if (entityNameColor.HasValue)
            {
                ApplyNameColor(row, elementGrid, entityNameColor.Value, darkenColor);
                return;
            }

            int qualityFieldIndex = getQualityFieldIndex != null ? getQualityFieldIndex(listIndex) : -1;
            if (qualityFieldIndex < 0)
            {
                ResetRowColors(row, elementGrid);
                return;
            }

            string raw = listCollection.GetValue(listIndex, entryIndex, qualityFieldIndex);
            int quality;
            if (!int.TryParse(raw, out quality))
            {
                ResetRowColors(row, elementGrid);
                return;
            }

            Color? color = getQualityColor != null ? getQualityColor(quality) : null;
            if (color.HasValue)
            {
                ApplyNameColor(row, elementGrid, color.Value, darkenColor);
            }
            else
            {
                ResetRowColors(row, elementGrid);
            }
        }

        private static void ApplyNameColor(
            DataGridViewRow row,
            DataGridView elementGrid,
            Color baseColor,
            Func<Color, float, Color> darkenColor)
        {
            Color hover = darkenColor != null ? darkenColor(baseColor, 0.25f) : baseColor;
            row.Cells[2].Style.ForeColor = baseColor;
            row.Cells[2].Style.SelectionForeColor = baseColor;
            row.Cells[2].Style.SelectionBackColor = hover;
            row.Cells[0].Style.SelectionBackColor = hover;
            row.Cells[1].Style.SelectionBackColor = hover;
            if (row.Cells.Count > 3)
            {
                row.Cells[3].Style.SelectionBackColor = hover;
                row.Cells[3].Style.SelectionForeColor = elementGrid.DefaultCellStyle.SelectionForeColor;
            }
        }

        private static void ResetRowColors(DataGridViewRow row, DataGridView elementGrid)
        {
            row.Cells[2].Style.ForeColor = elementGrid.DefaultCellStyle.ForeColor;
            row.Cells[2].Style.SelectionForeColor = elementGrid.DefaultCellStyle.SelectionForeColor;
            row.Cells[2].Style.SelectionBackColor = elementGrid.DefaultCellStyle.SelectionBackColor;
            row.Cells[0].Style.SelectionBackColor = elementGrid.DefaultCellStyle.SelectionBackColor;
            row.Cells[1].Style.SelectionBackColor = elementGrid.DefaultCellStyle.SelectionBackColor;
            if (row.Cells.Count > 3)
            {
                row.Cells[3].Style.SelectionBackColor = elementGrid.DefaultCellStyle.SelectionBackColor;
                row.Cells[3].Style.SelectionForeColor = elementGrid.DefaultCellStyle.SelectionForeColor;
            }
        }
    }
}
