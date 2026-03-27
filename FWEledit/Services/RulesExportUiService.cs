using System.Windows.Forms;

namespace FWEledit
{
    public sealed class RulesExportUiService
    {
        public void ExportRules(
            ElementsRulesExportWorkflowService workflow,
            eListCollection listCollection,
            ToolStripMenuItem menuItem)
        {
            if (workflow == null || listCollection == null || menuItem == null)
            {
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Elements File (*.data)|*.data|All Files (*.*)|*.*";
                if (dialog.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
                {
                    return;
                }

                string rulesLabel = menuItem.Text;
                Cursor.Current = Cursors.WaitCursor;
                FileExportResult result = workflow.ExportWithRules(listCollection, rulesLabel, dialog.FileName);
                Cursor.Current = Cursors.Default;
                if (!result.Success && !string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    MessageBox.Show(result.ErrorMessage);
                }
            }
        }
    }
}
