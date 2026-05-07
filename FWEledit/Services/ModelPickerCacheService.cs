using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ModelPickerCacheService
    {
        private sealed class PickerEntriesCacheEntry
        {
            public string Signature { get; set; }
            public List<ModelPickerEntry> Entries { get; set; }
        }

        private readonly Dictionary<int, string> modelPackageByPathIdCache = new Dictionary<int, string>();
        private string modelPackageCacheSignature = string.Empty;
        private readonly Dictionary<string, ModelPickerPackageCacheEntry> modelPickerPackageCache
            = new Dictionary<string, ModelPickerPackageCacheEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> modelPickerMissingExtractNotified
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, PickerEntriesCacheEntry> modelPickerEntriesCache
            = new Dictionary<int, PickerEntriesCacheEntry>();

        public void EnsurePackageCacheSignature(string signature)
        {
            string safeSignature = signature ?? string.Empty;
            if (!string.Equals(safeSignature, modelPackageCacheSignature, StringComparison.OrdinalIgnoreCase))
            {
                modelPackageByPathIdCache.Clear();
                modelPackageCacheSignature = safeSignature;
            }
        }

        public bool TryGetPackageForPathId(int pathId, out string package)
        {
            if (modelPackageByPathIdCache.TryGetValue(pathId, out package) && !string.IsNullOrWhiteSpace(package))
            {
                return true;
            }
            package = package ?? string.Empty;
            return false;
        }

        public void SetPackageForPathId(int pathId, string package)
        {
            if (pathId <= 0)
            {
                return;
            }
            modelPackageByPathIdCache[pathId] = package ?? string.Empty;
        }

        public bool TryGetPickerFiles(string cacheKey, DateTime pckTimestampUtc, out List<string> files)
        {
            files = null;
            ModelPickerPackageCacheEntry cached;
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return false;
            }
            if (modelPickerPackageCache.TryGetValue(cacheKey, out cached)
                && cached != null
                && cached.Files != null
                && cached.PckTimestampUtc == pckTimestampUtc)
            {
                files = new List<string>(cached.Files);
                return true;
            }
            return false;
        }

        public void SetPickerFiles(string cacheKey, DateTime pckTimestampUtc, List<string> files)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return;
            }
            modelPickerPackageCache[cacheKey] = new ModelPickerPackageCacheEntry
            {
                PckTimestampUtc = pckTimestampUtc,
                Files = files != null ? new List<string>(files) : new List<string>()
            };
        }

        public bool TryMarkMissingExtractNotified(string package)
        {
            if (string.IsNullOrWhiteSpace(package))
            {
                return false;
            }
            if (modelPickerMissingExtractNotified.Contains(package))
            {
                return false;
            }
            modelPickerMissingExtractNotified.Add(package);
            return true;
        }

        public bool TryGetPickerEntries(int listIndex, string signature, out List<ModelPickerEntry> entries)
        {
            entries = null;
            if (listIndex < 0 || string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            if (modelPickerEntriesCache.TryGetValue(listIndex, out PickerEntriesCacheEntry cached)
                && cached != null
                && string.Equals(cached.Signature, signature, StringComparison.Ordinal))
            {
                entries = cached.Entries != null
                    ? new List<ModelPickerEntry>(cached.Entries)
                    : new List<ModelPickerEntry>();
                return true;
            }

            return false;
        }

        public void SetPickerEntries(int listIndex, string signature, List<ModelPickerEntry> entries)
        {
            if (listIndex < 0 || string.IsNullOrWhiteSpace(signature))
            {
                return;
            }

            modelPickerEntriesCache[listIndex] = new PickerEntriesCacheEntry
            {
                Signature = signature,
                Entries = entries != null ? new List<ModelPickerEntry>(entries) : new List<ModelPickerEntry>()
            };
        }

        public void InvalidatePickerEntries(int listIndex)
        {
            if (listIndex < 0)
            {
                return;
            }

            modelPickerEntriesCache.Remove(listIndex);
        }
    }
}
