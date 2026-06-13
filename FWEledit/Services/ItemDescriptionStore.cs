using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public sealed class ItemDescriptionStore
    {
        private readonly Dictionary<int, string> map = new Dictionary<int, string>();
        private readonly Dictionary<int, string> originalMap = new Dictionary<int, string>();
        private readonly List<int> order = new List<int>();
        private readonly List<int> originalOrder = new List<int>();
        private readonly HashSet<int> pendingItemIds = new HashSet<int>();

        public string FilePath { get; private set; } = string.Empty;
        public bool HasPendingChanges
        {
            get { return pendingItemIds.Count > 0; }
        }

        public string LoadFromFile(string filePath)
        {
            map.Clear();
            originalMap.Clear();
            order.Clear();
            originalOrder.Clear();
            pendingItemIds.Clear();
            FilePath = filePath ?? string.Empty;

            if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
            {
                return "item_ext_desc.txt not found in configs.pck.files";
            }

            try
            {
                Regex rx = new Regex("^\\s*(\\d+)\\s+\"(.*)\"\\s*$", RegexOptions.Compiled);
                using (StreamReader sr = new StreamReader(FilePath, Encoding.Unicode, true))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
                        {
                            continue;
                        }

                        Match m = rx.Match(line);
                        if (!m.Success)
                        {
                            continue;
                        }

                        int id;
                        if (!int.TryParse(m.Groups[1].Value, out id))
                        {
                            continue;
                        }

                        string desc = m.Groups[2].Value;
                        if (!map.ContainsKey(id))
                        {
                            order.Add(id);
                        }
                        map[id] = desc;
                        originalMap[id] = desc;
                    }
                }

                originalOrder.AddRange(order);

                return "Loaded: " + Path.GetFileName(FilePath);
            }
            catch
            {
                return "Failed to parse item_ext_desc.txt";
            }
        }

        public bool TryGetRaw(int id, out string raw)
        {
            return map.TryGetValue(id, out raw);
        }

        public bool Stage(int id, string encoded)
        {
            if (id <= 0)
            {
                return false;
            }

            string safeEncoded = encoded ?? string.Empty;
            string currentValue;
            bool hasCurrent = map.TryGetValue(id, out currentValue);
            string originalValue;
            bool hasOriginal = originalMap.TryGetValue(id, out originalValue);

            if (hasOriginal)
            {
                if (string.Equals(originalValue ?? string.Empty, safeEncoded, StringComparison.Ordinal))
                {
                    bool changed = !hasCurrent || !string.Equals(currentValue ?? string.Empty, safeEncoded, StringComparison.Ordinal);
                    map[id] = safeEncoded;
                    if (!order.Contains(id))
                    {
                        order.Add(id);
                    }
                    pendingItemIds.Remove(id);
                    return changed;
                }

                bool contentChanged = !hasCurrent || !string.Equals(currentValue ?? string.Empty, safeEncoded, StringComparison.Ordinal);
                map[id] = safeEncoded;
                if (!order.Contains(id))
                {
                    order.Add(id);
                }
                pendingItemIds.Add(id);
                return contentChanged;
            }

            if (string.Equals(safeEncoded, string.Empty, StringComparison.Ordinal))
            {
                bool changed = hasCurrent;
                if (hasCurrent)
                {
                    map.Remove(id);
                    order.Remove(id);
                }
                pendingItemIds.Remove(id);
                return changed;
            }

            bool valueChanged = !hasCurrent || !string.Equals(currentValue ?? string.Empty, safeEncoded, StringComparison.Ordinal);
            map[id] = safeEncoded;
            if (!order.Contains(id))
            {
                order.Add(id);
            }
            pendingItemIds.Add(id);
            return valueChanged;
        }

        public bool RemapId(int oldId, int newId)
        {
            if (oldId <= 0 || newId <= 0 || oldId == newId)
            {
                return false;
            }
            string existing;
            if (!map.TryGetValue(oldId, out existing))
            {
                return false;
            }

            if (map.ContainsKey(newId))
            {
                return false;
            }

            map.Remove(oldId);
            map[newId] = existing;

            int oldIndex = order.IndexOf(oldId);
            if (oldIndex >= 0)
            {
                order[oldIndex] = newId;
            }
            else if (!order.Contains(newId))
            {
                order.Add(newId);
            }

            pendingItemIds.Add(newId);
            return true;
        }

        public string[] BuildRuntimeArray()
        {
            List<string> arr = new List<string>();
            for (int i = 0; i < order.Count; i++)
            {
                int id = order[i];
                string value;
                if (!map.TryGetValue(id, out value))
                {
                    continue;
                }
                arr.Add(id.ToString());
                arr.Add(value ?? string.Empty);
            }
            return arr.ToArray();
        }

        public bool FlushToDisk(AssetManager asm, out string statusText, out string errorMessage)
        {
            statusText = string.Empty;
            errorMessage = string.Empty;

            if (!HasPendingChanges)
            {
                statusText = "Description file saved with main Save";
                return true;
            }
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                errorMessage = "item_ext_desc.txt was not found in configs.pck.files.";
                return false;
            }

            try
            {
                using (StreamWriter sw = new StreamWriter(FilePath, false, Encoding.Unicode))
                {
                    sw.WriteLine("//  Element item extend descriptions.");
                    sw.WriteLine();
                    sw.WriteLine("#_index");
                    sw.WriteLine("#_begin");

                    for (int i = 0; i < order.Count; i++)
                    {
                        int id = order[i];
                        string value;
                        if (!map.TryGetValue(id, out value))
                        {
                            continue;
                        }
                        sw.WriteLine(id + "\t\"" + value + "\"");
                    }
                }

                if (asm != null)
                {
                    asm.MarkWorkspaceFileChanged(FilePath);
                }
                SyncBaselineToCurrent();
                statusText = "Description file saved with main Save";
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public void ResetPendingChanges()
        {
            SyncBaselineToCurrent();
        }

        public bool HasPendingChangeForItem(int id)
        {
            return id > 0 && pendingItemIds.Contains(id);
        }

        private void SyncBaselineToCurrent()
        {
            originalMap.Clear();
            foreach (KeyValuePair<int, string> pair in map)
            {
                originalMap[pair.Key] = pair.Value;
            }

            originalOrder.Clear();
            originalOrder.AddRange(order);
            pendingItemIds.Clear();
        }
    }
}
