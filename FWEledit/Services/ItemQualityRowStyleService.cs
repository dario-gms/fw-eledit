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

            Color? embeddedNameColor = GetEmbeddedNameColor(listCollection, listIndex, entryIndex);
            Color? entityNameColor = getEntityNameColor != null ? getEntityNameColor(listIndex, entryIndex) : null;
            if (entityNameColor.HasValue)
            {
                ApplyNameColor(row, elementGrid, embeddedNameColor.HasValue ? embeddedNameColor.Value : entityNameColor.Value, darkenColor);
                return;
            }

            int qualityFieldIndex = getQualityFieldIndex != null ? getQualityFieldIndex(listIndex) : -1;
            if (qualityFieldIndex < 0)
            {
                ResetRowColors(row, elementGrid);
                if (embeddedNameColor.HasValue)
                {
                    ApplyNameColor(row, elementGrid, embeddedNameColor.Value, darkenColor);
                }
                return;
            }

            string raw = listCollection.GetValue(listIndex, entryIndex, qualityFieldIndex);
            int quality;
            if (!int.TryParse(raw, out quality))
            {
                ResetRowColors(row, elementGrid);
                if (embeddedNameColor.HasValue)
                {
                    ApplyNameColor(row, elementGrid, embeddedNameColor.Value, darkenColor);
                }
                return;
            }

            Color? color = getQualityColor != null ? getQualityColor(quality) : null;
            if (color.HasValue)
            {
                ApplyNameColor(row, elementGrid, embeddedNameColor.HasValue ? embeddedNameColor.Value : color.Value, darkenColor);
            }
            else
            {
                ResetRowColors(row, elementGrid);
                if (embeddedNameColor.HasValue)
                {
                    ApplyNameColor(row, elementGrid, embeddedNameColor.Value, darkenColor);
                }
            }
        }

        private static Color? GetEmbeddedNameColor(eListCollection listCollection, int listIndex, int entryIndex)
        {
            if (listCollection == null
                || listCollection.Lists == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null)
            {
                return null;
            }

            int nameFieldIndex = -1;
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "name", StringComparison.OrdinalIgnoreCase))
                {
                    nameFieldIndex = i;
                    break;
                }
            }

            if (nameFieldIndex < 0)
            {
                return null;
            }

            Color color;
            string ignored;
            return FwTextColorService.TryParseLeadingColor(listCollection.GetValue(listIndex, entryIndex, nameFieldIndex), out color, out ignored)
                ? (Color?)color
                : null;
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
