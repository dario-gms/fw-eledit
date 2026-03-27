using System.Windows.Forms;

namespace FWEledit
{
	public partial class LoseQuestWindow : Form
	{
        private readonly LoseQuestWindowViewModel viewModel;
        private readonly LoseQuestWindowCoordinatorService loseQuestCoordinatorService = new LoseQuestWindowCoordinatorService();

	}
}

