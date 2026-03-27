using System.Windows.Forms;

namespace FWEledit
{
    public sealed class InfoMessageService
    {
        public void ShowInfo(eListCollection listCollection, System.Action<string> showMessage)
        {
            if (listCollection == null)
            {
                if (showMessage != null)
                {
                    showMessage("No File Loaded!");
                }
                return;
            }
        }
    }
}
