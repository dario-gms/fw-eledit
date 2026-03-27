using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed partial class IconPickerWindow : Form
    {
        private void grid_SelectedIndexChanged(object sender, EventArgs e)
        {
            iconPickerCoordinatorService.UpdateSelectionStatus(viewModel, grid, statusLabel);
        }


        private void grid_ItemDoubleClick(object sender, EventArgs e)
        {
            CommitSelection();
        }


        private void okButton_Click(object sender, EventArgs e)
        {
            CommitSelection();
        }


        private void CommitSelection()
        {
            iconPickerCoordinatorService.CommitSelection(
                viewModel,
                grid,
                value => SelectedPathId = value,
                result => DialogResult = result,
                Close);
        }


        private void IconPickerWindow_KeyDown(object sender, KeyEventArgs e)
        {
            iconPickerCoordinatorService.HandleKeyDown(
                e,
                () =>
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                });
        }


        private string BuildTooltip(IconEntryModel entry)
        {
            return iconPickerCoordinatorService.BuildTooltip(viewModel, entry);
        }
    }
}

