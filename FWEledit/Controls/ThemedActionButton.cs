using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit.Controls
{
    public sealed class ThemedActionButton : Control
    {
        private bool darkMode;

        public ThemedActionButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
                true);

            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Size = new Size(132, 28);
            MinimumSize = new Size(118, 28);
            Text = "Action";
        }

        public void ApplyTheme(bool useDarkMode)
        {
            darkMode = useDarkMode;
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Color back;
            Color border;
            Color text;

            if (!Enabled)
            {
                back = darkMode ? Color.FromArgb(37, 42, 50) : Color.FromArgb(229, 233, 239);
                border = darkMode ? Color.FromArgb(53, 60, 72) : Color.FromArgb(206, 213, 222);
                text = darkMode ? Color.FromArgb(138, 148, 163) : Color.FromArgb(124, 134, 146);
            }
            else if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                back = darkMode ? Color.FromArgb(58, 72, 95) : Color.FromArgb(221, 229, 239);
                border = darkMode ? Color.FromArgb(89, 116, 150) : Color.FromArgb(160, 181, 206);
                text = darkMode ? Color.FromArgb(244, 247, 251) : Color.FromArgb(29, 36, 45);
            }
            else
            {
                back = darkMode ? Color.FromArgb(48, 61, 82) : Color.FromArgb(232, 237, 243);
                border = darkMode ? Color.FromArgb(74, 94, 122) : Color.FromArgb(199, 208, 219);
                text = darkMode ? Color.FromArgb(229, 234, 242) : Color.FromArgb(29, 36, 45);
            }

            e.Graphics.Clear(Parent != null ? Parent.BackColor : back);

            Rectangle bounds = ClientRectangle;
            bounds.Width -= 1;
            bounds.Height -= 1;

            using (SolidBrush brush = new SolidBrush(back))
            {
                e.Graphics.FillRectangle(brush, bounds);
            }

            using (Pen pen = new Pen(border))
            {
                e.Graphics.DrawRectangle(pen, bounds);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text ?? string.Empty,
                Font,
                bounds,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
