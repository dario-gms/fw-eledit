using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementImportCommandService
    {
        public bool ImportSingle(
            eListCollection listCollection,
            int listIndex,
            int rowIndex,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            System.Action<int> markRowDirty,
            System.Action refreshListAction,
            System.Action<int> selectRowAction,
            System.Action<int, int> remapDescriptionId = null)
        {
            if (listCollection == null || importExportUiService == null || workflowService == null)
            {
                return false;
            }

            if (!importExportUiService.ValidateNotConversationList(listCollection, listIndex))
            {
                return false;
            }

            if (listIndex < 0 || rowIndex < 0)
            {
                return false;
            }

            ElementImportResult result = importExportUiService.ImportSingleItem(workflowService, listCollection, listIndex, rowIndex);
            if (result == null || !result.Success)
            {
                return false;
            }

            if (result.IdChanged && result.OldId > 0 && result.NewId > 0 && remapDescriptionId != null)
            {
                remapDescriptionId(result.OldId, result.NewId);
            }

            if (markRowDirty != null)
            {
                markRowDirty(rowIndex);
            }
            if (refreshListAction != null)
            {
                refreshListAction();
            }
            if (selectRowAction != null)
            {
                selectRowAction(rowIndex);
            }

            return true;
        }
    }
}
