using System.Windows.Forms;

namespace FWEledit
{
    partial class About : Form
    {
        private readonly AboutViewModel viewModel;
        private readonly AboutCoordinatorService aboutCoordinatorService = new AboutCoordinatorService();

    }
}

