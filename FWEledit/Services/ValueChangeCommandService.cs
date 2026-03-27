using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ValueChangeCommandService
    {
        public void HandleValueChange(
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            ComboBox listComboBox,
            DataGridView itemGrid,
            DataGridView elementGrid,
            DataGridViewCellEventArgs ea,
            GridActiveRowService gridActiveRowService,
            GridSelectionService gridSelectionService,
            ValueChangeSelectionService selectionService,
            ValueChangeRequestBuilderService requestBuilderService,
            ValueChangeService valueChangeService,
            ValueCompatibilityService compatibilityService,
            ListDisplayService listDisplayService,
            Func<int, int, int, string> composeListDisplayName,
            Func<int, int, int> resolveElementIndexFromGridRow,
            Func<int, int> getFieldIndexForValueRow,
            Func<string, bool> isModelFieldName,
            Action<int, int> remapDescriptionId,
            Action resetList0DisplayCache,
            Action<int, int> markRowDirty,
            Action<int, int, int> markFieldDirty,
            Action<int, int, int> markFieldInvalid,
            Action<int, int, int> clearFieldInvalid,
            ValueChangeResultUiService resultUiService,
            Action<int, int, DataGridViewRow> applyQualityColor,
            Action markUnsavedChanges,
            Action<string> showMessage,
            Action<string, Exception> logError,
            ref bool enableSelectionItem)
        {
            if (listCollection == null || ea == null || selectionService == null || requestBuilderService == null || valueChangeService == null || resultUiService == null)
            {
                return;
            }

            ValueChangeSelectionContext selection = selectionService.BuildSelection(
                listCollection,
                listComboBox,
                itemGrid,
                elementGrid,
                ea,
                gridActiveRowService,
                gridSelectionService,
                getFieldIndexForValueRow,
                resolveElementIndexFromGridRow);
            if (selection == null)
            {
                return;
            }

            try
            {
                enableSelectionItem = false;

                ValueChangeContext context = requestBuilderService.Build(
                    listCollection,
                    conversationList,
                    database,
                    selection.ListIndex,
                    selection.ElementIndex,
                    selection.CurrentGridRow,
                    selection.GridRow,
                    selection.FieldIndex,
                    elementGrid,
                    itemGrid,
                    gridSelectionService,
                    resolveElementIndexFromGridRow,
                    isModelFieldName,
                    compatibilityService,
                    composeListDisplayName,
                    remapDescriptionId,
                    resetList0DisplayCache,
                    markRowDirty,
                    markFieldDirty,
                    markFieldInvalid,
                    clearFieldInvalid);

                if (context == null)
                {
                    return;
                }

                ValueChangeResult result = valueChangeService.Apply(context.Request);
                resultUiService.ApplyResult(
                    result,
                    itemGrid,
                    elementGrid,
                    context,
                    listCollection,
                    conversationList,
                    applyQualityColor,
                    markUnsavedChanges,
                    showMessage);
            }
            catch (Exception ex)
            {
                if (logError != null)
                {
                    logError("change_value", ex);
                }
                if (showMessage != null)
                {
                    showMessage("CHANGING ERROR!\nFailed changing value, this value seems to be invalid.");
                }
            }
            finally
            {
                enableSelectionItem = true;
            }
        }

        public void HandleValueChange(
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            ComboBox listComboBox,
            DataGridView itemGrid,
            DataGridView elementGrid,
            DataGridViewCellEventArgs ea,
            GridActiveRowService gridActiveRowService,
            GridSelectionService gridSelectionService,
            ValueChangeSelectionService selectionService,
            ValueChangeRequestBuilderService requestBuilderService,
            ValueChangeService valueChangeService,
            ValueCompatibilityService compatibilityService,
            ListDisplayService listDisplayService,
            Func<int, int, int, string> composeListDisplayName,
            Func<int, int, int> resolveElementIndexFromGridRow,
            Func<int, int> getFieldIndexForValueRow,
            Func<string, bool> isModelFieldName,
            Action<int, int> remapDescriptionId,
            Action resetList0DisplayCache,
            Action<int, int> markRowDirty,
            Action<int, int, int> markFieldDirty,
            Action<int, int, int> markFieldInvalid,
            Action<int, int, int> clearFieldInvalid,
            ValueChangeResultUiService resultUiService,
            Action<int, int, DataGridViewRow> applyQualityColor,
            Action markUnsavedChanges,
            Action<string> showMessage,
            Action<string, Exception> logError,
            MainWindowViewModel viewModel)
        {
            if (listCollection == null || ea == null || selectionService == null || requestBuilderService == null || valueChangeService == null || resultUiService == null)
            {
                return;
            }

            ValueChangeSelectionContext selection = selectionService.BuildSelection(
                listCollection,
                listComboBox,
                itemGrid,
                elementGrid,
                ea,
                gridActiveRowService,
                gridSelectionService,
                getFieldIndexForValueRow,
                resolveElementIndexFromGridRow);
            if (selection == null)
            {
                return;
            }

            try
            {
                if (viewModel != null)
                {
                    viewModel.EnableSelectionItem = false;
                }

                ValueChangeContext context = requestBuilderService.Build(
                    listCollection,
                    conversationList,
                    database,
                    selection.ListIndex,
                    selection.ElementIndex,
                    selection.CurrentGridRow,
                    selection.GridRow,
                    selection.FieldIndex,
                    elementGrid,
                    itemGrid,
                    gridSelectionService,
                    resolveElementIndexFromGridRow,
                    isModelFieldName,
                    compatibilityService,
                    composeListDisplayName,
                    remapDescriptionId,
                    resetList0DisplayCache,
                    markRowDirty,
                    markFieldDirty,
                    markFieldInvalid,
                    clearFieldInvalid);

                if (context == null)
                {
                    return;
                }

                ValueChangeResult result = valueChangeService.Apply(context.Request);
                resultUiService.ApplyResult(
                    result,
                    itemGrid,
                    elementGrid,
                    context,
                    listCollection,
                    conversationList,
                    applyQualityColor,
                    markUnsavedChanges,
                    showMessage);
            }
            catch (Exception ex)
            {
                if (logError != null)
                {
                    logError("change_value", ex);
                }
                if (showMessage != null)
                {
                    showMessage("CHANGING ERROR!\nFailed changing value, this value seems to be invalid.");
                }
            }
            finally
            {
                if (viewModel != null)
                {
                    viewModel.EnableSelectionItem = true;
                }
            }
        }
    }
}
