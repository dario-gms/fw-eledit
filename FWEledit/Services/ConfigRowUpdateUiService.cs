using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigRowUpdateUiService
    {
        public void ApplyChange(
            ConfigData data,
            int listIndex,
            int rowIndex,
            int columnIndex,
            DataGridView grid,
            IDialogService dialogService,
            IWin32Window owner)
        {
            if (data == null || grid == null || listIndex < 0 || rowIndex < 0 || columnIndex < 0)
            {
                return;
            }

            try
            {
                object value = grid[columnIndex, rowIndex].Value;
                string text = value != null ? value.ToString() : string.Empty;

                if (columnIndex == 0 && data.FieldNames != null && listIndex < data.FieldNames.Length)
                {
                    data.FieldNames[listIndex][rowIndex] = text;
                }
                if (columnIndex == 1 && data.FieldTypes != null && listIndex < data.FieldTypes.Length)
                {
                    data.FieldTypes[listIndex][rowIndex] = text;
                }
            }
            catch
            {
                ShowChangeError(dialogService, owner);
            }
        }

        private void ShowChangeError(IDialogService dialogService, IWin32Window owner)
        {
            string message = "CHANGING ERROR!\nFailed changing value, this value seems to be invalid.";
            string title = Application.ProductName;
            if (dialogService != null)
            {
                dialogService.ShowMessage(message, title, MessageBoxButtons.OK, MessageBoxIcon.None, owner);
                return;
            }

            if (owner != null)
            {
                MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.None);
                return;
            }

            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
