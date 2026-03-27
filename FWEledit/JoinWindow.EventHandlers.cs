using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class JoinWindow : Form
    {
        private void click_OK(object sender, EventArgs e)
		{
			joinWindowCoordinatorService.HandleOk(this);
		}


        private void click_BrowseEL(object sender, EventArgs e)
		{
            joinWindowCoordinatorService.HandleBrowseElement(
                joinWindowDialogUiService,
                dialogService,
                textBox_ElementFile,
                Environment.CurrentDirectory,
                this);
		}


        private void click_BrowseLog(object sender, EventArgs e)
		{
            joinWindowCoordinatorService.HandleBrowseLog(
                joinWindowDialogUiService,
                folderDialogService,
                textBox_LogDir);
		}


        private void click_Cancel(object sender, EventArgs e)
		{
			joinWindowCoordinatorService.HandleCancel(this);
		}
    }
}


