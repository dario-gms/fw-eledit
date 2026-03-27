using System.Windows.Forms;

namespace FWEledit
{
    public sealed class DebugWindowCoordinatorService
    {
        public void Initialize(
            Form owner,
            TextBox messageBox,
            string title,
            string message)
        {
            if (owner == null)
            {
                return;
            }

            owner.Text = title ?? string.Empty;
            if (messageBox != null)
            {
                messageBox.Text = message ?? string.Empty;
                messageBox.SelectionStart = 0;
                messageBox.SelectionLength = 0;
            }

            owner.Show();
        }
    }
}
