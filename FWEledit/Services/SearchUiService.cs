using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class SearchUiService
    {
        public bool TrySearchAndSelect(
            SearchWorkflowService workflowService,
            ElementSearchService searchService,
            eListCollection listCollection,
            string rawQuery,
            Func<string, bool> isPlaceholder,
            bool matchCase,
            bool exactMatch,
            bool searchAllFields,
            int currentListIndex,
            int currentElementIndex,
            int currentFieldIndex,
            ComboBox listComboBox,
            DataGridView elementGrid,
            DataGridView itemGrid,
            Action<int, string> ensureTabForField,
            Func<int, int> findValueRow,
            Action<bool> setEnableSelectionItem,
            Action<string> showMessage)
        {
            if (workflowService == null || searchService == null || listCollection == null || listCollection.Lists == null)
            {
                return false;
            }

            string query = rawQuery ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query) || (isPlaceholder != null && isPlaceholder(query)))
            {
                return false;
            }

            int listIndex = currentListIndex >= 0 ? currentListIndex : 0;
            int startFieldIndex = currentFieldIndex >= 0 ? currentFieldIndex + 1 : 0;
            if (startFieldIndex < 0)
            {
                startFieldIndex = 0;
            }

            int startElementIndex = currentElementIndex >= 0 ? currentElementIndex : 0;
            if (!searchAllFields)
            {
                startElementIndex += 1;
            }
            if (startElementIndex < 0)
            {
                startElementIndex = 0;
            }

            ElementSearchResult result = workflowService.FindNext(
                searchService,
                listCollection,
                query,
                matchCase,
                exactMatch,
                searchAllFields,
                listIndex,
                startElementIndex,
                startFieldIndex);

            if (result == null || !result.Found)
            {
                if (showMessage != null)
                {
                    showMessage("Search reached End without Result!");
                }
                return false;
            }

            if (listComboBox != null && result.ListIndex >= 0 && result.ListIndex < listComboBox.Items.Count)
            {
                if (setEnableSelectionItem != null)
                {
                    setEnableSelectionItem(false);
                }
                listComboBox.SelectedIndex = result.ListIndex;
                if (setEnableSelectionItem != null)
                {
                    setEnableSelectionItem(true);
                }
            }

            if (elementGrid != null && result.ElementIndex >= 0 && result.ElementIndex < elementGrid.Rows.Count)
            {
                elementGrid.ClearSelection();
                elementGrid.CurrentCell = elementGrid[0, result.ElementIndex];
                elementGrid.Rows[result.ElementIndex].Selected = true;
                elementGrid.FirstDisplayedScrollingRowIndex = result.ElementIndex;
            }

            if (result.FieldIndex >= 0
                && result.ListIndex >= 0
                && result.ListIndex < listCollection.Lists.Length
                && result.FieldIndex < listCollection.Lists[result.ListIndex].elementFields.Length)
            {
                if (ensureTabForField != null)
                {
                    ensureTabForField(result.ListIndex, listCollection.Lists[result.ListIndex].elementFields[result.FieldIndex]);
                }
                if (itemGrid != null && findValueRow != null)
                {
                    int valueRow = findValueRow(result.FieldIndex);
                    if (valueRow > -1 && valueRow < itemGrid.Rows.Count)
                    {
                        itemGrid.CurrentCell = itemGrid.Rows[valueRow].Cells[2];
                    }
                }
            }

            return true;
        }

        public void NavigateToListItem(
            eListCollection listCollection,
            int listIndex,
            int rowIndex,
            ComboBox listComboBox,
            DataGridView elementGrid,
            Action<bool> setEnableSelectionItem)
        {
            if (listCollection == null || listCollection.Lists == null)
            {
                return;
            }
            if (listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return;
            }

            if (listComboBox != null && listIndex < listComboBox.Items.Count)
            {
                if (setEnableSelectionItem != null)
                {
                    setEnableSelectionItem(false);
                }
                listComboBox.SelectedIndex = listIndex;
                if (setEnableSelectionItem != null)
                {
                    setEnableSelectionItem(true);
                }
            }

            if (elementGrid != null && elementGrid.Rows.Count > rowIndex)
            {
                elementGrid.ClearSelection();
                elementGrid.CurrentCell = elementGrid[0, rowIndex];
                elementGrid.Rows[rowIndex].Selected = true;
                elementGrid.FirstDisplayedScrollingRowIndex = rowIndex;
            }
        }
    }
}
