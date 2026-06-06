using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace FWEledit
{
    public sealed class ModelPickerService
    {
        private sealed class ModelPickerListContext
        {
            public Dictionary<int, int> UsesByPathId { get; } = new Dictionary<int, int>();
            public Dictionary<int, int> SampleItemIdByPathId { get; } = new Dictionary<int, int>();
            public Dictionary<int, string> SampleItemNameByPathId { get; } = new Dictionary<int, string>();
            public Dictionary<int, string> SampleIconKeyByPathId { get; } = new Dictionary<int, string>();
            public Dictionary<int, float> SampleScaleByPathId { get; } = new Dictionary<int, float>();
            public Dictionary<string, List<int>> PathIdsByNormalizedPath { get; set; } = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        }

        private readonly ModelPickerCacheService cacheService;
        private readonly AssetManager assetManager;
        private readonly IconResolutionService iconResolutionService;
        private readonly IdGenerationService idGenerationService;
        private readonly ItemFieldClassifierService fieldClassifier;
        private readonly Dictionary<string, ModelPickerListContext> listContextCache
            = new Dictionary<string, ModelPickerListContext>(StringComparer.Ordinal);
        private readonly object listContextSyncRoot = new object();

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
            int nextIndex = 1;
            for (int p = 0; p < ModelPickerCatalog.PackageOrder.Length; p++)
            {
                string package = ModelPickerCatalog.PackageOrder[p];
                List<ModelPickerEntry> packageEntries = BuildModelPickerEntriesForPackage(
                    listCollection,
                    database,
                    listIndex,
                    package,
                    iconLoader,
                    missingPackageNotifier);

                for (int i = 0; i < packageEntries.Count; i++)
                {
                    packageEntries[i].Index = nextIndex++;
                    packageEntries[i].IndexSearch = packageEntries[i].Index.ToString();
                    entries.Add(packageEntries[i]);
                }
            }

            return entries;
        }

        public List<ModelPickerEntry> BuildModelPickerEntriesForPackage(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            string package,
            Func<string, Bitmap> iconLoader,
            Action<string> missingPackageNotifier,
            string contextCacheKey = null)
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
            if (string.IsNullOrWhiteSpace(package))
            {
                return entries;
            }

            ModelPickerListContext context = GetOrBuildListContext(
                listCollection,
                database,
                listIndex,
                contextCacheKey);
            int idx = 1;
            List<string> packageFiles = EnumerateModelPickerPackageFiles(package, missingPackageNotifier);
            for (int i = 0; i < packageFiles.Count; i++)
            {
                string relativePath = packageFiles[i];
                int pathId = ResolveModelEntryPathId(package, relativePath, context.PathIdsByNormalizedPath, context.UsesByPathId);
                int uses = 0;
                int itemId = 0;
                string itemName = string.Empty;
                string iconKey = string.Empty;
                float previewScale = 1f;
                string mappedPath = BuildMappedPath(package, relativePath);
                Bitmap icon = Properties.Resources.NoIcon;
                if (pathId > 0)
                {
                    context.UsesByPathId.TryGetValue(pathId, out uses);
                    context.SampleItemIdByPathId.TryGetValue(pathId, out itemId);
                    context.SampleItemNameByPathId.TryGetValue(pathId, out itemName);
                    context.SampleScaleByPathId.TryGetValue(pathId, out previewScale);
                    string mappedFromPathId;
                    if (database.pathById.TryGetValue(pathId, out mappedFromPathId) && !string.IsNullOrWhiteSpace(mappedFromPathId))
                    {
                        mappedPath = mappedFromPathId;
                    }

                    if (context.SampleIconKeyByPathId.TryGetValue(pathId, out iconKey)
                        && !string.IsNullOrWhiteSpace(iconKey)
                        && database != null
                        && database.sourceBitmap != null
                        && database.ContainsKey(iconKey))
                    {
                        icon = null;
                    }
                }

                entries.Add(new ModelPickerEntry
                {
                    Index = idx++,
                    PathId = pathId,
                    Package = package,
                    RelativePath = relativePath,
                    MappedPath = mappedPath,
                    Icon = icon,
                    IconKey = iconKey,
                    SearchKey = BuildEntrySearchKey(relativePath, itemName, itemId, pathId),
                    IndexSearch = idx.ToString(),
                    ItemId = itemId,
                    ItemName = itemName,
                    Uses = uses,
                    PreviewScale = NormalizePreviewScale(previewScale)
                });
            }

            return entries;
        }

        private ModelPickerListContext GetOrBuildListContext(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            string contextCacheKey)
        {
            string safeKey = contextCacheKey ?? string.Empty;
            if (string.IsNullOrWhiteSpace(safeKey))
            {
                return BuildListContext(listCollection, database, listIndex);
            }

            lock (listContextSyncRoot)
            {
                if (listContextCache.TryGetValue(safeKey, out ModelPickerListContext cached)
                    && cached != null)
                {
                    return cached;
                }
            }

            ModelPickerListContext built = BuildListContext(listCollection, database, listIndex);
            lock (listContextSyncRoot)
            {
                if (!listContextCache.ContainsKey(safeKey))
                {
                    listContextCache[safeKey] = built;
                }

                if (listContextCache.Count > 8)
                {
                    listContextCache.Clear();
                    listContextCache[safeKey] = built;
                }
            }

            return built;
        }

        private ModelPickerListContext BuildListContext(
            eListCollection listCollection,
            CacheSave database,
            int listIndex)
        {
            ModelPickerListContext context = new ModelPickerListContext();
            int[] modelFields = GetModelUsageFieldIndices(listCollection, listIndex);
            int idFieldIndex = idGenerationService.GetIdFieldIndex(listCollection, listIndex);
            int nameFieldIndex = -1;
            int iconFieldIndex = -1;
            List<int> scaleFieldIndices = new List<int>();
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                string fieldName = listCollection.Lists[listIndex].elementFields[i];
                if (string.Equals(fieldName, "name", StringComparison.OrdinalIgnoreCase))
                {
                    nameFieldIndex = i;
                }
                if (iconFieldIndex < 0 && fieldClassifier.IsIconFieldName(fieldName))
                {
                    iconFieldIndex = i;
                }
                if (LooksLikeScaleFieldName(fieldName))
                {
                    scaleFieldIndices.Add(i);
                }
            }

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
                        string modelRawValue = listCollection.GetValue(listIndex, row, modelFields[mf]);
                        int rawPathId;
                        if (!TryExtractPathId(modelRawValue, out rawPathId) || rawPathId <= 0)
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
                        context.UsesByPathId.TryGetValue(pathId, out c);
                        context.UsesByPathId[pathId] = c + 1;
                        if (!context.SampleItemIdByPathId.ContainsKey(pathId))
                        {
                            context.SampleItemIdByPathId[pathId] = itemId;
                            context.SampleItemNameByPathId[pathId] = itemName;
                            if (TryResolveRowPreviewScale(listCollection, listIndex, row, modelFieldName, modelRawValue, scaleFieldIndices, out float scaleValue))
                            {
                                context.SampleScaleByPathId[pathId] = scaleValue;
                            }
                            if (iconFieldIndex >= 0)
                            {
                                string iconKey = iconResolutionService.ResolveIconKeyForList(database, listCollection, listIndex, listCollection.GetValue(listIndex, row, iconFieldIndex));
                                if (!string.IsNullOrWhiteSpace(iconKey))
                                {
                                    context.SampleIconKeyByPathId[pathId] = iconKey;
                                }
                            }
                        }
                    }
                }
            }

            context.PathIdsByNormalizedPath = BuildModelPathIdLookup(database);
            return context;
        }

        private static string BuildEntrySearchKey(string relativePath, string itemName, int itemId, int pathId)
        {
            return ((relativePath ?? string.Empty)
                    + "|"
                    + (itemName ?? string.Empty)
                    + "|"
                    + itemId.ToString()
                    + "|"
                    + pathId.ToString())
                .ToLowerInvariant();
        }

        private static bool LooksLikeScaleFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim().ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty);

            if (normalized == "size")
            {
                return true;
            }

            return normalized.Contains("scalemodel")
                || normalized == "scale"
                || normalized.Contains("bodyscale")
                || normalized.Contains("modelscale")
                || normalized.Contains("weaponscale")
                || normalized.Contains("itemscale")
                || normalized.Contains("ratio")
                || normalized.Contains("factor")
                || normalized.Contains("proportion")
                || normalized.Contains("percent")
                || normalized.Contains("zoom")
                || normalized.Contains("mul")
                || normalized.Contains("size")
                || fieldName.Contains("缩放")
                || fieldName.Contains("比例")
                || fieldName.Contains("尺寸")
                || fieldName.Contains("大小")
                || fieldName.Contains("倍率");
        }

        private static bool TryResolveRowPreviewScale(
            eListCollection listCollection,
            int listIndex,
            int row,
            string modelFieldName,
            string modelFieldRawValue,
            List<int> scaleFieldIndices,
            out float scale)
        {
            scale = 1f;
            if (listCollection == null)
            {
                return false;
            }

            int bestScore = int.MinValue;
            float bestValue = 1f;
            if (TryExtractInlineScaleFromModelValue(modelFieldRawValue, out float inlineScale))
            {
                bestScore = 500;
                bestValue = inlineScale;
            }

            if (scaleFieldIndices == null || scaleFieldIndices.Count == 0)
            {
                if (bestScore == int.MinValue)
                {
                    return false;
                }

                scale = bestValue;
                return scale > 0.01f;
            }

            for (int i = 0; i < scaleFieldIndices.Count; i++)
            {
                int fieldIndex = scaleFieldIndices[i];
                if (fieldIndex < 0)
                {
                    continue;
                }

                string scaleFieldName = listCollection.Lists[listIndex].elementFields[fieldIndex];
                if (!TryParsePreviewScale(listCollection.GetValue(listIndex, row, fieldIndex), out float parsed))
                {
                    continue;
                }

                int score = GetScaleFieldAffinity(modelFieldName, scaleFieldName);
                if (IsPrimaryScaleField(scaleFieldName))
                {
                    score += 200;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestValue = parsed;
                }
            }

            if (bestScore == int.MinValue)
            {
                return false;
            }

            scale = bestValue;
            return scale > 0.01f;
        }

        private static bool TryExtractInlineScaleFromModelValue(string modelFieldRawValue, out float scale)
        {
            scale = 1f;
            if (string.IsNullOrWhiteSpace(modelFieldRawValue))
            {
                return false;
            }

            string[] parts = modelFieldRawValue.Split(new[] { '|', ';', ',', ':', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts == null || parts.Length < 2)
            {
                return false;
            }

            int parsedPathId = 0;
            int.TryParse(parts[0], out parsedPathId);

            for (int i = 1; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (!float.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed)
                    && !float.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out parsed))
                {
                    continue;
                }

                if (parsedPathId > 0 && Math.Abs(parsed - parsedPathId) < 0.01f)
                {
                    continue;
                }

                if (parsed <= 0f || parsed < 0.1f || parsed > 5000f)
                {
                    continue;
                }

                scale = NormalizePreviewScale(parsed);
                return scale > 0.01f;
            }

            return false;
        }

        private static bool IsPrimaryScaleField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim().ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty);

            return normalized.Contains("scalemodel")
                || normalized == "scale"
                || normalized == "modelscale";
        }

        private static int GetScaleFieldAffinity(string modelFieldName, string scaleFieldName)
        {
            if (string.IsNullOrWhiteSpace(scaleFieldName))
            {
                return int.MinValue;
            }

            int score = 0;
            string model = NormalizeFieldToken(modelFieldName);
            string scale = NormalizeFieldToken(scaleFieldName);

            if (IsFemaleMarker(model) && IsFemaleMarker(scale))
            {
                score += 120;
            }
            if (IsMaleMarker(model) && IsMaleMarker(scale))
            {
                score += 120;
            }
            if (model.Contains("weapon") && scale.Contains("weapon"))
            {
                score += 30;
            }
            if (model.Contains("model") && scale.Contains("model"))
            {
                score += 20;
            }
            if (!string.IsNullOrWhiteSpace(model)
                && !string.IsNullOrWhiteSpace(scale)
                && (scale.Contains(model) || model.Contains(scale)))
            {
                score += 40;
            }

            return score;
        }

        private static string NormalizeFieldToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);
        }

        private static bool IsFemaleMarker(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return token.Contains("female")
                || token.Contains("woman")
                || token.Contains("girl")
                || token.Contains("feminino")
                || token.Contains("mulher")
                || token.Contains("lady")
                || token.Contains("女");
        }

        private static bool IsMaleMarker(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return token.Contains("male")
                || token.Contains("man")
                || token.Contains("boy")
                || token.Contains("masculino")
                || token.Contains("homem")
                || token.Contains("gent")
                || token.Contains("男");
        }

        private static bool TryParsePreviewScale(string raw, out float scale)
        {
            scale = 1f;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string normalized = raw.Trim();
            int pipe = normalized.IndexOf('|');
            if (pipe > 0)
            {
                normalized = normalized.Substring(0, pipe).Trim();
            }

            if (!float.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed)
                && !float.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out parsed))
            {
                return false;
            }
            if (parsed <= 0f)
            {
                return false;
            }
            if (parsed < 0.1f)
            {
                return false;
            }

            scale = NormalizePreviewScale(parsed);
            return scale > 0.01f;
        }

        private static float NormalizePreviewScale(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 1f;
            }

            float normalized = value;
            if (normalized > 10f && normalized <= 500f)
            {
                normalized /= 100f;
            }
            else if (normalized > 500f && normalized <= 5000f)
            {
                normalized /= 1000f;
            }

            if (normalized < 0.05f)
            {
                normalized = 0.05f;
            }
            if (normalized > 8f)
            {
                normalized = 8f;
            }

            return normalized;
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
                return mappedPath;
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
            bool shiftedField = fieldClassifier.IsShiftedModelFieldName(fieldName, listName);
            bool directModelPathField = IsDirectModelPathFieldName(fieldName);
            if (shiftedField)
            {
                // Some legacy model fields store a shifted PathID reference (+1).
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

            bool requireModelLikePath = fieldClassifier.IsModelFieldName(fieldName) || fieldClassifier.IsGfxFieldName(fieldName);
            if (requireModelLikePath)
            {
                if (!shiftedField
                    && directModelPathField
                    && TryGetModelLikeCandidate(database, pathId, out string exactMapped))
                {
                    resolvedPathId = pathId;
                    mappedPath = exactMapped;
                    return true;
                }

                int bestScore = int.MinValue;
                int bestPathId = 0;
                string bestMappedPath = string.Empty;
                for (int i = 0; i < candidates.Count; i++)
                {
                    int candidate = candidates[i];
                    if (!TryGetModelLikeCandidate(database, candidate, out string mapped))
                    {
                        continue;
                    }
                    string ext = Path.GetExtension(mapped);

                    int score = ScoreModelPathCandidate(pathId, candidate, mapped, ext, fieldName, listName);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPathId = candidate;
                        bestMappedPath = mapped;
                    }
                }

                if (bestScore > int.MinValue)
                {
                    resolvedPathId = bestPathId;
                    mappedPath = bestMappedPath;
                    return true;
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

        private bool TryGetModelLikeCandidate(CacheSave database, int candidatePathId, out string mappedPath)
        {
            mappedPath = string.Empty;
            if (database == null || database.pathById == null || candidatePathId <= 0)
            {
                return false;
            }

            if (!database.pathById.TryGetValue(candidatePathId, out string mapped) || string.IsNullOrWhiteSpace(mapped))
            {
                return false;
            }

            if (!LooksLikeModelMappedPath(mapped))
            {
                return false;
            }

            string ext = Path.GetExtension(mapped);
            if (!IsModelFieldExtensionAllowed(ext))
            {
                return false;
            }

            mappedPath = mapped;
            return true;
        }

        public bool TryExtractPathId(string rawValue, out int pathId)
        {
            pathId = 0;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            string trimmed = rawValue.Trim();
            string[] parts = trimmed.Split(new[] { '|', ';', ',', ':', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts == null || parts.Length == 0)
            {
                return false;
            }

            return int.TryParse(parts[0], out pathId);
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
            return IsModelFieldExtensionAllowed(extension);
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

        private static string BuildMappedPath(string package, string relativePath)
        {
            string safePackage = (package ?? string.Empty).Trim();
            string safeRelative = (relativePath ?? string.Empty).Replace('/', '\\').Trim().TrimStart('\\');
            if (string.IsNullOrWhiteSpace(safeRelative))
            {
                return string.Empty;
            }
            if (string.IsNullOrWhiteSpace(safePackage))
            {
                return safeRelative;
            }
            return safePackage + "\\" + safeRelative;
        }

        private static bool IsModelFieldExtensionAllowed(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }
            return string.Equals(extension, ".ecm", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".gfx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".ski", StringComparison.OrdinalIgnoreCase);
        }

        private static int ScoreModelPathCandidate(
            int requestedPathId,
            int candidatePathId,
            string mappedPath,
            string extension,
            string fieldName,
            string listName)
        {
            int score = 0;
            int distance = Math.Abs(candidatePathId - requestedPathId);
            score += Math.Max(0, 180 - (distance * 70));

            if (string.Equals(extension, ".ecm", StringComparison.OrdinalIgnoreCase))
            {
                score += 220;
            }
            else if (string.Equals(extension, ".gfx", StringComparison.OrdinalIgnoreCase))
            {
                score += 170;
            }
            else if (string.Equals(extension, ".ski", StringComparison.OrdinalIgnoreCase))
            {
                score += 90;
            }

            bool modelNameField = !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.Trim().StartsWith("model_name", StringComparison.OrdinalIgnoreCase);
            bool aircraftLikeList = IsAircraftLikeListName(listName);
            string normalizedPath = (mappedPath ?? string.Empty).Replace('/', '\\').ToLowerInvariant();

            if (modelNameField)
            {
                if (string.Equals(extension, ".ecm", StringComparison.OrdinalIgnoreCase))
                {
                    score += 120;
                }
                else if (string.Equals(extension, ".ski", StringComparison.OrdinalIgnoreCase))
                {
                    score -= 120;
                }
            }

            if (aircraftLikeList)
            {
                if (string.Equals(extension, ".ecm", StringComparison.OrdinalIgnoreCase))
                {
                    score += 160;
                }
                else if (string.Equals(extension, ".ski", StringComparison.OrdinalIgnoreCase))
                {
                    score -= 260;
                }
            }

            if (normalizedPath.Contains("\\players\\"))
            {
                score -= modelNameField || aircraftLikeList ? 300 : 100;
            }
            if (aircraftLikeList && normalizedPath.Contains("\\npcs\\"))
            {
                score += 40;
            }

            return score;
        }

        private static bool IsDirectModelPathFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return normalized.StartsWith("models_", StringComparison.OrdinalIgnoreCase)
                && normalized.IndexOf("_file_model_", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsAircraftLikeListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return false;
            }

            string normalized = listName.Trim();
            return normalized.IndexOf("AIRCRAFT", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("FLYSWORD", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("MOUNT", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
