using System;
using System.Windows.Forms;

namespace FWEledit
{
    partial class About : Form
    {
        private void okButton_Click(object sender, EventArgs e)
        {
            aboutCoordinatorService.HandleClose(this);
        }
    }
}


