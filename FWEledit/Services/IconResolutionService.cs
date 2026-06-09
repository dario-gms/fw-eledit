using System;
using System.Collections.Generic;
using System.IO;

namespace FWEledit
{
    public sealed class IconResolutionService
    {
        private CacheSave cachedDatabase;
        private readonly Dictionary<string, string> rawIconKeyCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, string> pathIdIconKeyCache = new Dictionary<int, string>();
        private readonly Dictionary<string, string> listIconKeyCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string ResolveIconKey(CacheSave database, string rawValue)
        {
            if (database == null || string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            EnsureCacheContext(database);

            string value = rawValue.Trim();
            string cached;
            if (rawIconKeyCache.TryGetValue(value, out cached))
            {
                return cached ?? string.Empty;
            }

            int index;
            if (int.TryParse(value, out index))
            {
                if (database.imagesx != null && database.imagesx.ContainsKey(index))
                {
                    cached = database.imagesx[index];
                    rawIconKeyCache[value] = cached;
                    return cached;
                }
                if (database.imagesById != null && database.imagesById.ContainsKey(index))
                {
                    cached = database.imagesById[index];
                    rawIconKeyCache[value] = cached;
                    return cached;
                }
                rawIconKeyCache[value] = string.Empty;
                return string.Empty;
            }

            string key = Path.GetFileName(value);
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (database.ContainsKey(key))
            {
                rawIconKeyCache[value] = key;
                return key;
            }

            if (!Path.HasExtension(key))
            {
                string dds = key + ".dds";
                if (database.ContainsKey(dds))
                {
                    rawIconKeyCache[value] = dds;
                    return dds;
                }
                string png = key + ".png";
                if (database.ContainsKey(png))
                {
                    rawIconKeyCache[value] = png;
                    return png;
                }
            }

            rawIconKeyCache[value] = key;
            return key;
        }

        public string ResolveIconKeyFromPathId(CacheSave database, int pathId)
        {
            if (database == null || database.pathById == null || database.pathById.Count == 0)
            {
                return string.Empty;
            }

            EnsureCacheContext(database);

            string cached;
            if (pathIdIconKeyCache.TryGetValue(pathId, out cached))
            {
                return cached ?? string.Empty;
            }

            int[] candidates = new int[] { pathId, pathId + 1, pathId - 1 };
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
                    pathIdIconKeyCache[pathId] = key;
                    return key;
                }

                string baseName = Path.GetFileName(mapped);
                if (!string.IsNullOrWhiteSpace(baseName) && !Path.HasExtension(baseName))
                {
                    string dds = baseName + ".dds";
                    if (database.ContainsKey(dds))
                    {
                        pathIdIconKeyCache[pathId] = dds;
                        return dds;
                    }
                }
            }

            pathIdIconKeyCache[pathId] = string.Empty;
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

            EnsureCacheContext(database);

            string cacheKey = listIndex.ToString() + "|" + ((rawValue ?? string.Empty).Trim());
            string cached;
            if (listIconKeyCache.TryGetValue(cacheKey, out cached))
            {
                return cached ?? string.Empty;
            }

            int iconId;
            if (!int.TryParse((rawValue ?? string.Empty).Trim(), out iconId))
            {
                cached = ResolveIconKey(database, rawValue);
                listIconKeyCache[cacheKey] = cached;
                return cached;
            }

            string mappedByPathData = ResolveIconKeyFromPathId(database, iconId);
            if (!string.IsNullOrWhiteSpace(mappedByPathData))
            {
                listIconKeyCache[cacheKey] = mappedByPathData;
                return mappedByPathData;
            }

            string directIndexKey = GetIconKeyByIndex(database, iconId);
            if (!string.IsNullOrWhiteSpace(directIndexKey))
            {
                listIconKeyCache[cacheKey] = directIndexKey;
                return directIndexKey;
            }

            cached = ResolveIconKey(database, rawValue);
            listIconKeyCache[cacheKey] = cached;
            return cached;
        }

        public string FormatIconPathIdDisplay(CacheSave database, string rawValue)
        {
            string value = (rawValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            int pathId;
            if (int.TryParse(value, out pathId))
            {
                string mappedPath = ResolveMappedPath(database, pathId);
                if (!string.IsNullOrWhiteSpace(mappedPath))
                {
                    string fileName = Path.GetFileName(mappedPath);
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        fileName = mappedPath;
                    }

                    return fileName + " | " + mappedPath;
                }

                string iconKey = ResolveIconKeyFromPathId(database, pathId);
                if (!string.IsNullOrWhiteSpace(iconKey))
                {
                    string fileName = Path.GetFileName(iconKey);
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        fileName = iconKey;
                    }

                    return fileName + " | " + iconKey;
                }

                return rawValue ?? string.Empty;
            }

            string resolved = ResolveIconKey(database, value);
            if (!string.IsNullOrWhiteSpace(resolved) && !string.Equals(resolved, value, StringComparison.OrdinalIgnoreCase))
            {
                string resolvedName = Path.GetFileName(resolved);
                if (string.IsNullOrWhiteSpace(resolvedName))
                {
                    resolvedName = resolved;
                }

                return resolvedName + " | " + resolved;
            }

            if (LooksLikePath(value))
            {
                string fileName = Path.GetFileName(value);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = value;
                }

                return fileName + " | " + value;
            }

            return rawValue ?? string.Empty;
        }

        public void ClearCache()
        {
            cachedDatabase = null;
            rawIconKeyCache.Clear();
            pathIdIconKeyCache.Clear();
            listIconKeyCache.Clear();
        }

        private void EnsureCacheContext(CacheSave database)
        {
            if (!object.ReferenceEquals(cachedDatabase, database))
            {
                ClearCache();
                cachedDatabase = database;
            }
        }

        private static string ResolveMappedPath(CacheSave database, int pathId)
        {
            if (database == null || database.pathById == null || database.pathById.Count == 0 || pathId <= 0)
            {
                return string.Empty;
            }

            int[] candidates = new int[] { pathId, pathId + 1, pathId - 1 };
            for (int i = 0; i < candidates.Length; i++)
            {
                string mappedPath;
                if (database.pathById.TryGetValue(candidates[i], out mappedPath) && !string.IsNullOrWhiteSpace(mappedPath))
                {
                    return mappedPath;
                }
            }

            return string.Empty;
        }

        private static bool LooksLikePath(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && (value.IndexOf('\\') >= 0
                    || value.IndexOf('/') >= 0
                    || Path.HasExtension(value));
        }
    }
}
