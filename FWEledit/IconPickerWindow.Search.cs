using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed partial class IconPickerWindow : Form
    {
        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }


        private void ApplyFilter()
        {
            iconPickerCoordinatorService.ApplyFilter(viewModel, grid, statusLabel, searchBox.Text);
        }


        private void SelectPending()
        {
            iconPickerCoordinatorService.SelectPending(viewModel, grid);
        }
    }
}

