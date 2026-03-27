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

            setSuppressValuesUiRefresh?.Invoke(true);
            itemGrid.SuspendLayout();
            itemGrid.Rows.Clear();

            resetProctypeLocations?.Invoke();

            try
            {
                uiService.ApplySelection(workflowService, request, itemGrid, context.ScrollIndex);
            }
            finally
            {
                itemGrid.ResumeLayout();
                setSuppressValuesUiRefresh?.Invoke(false);
            }

            updateDescription?.Invoke();
            updatePickIcon?.Invoke();
            persistNavigation?.Invoke();
        }
    }
}
