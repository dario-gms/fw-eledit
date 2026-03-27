using System.Windows.Forms;

namespace FWEledit
{
    public sealed class QuestOverflowUiService
    {
        public void ShowReport(eListCollection listCollection, QuestOverflowService overflowService)
        {
            if (listCollection == null || overflowService == null)
            {
                return;
            }

            QuestOverflowReport report = overflowService.BuildReport(listCollection);
            LoseQuestWindow questWindow = new LoseQuestWindow();
            for (int i = 0; i < report.ReceiveItems.Count; i++)
            {
                questWindow.listBox_Receive.Items.Add(report.ReceiveItems[i]);
            }
            for (int i = 0; i < report.ActivateItems.Count; i++)
            {
                questWindow.listBox_Activate.Items.Add(report.ActivateItems[i]);
            }
            questWindow.Show();
        }
    }
}
