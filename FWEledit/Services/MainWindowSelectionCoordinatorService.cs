using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowSelectionCoordinatorService
    {
        public void HandleChangeList(
            MainWindowSelectionUiService selectionUiService,
            bool enableSelectionList,
            ComboBox listCombo,
            ISessionService sessionService,
            Action<int> updateEquipmentTabsVisibility,
            ListSelectionCommandService listSelectionCommandService,
            ListSelectionRequestBuilderService requestBuilderService,
            ListSelectionWorkflowService workflowService,
            ListSelectionUiService listSelectionUiService,
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
            if (selectionUiService == null || sessionService == null)
            {
                return;
            }

            selectionUiService.ChangeList(
                enableSelectionList,
                listCombo,
                sessionService.ListCollection,
                updateEquipmentTabsVisibility,
                listSelectionCommandService,
                requestBuilderService,
                workflowService,
                listSelectionUiService,
                sessionService.ConversationList,
                sessionService.Database,
                sessionService.Xrefs,
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

        public void HandleChangeItem(
            MainWindowSelectionUiService selectionUiService,
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
            Func<int, string, bool> shouldIncludeField,
            Func<int, int, int, string> getDisplayEntryName,
            Func<System.Collections.Generic.Dictionary<int, string>> loadAddonTypeHints,
            Func<string, bool> isModelFieldName,
            Func<int, int, int, bool> isFieldInvalid,
            Func<int, int, int, bool> isFieldDirty,
            ItemSelectionCommandService commandService,
            ItemSelectionWorkflowService workflowService,
            ItemSelectionUiService itemSelectionUiService,
            Action resetProcType,
            Action updateDescriptionTab,
            Action updatePickIconButtonState,
            Action persistNavigationState,
            Action<bool> setSuppressValuesUiRefresh)
        {
            if (selectionUiService == null || sessionService == null)
            {
                return;
            }

            selectionUiService.ChangeItem(
                enableSelectionItem,
                listCombo,
                elementsGrid,
                valuesGrid,
                gridActiveRowService,
                gridSelectionService,
                resolveElementIndexFromGridRow,
                contextService,
                requestBuilderService,
                sessionService,
                sessionService.ListCollection,
                sessionService.ConversationList,
                sessionService.Database,
                shouldIncludeField,
                getDisplayEntryName,
                loadAddonTypeHints,
                isModelFieldName,
                isFieldInvalid,
                isFieldDirty,
                commandService,
                workflowService,
                itemSelectionUiService,
                resetProcType,
                updateDescriptionTab,
                updatePickIconButtonState,
                persistNavigationState,
                setSuppressValuesUiRefresh);
        }

        public bool HandleChangeOffset(
            MainWindowSelectionUiService selectionUiService,
            OffsetChangeUiService offsetChangeUiService,
            OffsetUpdateService offsetUpdateService,
            ISessionService sessionService,
            ComboBox listCombo,
            string offsetText)
        {
            if (selectionUiService == null || sessionService == null)
            {
                return false;
            }

            return selectionUiService.ChangeOffset(
                offsetChangeUiService,
                offsetUpdateService,
                sessionService.ListCollection,
                listCombo,
                offsetText);
        }

        public void HandleChangeValue(
            MainWindowSelectionUiService selectionUiService,
            ValueChangeCommandService valueChangeCommandService,
            ISessionService sessionService,
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
            if (selectionUiService == null || sessionService == null)
            {
                return;
            }

            selectionUiService.ChangeValue(
                valueChangeCommandService,
                sessionService.ListCollection,
                sessionService.ConversationList,
                sessionService.Database,
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
