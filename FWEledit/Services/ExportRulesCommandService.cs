using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ExportRulesCommandService
    {
        public void ExportRules(
            RulesExportUiService exportUiService,
            ElementsRulesExportWorkflowService workflowService,
            eListCollection listCollection,
            ToolStripMenuItem menuItem)
        {
            if (exportUiService == null || workflowService == null)
            {
                return;
            }

            exportUiService.ExportRules(workflowService, listCollection, menuItem);
        }
    }
}
