using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowListBoxCoordinatorService
    {
        public void HandleCopySelectedElement(
            MainWindowXrefUiService mainWindowXrefUiService,
            ClipboardUiService clipboardUiService,
            ElementClipboardService elementClipboardService,
            ISessionService sessionService,
            int listIndex,
            int rowIndex,
            Action<string> showMessage,
            Action<string> copyText)
        {
            if (mainWindowXrefUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowXrefUiService.CopySelectedElement(
                clipboardUiService,
                elementClipboardService,
                sessionService.ListCollection,
                listIndex,
                rowIndex,
                showMessage,
                copyText);
        }

        public void HandleListHover(
            ListItemHoverTooltipService listItemHoverTooltipService,
            ISessionService sessionService,
            int listIndex,
            DataGridView elementsGrid,
            DataGridViewCellEventArgs e,
            ListItemTooltipService listItemTooltipService,
            ref IToolType customTooltype)
        {
            if (listItemHoverTooltipService == null || sessionService == null)
            {
                return;
            }

            listItemHoverTooltipService.HandleCellMouseEnter(
                sessionService,
                listIndex,
                elementsGrid,
                e,
                listItemTooltipService,
                ref customTooltype);
        }
    }
}
