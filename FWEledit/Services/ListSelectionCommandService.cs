using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ListSelectionCommandService
    {
        public void HandleListChange(
            bool enableSelectionList,
            int selectedListIndex,
            bool useLightweightRows,
            eListCollection listCollection,
            Action<int> updateEquipmentTabs,
            ListSelectionRequestBuilderService requestBuilder,
            ListSelectionWorkflowService workflowService,
            ListSelectionUiService uiService,
            eListConversation conversationList,
            CacheSave database,
            string[][] xrefs,
            ListDisplayService listDisplayService,
            ListRowBuilderService listRowBuilderService,
            Func<int, int, int, string> composeDisplayName,
            DataGridView elementGrid,
            DataGridView itemGrid,
            TextBox offsetBox,
            ToolStripMenuItem xrefMenuItem,
            Action<int, int, DataGridViewRow> applyQualityColor,
            Action updateDescription,
            Action updatePickIcon,
            Action persistNavigation)
        {
            if (!enableSelectionList || selectedListIndex < 0)
            {
                return;
            }

            if (updateEquipmentTabs != null)
            {
                updateEquipmentTabs(selectedListIndex);
            }

            if (requestBuilder == null || workflowService == null || uiService == null)
            {
                return;
            }

            ListSelectionRequest request = requestBuilder.Build(
                listCollection,
                conversationList,
                database,
                selectedListIndex,
                xrefs,
                listDisplayService,
                listRowBuilderService,
                composeDisplayName,
                useLightweightRows);

            uiService.ApplySelection(
                workflowService,
                request,
                selectedListIndex,
                listCollection != null ? listCollection.ConversationListIndex : -1,
                elementGrid,
                itemGrid,
                offsetBox,
                xrefMenuItem,
                applyQualityColor,
                updateDescription,
                updatePickIcon,
                persistNavigation);
        }
    }
}
