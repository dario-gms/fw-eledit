using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class IToolType : Form
    {
        private void rtb_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            toolTypeCoordinatorService.HandleContentsResized(sender as RichTextBox, e);
        }


        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x204) return; // WM_RBUTTONDOWN
            if (m.Msg == 0x205) return; // WM_RBUTTONUP
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (e == null || e.Graphics == null)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.None;
            using (Pen border = new Pen(Color.FromArgb(74, 86, 102)))
            {
                Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
                e.Graphics.DrawRectangle(border, bounds);
            }
        }


        private void fadeTimer_Tick(object sender, EventArgs e)
        {
            toolTypeCoordinatorService.HandleFadeTick(this, fadeTimer, 0.04);
        }
    }
}


