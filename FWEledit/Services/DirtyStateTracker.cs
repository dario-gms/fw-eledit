using System.Collections.Generic;

namespace FWEledit
{
    public sealed class DirtyStateTracker
    {
        private readonly Dictionary<int, HashSet<int>> dirtyRowsByList = new Dictionary<int, HashSet<int>>();
        private readonly Dictionary<int, Dictionary<int, HashSet<int>>> dirtyFieldsByList = new Dictionary<int, Dictionary<int, HashSet<int>>>();
        private readonly Dictionary<int, Dictionary<int, HashSet<int>>> invalidFieldsByList = new Dictionary<int, Dictionary<int, HashSet<int>>>();

        public void Clear()
        {
            dirtyRowsByList.Clear();
            dirtyFieldsByList.Clear();
            invalidFieldsByList.Clear();
        }

        public void MarkRowDirty(int listIndex, int rowIndex)
        {
            HashSet<int> set;
            if (!dirtyRowsByList.TryGetValue(listIndex, out set))
            {
                set = new HashSet<int>();
                dirtyRowsByList[listIndex] = set;
            }
            set.Add(rowIndex);
        }

        public bool IsRowDirty(int listIndex, int rowIndex)
        {
            HashSet<int> set;
            return dirtyRowsByList.TryGetValue(listIndex, out set) && set.Contains(rowIndex);
        }

        public void MarkFieldDirty(int listIndex, int rowIndex, int fieldIndex)
        {
            Dictionary<int, HashSet<int>> rows;
            if (!dirtyFieldsByList.TryGetValue(listIndex, out rows))
            {
                rows = new Dictionary<int, HashSet<int>>();
                dirtyFieldsByList[listIndex] = rows;
            }
            HashSet<int> fields;
            if (!rows.TryGetValue(rowIndex, out fields))
            {
                fields = new HashSet<int>();
                rows[rowIndex] = fields;
            }
            fields.Add(fieldIndex);
        }

        public bool IsFieldDirty(int listIndex, int rowIndex, int fieldIndex)
        {
            Dictionary<int, HashSet<int>> rows;
            HashSet<int> fields;
            return dirtyFieldsByList.TryGetValue(listIndex, out rows)
                && rows.TryGetValue(rowIndex, out fields)
                && fields.Contains(fieldIndex);
        }

        public void MarkFieldInvalid(int listIndex, int rowIndex, int fieldIndex)
        {
            Dictionary<int, HashSet<int>> rows;
            if (!invalidFieldsByList.TryGetValue(listIndex, out rows))
            {
                rows = new Dictionary<int, HashSet<int>>();
                invalidFieldsByList[listIndex] = rows;
            }
            HashSet<int> fields;
            if (!rows.TryGetValue(rowIndex, out fields))
            {
                fields = new HashSet<int>();
                rows[rowIndex] = fields;
            }
            fields.Add(fieldIndex);
        }

        public void ClearFieldInvalid(int listIndex, int rowIndex, int fieldIndex)
        {
            Dictionary<int, HashSet<int>> rows;
            HashSet<int> fields;
            if (invalidFieldsByList.TryGetValue(listIndex, out rows) &&
                rows.TryGetValue(rowIndex, out fields))
            {
                fields.Remove(fieldIndex);
                if (fields.Count == 0)
                {
                    rows.Remove(rowIndex);
                }
                if (rows.Count == 0)
                {
                    invalidFieldsByList.Remove(listIndex);
                }
            }
        }

        public bool IsFieldInvalid(int listIndex, int rowIndex, int fieldIndex)
        {
            Dictionary<int, HashSet<int>> rows;
            HashSet<int> fields;
            return invalidFieldsByList.TryGetValue(listIndex, out rows)
                && rows.TryGetValue(rowIndex, out fields)
                && fields.Contains(fieldIndex);
        }
    }
}
