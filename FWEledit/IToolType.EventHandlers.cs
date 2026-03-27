using System;
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


        private void fadeTimer_Tick(object sender, EventArgs e)
        {
            toolTypeCoordinatorService.HandleFadeTick(this, fadeTimer, 0.04);
        }
    }
}


