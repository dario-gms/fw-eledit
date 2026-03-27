using System;

namespace FWEledit
{
    public sealed class XrefItemUiService
    {
        public void ShowXrefForSelection(
            eListCollection listCollection,
            string[][] xrefs,
            int listIndex,
            int rowIndex,
            XrefLookupService lookupService,
            XrefLookupUiService uiService,
            Action<string> showMessage)
        {
            if (lookupService == null || uiService == null)
            {
                return;
            }

            XrefLookupResult result = lookupService.FindReferences(listCollection, xrefs, listIndex, rowIndex);
            uiService.ShowResults(
                result,
                listCollection != null ? listCollection.ConversationListIndex : -1,
                showMessage);
        }
    }
}
