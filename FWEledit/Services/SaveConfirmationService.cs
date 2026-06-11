using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class SaveConfirmationService
    {
        public void Show(string details)
        {
            string message = "Save completed.";
            if (!string.IsNullOrWhiteSpace(details))
            {
                message += "\n" + details.Trim();
            }
            MessageBox.Show(message, "FWEledit", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
