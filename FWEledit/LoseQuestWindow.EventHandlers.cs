using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class LoseQuestWindow : Form
    {
        private void select_quest(object sender, EventArgs e)
		{
            loseQuestCoordinatorService.HandleQuestSelection(
                viewModel,
                sender as ListBox,
                webBrowser);
		}
    }
}


