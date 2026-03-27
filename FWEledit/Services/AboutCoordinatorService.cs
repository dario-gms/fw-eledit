using System.Windows.Forms;

namespace FWEledit
{
    public sealed class AboutCoordinatorService
    {
        public void ApplyViewModel(
            AboutViewModel viewModel,
            Form owner,
            Label productLabel,
            Label versionLabel,
            Label copyrightLabel,
            Label companyLabel,
            TextBox descriptionBox)
        {
            if (viewModel == null || owner == null)
            {
                return;
            }

            owner.Text = string.Format("About {0}", viewModel.Title);
            if (productLabel != null)
            {
                productLabel.Text = viewModel.Product;
            }
            if (versionLabel != null)
            {
                versionLabel.Text = string.Format("Version {0}", viewModel.Version);
            }
            if (copyrightLabel != null)
            {
                copyrightLabel.Text = viewModel.Copyright;
            }
            if (companyLabel != null)
            {
                companyLabel.Text = viewModel.Company;
            }
            if (descriptionBox != null)
            {
                descriptionBox.Text = viewModel.Description;
            }
        }

        public void HandleClose(Form owner)
        {
            owner?.Close();
        }
    }
}
