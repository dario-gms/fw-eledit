using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;

namespace FWEledit
{
    public sealed class ItemTransferPackageService
    {
        private const string PackageFormat = "FWEledit.ItemTransfer";
        private const int PackageFormatVersion = 1;
        private static readonly string[] KnownPackages = new string[]
        {
            "building", "configs", "gfx", "grasses", "interfaces", "litmodels", "loddata", "models", "models2",
            "moxing", "script", "sfx", "shaders", "surfaces", "textures", "music"
        };

        private static readonly Regex AssetExtensionPattern = new Regex(
            "\\.(dds|tga|bmp|png|jpg|jpeg|ski|smd|ecm|gfx|att|sgc|bon|stck|txt|ini|cfg|sdr)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ItemTransferExportResult ExportItemPackage(
            eListCollection listCollection,
            CacheSave database,
            AssetManager assetManager,
            int listIndex,
            int itemIndex,
            string outputFile,
            Action<ItemTransferProgressInfo> progress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ItemTransferExportResult result = new ItemTransferExportResult { MissingAssets = new List<string>() };
            try
            {
                ReportProgress(progress, "Preparing export", "Validating selected item...", 0, 0, true);
                cancellationToken.ThrowIfCancellationRequested();

                if (listCollection == null || database == null || assetManager == null)
                {
                    result.ErrorMessage = "No loaded elements data.";
                    return result;
                }
                if (listIndex < 0 || listIndex >= listCollection.Lists.Length)
                {
                    result.ErrorMessage = "Invalid source list.";
                    return result;
                }
                eList list = listCollection.Lists[listIndex];
                if (list == null || itemIndex < 0 || itemIndex >= list.elementValues.Length)
                {
                    result.ErrorMessage = "Invalid source item.";
                    return result;
                }
                if (!IsEquipmentEssenceList(list))
                {
                    result.ErrorMessage = "Item package export currently supports only Equipment Essence.";
                    return result;
                }
                if (string.IsNullOrWhiteSpace(outputFile))
                {
                    result.ErrorMessage = "Invalid output file.";
                    return result;
                }

                ReportProgress(progress, "Reading item", "Collecting element fields and path.data entries...", 0, 0, true);
                ItemTransferPackageManifest manifest = BuildManifest(listCollection, database, listIndex, itemIndex);

                ReportProgress(progress, "Collecting equipment assets", "Finding direct model, icon and file paths from this equipment item...", 0, 0, true);
                Dictionary<string, ItemTransferAssetEntry> assetsByKey = CollectDirectAssets(manifest, database, assetManager);

                ExpandEquipmentGfxDependencies(assetsByKey, assetManager, progress, cancellationToken);
                ExpandEquipmentModelCompanions(assetsByKey, assetManager, progress, cancellationToken);
                manifest.Assets = assetsByKey.Values.OrderBy(a => a.Package).ThenBy(a => a.RelativePath).ToList();

                string folder = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                ReportProgress(progress, "Creating package", "Writing manifest and assets...", 0, manifest.Assets.Count, false);
                using (ZipArchive archive = ZipFile.Open(outputFile, ZipArchiveMode.Create))
                {
                    WriteTextEntry(archive, "manifest.json", JsonConvert.SerializeObject(manifest, Formatting.Indented));
                    int written = 0;
                    foreach (ItemTransferAssetEntry asset in manifest.Assets)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        ReportProgress(
                            progress,
                            "Writing package",
                            asset.MappedPath ?? asset.RelativePath ?? string.Empty,
                            written,
                            manifest.Assets.Count,
                            false);

                        byte[] payload;
                        string error;
                        if (assetManager.TryReadPackageEntry(asset.Package, asset.RelativePath, out payload, out error) && payload != null)
                        {
                            asset.Size = payload.Length;
                            ZipArchiveEntry entry = archive.CreateEntry(asset.ZipPath, CompressionLevel.Optimal);
                            using (Stream stream = entry.Open())
                            {
                                stream.Write(payload, 0, payload.Length);
                            }
                        }
                        else
                        {
                            result.MissingAssets.Add(asset.MappedPath ?? asset.RelativePath ?? string.Empty);
                        }
                        written++;
                    }
                }

                result.Success = true;
                result.AssetCount = manifest.Assets.Count;
                result.MissingAssetCount = result.MissingAssets.Count;
                ReportProgress(progress, "Export complete", "Item package created.", result.AssetCount, result.AssetCount, false);
                return result;
            }
            catch (OperationCanceledException)
            {
                TryDeletePartialPackage(outputFile);
                result.ErrorMessage = "Export cancelled.";
                return result;
            }
            catch (Exception ex)
            {
                TryDeletePartialPackage(outputFile);
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public ItemTransferImportResult ImportItemPackage(
            eListCollection listCollection,
            CacheSave database,
            AssetManager assetManager,
            IdGenerationService idGenerationService,
            string packageFile)
        {
            ItemTransferImportResult result = new ItemTransferImportResult { TargetListIndex = -1, NewItemIndex = -1 };
            if (listCollection == null || database == null || assetManager == null || idGenerationService == null)
            {
                result.ErrorMessage = "No loaded elements data.";
                return result;
            }
            if (string.IsNullOrWhiteSpace(packageFile) || !File.Exists(packageFile))
            {
                result.ErrorMessage = "Item package was not found.";
                return result;
            }

            using (ZipArchive archive = ZipFile.OpenRead(packageFile))
            {
                ZipArchiveEntry manifestEntry = archive.GetEntry("manifest.json");
                if (manifestEntry == null)
                {
                    result.ErrorMessage = "Invalid item package: manifest.json was not found.";
                    return result;
                }

                ItemTransferPackageManifest manifest;
                using (StreamReader reader = new StreamReader(manifestEntry.Open()))
                {
                    manifest = JsonConvert.DeserializeObject<ItemTransferPackageManifest>(reader.ReadToEnd());
                }

                if (manifest == null
                    || !string.Equals(manifest.Format, PackageFormat, StringComparison.Ordinal)
                    || manifest.FormatVersion < 1)
                {
                    result.ErrorMessage = "Invalid or unsupported item package.";
                    return result;
                }

                int targetListIndex = ResolveTargetListIndex(listCollection, manifest);
                if (targetListIndex < 0)
                {
                    result.ErrorMessage = "Target list was not found: " + (manifest.SourceListName ?? string.Empty);
                    return result;
                }

                Dictionary<int, int> pathIdRemap = ImportPathDataEntries(manifest, database, assetManager, result);
                ImportAssets(archive, manifest, assetManager, result);

                int newIndex = AddManifestItem(listCollection, targetListIndex, manifest, pathIdRemap, idGenerationService, out int newId);
                result.Success = true;
                result.TargetListIndex = targetListIndex;
                result.NewItemIndex = newIndex;
                result.NewId = newId;
                result.RemappedPathIdCount = pathIdRemap.Count(pair => pair.Key != pair.Value);
                return result;
            }
        }

        private static ItemTransferPackageManifest BuildManifest(eListCollection listCollection, CacheSave database, int listIndex, int itemIndex)
        {
            eList list = listCollection.Lists[listIndex];
            List<ItemTransferFieldValue> fields = new List<ItemTransferFieldValue>();
            for (int fieldIndex = 0; fieldIndex < list.elementFields.Length; fieldIndex++)
            {
                fields.Add(new ItemTransferFieldValue
                {
                    Name = list.elementFields[fieldIndex],
                    Type = list.elementTypes[fieldIndex],
                    Value = list.GetValue(itemIndex, fieldIndex)
                });
            }

            int id = TryParseInt(GetFieldValue(fields, "id"));
            string name = GetFieldValue(fields, "name");
            return new ItemTransferPackageManifest
            {
                Format = PackageFormat,
                FormatVersion = PackageFormatVersion,
                ElementsVersion = listCollection.Version,
                SourceListIndex = listIndex,
                SourceListName = list.listName,
                OriginalItemIndex = itemIndex,
                OriginalId = id,
                OriginalName = name,
                ExportedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Fields = fields,
                PathDataEntries = new List<ItemTransferPathDataEntry>(),
                Assets = new List<ItemTransferAssetEntry>()
            };
        }

        private static Dictionary<string, ItemTransferAssetEntry> CollectDirectAssets(
            ItemTransferPackageManifest manifest,
            CacheSave database,
            AssetManager assetManager)
        {
            Dictionary<string, ItemTransferAssetEntry> assets = new Dictionary<string, ItemTransferAssetEntry>(StringComparer.OrdinalIgnoreCase);
            HashSet<int> addedPathIds = new HashSet<int>();
            if (manifest.Fields == null)
            {
                return assets;
            }

            foreach (ItemTransferFieldValue field in manifest.Fields)
            {
                string fieldName = field.Name ?? string.Empty;
                string value = field.Value ?? string.Empty;
                if (IsPackageAssetPathIdField(fieldName) && database.pathById != null)
                {
                    int pathId = TryParseInt(value);
                    string mapped;
                    if (pathId > 0 && database.pathById.TryGetValue(pathId, out mapped) && !string.IsNullOrWhiteSpace(mapped))
                    {
                        if (addedPathIds.Add(pathId))
                        {
                            manifest.PathDataEntries.Add(new ItemTransferPathDataEntry { PathId = pathId, MappedPath = NormalizePath(mapped) });
                        }
                        AddAsset(assets, mapped, assetManager);
                    }
                }

                if (LooksLikeAssetPath(value))
                {
                    AddAsset(assets, value, assetManager);
                }
            }

            return assets;
        }

        private static bool AddAsset(Dictionary<string, ItemTransferAssetEntry> assets, string mappedPath, AssetManager assetManager)
        {
            string package;
            string relative;
            if (!TrySplitPackagePath(mappedPath, out package, out relative))
            {
                return false;
            }

            string key = package + "|" + relative;
            if (assets.ContainsKey(key))
            {
                return false;
            }

            assets.Add(key, new ItemTransferAssetEntry
            {
                Package = package,
                RelativePath = relative,
                MappedPath = package + "\\" + relative,
                ZipPath = "assets/" + package + "/" + relative.Replace('\\', '/')
            });
            return true;
        }

        private static void ExpandEquipmentGfxDependencies(
            Dictionary<string, ItemTransferAssetEntry> assets,
            AssetManager assetManager,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            Queue<ItemTransferAssetEntry> pending = new Queue<ItemTransferAssetEntry>(
                assets.Values.Where(IsGfxAsset).ToArray());
            HashSet<string> processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const int MaxProcessedGfx = 256;

            while (pending.Count > 0 && processed.Count < MaxProcessedGfx)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ItemTransferAssetEntry current = pending.Dequeue();
                if (current == null || !processed.Add(current.Package + "|" + current.RelativePath))
                {
                    continue;
                }

                ReportProgress(
                    progress,
                    "Resolving GFX dependencies",
                    current.MappedPath ?? current.RelativePath ?? string.Empty,
                    processed.Count,
                    Math.Max(processed.Count + pending.Count, assets.Count),
                    false);

                byte[] payload;
                string error;
                if (!assetManager.TryReadPackageEntry(current.Package, current.RelativePath, out payload, out error) || payload == null)
                {
                    continue;
                }

                foreach (string dependency in CollectGfxReferenceCandidates(current, payload))
                {
                    ItemTransferAssetEntry addedAsset;
                    if (TryAddExistingAsset(assets, dependency, assetManager, out addedAsset)
                        && IsGfxAsset(addedAsset))
                    {
                        pending.Enqueue(addedAsset);
                    }
                }
            }
        }

