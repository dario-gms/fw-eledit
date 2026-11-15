using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    internal sealed class GameCaptureRegionOverlay : Form
    {
        private Point dragStart;
        private Rectangle selection;
        private bool dragging;

        public Rectangle SelectedRegion
        {
            get { return selection; }
        }

        public GameCaptureRegionOverlay(Rectangle screenBounds)
        {
            Bounds = screenBounds;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            DoubleBuffered = true;
            BackColor = Color.Black;
            Opacity = 0.42D;
            Cursor = Cursors.Cross;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }
            if (e.Button == MouseButtons.Left)
            {
                dragStart = e.Location;
                selection = Rectangle.Empty;
                dragging = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                selection = NormalizeRectangle(dragStart, e.Location);
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (dragging && e.Button == MouseButtons.Left)
            {
                dragging = false;
                selection = Rectangle.Intersect(ClientRectangle, NormalizeRectangle(dragStart, e.Location));
                if (selection.Width >= 24 && selection.Height >= 12)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }
                selection = Rectangle.Empty;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            string instructions = "Drag a rectangle around the character coordinates  •  Esc/right-click cancels";
            using (Font font = new Font("Segoe UI", 12F, FontStyle.Bold))
            using (Brush shadow = new SolidBrush(Color.FromArgb(210, 0, 0, 0)))
            using (Brush textBrush = new SolidBrush(Color.White))
            {
                SizeF textSize = e.Graphics.MeasureString(instructions, font);
                RectangleF box = new RectangleF(14, 14, textSize.Width + 24, textSize.Height + 14);
                e.Graphics.FillRectangle(shadow, box);
                e.Graphics.DrawString(instructions, font, textBrush, box.X + 12, box.Y + 7);
            }

            if (!selection.IsEmpty)
            {
                using (Brush fill = new SolidBrush(Color.FromArgb(75, 255, 0, 0)))
                using (Pen border = new Pen(Color.Red, 4F))
                {
                    e.Graphics.FillRectangle(fill, selection);
                    Rectangle borderRect = selection;
                    borderRect.Width = Math.Max(1, borderRect.Width - 1);
                    borderRect.Height = Math.Max(1, borderRect.Height - 1);
                    e.Graphics.DrawRectangle(border, borderRect);
                }
            }
        }

        private static Rectangle NormalizeRectangle(Point start, Point end)
        {
            int left = Math.Min(start.X, end.X);
            int top = Math.Min(start.Y, end.Y);
            return new Rectangle(left, top, Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
        }
    }
}
