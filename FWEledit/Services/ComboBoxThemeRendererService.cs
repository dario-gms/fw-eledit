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
                    Rectangle textBounds = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
                    TextRenderer.DrawText(
                        e.Graphics,
                        combo.Items[e.Index].ToString(),
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
