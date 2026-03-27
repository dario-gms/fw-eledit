using System.Windows.Forms;

namespace FWEledit
{
    public partial class LoseQuestWindow : Form
    {
        public LoseQuestWindow()
		{
			InitializeComponent();
            viewModel = new LoseQuestWindowViewModel();
		}
    }
}


