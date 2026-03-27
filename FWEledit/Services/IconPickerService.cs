using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace FWEledit
{
    public sealed class IconPickerService
    {
        public IconPickerData BuildData(CacheSave database, Dictionary<int, int> usageByPathId, int pendingSelectPathId)
        {
            IconPickerData data = new IconPickerData
            {
                PendingSelectPathId = pendingSelectPathId,
                UsageByPathId = usageByPathId ?? new Dictionary<int, int>()
            };

            if (database == null || database.sourceBitmap == null || database.imagesx == null || database.imagesx.Count == 0)
            {
                return data;
            }

            data.AtlasBitmap = database.sourceBitmap;
            data.IconWidth = Math.Max(1, database.iconWidth);
            data.IconHeight = Math.Max(1, database.iconHeight);
            data.AtlasCols = Math.Max(1, database.cols);

            Dictionary<string, List<int>> pathIdsByKey = BuildPathIdsByKey(database);
            data.UsageByIconKey = BuildUsageByIconKey(database, data.UsageByPathId);

            for (int i = 0; i < database.imagesx.Count; i++)
            {
                string key = NormalizeKey(database.imagesx.Values[i]);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                List<int> candidates;
                if (!pathIdsByKey.TryGetValue(key, out candidates) || candidates.Count == 0)
                {
                    continue;
                }

                int pathId = ChooseBestPathId(candidates, data.UsageByPathId, pendingSelectPathId);
                if (pathId <= 0)
                {
                    continue;
                }

                data.Entries.Add(new IconEntryModel
                {
                    PathId = pathId,
                    AtlasIndex = i,
                    Key = key,
                    FileName = Path.GetFileName(key)
                });
            }

            return data;
        }

        public List<IconEntryModel> FilterEntries(List<IconEntryModel> entries, string search)
        {
            if (entries == null)
            {
                return new List<IconEntryModel>();
            }

            string term = (search ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                return new List<IconEntryModel>(entries);
            }

            List<IconEntryModel> result = new List<IconEntryModel>();
            for (int i = 0; i < entries.Count; i++)
            {
                IconEntryModel entry = entries[i];
                if (entry.PathId.ToString().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0
                    || (!string.IsNullOrEmpty(entry.FileName) && entry.FileName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(entry.Key) && entry.Key.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        private static Dictionary<string, List<int>> BuildPathIdsByKey(CacheSave database)
        {
            Dictionary<string, List<int>> pathIdsByKey = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            if (database == null || database.pathById == null)
            {
                return pathIdsByKey;
            }

            foreach (KeyValuePair<int, string> kv in database.pathById)
            {
                if (kv.Key <= 0)
                {
                    continue;
                }
                string key = ResolveIconKeyForStoredPathId(database, kv.Key);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                List<int> ids;
                if (!pathIdsByKey.TryGetValue(key, out ids))
                {
                    ids = new List<int>();
                    pathIdsByKey[key] = ids;
                }
                if (!ids.Contains(kv.Key))
                {
                    ids.Add(kv.Key);
                }
            }

            return pathIdsByKey;
        }

        private static Dictionary<string, int> BuildUsageByIconKey(CacheSave database, Dictionary<int, int> usageByPathId)
        {
            Dictionary<string, int> usageByIconKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (database == null || usageByPathId == null)
            {
                return usageByIconKey;
            }

            foreach (KeyValuePair<int, int> usage in usageByPathId)
            {
                string key = ResolveIconKeyForStoredPathId(database, usage.Key);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }
                int count;
                usageByIconKey.TryGetValue(key, out count);
                usageByIconKey[key] = count + usage.Value;
            }

            return usageByIconKey;
        }

        private static int ChooseBestPathId(List<int> candidates, Dictionary<int, int> usageByPathId, int pendingSelectPathId)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return 0;
            }

            if (pendingSelectPathId > 0 && candidates.Contains(pendingSelectPathId))
            {
                return pendingSelectPathId;
            }

            int bestId = candidates[0];
            int bestScore = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                int id = candidates[i];
                int score;
                usageByPathId.TryGetValue(id, out score);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = id;
                }
            }

            return bestId;
        }

        private static string ResolveIconKeyForStoredPathId(CacheSave database, int pathId)
        {
            if (database == null || database.pathById == null || database.pathById.Count == 0 || pathId <= 0)
            {
                return string.Empty;
            }

            int[] candidates = new int[] { pathId + 1, pathId, pathId - 1 };
            for (int i = 0; i < candidates.Length; i++)
            {
                int candidate = candidates[i];
                if (candidate <= 0 || !database.pathById.ContainsKey(candidate))
                {
                    continue;
                }
                string raw = database.pathById[candidate];
                string key = NormalizeKey(raw);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    return key;
                }
            }
            return string.Empty;
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            string k = value.Trim().Replace('/', '\\');
            return Path.GetFileName(k);
        }
    }
}
