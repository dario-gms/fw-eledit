using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowXrefUiService
    {
        public void CopySelectedElement(
            ClipboardUiService clipboardUiService,
            ElementClipboardService clipboardService,
            eListCollection listCollection,
            int listIndex,
            int rowIndex,
            Action<string> showMessage,
            Action<string> setClipboard)
        {
            if (clipboardUiService == null)
            {
                return;
            }

            clipboardUiService.CopySelectedElement(
                listCollection,
                listIndex,
                rowIndex,
                clipboardService,
                showMessage,
                setClipboard);
        }

        public void ShowXrefForSelection(
            XrefItemUiService xrefItemUiService,
            eListCollection listCollection,
            string[][] xrefs,
            int listIndex,
            int rowIndex,
            XrefLookupService xrefLookupService,
            XrefLookupUiService xrefLookupUiService,
            Action<string> showMessage)
        {
            if (xrefItemUiService == null)
            {
                return;
            }

            xrefItemUiService.ShowXrefForSelection(
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
