using System;
using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public sealed class MonsterDropPortraitService
    {
        private struct DropPortraitSource
        {
            public int MonsterListIndex;
            public string RawIconValue;
        }

        private readonly CreaturePortraitIconService portraitIconService = new CreaturePortraitIconService();
        private eListCollection cachedListCollection;
        private Dictionary<int, DropPortraitSource> cachedSourcesByDropTableId;

        public bool TryResolveDropPortraitPath(
            eListCollection listCollection,
            CacheSave database,
            int dropTableId,
            out string mappedPath)
        {
            mappedPath = string.Empty;
            if (dropTableId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            DropPortraitSource source;
            if (!TryGetDropPortraitSource(listCollection, dropTableId, out source))
            {
                return false;
            }

            int pathId;
            return portraitIconService.TryResolvePortraitPath(database, source.RawIconValue, out pathId, out mappedPath);
        }

        public bool TryResolveDropPortrait(
            eListCollection listCollection,
            CacheSave database,
            int dropTableId,
            out Bitmap icon)
        {
            icon = null;
            if (dropTableId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            DropPortraitSource source;
            if (!TryGetDropPortraitSource(listCollection, dropTableId, out source))
            {
                return false;
            }

            return portraitIconService.TryResolvePortrait(database, listCollection, source.MonsterListIndex, source.RawIconValue, out icon);
        }

        private bool TryGetDropPortraitSource(
            eListCollection listCollection,
            int dropTableId,
            out DropPortraitSource source)
        {
            source = default(DropPortraitSource);
            if (listCollection == null || dropTableId <= 0)
            {
                return false;
            }

            Dictionary<int, DropPortraitSource> map = EnsureDropPortraitIndex(listCollection);
            return map != null && map.TryGetValue(dropTableId, out source) && !string.IsNullOrWhiteSpace(source.RawIconValue);
        }

        private Dictionary<int, DropPortraitSource> EnsureDropPortraitIndex(eListCollection listCollection)
        {
            if (object.ReferenceEquals(cachedListCollection, listCollection) && cachedSourcesByDropTableId != null)
            {
                return cachedSourcesByDropTableId;
            }

            Dictionary<int, DropPortraitSource> map = new Dictionary<int, DropPortraitSource>();
            if (listCollection == null || listCollection.Lists == null)
            {
                cachedListCollection = listCollection;
                cachedSourcesByDropTableId = map;
                return map;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                eList list = listCollection.Lists[listIndex];
                if (list == null || list.elementFields == null || list.elementValues == null)
                {
                    continue;
                }

                string listName = NormalizeListName(list.listName);
                if (!string.Equals(listName, "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int iconFieldIndex = GetIconFieldIndex(list.elementFields);
                List<int> dropFieldIndexes = GetDropFieldIndexes(list.elementFields);
                if (iconFieldIndex < 0 || dropFieldIndexes.Count == 0)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < list.elementValues.Length; rowIndex++)
                {
                    string rawIconValue = listCollection.GetValue(listIndex, rowIndex, iconFieldIndex);
                    if (string.IsNullOrWhiteSpace(rawIconValue))
                    {
                        continue;
                    }

                    for (int i = 0; i < dropFieldIndexes.Count; i++)
                    {
                        int fieldIndex = dropFieldIndexes[i];
                        int currentDropTableId;
                        if (!int.TryParse(listCollection.GetValue(listIndex, rowIndex, fieldIndex), out currentDropTableId)
                            || currentDropTableId <= 0
                            || map.ContainsKey(currentDropTableId))
                        {
                            continue;
                        }

                        map[currentDropTableId] = new DropPortraitSource
                        {
                            MonsterListIndex = listIndex,
                            RawIconValue = rawIconValue
                        };
                    }
                }
            }

            cachedListCollection = listCollection;
            cachedSourcesByDropTableId = map;
            return map;
        }

        private static int GetIconFieldIndex(string[] fields)
        {
            return GetFieldIndex(fields, "file_icon") >= 0
                ? GetFieldIndex(fields, "file_icon")
                : GetFieldIndex(fields, "file_icon1");
        }

        private static List<int> GetDropFieldIndexes(string[] fields)
        {
            List<int> indexes = new List<int>();
            if (fields == null)
            {
                return indexes;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i] ?? string.Empty;
                if (fieldName.StartsWith("id_drop_table_", StringComparison.OrdinalIgnoreCase))
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private static int GetFieldIndex(string[] fields, string fieldName)
        {
            if (fields == null)
            {
                return -1;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string NormalizeListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return string.Empty;
            }

            string[] split = listName.Split(new[] { " - " }, StringSplitOptions.None);
            return split.Length > 1 ? split[1].Trim() : listName.Trim();
        }
    }
}
