using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace FWEledit
{
    public sealed class ModelPickerService
    {
        private readonly ModelPickerCacheService cacheService;
        private readonly AssetManager assetManager;
        private readonly IconResolutionService iconResolutionService;
        private readonly IdGenerationService idGenerationService;
        private readonly ItemFieldClassifierService fieldClassifier;

        public ModelPickerService(
            ModelPickerCacheService cacheService,
            AssetManager assetManager,
            IconResolutionService iconResolutionService,
            IdGenerationService idGenerationService,
            ItemFieldClassifierService fieldClassifier)
        {
            this.cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            this.assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
            this.iconResolutionService = iconResolutionService ?? throw new ArgumentNullException(nameof(iconResolutionService));
            this.idGenerationService = idGenerationService ?? throw new ArgumentNullException(nameof(idGenerationService));
            this.fieldClassifier = fieldClassifier ?? throw new ArgumentNullException(nameof(fieldClassifier));
        }

        public List<ModelPickerEntry> BuildModelPickerEntries(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            Func<string, Bitmap> iconLoader,
            Action<string> missingPackageNotifier)
        {
            List<ModelPickerEntry> entries = new List<ModelPickerEntry>();
            if (database == null || database.pathById == null || database.pathById.Count == 0)
            {
                return entries;
            }
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return entries;
            }

            int[] modelFields = GetModelUsageFieldIndices(listCollection, listIndex);
            int idFieldIndex = idGenerationService.GetIdFieldIndex(listCollection, listIndex);
            int nameFieldIndex = -1;
            int iconFieldIndex = -1;
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "name", StringComparison.OrdinalIgnoreCase))
                {
                    nameFieldIndex = i;
                }
                if (iconFieldIndex < 0 && fieldClassifier.IsIconFieldName(listCollection.Lists[listIndex].elementFields[i]))
                {
                    iconFieldIndex = i;
                }
                if (nameFieldIndex >= 0 && iconFieldIndex >= 0)
                {
                    break;
                }
            }

            Dictionary<int, int> usesByPathId = new Dictionary<int, int>();
            Dictionary<int, int> sampleItemIdByPathId = new Dictionary<int, int>();
            Dictionary<int, string> sampleItemNameByPathId = new Dictionary<int, string>();
            Dictionary<int, string> sampleIconKeyByPathId = new Dictionary<int, string>();
            Dictionary<string, Bitmap> iconCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

            if (modelFields.Length > 0)
            {
                string listName = listCollection.Lists[listIndex].listName ?? string.Empty;
                for (int row = 0; row < listCollection.Lists[listIndex].elementValues.Length; row++)
                {
                    int itemId = 0;
                    if (idFieldIndex >= 0)
                    {
                        int.TryParse(listCollection.GetValue(listIndex, row, idFieldIndex), out itemId);
                    }
                    string itemName = nameFieldIndex >= 0 ? listCollection.GetValue(listIndex, row, nameFieldIndex) : string.Empty;

                    for (int mf = 0; mf < modelFields.Length; mf++)
                    {
                        int rawPathId;
                        if (!int.TryParse(listCollection.GetValue(listIndex, row, modelFields[mf]), out rawPathId) || rawPathId <= 0)
                        {
                            continue;
                        }

                        int pathId = rawPathId;
                        int resolvedPathId;
                        string resolvedMappedPath;
                        string modelFieldName = listCollection.Lists[listIndex].elementFields[modelFields[mf]];
                        bool allowNeighborOffsets = fieldClassifier.IsModelFieldName(modelFieldName);
                        if (TryResolveModelPathById(database, rawPathId, modelFieldName, listName, out resolvedPathId, out resolvedMappedPath, allowNeighborOffsets) && resolvedPathId > 0)
                        {
                            pathId = resolvedPathId;
                        }

                        int c;
                        usesByPathId.TryGetValue(pathId, out c);
                        usesByPathId[pathId] = c + 1;
                        if (!sampleItemIdByPathId.ContainsKey(pathId))
                        {
                            sampleItemIdByPathId[pathId] = itemId;
                            sampleItemNameByPathId[pathId] = itemName;
                            if (iconFieldIndex >= 0)
                            {
                                string iconKey = iconResolutionService.ResolveIconKeyForList(database, listCollection, listIndex, listCollection.GetValue(listIndex, row, iconFieldIndex));
                                if (!string.IsNullOrWhiteSpace(iconKey))
                                {
                                    sampleIconKeyByPathId[pathId] = iconKey;
                                }
                            }
                        }
                    }
                }
            }

            Dictionary<string, List<int>> pathIdsByNormalizedPath = BuildModelPathIdLookup(database);
            int idx = 1;
            for (int p = 0; p < ModelPickerCatalog.PackageOrder.Length; p++)
            {
                string package = ModelPickerCatalog.PackageOrder[p];
                List<string> packageFiles = EnumerateModelPickerPackageFiles(package, missingPackageNotifier);
                for (int i = 0; i < packageFiles.Count; i++)
                {
                    string relativePath = packageFiles[i];
                    int pathId = ResolveModelEntryPathId(package, relativePath, pathIdsByNormalizedPath, usesByPathId);
                    int uses = 0;
                    int itemId = 0;
                    string itemName = string.Empty;
                    Bitmap icon = Properties.Resources.NoIcon;
                    if (pathId > 0)
                    {
                        usesByPathId.TryGetValue(pathId, out uses);
                        sampleItemIdByPathId.TryGetValue(pathId, out itemId);
                        sampleItemNameByPathId.TryGetValue(pathId, out itemName);
                        string iconKey;
                        if (sampleIconKeyByPathId.TryGetValue(pathId, out iconKey)
                            && !string.IsNullOrWhiteSpace(iconKey)
                            && database != null
                            && database.sourceBitmap != null
                            && database.ContainsKey(iconKey))
                        {
                            Bitmap cachedIcon;
                            if (!iconCache.TryGetValue(iconKey, out cachedIcon))
                            {
                                cachedIcon = iconLoader != null ? iconLoader(iconKey) : database.images(iconKey);
                                iconCache[iconKey] = cachedIcon;
                            }
                            icon = cachedIcon ?? Properties.Resources.NoIcon;
                        }
                    }

                    entries.Add(new ModelPickerEntry
                    {
                        Index = idx++,
                        PathId = pathId,
                        Package = package,
                        RelativePath = relativePath,
                        Icon = icon,
                        ItemId = itemId,
                        ItemName = itemName,
                        Uses = uses
                    });
                }
            }

            return entries;
        }

        public List<string> EnumerateModelPickerPackageFiles(string package, Action<string> missingPackageNotifier)
        {
            List<string> files = new List<string>();
            try
            {
                string resourcesRoot = string.Empty;
                if (!string.IsNullOrWhiteSpace(AssetManager.GameRootPath))
                {
                    resourcesRoot = Path.Combine(AssetManager.GameRootPath, "resources");
                }
                string packagePck = string.IsNullOrWhiteSpace(resourcesRoot)
                    ? string.Empty
                    : Path.Combine(resourcesRoot, package + ".pck");
                string packagePkx = string.IsNullOrWhiteSpace(resourcesRoot)
                    ? string.Empty
                    : Path.Combine(resourcesRoot, package + ".pkx");

                DateTime pckTimestampUtc = DateTime.MinValue;
                if (!string.IsNullOrWhiteSpace(packagePck) && File.Exists(packagePck))
                {
                    pckTimestampUtc = File.GetLastWriteTimeUtc(packagePck);
                }
                if (!string.IsNullOrWhiteSpace(packagePkx) && File.Exists(packagePkx))
                {
                    DateTime pkxTime = File.GetLastWriteTimeUtc(packagePkx);
                    if (pkxTime > pckTimestampUtc)
                    {
                        pckTimestampUtc = pkxTime;
                    }
                }

                string cacheKey = (packagePck ?? string.Empty).Trim().ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(packagePkx) && File.Exists(packagePkx))
                {
                    cacheKey += "|" + packagePkx.Trim().ToLowerInvariant();
                }
                List<string> cachedFiles;
                if (cacheService.TryGetPickerFiles(cacheKey, pckTimestampUtc, out cachedFiles))
                {
                    return cachedFiles;
                }

                List<string> packageEntries = null;
                if (assetManager == null || !assetManager.TryEnumeratePckIndexEntries(package, out packageEntries))
                {
                    if (missingPackageNotifier != null && ShouldNotifyMissingPackageExtraction(package))
                    {
                        missingPackageNotifier(package);
                    }
                    return files;
                }

                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < packageEntries.Count; i++)
                {
                    string relative = packageEntries[i];
                    string ext = Path.GetExtension(relative);
                    if (!IsModelPickerExtensionAllowed(package, ext))
                    {
                        continue;
                    }

                    relative = relative.Trim().Replace('/', '\\');
                    if (string.IsNullOrWhiteSpace(relative))
                    {
                        continue;
                    }

                    if (seen.Add(relative))
                    {
                        files.Add(relative);
                    }
                }

                files.Reverse();

                cacheService.SetPickerFiles(cacheKey, pckTimestampUtc, files);
            }
            catch
            { }

            return files;
        }

        public bool ShouldNotifyMissingPackageExtraction(string package)
        {
            if (string.IsNullOrWhiteSpace(package))
            {
                return false;
            }
            if (!string.Equals(package, "models", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(package, "gfx", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(package, "grasses", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return false;
            }
            string gamePck = Path.Combine(gameRoot, "resources", package + ".pck");
            return File.Exists(gamePck);
        }

        public string FormatModelPathIdDisplay(CacheSave database, string rawValue, string fieldName)
        {
            return FormatModelPathIdDisplay(database, rawValue, fieldName, string.Empty);
        }

        public string FormatModelPathIdDisplay(CacheSave database, string rawValue, string fieldName, string listName)
        {
            int pathId;
            if (!TryExtractPathId(rawValue, out pathId))
            {
                return rawValue;
            }

            int resolvedPathId;
            string mappedPath;
            if (TryResolveModelPathById(database, pathId, fieldName, listName, out resolvedPathId, out mappedPath, true))
            {
                return pathId + " | " + mappedPath;
            }

            return pathId.ToString();
        }

        public bool TryResolveModelPathById(CacheSave database, int pathId, out int resolvedPathId, out string mappedPath, bool allowNeighborOffsets)
        {
            return TryResolveModelPathById(database, pathId, null, string.Empty, out resolvedPathId, out mappedPath, allowNeighborOffsets);
        }

        public bool TryResolveModelPathById(CacheSave database, int pathId, string fieldName, out int resolvedPathId, out string mappedPath, bool allowNeighborOffsets)
        {
            return TryResolveModelPathById(database, pathId, fieldName, string.Empty, out resolvedPathId, out mappedPath, allowNeighborOffsets);
        }

        public bool TryResolveModelPathById(CacheSave database, int pathId, string fieldName, string listName, out int resolvedPathId, out string mappedPath, bool allowNeighborOffsets)
        {
            resolvedPathId = 0;
            mappedPath = string.Empty;
            if (database == null || database.pathById == null || database.pathById.Count == 0 || pathId <= 0)
            {
                return false;
            }

            List<int> candidates = new List<int>();
            if (fieldClassifier.IsShiftedModelFieldName(fieldName, listName))
            {
                candidates.Add(pathId + 1);
                candidates.Add(pathId);
                candidates.Add(pathId - 1);
            }
            else
            {
                candidates.Add(pathId);
                candidates.Add(pathId + 1);
                candidates.Add(pathId - 1);
            }
            if (allowNeighborOffsets)
            {
                if (!candidates.Contains(pathId + 1)) { candidates.Add(pathId + 1); }
                if (!candidates.Contains(pathId - 1)) { candidates.Add(pathId - 1); }
            }
            else
            {
                while (candidates.Count > 1)
                {
                    candidates.RemoveAt(candidates.Count - 1);
                }
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                int candidate = candidates[i];
                string mapped;
                if (database.pathById.TryGetValue(candidate, out mapped) && !string.IsNullOrWhiteSpace(mapped))
                {
                    resolvedPathId = candidate;
                    mappedPath = mapped;
                    return true;
                }
            }

            return false;
        }

        public bool TryExtractPathId(string rawValue, out int pathId)
        {
            pathId = 0;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            string trimmed = rawValue.Trim();
            int pipeIndex = trimmed.IndexOf('|');
            if (pipeIndex > 0)
            {
                trimmed = trimmed.Substring(0, pipeIndex).Trim();
            }

            return int.TryParse(trimmed, out pathId);
        }

        public string GuessModelPackageFromMappedPath(string mappedPath)
        {
            if (string.IsNullOrWhiteSpace(mappedPath))
            {
                return string.Empty;
            }

            string normalized = NormalizeModelPathLookupKey(mappedPath);
            string package;
            string relative;
            if (TrySplitModelPackagePath(normalized, out package, out relative))
            {
                return package;
            }

            if (normalized.StartsWith("char\\", StringComparison.OrdinalIgnoreCase))
            {
                return "models";
            }
            if (normalized.StartsWith("gfx\\", StringComparison.OrdinalIgnoreCase))
            {
                return "gfx";
            }
            if (normalized.StartsWith("grass\\", StringComparison.OrdinalIgnoreCase))
            {
                return "grasses";
            }

            return string.Empty;
        }

        public string GuessModelPackageFromExtractedSource(int pathId, string mappedPath)
        {
            if (pathId <= 0)
            {
                return string.Empty;
            }

            string package;
            string relative;
            if (TrySplitModelPackagePath(NormalizeModelPathLookupKey(mappedPath), out package, out relative))
            {
                return package;
            }

            return string.Empty;
        }

        public bool LooksLikeModelMappedPath(string mappedPath)
        {
            if (string.IsNullOrWhiteSpace(mappedPath))
            {
                return false;
            }

            string normalized = NormalizeModelPathLookupKey(mappedPath);
            for (int i = 0; i < ModelPickerCatalog.PathPackages.Length; i++)
            {
                string pkg = ModelPickerCatalog.PathPackages[i];
                string prefix = pkg + "\\";
                if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public Dictionary<string, List<int>> BuildModelPathIdLookup(CacheSave database)
        {
            Dictionary<string, List<int>> lookup = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            if (database == null || database.pathById == null)
            {
                return lookup;
            }

            foreach (KeyValuePair<int, string> kv in database.pathById)
            {
                int pathId = kv.Key;
                if (pathId <= 0)
                {
                    continue;
                }

                string normalized = NormalizeModelPathLookupKey(kv.Value);
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                AddPathLookupKey(lookup, normalized, pathId);

                string package;
                string relative;
                if (TrySplitModelPackagePath(normalized, out package, out relative)
                    && !string.IsNullOrWhiteSpace(relative))
                {
                    AddPathLookupKey(lookup, relative, pathId);
                }
            }

            return lookup;
        }

        public int ResolveModelEntryPathId(string package, string relativePath, Dictionary<string, List<int>> lookup, Dictionary<int, int> usesByPathId)
        {
            if (lookup == null || lookup.Count == 0)
            {
                return 0;
            }

            string normalizedRelative = NormalizeModelPathLookupKey(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return 0;
            }

            List<int> candidates = new List<int>();
            string[] keys = new string[]
            {
                NormalizeModelPathLookupKey(package + "\\" + normalizedRelative),
                normalizedRelative
            };

            for (int i = 0; i < keys.Length; i++)
            {
                List<int> ids;
                if (lookup.TryGetValue(keys[i], out ids))
                {
                    for (int j = 0; j < ids.Count; j++)
                    {
                        if (!candidates.Contains(ids[j]))
                        {
                            candidates.Add(ids[j]);
                        }
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return 0;
            }

            int best = candidates[0];
            int bestUses = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                int uses;
                if (usesByPathId.TryGetValue(candidates[i], out uses) && uses > bestUses)
                {
                    bestUses = uses;
                    best = candidates[i];
                }
            }

            return best;
        }

        public int[] GetModelFieldIndices(eListCollection listCollection, int listIndex)
        {
            List<int> indices = new List<int>();
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return indices.ToArray();
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (fieldClassifier.IsModelFieldName(listCollection.Lists[listIndex].elementFields[i]))
                {
                    indices.Add(i);
                }
            }

            return indices.ToArray();
        }

        public int[] GetModelUsageFieldIndices(eListCollection listCollection, int listIndex)
        {
            List<int> indices = new List<int>();
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return indices.ToArray();
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (fieldClassifier.IsModelUsageFieldName(listCollection.Lists[listIndex].elementFields[i]))
                {
                    indices.Add(i);
                }
            }

            return indices.ToArray();
        }

        public static bool IsModelPickerExtensionAllowed(string package, string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }
            if (string.Equals(extension, ".ecm", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public static void AddPathLookupKey(Dictionary<string, List<int>> lookup, string key, int pathId)
        {
            List<int> ids;
            if (!lookup.TryGetValue(key, out ids))
            {
                ids = new List<int>();
                lookup[key] = ids;
            }
            if (!ids.Contains(pathId))
            {
                ids.Add(pathId);
            }
        }

        public static string NormalizeModelPathLookupKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace('/', '\\').Trim().TrimStart('\\').ToLowerInvariant();
        }

        public static bool TrySplitModelPackagePath(string normalizedPath, out string package, out string relative)
        {
            package = string.Empty;
            relative = normalizedPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return false;
            }

            for (int i = 0; i < ModelPickerCatalog.PathPackages.Length; i++)
            {
                string pkg = ModelPickerCatalog.PathPackages[i];
                string prefix = pkg + "\\";
                if (normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    package = pkg;
                    relative = normalizedPath.Substring(prefix.Length);
                    return !string.IsNullOrWhiteSpace(relative);
                }
            }

            return false;
        }
    }
}
