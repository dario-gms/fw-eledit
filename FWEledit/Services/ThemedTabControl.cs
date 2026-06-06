using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ThemedTabControl : TabControl
    {
        private bool darkMode;

        public ThemedTabControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DrawMode = TabDrawMode.OwnerDrawFixed;
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(92, 24);
            Padding = new Point(10, 3);
        }

        public void ApplyTheme(bool useDarkMode)
        {
            darkMode = useDarkMode;
            BackColor = Surface;
            ForeColor = TextPrimary;
            foreach (TabPage page in TabPages)
            {
                page.BackColor = Surface;
                page.ForeColor = TextPrimary;
            }
            Invalidate();
        }

        private Color Surface
        {
            get { return darkMode ? Color.FromArgb(23, 26, 31) : Color.FromArgb(238, 241, 245); }
        }

        private Color SurfaceRaised
        {
            get { return darkMode ? Color.FromArgb(31, 35, 42) : Color.FromArgb(250, 251, 253); }
        }

        private Color TabIdle
        {
            get { return darkMode ? Color.FromArgb(26, 31, 38) : Color.FromArgb(232, 237, 243); }
        }

        private Color GridLine
        {
            get { return darkMode ? Color.FromArgb(48, 56, 68) : Color.FromArgb(211, 218, 226); }
        }

        private Color TextPrimary
        {
            get { return darkMode ? Color.FromArgb(229, 234, 242) : Color.FromArgb(29, 36, 45); }
        }

        private Color TextSecondary
        {
            get { return darkMode ? Color.FromArgb(156, 169, 187) : Color.FromArgb(83, 96, 112); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Surface);

            Rectangle strip = ClientRectangle;
            strip.Height = ItemSize.Height + 4;
            using (SolidBrush brush = new SolidBrush(Surface))
            {
                e.Graphics.FillRectangle(brush, strip);
            }

            for (int i = 0; i < TabPages.Count; i++)
            {
                DrawTab(e.Graphics, i);
            }

            Rectangle pageBounds = DisplayRectangle;
            pageBounds.Inflate(1, 1);
            using (Pen pen = new Pen(GridLine))
            {
                e.Graphics.DrawRectangle(pen, pageBounds);
            }
        }

        private void DrawTab(Graphics graphics, int index)
        {
            Rectangle bounds = GetTabRect(index);
            bool selected = index == SelectedIndex;
            Color back = selected ? SurfaceRaised : TabIdle;
            Color border = selected ? SurfaceRaised : GridLine;
            Color text = selected ? TextPrimary : TextSecondary;

            using (SolidBrush brush = new SolidBrush(back))
            {
                graphics.FillRectangle(brush, bounds);
            }
            using (Pen pen = new Pen(border))
            {
                Rectangle borderBounds = bounds;
                borderBounds.Width -= 1;
                borderBounds.Height -= 1;
                graphics.DrawRectangle(pen, borderBounds);
            }

            TextRenderer.DrawText(
                graphics,
                TabPages[index].Text,
                Font,
                bounds,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
