using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowTooltipCoordinatorService
    {
        public void HandleCellMouseMove(
            ItemTooltipUiService itemTooltipUiService,
            eListCollection listCollection,
            int listIndex,
            int rowIndex,
            DataGridView valuesGrid,
            DataGridView elementsGrid,
            DataGridViewCellMouseEventArgs e,
            Control owner,
            ToolTip toolTip,
            ItemTooltipService itemTooltipService,
            Func<int, int> getFieldIndexForRow,
            ref Point mouseMoveCheck)
        {
            if (itemTooltipUiService == null)
            {
                return;
            }

            itemTooltipUiService.HandleCellMouseMove(
                listCollection,
                listIndex,
                rowIndex,
                valuesGrid,
                elementsGrid,
                e,
                owner,
                toolTip,
                itemTooltipService,
                getFieldIndexForRow,
                ref mouseMoveCheck);
        }

        public void ShowXrefForSelection(
            MainWindowXrefUiService mainWindowXrefUiService,
            XrefItemUiService xrefItemUiService,
            eListCollection listCollection,
            string[][] xrefs,
            int listIndex,
            int rowIndex,
            XrefLookupService xrefLookupService,
            XrefLookupUiService xrefLookupUiService,
            Action<string> showMessage)
        {
            if (mainWindowXrefUiService == null)
            {
                return;
            }

            mainWindowXrefUiService.ShowXrefForSelection(
                xrefItemUiService,
                listCollection,
                xrefs,
                listIndex,
                rowIndex,
                xrefLookupService,
                xrefLookupUiService,
                showMessage);
        }
    }
}
