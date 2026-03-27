using System;
using System.Collections.Generic;
using System.IO;

namespace FWEledit
{
    public sealed class IconResolutionService
    {
        public string ResolveIconKey(CacheSave database, string rawValue)
        {
            if (database == null || string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            string value = rawValue.Trim();
            int index;
            if (int.TryParse(value, out index))
            {
                if (database.imagesx != null && database.imagesx.ContainsKey(index))
                {
                    return database.imagesx[index];
                }
                if (database.imagesById != null && database.imagesById.ContainsKey(index))
                {
                    return database.imagesById[index];
                }
                return string.Empty;
            }

            string key = Path.GetFileName(value);
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (database.ContainsKey(key))
            {
                return key;
            }

            if (!Path.HasExtension(key))
            {
                string dds = key + ".dds";
                if (database.ContainsKey(dds))
                {
                    return dds;
                }
                string png = key + ".png";
                if (database.ContainsKey(png))
                {
                    return png;
                }
            }

            return key;
        }

        public string ResolveIconKeyFromPathId(CacheSave database, int pathId)
        {
            if (database == null || database.pathById == null || database.pathById.Count == 0)
            {
                return string.Empty;
            }

            int[] candidates = new int[] { pathId + 1, pathId, pathId - 1 };
            for (int i = 0; i < candidates.Length; i++)
            {
                int candidate = candidates[i];
                if (candidate < 0 || !database.pathById.ContainsKey(candidate))
                {
                    continue;
                }

                string mapped = database.pathById[candidate];
                if (string.IsNullOrWhiteSpace(mapped))
                {
                    continue;
                }

                string key = ResolveIconKey(database, mapped);
                if (!string.IsNullOrWhiteSpace(key) && database.ContainsKey(key))
                {
                    return key;
                }

                string baseName = Path.GetFileName(mapped);
                if (!string.IsNullOrWhiteSpace(baseName) && !Path.HasExtension(baseName))
                {
                    string dds = baseName + ".dds";
                    if (database.ContainsKey(dds))
                    {
                        return dds;
                    }
                }
            }

            return string.Empty;
        }

        public string GetIconKeyByIndex(CacheSave database, int index)
        {
            if (database == null || index < 0)
            {
                return string.Empty;
            }
            if (database.imagesById != null && database.imagesById.ContainsKey(index))
            {
                return database.imagesById[index];
            }
            if (database.imagesx != null && database.imagesx.ContainsKey(index))
            {
                return database.imagesx[index];
            }
            return string.Empty;
        }

        public string ResolveIconKeyForList(CacheSave database, eListCollection listCollection, int listIndex, string rawValue)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return ResolveIconKey(database, rawValue);
            }

            int iconId;
            if (!int.TryParse((rawValue ?? string.Empty).Trim(), out iconId))
            {
                return ResolveIconKey(database, rawValue);
            }

            string mappedByPathData = ResolveIconKeyFromPathId(database, iconId);
            if (!string.IsNullOrWhiteSpace(mappedByPathData))
            {
                return mappedByPathData;
            }

            string directIndexKey = GetIconKeyByIndex(database, iconId);
            if (!string.IsNullOrWhiteSpace(directIndexKey))
            {
                return directIndexKey;
            }

            return ResolveIconKey(database, rawValue);
        }
    }
}
