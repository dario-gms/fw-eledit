using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ListDisplayService
    {
        private readonly Dictionary<int, string> list0DisplayNameCache = new Dictionary<int, string>();
        private readonly Dictionary<int, List<object[]>> listDisplayRowsCache = new Dictionary<int, List<object[]>>();
        private readonly Dictionary<string, string> listFriendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "EQUIPMENT_ADDON", "Added Attribute" }
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
            return listDisplayRowsCache.TryGetValue(listIndex, out rows);
        }

        public void SetListDisplayRows(int listIndex, List<object[]> rows)
        {
            listDisplayRowsCache[listIndex] = rows ?? new List<object[]>();
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
            return key;
        }

        public string GetDisplayEntryName(ISessionService sessionService, eListCollection listCollection, int listIndex, int entryIndex, int nameFieldIndex)
        {
            string fallback = nameFieldIndex >= 0 ? listCollection.GetValue(listIndex, entryIndex, nameFieldIndex) : string.Empty;
            if (listIndex != 0)
            {
                return fallback;
            }

            string cached;
            if (list0DisplayNameCache.TryGetValue(entryIndex, out cached))
            {
                return cached;
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

            list0DisplayNameCache[entryIndex] = fallback;
            return fallback;
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
