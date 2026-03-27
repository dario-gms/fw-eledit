using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ComboBoxThemeRendererService
    {
        public void DrawItem(ComboBox combo, DrawItemEventArgs e, IList<string> theme)
        {
            if (combo == null || e == null || theme == null || theme.Count <= 10)
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
                Color backColor = Color.FromName(selected ? theme[8] : theme[7]);
                Color foreColor = Color.FromName(selected ? theme[10] : theme[9]);

                using (SolidBrush backBrush = new SolidBrush(backColor))
                {
                    e.Graphics.FillRectangle(backBrush, e.Bounds);
                }
                using (SolidBrush textBrush = new SolidBrush(foreColor))
                {
                    e.Graphics.DrawString(combo.Items[e.Index].ToString(), e.Font, textBrush, new Point(e.Bounds.X, e.Bounds.Y));
                }
            }
            catch
            {
            }
        }
    }
}
