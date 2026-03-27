using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowSelectionUiService
    {
        public void ChangeList(
            bool enableSelectionList,
            ComboBox listCombo,
            eListCollection listCollection,
            Action<int> updateEquipmentTabsVisibility,
            ListSelectionCommandService listSelectionCommandService,
            ListSelectionRequestBuilderService requestBuilderService,
            ListSelectionWorkflowService workflowService,
            ListSelectionUiService selectionUiService,
            eListConversation conversationList,
            CacheSave database,
            string[][] xrefs,
            ListDisplayService listDisplayService,
            ListRowBuilderService listRowBuilderService,
            Func<int, int, int, string> composeDisplayName,
            DataGridView elementsGrid,
            DataGridView valuesGrid,
            TextBox offsetBox,
            ToolStripMenuItem xrefItemMenuItem,
            Action<int, int, DataGridViewRow> applyItemQualityColor,
            Action updateDescriptionTab,
            Action updatePickIconButtonState,
            Action persistNavigationState)
        {
            if (listCombo == null)
            {
                return;
            }

            int listIndex = listCombo.SelectedIndex;
            if (listSelectionCommandService == null)
            {
                return;
            }
            listSelectionCommandService.HandleListChange(
                enableSelectionList,
                listIndex,
                listCollection,
                updateEquipmentTabsVisibility,
                requestBuilderService,
                workflowService,
                selectionUiService,
                conversationList,
                database,
                xrefs,
                listDisplayService,
                listRowBuilderService,
                composeDisplayName,
                elementsGrid,
                valuesGrid,
                offsetBox,
                xrefItemMenuItem,
                applyItemQualityColor,
                updateDescriptionTab,
                updatePickIconButtonState,
                persistNavigationState);
        }

        public void ChangeItem(
            bool enableSelectionItem,
            ComboBox listCombo,
            DataGridView elementsGrid,
            DataGridView valuesGrid,
            GridActiveRowService gridActiveRowService,
            GridSelectionService gridSelectionService,
            Func<int, int, int> resolveElementIndexFromGridRow,
            ItemSelectionContextService contextService,
            ItemSelectionRequestBuilderService requestBuilderService,
            ISessionService sessionService,
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            Func<int, string, bool> shouldIncludeField,
            Func<int, int, int, string> getDisplayEntryName,
            Func<Dictionary<int, string>> loadAddonTypeHints,
            Func<string, bool> isModelFieldName,
            Func<int, int, int, bool> isFieldInvalid,
            Func<int, int, int, bool> isFieldDirty,
            ItemSelectionCommandService commandService,
            ItemSelectionWorkflowService workflowService,
            ItemSelectionUiService uiService,
            Action resetProcType,
            Action updateDescriptionTab,
            Action updatePickIconButtonState,
            Action persistNavigationState,
            Action<bool> setSuppressValuesUiRefresh)
        {
            if (contextService == null || listCombo == null)
            {
                return;
            }

            ItemSelectionContext context = contextService.BuildContext(
                enableSelectionItem,
                listCombo.SelectedIndex,
                elementsGrid,
                valuesGrid,
                gridActiveRowService,
                gridSelectionService,
                resolveElementIndexFromGridRow);
            if (context == null)
            {
                return;
            }

            ItemSelectionRequest request = requestBuilderService.Build(
                sessionService,
                listCollection,
                conversationList,
                database,
                context,
                shouldIncludeField,
                getDisplayEntryName,
                loadAddonTypeHints,
                isModelFieldName,
                isFieldInvalid,
                isFieldDirty);
            if (request == null)
            {
                return;
            }

            try
            {
                commandService.HandleItemChange(
                    context,
                    workflowService,
                    request,
                    uiService,
                    valuesGrid,
                    resetProcType,
                    updateDescriptionTab,
                    updatePickIconButtonState,
                    persistNavigationState,
                    setSuppressValuesUiRefresh);
            }
            catch
            {
            }
        }

        public bool ChangeOffset(
            OffsetChangeUiService offsetChangeUiService,
            OffsetUpdateService offsetUpdateService,
            eListCollection listCollection,
            ComboBox listCombo,
            string offsetText)
        {
            if (offsetChangeUiService == null || listCombo == null)
            {
                return false;
            }

            return offsetChangeUiService.ApplyOffset(
                offsetUpdateService,
                listCollection,
                listCombo.SelectedIndex,
                offsetText);
        }

        public void ChangeValue(
            ValueChangeCommandService valueChangeCommandService,
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            ComboBox listCombo,
            DataGridView valuesGrid,
            DataGridView elementsGrid,
            DataGridViewCellEventArgs args,
            GridActiveRowService gridActiveRowService,
            GridSelectionService gridSelectionService,
            ValueChangeSelectionService selectionService,
            ValueChangeRequestBuilderService requestBuilderService,
            ValueChangeService valueChangeService,
            ValueCompatibilityService compatibilityService,
            ListDisplayService listDisplayService,
            Func<int, int, int, string> composeDisplayName,
            Func<int, int, int> resolveElementIndexFromGridRow,
            Func<int, int> getFieldIndexForValueRow,
            Func<string, bool> isModelFieldName,
            Action<int, int> remapDescriptionIdIfNeeded,
            Action resetList0DisplayCache,
            Action<int, int> markRowDirty,
            Action<int, int, int> markFieldDirty,
            Action<int, int, int> markFieldInvalid,
            Action<int, int, int> clearFieldInvalid,
            ValueChangeResultUiService resultUiService,
            Action<int, int, DataGridViewRow> applyItemQualityColorToRow,
            Action markUnsaved,
            Action<string> showMessage,
            Action<string, Exception> logError,
            ref bool enableSelectionItem)
        {
            if (valueChangeCommandService == null || listCombo == null)
            {
                return;
            }

            valueChangeCommandService.HandleValueChange(
                listCollection,
                conversationList,
                database,
                listCombo,
                valuesGrid,
                elementsGrid,
                args,
                gridActiveRowService,
                gridSelectionService,
                selectionService,
                requestBuilderService,
                valueChangeService,
                compatibilityService,
                listDisplayService,
                composeDisplayName,
                resolveElementIndexFromGridRow,
                getFieldIndexForValueRow,
                isModelFieldName,
                remapDescriptionIdIfNeeded,
                resetList0DisplayCache,
                markRowDirty,
                markFieldDirty,
                markFieldInvalid,
                clearFieldInvalid,
                resultUiService,
                applyItemQualityColorToRow,
                markUnsaved,
                showMessage,
                logError,
                ref enableSelectionItem);
        }

        public void ChangeValue(
            ValueChangeCommandService valueChangeCommandService,
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            ComboBox listCombo,
            DataGridView valuesGrid,
            DataGridView elementsGrid,
            DataGridViewCellEventArgs args,
            GridActiveRowService gridActiveRowService,
            GridSelectionService gridSelectionService,
            ValueChangeSelectionService selectionService,
            ValueChangeRequestBuilderService requestBuilderService,
            ValueChangeService valueChangeService,
            ValueCompatibilityService compatibilityService,
            ListDisplayService listDisplayService,
            Func<int, int, int, string> composeDisplayName,
            Func<int, int, int> resolveElementIndexFromGridRow,
            Func<int, int> getFieldIndexForValueRow,
            Func<string, bool> isModelFieldName,
            Action<int, int> remapDescriptionIdIfNeeded,
            Action resetList0DisplayCache,
            Action<int, int> markRowDirty,
            Action<int, int, int> markFieldDirty,
            Action<int, int, int> markFieldInvalid,
            Action<int, int, int> clearFieldInvalid,
            ValueChangeResultUiService resultUiService,
            Action<int, int, DataGridViewRow> applyItemQualityColorToRow,
            Action markUnsaved,
            Action<string> showMessage,
            Action<string, Exception> logError,
            MainWindowViewModel viewModel)
        {
            if (valueChangeCommandService == null || listCombo == null)
            {
                return;
            }

            valueChangeCommandService.HandleValueChange(
                listCollection,
                conversationList,
                database,
                listCombo,
                valuesGrid,
                elementsGrid,
                args,
                gridActiveRowService,
                gridSelectionService,
                selectionService,
                requestBuilderService,
                valueChangeService,
                compatibilityService,
                listDisplayService,
                composeDisplayName,
                resolveElementIndexFromGridRow,
                getFieldIndexForValueRow,
                isModelFieldName,
                remapDescriptionIdIfNeeded,
                resetList0DisplayCache,
                markRowDirty,
                markFieldDirty,
                markFieldInvalid,
                clearFieldInvalid,
                resultUiService,
                applyItemQualityColorToRow,
                markUnsaved,
                showMessage,
                logError,
                viewModel);
        }
    }
}