        private static bool IsGfxAsset(ItemTransferAssetEntry asset)
        {
            return asset != null
                && string.Equals(asset.Package, "gfx", StringComparison.OrdinalIgnoreCase)
                && string.Equals(Path.GetExtension(asset.RelativePath), ".gfx", StringComparison.OrdinalIgnoreCase);
        }

        private static void ExpandEquipmentModelCompanions(
            Dictionary<string, ItemTransferAssetEntry> assets,
            AssetManager assetManager,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            if (assets == null || assets.Count == 0 || assetManager == null)
            {
                return;
            }

            Dictionary<string, List<string>> entriesByPackage = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> processedCompanionPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Queue<ItemTransferAssetEntry> pending = new Queue<ItemTransferAssetEntry>(
                assets.Values.Where(a => a != null && IsModelLikeExtension(Path.GetExtension(a.RelativePath))).ToArray());
            HashSet<string> processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const int MaxProcessedModels = 512;

            while (pending.Count > 0 && processed.Count < MaxProcessedModels)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ItemTransferAssetEntry current = pending.Dequeue();
                if (current == null || !processed.Add(current.Package + "|" + current.RelativePath))
                {
                    continue;
                }

                ReportProgress(
                    progress,
                    "Collecting model companions",
                    current.MappedPath ?? current.RelativePath ?? string.Empty,
                    processed.Count,
                    Math.Max(processed.Count + pending.Count, assets.Count),
                    false);

                byte[] payload;
                string error;
                if (assetManager.TryReadPackageEntry(current.Package, current.RelativePath, out payload, out error) && payload != null)
                {
                    foreach (string dependency in CollectModelReferenceCandidates(current.MappedPath, payload))
                    {
                        ItemTransferAssetEntry addedAsset;
                        if (TryAddExistingAsset(assets, dependency, assetManager, out addedAsset)
                            && addedAsset != null
                            && IsModelLikeExtension(Path.GetExtension(addedAsset.RelativePath)))
                        {
                            pending.Enqueue(addedAsset);
                        }
                    }
                }

                foreach (string companion in CollectCompanionAssets(current, assetManager, entriesByPackage, processedCompanionPrefixes))
                {
                    ItemTransferAssetEntry addedAsset;
                    if (TryAddExistingAsset(assets, companion, assetManager, out addedAsset)
                        && addedAsset != null
                        && IsModelLikeExtension(Path.GetExtension(addedAsset.RelativePath)))
                    {
                        pending.Enqueue(addedAsset);
                    }
                }
            }
        }

