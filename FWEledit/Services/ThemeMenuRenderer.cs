using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly Func<IList<string>> getTheme;

        public ThemeMenuRenderer(Func<IList<string>> getTheme)
        {
            this.getTheme = getTheme;
        }

        private IList<string> Theme
        {
            get
            {
                return getTheme != null ? getTheme() : null;
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            IList<string> theme = Theme;
            if (theme == null)
            {
                base.OnRenderMenuItemBackground(e);
                return;
            }

            if (!e.Item.Selected)
            {
                base.OnRenderMenuItemBackground(e);
                e.Item.BackColor = Color.FromName(theme[7]);
            }
            else
            {
                Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
                Brush myBrush = new SolidBrush(Color.FromName(theme[3]));
                e.Graphics.FillRectangle(myBrush, rc);
                Pen myPen = new Pen(Color.FromName(theme[2]));
                e.Graphics.DrawRectangle(myPen, 1, 0, rc.Width - 2, rc.Height - 1);
                e.Item.BackColor = Color.FromName(theme[3]);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            IList<string> theme = Theme;
            if (theme == null)
            {
                base.OnRenderSeparator(e);
                return;
            }

            Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
            Brush myBrush = new SolidBrush(Color.FromName(theme[3]));
            e.Graphics.FillRectangle(myBrush, rc);
            Pen myPen = new Pen(Color.FromName(theme[2]));
            e.Graphics.DrawRectangle(myPen, 1, 0, rc.Width - 2, rc.Height - 1);
            e.Item.BackColor = Color.FromName(theme[3]);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            IList<string> theme = Theme;
            if (theme == null)
            {
                base.OnRenderToolStripBorder(e);
                return;
            }

            Rectangle rc = new Rectangle(Point.Empty, e.ToolStrip.Size);
            Brush myBrush = new SolidBrush(Color.FromName(theme[3]));
            e.Graphics.FillRectangle(myBrush, rc);
            Pen myPen = new Pen(Color.FromName(theme[2]));
            e.Graphics.DrawRectangle(myPen, 1, 0, rc.Width - 2, rc.Height - 1);
            e.ToolStrip.BackColor = Color.FromName(theme[3]);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            IList<string> theme = Theme;
            if (theme == null)
            {
                base.OnRenderItemText(e);
                return;
            }

            base.OnRenderItemText(e);
            if (!e.Item.Selected)
            {
                e.Item.ForeColor = Color.FromName(theme[4]);
            }
            else
            {
                e.Item.ForeColor = Color.FromName(theme[5]);
            }
        }
    }
}
