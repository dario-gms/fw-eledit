using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public sealed class ListDisplayService
    {
        private readonly Dictionary<int, string> list0DisplayNameCache = new Dictionary<int, string>();
        private readonly Dictionary<int, List<object[]>> listDisplayRowsCache = new Dictionary<int, List<object[]>>();
        private eListCollection listDisplayRowsCacheCollection;
        private readonly Dictionary<string, string> listFriendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Equipment", "Equipment Essence" },
            { "EQUIPMENT_ESSENCE", "Equipment Essence" },
            { "EQUIPMENT_ADDON", "Equipment Addon" }
        };

        public int List0DisplayNameCount
        {
            get { return list0DisplayNameCache.Count; }
        }

        public void ResetList0DisplayCache()
        {
            list0DisplayNameCache.Clear();
            EQUIPMENT_ADDON.ResetRuntimeCaches();
        }

        public void ClearListDisplayCache()
        {
            listDisplayRowsCache.Clear();
            listDisplayRowsCacheCollection = null;
        }

        public void InvalidateListDisplayCache(int listIndex)
        {
            if (listDisplayRowsCache.ContainsKey(listIndex))
            {
                listDisplayRowsCache.Remove(listIndex);
            }
        }

        public bool TryGetListDisplayRows(int listIndex, out List<object[]> rows)
        {
            return TryGetListDisplayRows(null, listIndex, out rows);
        }

        public bool TryGetListDisplayRows(eListCollection listCollection, int listIndex, out List<object[]> rows)
        {
            if (listCollection != null && listDisplayRowsCacheCollection != null && !object.ReferenceEquals(listDisplayRowsCacheCollection, listCollection))
            {
                ClearListDisplayCache();
            }

            if (!listDisplayRowsCache.TryGetValue(listIndex, out rows))
            {
                return false;
            }

            if (listCollection != null
                && (listIndex < 0
                    || listCollection.Lists == null
                    || listIndex >= listCollection.Lists.Length
                    || listCollection.Lists[listIndex] == null
                    || listCollection.Lists[listIndex].elementValues == null
                    || rows.Count != listCollection.Lists[listIndex].elementValues.Length))
            {
                listDisplayRowsCache.Remove(listIndex);
                rows = null;
                return false;
            }

            if (listIndex == 0 && ContainsUnresolvedSkillToken(rows))
            {
                listDisplayRowsCache.Remove(listIndex);
                rows = null;
                return false;
            }

            return true;
        }

        public void SetListDisplayRows(int listIndex, List<object[]> rows)
        {
            listDisplayRowsCache[listIndex] = rows ?? new List<object[]>();
        }

        public void SetListDisplayRows(eListCollection listCollection, int listIndex, List<object[]> rows)
        {
            if (listCollection != null && listDisplayRowsCacheCollection != null && !object.ReferenceEquals(listDisplayRowsCacheCollection, listCollection))
            {
                ClearListDisplayCache();
            }

            listDisplayRowsCacheCollection = listCollection;
            SetListDisplayRows(listIndex, rows);
        }

        public string GetFriendlyListName(string rawListName)
        {
            if (string.IsNullOrWhiteSpace(rawListName))
            {
                return "Unknown";
            }

            string[] split = rawListName.Split(new string[] { " - " }, StringSplitOptions.None);
            string key = split.Length > 1 ? split[1].Trim() : rawListName.Trim();
            string friendly;
            if (listFriendlyNames.TryGetValue(key, out friendly))
            {
                return friendly;
            }

            return HumanizeListName(key);
        }

        private static string HumanizeListName(string rawListName)
        {
            string normalized = (rawListName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "Unknown";
            }

            normalized = normalized.Replace('_', ' ');
            normalized = CollapseWhitespace(normalized);

            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
            return textInfo.ToTitleCase(normalized.ToLowerInvariant());
        }

        private static string CollapseWhitespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            string[] split = value.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < split.Length; i++)
            {
                parts.Add(split[i]);
            }

            return string.Join(" ", parts.ToArray());
        }

        public string GetDisplayEntryName(ISessionService sessionService, eListCollection listCollection, int listIndex, int entryIndex, int nameFieldIndex)
        {
            string fallback = nameFieldIndex >= 0 ? listCollection.GetValue(listIndex, entryIndex, nameFieldIndex) : string.Empty;
            if (listIndex != 0)
            {
                return FwTextColorService.StripLeadingColor(fallback);
            }

            string cached;
            if (list0DisplayNameCache.TryGetValue(entryIndex, out cached))
            {
                if (!HasUnresolvedSkillToken(cached))
                {
                    return cached;
                }

                list0DisplayNameCache.Remove(entryIndex);
            }

            try
            {
                string id = listCollection.GetValue(listIndex, entryIndex, 0);
                string decoded = EQUIPMENT_ADDON.GetAddon(sessionService, id);
                if (!string.IsNullOrWhiteSpace(decoded))
                {
                    string normalized = decoded.Replace("\r", " ").Replace("\n", " / ").Trim();
                    list0DisplayNameCache[entryIndex] = normalized;
                    return normalized;
                }
            }
            catch
            { }

            string strippedFallback = FwTextColorService.StripLeadingColor(fallback);
            list0DisplayNameCache[entryIndex] = strippedFallback;
            return strippedFallback;
        }

        private static bool ContainsUnresolvedSkillToken(List<object[]> rows)
        {
            if (rows == null)
            {
                return false;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                object[] row = rows[i];
                if (row == null || row.Length <= 2)
                {
                    continue;
                }

                string name = row[2] as string;
                if (HasUnresolvedSkillToken(name))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUnresolvedSkillToken(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf("$skill", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public string ComposeListDisplayName(ISessionService sessionService, eListCollection listCollection, int listIndex, int entryIndex, int nameFieldIndex, bool isRowDirty)
        {
            string name = GetDisplayEntryName(sessionService, listCollection, listIndex, entryIndex, nameFieldIndex);
            if (isRowDirty)
            {
                return "* " + name;
            }
            return name;
        }
    }
}
