using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class FieldIndexLookupService
    {
        private eListCollection cachedListCollection;
        private readonly Dictionary<int, int> itemQualityIndexByList = new Dictionary<int, int>();
        private readonly Dictionary<int, int> nameIndexByList = new Dictionary<int, int>();
        private readonly Dictionary<int, int> iconIndexByList = new Dictionary<int, int>();

        public int GetItemQualityFieldIndex(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return -1;
            }

            EnsureCacheContext(listCollection);

            int cached;
            if (itemQualityIndexByList.TryGetValue(listIndex, out cached))
            {
                return cached;
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "item_quality", StringComparison.OrdinalIgnoreCase))
                {
                    itemQualityIndexByList[listIndex] = i;
                    return i;
                }
            }
            itemQualityIndexByList[listIndex] = -1;
            return -1;
        }

        public int GetNameFieldIndex(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return -1;
            }

            EnsureCacheContext(listCollection);

            int cached;
            if (nameIndexByList.TryGetValue(listIndex, out cached))
            {
                return cached;
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                string field = listCollection.Lists[listIndex].elementFields[i];
                if (string.Equals(field, "Name", StringComparison.OrdinalIgnoreCase))
                {
                    nameIndexByList[listIndex] = i;
                    return i;
                }
            }
            nameIndexByList[listIndex] = -1;
            return -1;
        }

        public int GetIconFieldIndex(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return -1;
            }

            EnsureCacheContext(listCollection);

            int cached;
            if (iconIndexByList.TryGetValue(listIndex, out cached))
            {
                return cached;
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                string field = listCollection.Lists[listIndex].elementFields[i];
                if (string.Equals(field, "file_icon", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(field, "file_icon1", StringComparison.OrdinalIgnoreCase))
                {
                    iconIndexByList[listIndex] = i;
                    return i;
                }
            }

            iconIndexByList[listIndex] = -1;
            return -1;
        }

        public void ClearCache()
        {
            cachedListCollection = null;
            itemQualityIndexByList.Clear();
            nameIndexByList.Clear();
            iconIndexByList.Clear();
        }

        private void EnsureCacheContext(eListCollection listCollection)
        {
            if (!object.ReferenceEquals(cachedListCollection, listCollection))
            {
                ClearCache();
                cachedListCollection = listCollection;
            }
        }
    }
}
