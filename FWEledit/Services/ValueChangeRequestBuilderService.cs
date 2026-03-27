using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ValueChangeRequestBuilderService
    {
        public ValueChangeContext Build(
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            int listIndex,
            int currentElementIndex,
            int currentGridRow,
            int gridRow,
            int fieldIndex,
            DataGridView elementGrid,
            DataGridView itemGrid,
            GridSelectionService selectionService,
            Func<int, int, int> resolveElementIndexFromGridRow,
            Func<string, bool> isModelField,
            ValueCompatibilityService compatibilityService,
            Func<int, int, int, string> composeListDisplayName,
            Action<int, int> remapDescriptionId,
            Action resetList0DisplayCache,
            Action<int, int> markRowDirty,
            Action<int, int, int> markFieldDirty,
            Action<int, int, int> markFieldInvalid,
            Action<int, int, int> clearFieldInvalid)
        {
            if (listCollection == null || itemGrid == null || elementGrid == null || selectionService == null || resolveElementIndexFromGridRow == null)
            {
                return null;
            }

            int[] selGridIndices = selectionService.GetSelectedIndices(elementGrid);
            if (selGridIndices.Length == 0)
            {
                selGridIndices = new int[] { currentGridRow };
            }
            if (selGridIndices.Length == 0 || selGridIndices[0] < 0)
            {
                return null;
            }

            int[] selElementIndices = new int[selGridIndices.Length];
            for (int i = 0; i < selGridIndices.Length; i++)
            {
                selElementIndices[i] = resolveElementIndexFromGridRow(listIndex, selGridIndices[i]);
                if (selElementIndices[i] < 0)
                {
                    selElementIndices[i] = selGridIndices[i];
                }
            }

            string editedField = Convert.ToString(itemGrid.Rows[gridRow].Cells[0].Value);
            string fieldType = string.Empty;
            if (listIndex >= 0
                && listIndex < listCollection.Lists.Length
                && fieldIndex >= 0
                && fieldIndex < listCollection.Lists[listIndex].elementTypes.Length)
            {
                fieldType = listCollection.Lists[listIndex].elementTypes[fieldIndex];
            }
            if (string.IsNullOrWhiteSpace(fieldType))
            {
                fieldType = Convert.ToString(itemGrid.Rows[gridRow].Cells[1].Value);
            }
            string valueToSet = Convert.ToString(itemGrid.Rows[gridRow].Cells[2].Value);

            Func<string, string, bool> isValueCompatible = null;
            if (compatibilityService != null)
            {
                isValueCompatible = compatibilityService.IsValueCompatible;
            }

            ValueChangeRequest request = new ValueChangeRequest
            {
                ListCollection = listCollection,
                ConversationList = conversationList,
                Database = database,
                ListIndex = listIndex,
                FieldIndex = fieldIndex,
                CurrentElementIndex = currentElementIndex,
                SelectedElementIndices = selElementIndices,
                SelectedGridIndices = selGridIndices,
                FieldName = editedField,
                FieldType = fieldType,
                NewValue = valueToSet,
                IsModelField = isModelField != null && isModelField(editedField),
                IsIdEdit = string.Equals(editedField, "id", StringComparison.OrdinalIgnoreCase),
                IsValueCompatible = isValueCompatible,
                ComposeListDisplayName = composeListDisplayName,
                RemapDescriptionId = remapDescriptionId,
                ResetList0DisplayCache = resetList0DisplayCache,
                MarkRowDirty = markRowDirty,
                MarkFieldDirty = markFieldDirty,
                MarkFieldInvalid = markFieldInvalid,
                ClearFieldInvalid = clearFieldInvalid
            };

            return new ValueChangeContext
            {
                Request = request,
                EditedField = editedField ?? string.Empty,
                ListIndex = listIndex,
                ElementIndex = currentElementIndex,
                FieldIndex = fieldIndex,
                GridRow = gridRow
            };
        }
    }
}
