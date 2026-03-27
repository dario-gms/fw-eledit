using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ModelPickerCacheService
    {
        private readonly Dictionary<int, string> modelPackageByPathIdCache = new Dictionary<int, string>();
        private string modelPackageCacheSignature = string.Empty;
        private readonly Dictionary<string, ModelPickerPackageCacheEntry> modelPickerPackageCache
            = new Dictionary<string, ModelPickerPackageCacheEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> modelPickerMissingExtractNotified
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
    }
}
