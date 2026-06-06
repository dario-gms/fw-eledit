using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemSelectionCommandService
    {
        public void HandleItemChange(
            ItemSelectionContext context,
            ItemSelectionWorkflowService workflowService,
            ItemSelectionRequest request,
            ItemSelectionUiService uiService,
            DataGridView itemGrid,
            Action resetProctypeLocations,
            Action updateDescription,
            Action updatePickIcon,
            Action persistNavigation,
            Action<bool> setSuppressValuesUiRefresh)
        {
            if (context == null || request == null || uiService == null || itemGrid == null || workflowService == null)
            {
                return;
            }

            try
            {
                setSuppressValuesUiRefresh?.Invoke(true);
                using (new ControlRedrawScope(itemGrid))
                {
                    itemGrid.Rows.Clear();

                    resetProctypeLocations?.Invoke();

                    uiService.ApplySelection(workflowService, request, itemGrid, context.ScrollIndex);
                }
            }
            finally
            {
                setSuppressValuesUiRefresh?.Invoke(false);
            }

            updateDescription?.Invoke();
            updatePickIcon?.Invoke();
            persistNavigation?.Invoke();
        }
    }
}
