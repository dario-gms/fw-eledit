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
    }
}
