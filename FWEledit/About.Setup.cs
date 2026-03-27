using System.Windows.Forms;

namespace FWEledit
{
    partial class About : Form
    {
        public About()
        {
            InitializeComponent();
            viewModel = new AboutViewModel();
            aboutCoordinatorService.ApplyViewModel(
                viewModel,
                this,
                labelProductName,
                labelVersion,
                labelCopyright,
                labelCompanyName,
                textBoxDescription);
        }
    }
}


