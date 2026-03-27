using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class LoseQuestWindowCoordinatorService
    {
        public void HandleQuestSelection(
            LoseQuestWindowViewModel viewModel,
            ListBox listBox,
            WebBrowser browser)
        {
            if (viewModel == null || listBox == null || browser == null)
            {
                return;
            }

            if (listBox.SelectedIndex <= -1 || listBox.SelectedItem == null)
            {
                return;
            }

            string selection = listBox.SelectedItem.ToString();
            if (selection.StartsWith("+"))
            {
                return;
            }

            Uri url = viewModel.BuildQuestUrl(selection);
            if (url != null)
            {
                browser.Url = url;
            }
        }
    }
}
