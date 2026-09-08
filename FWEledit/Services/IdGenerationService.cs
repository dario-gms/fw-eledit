using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class IdGenerationService
    {
        public int GetIdFieldIndex(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return -1;
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                string field = listCollection.Lists[listIndex].elementFields[i];
                if (string.Equals(field, "id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(field, "ID", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        public HashSet<int> BuildUsedIds(eListCollection listCollection, int listIndex, int idFieldIndex)
        {
            HashSet<int> used = new HashSet<int>();
            if (listCollection == null || idFieldIndex < 0 || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return used;
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementValues.Length; i++)
            {
                int id;
                if (int.TryParse(listCollection.GetValue(listIndex, i, idFieldIndex), out id))
                {
                    used.Add(id);
                }
            }
            return used;
        }

        public HashSet<int> BuildUsedIdsAcrossLists(eListCollection listCollection)
        {
            return BuildUsedIdsAcrossLists(listCollection, -1, -1);
        }

        public HashSet<int> BuildUsedIdsAcrossLists(eListCollection listCollection, int excludedListIndex, int excludedRowIndex)
        {
            HashSet<int> used = new HashSet<int>();
            if (listCollection == null || listCollection.Lists == null)
            {
                return used;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                int idFieldIndex = GetIdFieldIndex(listCollection, listIndex);
                if (idFieldIndex < 0)
                {
                    continue;
                }

                object[][] values = listCollection.Lists[listIndex].elementValues;
                if (values == null)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < values.Length; rowIndex++)
                {
                    if (listIndex == excludedListIndex && rowIndex == excludedRowIndex)
                    {
                        continue;
                    }

                    int id;
                    if (int.TryParse(listCollection.GetValue(listIndex, rowIndex, idFieldIndex), out id))
                    {
                        used.Add(id);
                    }
                }
            }

            return used;
        }

        public HashSet<int> BuildUsedIdsForElementNamespace(eListCollection listCollection, int listIndex, int excludedRowIndex)
        {
            if (ItemListCatalog.IsItemList(listCollection, listIndex))
            {
                return BuildUsedItemIdsAcrossLists(listCollection, listIndex, excludedRowIndex);
            }

            if (IsNpcRuntimeEssenceList(listCollection, listIndex))
            {
                return BuildUsedIdsAcrossLists(listCollection, listIndex, excludedRowIndex);
            }

            int idFieldIndex = GetIdFieldIndex(listCollection, listIndex);
            HashSet<int> used = BuildUsedIds(listCollection, listIndex, idFieldIndex);
            if (listCollection != null
                && listIndex >= 0
                && listIndex < listCollection.Lists.Length
                && excludedRowIndex >= 0
                && excludedRowIndex < listCollection.Lists[listIndex].elementValues.Length
                && idFieldIndex >= 0)
            {
                int currentId;
                if (int.TryParse(listCollection.GetValue(listIndex, excludedRowIndex, idFieldIndex), out currentId))
                {
                    used.Remove(currentId);
                }
            }

            return used;
        }

        private static bool IsNpcRuntimeEssenceList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }

            string listName = ItemListCatalog.NormalizeListName(listCollection.Lists[listIndex].listName);
            return string.Equals(listName, "NPC_ESSENCE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "NPC_TASK_IN_SERVICE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "NPC_TASK_OUT_SERVICE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "NPC_TASK_MATTER_SERVICE", StringComparison.OrdinalIgnoreCase);
        }

        public HashSet<int> BuildUsedItemIdsAcrossLists(eListCollection listCollection, int excludedListIndex, int excludedRowIndex)
        {
            HashSet<int> used = new HashSet<int>();
            if (listCollection == null || listCollection.Lists == null)
            {
                return used;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (!ItemListCatalog.IsItemList(listCollection, listIndex))
                {
                    continue;
                }

                int idFieldIndex = GetIdFieldIndex(listCollection, listIndex);
                if (idFieldIndex < 0)
                {
                    continue;
                }

                object[][] values = listCollection.Lists[listIndex].elementValues;
                if (values == null)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < values.Length; rowIndex++)
                {
                    if (listIndex == excludedListIndex && rowIndex == excludedRowIndex)
                    {
                        continue;
                    }

                    int id;
                    if (int.TryParse(listCollection.GetValue(listIndex, rowIndex, idFieldIndex), out id))
                    {
                        used.Add(id);
                    }
                }
            }

            return used;
        }

        public int GetNextUniqueId(HashSet<int> used, int startCandidate)
        {
            if (used == null)
            {
                throw new ArgumentNullException(nameof(used));
            }

            int candidate = Math.Max(1, startCandidate);
            while (used.Contains(candidate))
            {
                candidate++;
            }
            used.Add(candidate);
            return candidate;
        }

        public bool EnsureElementIdUnique(
            eListCollection listCollection,
            int listIndex,
            int rowIndex,
            bool forceNew,
            int startCandidate,
            out int oldId,
            out int newId)
        {
            oldId = 0;
            newId = 0;
            int idFieldIndex = GetIdFieldIndex(listCollection, listIndex);
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || rowIndex < 0
                || rowIndex >= listCollection.Lists[listIndex].elementValues.Length
                || idFieldIndex < 0)
            {
                return false;
            }

            int.TryParse(listCollection.GetValue(listIndex, rowIndex, idFieldIndex), out oldId);
            HashSet<int> used = BuildUsedIdsForElementNamespace(listCollection, listIndex, rowIndex);
            if (!forceNew && oldId > 0 && !used.Contains(oldId))
            {
                newId = oldId;
                return false;
            }

            int candidate = startCandidate > 0
                ? startCandidate
                : oldId > 0 ? oldId + 1 : 1;
            newId = GetNextUniqueId(used, candidate);
            listCollection.SetValue(listIndex, rowIndex, idFieldIndex, newId.ToString());
            return oldId != newId;
        }
    }
}
