using System.Windows.Forms;

namespace FWEledit
{
    public partial class ReferencesWindow : Form
    {
        public ReferencesWindow()
		{
			InitializeComponent();
            viewModel = referencesWindowCoordinatorService.CreateViewModel();
		}
    }
}


