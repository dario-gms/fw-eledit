using System;
using System.Collections.Generic;
using System.Linq;

namespace FWEledit
{
    public sealed class ElementListMutationService
    {
        private readonly IdGenerationService idGenerationService;

        public ElementListMutationService(IdGenerationService idGenerationService)
        {
            this.idGenerationService = idGenerationService ?? throw new ArgumentNullException(nameof(idGenerationService));
        }

        public ElementDeleteResult DeleteItems(eListCollection listCollection, int listIndex, int[] selectedIndices)
        {
            ElementDeleteResult result = new ElementDeleteResult();
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return result;
            }

            if (listIndex == listCollection.ConversationListIndex)
            {
                result.IsConversationList = true;
                return result;
            }

            int[] normalized = NormalizeSelection(selectedIndices, listCollection.Lists[listIndex].elementValues.Length);
            if (normalized.Length == 0)
            {
                return result;
            }

            if (normalized.Length >= listCollection.Lists[listIndex].elementValues.Length)
            {
                result.DeleteAllBlocked = true;
                return result;
            }

            for (int i = normalized.Length - 1; i > -1; i--)
            {
                listCollection.Lists[listIndex].RemoveItem(normalized[i]);
            }

            result.Success = true;
            result.DeletedIndices = normalized;
            return result;
        }

        public ElementCloneResult CloneItems(eListCollection listCollection, int listIndex, int[] selectedIndices)
        {
            ElementCloneResult result = new ElementCloneResult();
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return result;
            }

            if (listIndex == listCollection.ConversationListIndex)
            {
                result.IsConversationList = true;
                return result;
            }

            int[] normalized = NormalizeSelection(selectedIndices, listCollection.Lists[listIndex].elementValues.Length);
            if (normalized.Length == 0)
            {
                return result;
            }

            int idFieldIndex = idGenerationService.GetIdFieldIndex(listCollection, listIndex);
            HashSet<int> usedIds = idGenerationService.BuildUsedIds(listCollection, listIndex, idFieldIndex);

            List<int> newRows = new List<int>();
            for (int i = 0; i < normalized.Length; i++)
            {
                int sourceRow = normalized[i];
                object[] clone = CloneElementValuesDeep(listCollection.Lists[listIndex].elementValues[sourceRow]);
                listCollection.Lists[listIndex].AddItem(clone);
                int newRow = listCollection.Lists[listIndex].elementValues.Length - 1;
                newRows.Add(newRow);

                if (idFieldIndex > -1)
                {
                    int sourceId;
                    if (!int.TryParse(listCollection.GetValue(listIndex, sourceRow, idFieldIndex), out sourceId))
                    {
                        sourceId = 1;
                    }
                    int newId = idGenerationService.GetNextUniqueId(usedIds, sourceId + 1);
                    listCollection.SetValue(listIndex, newRow, idFieldIndex, newId.ToString());
                }
            }

            result.Success = true;
            result.NewIndices = newRows.ToArray();
            return result;
        }

        public ElementMoveResult MoveItemsToTop(eListCollection listCollection, int listIndex, int[] selectedIndices)
        {
            ElementMoveResult result = new ElementMoveResult();
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return result;
            }

            if (listIndex == listCollection.ConversationListIndex)
            {
                result.IsConversationList = true;
                return result;
            }

            int[] normalized = NormalizeSelection(selectedIndices, listCollection.Lists[listIndex].elementValues.Length);
            if (normalized.Length == 0)
            {
                return result;
            }

            if (normalized[0] <= 0)
            {
                return result;
            }

            object[][] source = listCollection.Lists[listIndex].elementValues;
            object[][] reordered = new object[source.Length][];
            int pos = 0;
            for (int i = 0; i < normalized.Length; i++)
            {
                reordered[pos++] = source[normalized[i]];
            }

            HashSet<int> selected = new HashSet<int>(normalized);
            for (int i = 0; i < source.Length; i++)
            {
                if (!selected.Contains(i))
                {
                    reordered[pos++] = source[i];
                }
            }

            listCollection.Lists[listIndex].elementValues = reordered;
            result.Success = true;
            result.NewSelectedIndices = Enumerable.Range(0, normalized.Length).ToArray();
            return result;
        }

        public ElementMoveResult MoveItemsToEnd(eListCollection listCollection, int listIndex, int[] selectedIndices)
        {
            ElementMoveResult result = new ElementMoveResult();
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return result;
            }

            if (listIndex == listCollection.ConversationListIndex)
            {
                result.IsConversationList = true;
                return result;
            }

            int[] normalized = NormalizeSelection(selectedIndices, listCollection.Lists[listIndex].elementValues.Length);
            if (normalized.Length == 0)
            {
                return result;
            }

            if (normalized[normalized.Length - 1] >= listCollection.Lists[listIndex].elementValues.Length - 1)
            {
                return result;
            }

            object[][] source = listCollection.Lists[listIndex].elementValues;
            object[][] reordered = new object[source.Length][];
            HashSet<int> selected = new HashSet<int>(normalized);
            int pos = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (!selected.Contains(i))
                {
                    reordered[pos++] = source[i];
                }
            }

            int start = pos;
            for (int i = 0; i < normalized.Length; i++)
            {
                reordered[pos++] = source[normalized[i]];
            }

            listCollection.Lists[listIndex].elementValues = reordered;
            result.Success = true;
            result.NewSelectedIndices = Enumerable.Range(start, normalized.Length).ToArray();
            return result;
        }

        private int[] NormalizeSelection(int[] selectedIndices, int count)
        {
            if (selectedIndices == null || selectedIndices.Length == 0 || count <= 0)
            {
                return new int[0];
            }

            List<int> filtered = new List<int>();
            for (int i = 0; i < selectedIndices.Length; i++)
            {
                int index = selectedIndices[i];
                if (index < 0 || index >= count)
                {
                    continue;
                }
                filtered.Add(index);
            }

            if (filtered.Count == 0)
            {
                return new int[0];
            }

            int[] arr = filtered.Distinct().ToArray();
            Array.Sort(arr);
            return arr;
        }

        private static object[] CloneElementValuesDeep(object[] source)
        {
            if (source == null)
            {
                return new object[0];
            }

            object[] clone = new object[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                byte[] bytes = source[i] as byte[];
                if (bytes != null)
                {
                    byte[] copy = new byte[bytes.Length];
                    Array.Copy(bytes, copy, bytes.Length);
                    clone[i] = copy;
                }
                else
                {
                    clone[i] = source[i];
                }
            }
            return clone;
        }
    }
}