        private static IEnumerable<string> CollectModelReferenceCandidates(string currentMappedPath, byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                yield break;
            }

            HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string text = DecodeGbkPayload(payload);
            MatchCollection matches = Regex.Matches(
                text,
                @"[^\0\r\n\t""'<>|:*?]{1,220}\.(?:dds|tga|bmp|png|jpg|jpeg|ski|smd|ecm|att|sgc|bon|stck|sdr)",
                RegexOptions.IgnoreCase);

            for (int i = 0; i < matches.Count; i++)
            {
                string raw = NormalizePath(matches[i].Value.Trim().Trim('\0').TrimStart('.', '\\', '/'));
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                foreach (string candidate in ResolveReferenceCandidates(currentMappedPath, raw))
                {
                    if (yielded.Add(candidate))
                    {
                        yield return candidate;
                    }
                }
            }
        }

        private static bool TryAddExistingAsset(
            Dictionary<string, ItemTransferAssetEntry> assets,
            string mappedPath,
            AssetManager assetManager,
            out ItemTransferAssetEntry addedAsset)
        {
            addedAsset = null;
            string package;
            string relative;
            if (!TrySplitPackagePath(mappedPath, out package, out relative))
            {
                return false;
            }

            string key = package + "|" + relative;
            if (assets.TryGetValue(key, out addedAsset))
            {
                return false;
            }

            byte[] payload;
            string error;
            if (!assetManager.TryReadPackageEntry(package, relative, out payload, out error) || payload == null)
            {
                return false;
            }

            AddAsset(assets, package + "\\" + relative, assetManager);
            addedAsset = assets[key];
            return true;
        }

        private static IEnumerable<string> CollectGfxReferenceCandidates(ItemTransferAssetEntry current, byte[] payload)
        {
            if (current == null || payload == null || payload.Length == 0)
            {
                yield break;
            }

            HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string text = DecodeGbkPayload(payload);
            MatchCollection matches = Regex.Matches(
                text,
                @"[^\0\r\n\t""'<>|:*?]{1,220}\.(?:dds|tga|bmp|png|jpg|jpeg|smd|gfx)",
                RegexOptions.IgnoreCase);

            for (int i = 0; i < matches.Count; i++)
            {
                string raw = NormalizePath(matches[i].Value.Trim().Trim('\0').TrimStart('.', '\\', '/'));
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                foreach (string candidate in ResolveGfxDependencyCandidates(current, raw))
                {
                    if (yielded.Add(candidate))
                    {
                        yield return candidate;
                    }
                }
            }
        }

        private static IEnumerable<string> ResolveGfxDependencyCandidates(ItemTransferAssetEntry current, string reference)
        {
            string package;
            string relative;
            if (TrySplitPackagePath(reference, out package, out relative))
            {
                yield return package + "\\" + relative;
                yield break;
            }

            string currentPackage = current.Package ?? string.Empty;
            string currentDirectory = Path.GetDirectoryName(current.RelativePath) ?? string.Empty;

            yield return currentPackage + "\\" + reference;
            if (!string.IsNullOrWhiteSpace(currentDirectory))
            {
                yield return currentPackage + "\\" + currentDirectory + "\\" + reference;
            }

            yield return currentPackage + "\\models\\" + reference;
            yield return currentPackage + "\\textures\\" + reference;
        }

        private static void ExpandAssetDependencies(
            Dictionary<string, ItemTransferAssetEntry> assets,
            AssetManager assetManager,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            Queue<ItemTransferAssetEntry> pending = new Queue<ItemTransferAssetEntry>(assets.Values.ToArray());
            HashSet<string> processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> processedCompanionPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<string>> entriesByPackage = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            while (pending.Count > 0 && processed.Count < 4000)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ItemTransferAssetEntry current = pending.Dequeue();
                if (current == null || !processed.Add(current.Package + "|" + current.RelativePath))
                {
                    continue;
                }

                ReportProgress(
                    progress,
                    "Resolving dependencies",
                    current.MappedPath ?? current.RelativePath ?? string.Empty,
                    processed.Count,
                    Math.Max(processed.Count + pending.Count, assets.Count),
                    false);

                byte[] payload;
                string error;
                if (assetManager.TryReadPackageEntry(current.Package, current.RelativePath, out payload, out error) && payload != null)
                {
                    foreach (string dependency in CollectReferenceCandidates(current.MappedPath, payload))
                    {
                        if (AddAsset(assets, dependency, assetManager))
                        {
                            pending.Enqueue(assets[BuildAssetKey(dependency)]);
                        }
                    }
                }

                foreach (string companion in CollectCompanionAssets(current, assetManager, entriesByPackage, processedCompanionPrefixes))
                {
                    if (AddAsset(assets, companion, assetManager))
                    {
                        pending.Enqueue(assets[BuildAssetKey(companion)]);
                    }
                }
            }
        }

        private static IEnumerable<string> CollectReferenceCandidates(string currentMappedPath, byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                yield break;
            }

            HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string text = DecodePrintablePayload(payload);
            MatchCollection matches = Regex.Matches(
                text,
                @"[^\0\r\n\t""'<>|:*?]{1,220}\.(?:dds|tga|bmp|png|jpg|jpeg|ski|smd|ecm|gfx|att|sgc|bon|stck|sdr)",
                RegexOptions.IgnoreCase);
            for (int i = 0; i < matches.Count; i++)
            {
                string raw = matches[i].Value.Trim().Trim('\0').TrimStart('.', '\\', '/');
                foreach (string candidate in ResolveReferenceCandidates(currentMappedPath, raw))
                {
                    if (yielded.Add(candidate))
                    {
                        yield return candidate;
                    }
                }
            }
        }

        private static IEnumerable<string> CollectCompanionAssets(
            ItemTransferAssetEntry current,
            AssetManager assetManager,
            Dictionary<string, List<string>> entriesByPackage,
            HashSet<string> processedCompanionPrefixes)
        {
            string ext = Path.GetExtension(current.RelativePath);
            if (!IsModelLikeExtension(ext))
            {
                yield break;
            }

            List<string> entries;
            if (!entriesByPackage.TryGetValue(current.Package, out entries))
            {
                if (!assetManager.TryEnumeratePckIndexEntries(current.Package, out entries))
                {
                    entries = new List<string>();
                }
                entriesByPackage[current.Package] = entries;
            }
            if (entries == null || entries.Count == 0)
            {
                yield break;
            }

            string directory = Path.GetDirectoryName(current.RelativePath);
            string fileName = Path.GetFileNameWithoutExtension(current.RelativePath);
            foreach (string prefix in BuildCompanionPrefixes(current.Package, directory, fileName))
            {
                string package;
                string relativePrefix;
                if (!TrySplitPackagePath(prefix, out package, out relativePrefix)
                    || !string.Equals(package, current.Package, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string normalizedPrefix = NormalizePath(relativePrefix);
                if (!normalizedPrefix.EndsWith("\\", StringComparison.Ordinal))
                {
                    normalizedPrefix += "\\";
                }
                if (!processedCompanionPrefixes.Add(current.Package + "|" + normalizedPrefix))
                {
                    continue;
                }

                foreach (string entry in entries)
                {
                    string normalizedEntry = NormalizePath(entry);
                    if (normalizedEntry.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase)
                        && AssetExtensionPattern.IsMatch(normalizedEntry))
                    {
                        yield return current.Package + "\\" + normalizedEntry;
                    }
                }
            }
        }

        private static IEnumerable<string> ResolveReferenceCandidates(string currentMappedPath, string reference)
        {
            string currentPackage;
            string currentRelative;
            if (!TrySplitPackagePath(currentMappedPath, out currentPackage, out currentRelative))
            {
                yield break;
            }

            string normalizedReference = NormalizePath(reference);
            if (string.IsNullOrWhiteSpace(normalizedReference))
            {
                yield break;
            }

            string package;
            string relative;
            if (TrySplitPackagePath(normalizedReference, out package, out relative))
            {
                yield return package + "\\" + relative;
                yield break;
            }

            string currentDirectory = Path.GetDirectoryName(currentRelative) ?? string.Empty;
            if (normalizedReference.Contains("\\"))
            {
                yield return currentPackage + "\\" + normalizedReference;
                if (!string.IsNullOrWhiteSpace(currentDirectory))
                {
                    yield return currentPackage + "\\" + currentDirectory + "\\" + normalizedReference;
                }
                yield break;
            }

            if (!string.IsNullOrWhiteSpace(currentDirectory))
            {
                yield return currentPackage + "\\" + currentDirectory + "\\" + normalizedReference;
                yield return currentPackage + "\\" + currentDirectory + "\\textures\\" + normalizedReference;
                yield return currentPackage + "\\" + currentDirectory + "\\texture\\" + normalizedReference;
            }
            yield return currentPackage + "\\textures\\" + normalizedReference;
            yield return currentPackage + "\\" + normalizedReference;
        }

        private static IEnumerable<string> BuildCompanionPrefixes(string package, string directory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(package) || string.IsNullOrWhiteSpace(directory))
            {
                yield break;
            }

            yield return package + "\\" + directory;
            yield return package + "\\" + directory + "\\textures";
            yield return package + "\\" + directory + "\\texture";
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                yield return package + "\\" + directory + "\\tcks_" + fileName;
                yield return package + "\\" + directory + "\\tex_" + fileName;
            }
        }

        private static string DecodePrintablePayload(byte[] payload)
        {
            char[] chars = new char[payload.Length];
            for (int i = 0; i < payload.Length; i++)
            {
                byte value = payload[i];
                chars[i] = value >= 32 && value != 127 ? (char)value : '\0';
            }

            return new string(chars);
        }

        private static string DecodeGbkPayload(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                return Encoding.GetEncoding("GBK").GetString(payload).Replace('\0', '\n');
            }
            catch
            {
                return DecodePrintablePayload(payload);
            }
        }

        private static bool IsModelLikeExtension(string ext)
        {
            return string.Equals(ext, ".ecm", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".smd", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".ski", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".bon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".stck", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".att", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".sgc", StringComparison.OrdinalIgnoreCase);
        }

        private static void ReportProgress(
            Action<ItemTransferProgressInfo> progress,
            string stage,
            string detail,
            int current,
            int total,
            bool indeterminate)
        {
            if (progress == null)
            {
                return;
            }

            progress(new ItemTransferProgressInfo
            {
                Stage = stage,
                Detail = detail,
                Current = current,
                Total = total,
                IsIndeterminate = indeterminate
            });
        }

        private static void TryDeletePartialPackage(string outputFile)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(outputFile) && File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }
            }
            catch
            {
            }
        }

        private static string BuildAssetKey(string mappedPath)
        {
            string package;
            string relative;
            return TrySplitPackagePath(mappedPath, out package, out relative)
                ? package + "|" + relative
                : string.Empty;
        }

        private static Dictionary<int, int> ImportPathDataEntries(
            ItemTransferPackageManifest manifest,
            CacheSave database,
            AssetManager assetManager,
            ItemTransferImportResult result)
        {
            Dictionary<int, int> remap = new Dictionary<int, int>();
            if (manifest.PathDataEntries == null || manifest.PathDataEntries.Count == 0)
            {
                return remap;
            }
            if (database.pathById == null)
            {
                database.pathById = new SortedList<int, string>();
            }

            int nextPathId = database.pathById.Count > 0 ? database.pathById.Keys[database.pathById.Count - 1] + 1 : 1;
            foreach (ItemTransferPathDataEntry entry in manifest.PathDataEntries)
            {
                if (entry == null || entry.PathId <= 0 || string.IsNullOrWhiteSpace(entry.MappedPath))
                {
                    continue;
                }

                int existingPathId;
                if (assetManager.TryFindPathIdByMappedPath(entry.MappedPath, out existingPathId) && existingPathId > 0)
                {
                    remap[entry.PathId] = existingPathId;
                    if (!database.pathById.ContainsKey(existingPathId))
                    {
                        database.pathById[existingPathId] = NormalizePath(entry.MappedPath);
                    }
                    continue;
                }

                int targetPathId = entry.PathId;
                string existingMapped;
                if (database.pathById.TryGetValue(targetPathId, out existingMapped)
                    && !PathsEqual(existingMapped, entry.MappedPath))
                {
                    while (database.pathById.ContainsKey(nextPathId))
                    {
                        nextPathId++;
                    }
                    targetPathId = nextPathId++;
                }

                string canonicalPath = NormalizePath(entry.MappedPath);
                database.pathById[targetPathId] = canonicalPath;
                string queueError;
                if (!assetManager.QueuePathDataEntry(targetPathId, canonicalPath, out queueError))
                {
                    result.MissingAssetCount++;
                }
                remap[entry.PathId] = targetPathId;
            }

            return remap;
        }

        private static void ImportAssets(
            ZipArchive archive,
            ItemTransferPackageManifest manifest,
            AssetManager assetManager,
            ItemTransferImportResult result)
        {
            if (manifest.Assets == null)
            {
                return;
            }

            foreach (ItemTransferAssetEntry asset in manifest.Assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.Package) || string.IsNullOrWhiteSpace(asset.RelativePath))
                {
                    continue;
                }

                ZipArchiveEntry entry = archive.GetEntry(asset.ZipPath);
                if (entry == null)
                {
                    result.MissingAssetCount++;
                    continue;
                }

                if (!assetManager.EnsurePackageExtracted(asset.Package))
                {
                    result.MissingAssetCount++;
                    continue;
                }

                string root = assetManager.GetExtractedPackageRoot(asset.Package);
                if (string.IsNullOrWhiteSpace(root))
                {
                    result.MissingAssetCount++;
                    continue;
                }

                string outputPath = Path.Combine(root, NormalizePath(asset.RelativePath));
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                using (Stream input = entry.Open())
                using (FileStream output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    input.CopyTo(output);
                }

                assetManager.MarkWorkspacePackageDirty(asset.Package);
                result.ImportedAssetCount++;
            }
        }

        private static int AddManifestItem(
            eListCollection listCollection,
            int targetListIndex,
            ItemTransferPackageManifest manifest,
            Dictionary<int, int> pathIdRemap,
            IdGenerationService idGenerationService,
            out int newId)
        {
            eList list = listCollection.Lists[targetListIndex];
            int templateIndex = list.elementValues.Length > 0 ? list.elementValues.Length - 1 : -1;
            object[] values = templateIndex >= 0
                ? (object[])list.elementValues[templateIndex].Clone()
                : new object[list.elementFields.Length];
            list.AddItem(values);
            int newIndex = list.elementValues.Length - 1;

            Dictionary<string, ItemTransferFieldValue> fields = manifest.Fields
                .GroupBy(f => f.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            for (int fieldIndex = 0; fieldIndex < list.elementFields.Length; fieldIndex++)
            {
                ItemTransferFieldValue source;
                if (!fields.TryGetValue(list.elementFields[fieldIndex], out source))
                {
                    continue;
                }

                string value = source.Value ?? string.Empty;
                if (IsPackageAssetPathIdField(source.Name))
                {
                    int oldPathId = TryParseInt(value);
                    int mappedPathId;
                    if (oldPathId > 0 && pathIdRemap.TryGetValue(oldPathId, out mappedPathId))
                    {
                        value = mappedPathId.ToString(CultureInfo.InvariantCulture);
                    }
                }
                list.SetValue(newIndex, fieldIndex, value);
            }

            int idFieldIndex = idGenerationService.GetIdFieldIndex(listCollection, targetListIndex);
            newId = 0;
            if (idFieldIndex >= 0)
            {
                HashSet<int> used = idGenerationService.BuildUsedIds(listCollection, targetListIndex, idFieldIndex);
                int maxId = used.Count > 0 ? used.Max() : 0;
                newId = idGenerationService.GetNextUniqueId(used, maxId + 1);
                list.SetValue(newIndex, idFieldIndex, newId.ToString(CultureInfo.InvariantCulture));
            }

            return newIndex;
        }

        private static int ResolveTargetListIndex(eListCollection listCollection, ItemTransferPackageManifest manifest)
        {
            if (manifest.SourceListIndex >= 0
                && manifest.SourceListIndex < listCollection.Lists.Length
                && string.Equals(listCollection.Lists[manifest.SourceListIndex].listName, manifest.SourceListName, StringComparison.OrdinalIgnoreCase))
            {
                return manifest.SourceListIndex;
            }

            for (int i = 0; i < listCollection.Lists.Length; i++)
            {
                if (string.Equals(listCollection.Lists[i].listName, manifest.SourceListName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool IsEquipmentEssenceList(eList list)
        {
            string listName = list != null ? list.listName : string.Empty;
            if (string.Equals(listName, "Equipment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "Equipment Essence", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "EQUIPMENT_ESSENCE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string normalizedName = NormalizeListName(listName);
            if (string.Equals(normalizedName, "Equipment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedName, "Equipment Essence", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedName, "EQUIPMENT_ESSENCE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return HasEquipmentEssenceFields(list);
        }

        private static bool HasEquipmentEssenceFields(eList list)
        {
            if (list == null || list.elementFields == null)
            {
                return false;
            }

            bool hasItemQuality = false;
            bool hasEquipMask = false;
            bool hasFileMatter = false;
            bool hasFileIcon = false;
            bool hasModelPath = false;
            for (int i = 0; i < list.elementFields.Length; i++)
            {
                string field = list.elementFields[i] ?? string.Empty;
                hasItemQuality |= string.Equals(field, "item_quality", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(field, "id_quality", StringComparison.OrdinalIgnoreCase);
                hasEquipMask |= string.Equals(field, "equip_mask", StringComparison.OrdinalIgnoreCase);
                hasFileMatter |= string.Equals(field, "file_matter", StringComparison.OrdinalIgnoreCase);
                hasFileIcon |= string.Equals(field, "file_icon", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(field, "file_icon1", StringComparison.OrdinalIgnoreCase);
                hasModelPath |= field.StartsWith("models_", StringComparison.OrdinalIgnoreCase)
                    || field.StartsWith("file_model", StringComparison.OrdinalIgnoreCase)
                    || field.StartsWith("model_", StringComparison.OrdinalIgnoreCase);
            }

            return hasItemQuality && hasEquipMask && hasFileMatter && hasFileIcon && hasModelPath;
        }

        private static string NormalizeListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return string.Empty;
            }

            string[] split = listName.Split(new string[] { " - " }, StringSplitOptions.None);
            return split.Length > 1 ? split[1].Trim() : listName.Trim();
        }

        private static void WriteTextEntry(ZipArchive archive, string name, string text)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (StreamWriter writer = new StreamWriter(entry.Open()))
            {
                writer.Write(text ?? string.Empty);
            }
        }

        private static int CountPackageAssets(string packageFile)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(packageFile))
                {
                    return archive.Entries.Count(entry => entry.FullName.StartsWith("assets/", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch
            {
                return 0;
            }
        }

        private static bool IsPackageAssetPathIdField(string fieldName)
        {
            string normalized = (fieldName ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            return string.Equals(normalized, "file_matter", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "file_icon1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "normal_attack_sfx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "music_pick", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "music_drop", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_change_model", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("file_model", StringComparison.OrdinalIgnoreCase)
                || (normalized.StartsWith("models_", StringComparison.OrdinalIgnoreCase)
                    && normalized.Contains("_file_model"))
                || normalized.StartsWith("gfx_", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("_gfx_")
                || normalized.EndsWith("_gfx", StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeAssetPath(string value)
        {
            string normalized = NormalizePath(value);
            return normalized.Contains("\\") && AssetExtensionPattern.IsMatch(normalized);
        }

        private static bool TrySplitPackagePath(string mappedPath, out string package, out string relative)
        {
            package = string.Empty;
            relative = string.Empty;
            string normalized = NormalizePath(mappedPath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            for (int i = 0; i < KnownPackages.Length; i++)
            {
                string known = KnownPackages[i];
                string prefix = known + "\\";
                if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    package = known;
                    relative = normalized.Substring(prefix.Length).TrimStart('\\');
                    return !string.IsNullOrWhiteSpace(relative);
                }
            }

            return false;
        }

        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Replace('/', '\\').Trim().TrimStart('\\');
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string GetFieldValue(List<ItemTransferFieldValue> fields, string fieldName)
        {
            if (fields == null)
            {
                return string.Empty;
            }

            ItemTransferFieldValue field = fields.FirstOrDefault(f => string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase));
            return field != null ? field.Value ?? string.Empty : string.Empty;
        }

        private static int TryParseInt(string value)
        {
            int result;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0;
        }
    }
}
