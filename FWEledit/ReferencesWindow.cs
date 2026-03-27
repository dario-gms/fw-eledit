using System.Windows.Forms;

namespace FWEledit
{
	public partial class ReferencesWindow : Form
	{
        private readonly ReferencesWindowViewModel viewModel;
        private readonly ReferencesWindowCoordinatorService referencesWindowCoordinatorService = new ReferencesWindowCoordinatorService();

	}
}

