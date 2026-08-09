using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ComboBoxThemeRendererService
    {
        public void DrawItem(ComboBox combo, DrawItemEventArgs e, IList<string> theme)
        {
            if (combo == null || e == null)
            {
                return;
            }
            if (e.Index < 0 || e.Index >= combo.Items.Count)
            {
                return;
            }

            try
            {
                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color backColor = selected ? Color.FromArgb(225, 231, 238) : Color.White;
                Color foreColor = Color.FromArgb(29, 36, 45);

                using (SolidBrush backBrush = new SolidBrush(backColor))
                {
                    e.Graphics.FillRectangle(backBrush, e.Bounds);
                }
                using (SolidBrush textBrush = new SolidBrush(foreColor))
                {
                    object item = combo.Items[e.Index];
                    ListComboPopulationService.ListComboItem listItem = item as ListComboPopulationService.ListComboItem;
                    int textLeft = e.Bounds.X + 4;
                    if (listItem != null && listItem.Icon != null)
                    {
                        int iconSize = System.Math.Min(18, System.Math.Max(12, e.Bounds.Height - 4));
                        Rectangle iconBounds = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + ((e.Bounds.Height - iconSize) / 2), iconSize, iconSize);
                        e.Graphics.DrawImage(listItem.Icon, iconBounds);
                        textLeft = iconBounds.Right + 6;
                    }

                    Rectangle textBounds = new Rectangle(textLeft, e.Bounds.Y, e.Bounds.Right - textLeft - 4, e.Bounds.Height);
                    TextRenderer.DrawText(
                        e.Graphics,
                        item != null ? item.ToString() : string.Empty,
                        e.Font,
                        textBounds,
                        foreColor,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                }
            }
            catch
            {
            }
        }
    }
}
