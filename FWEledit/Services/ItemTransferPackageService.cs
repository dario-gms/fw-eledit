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
        private const string ImportFallbackPackage = "models";
        private const string PlayerAssetFallbackPackage = "shaders";
        private static readonly string[] KnownPackages = new string[]
        {
            "building", "configs", "gfx", "grasses", "interfaces", "litmodels", "loddata", "models", "models2",
            "moxing", "script", "sfx", "shaders", "surfaces", "textures", "music"
        };
        private static readonly HashSet<string> CoreRuntimePackages = new HashSet<string>(
            new string[]
            {
                "building", "configs", "gfx", "grasses", "interfaces", "litmodels", "loddata", "models", "models2",
                "script", "sfx", "shaders", "surfaces", "textures", "music"
            },
            StringComparer.OrdinalIgnoreCase);

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

                AddPreviewResolvedDependencies(assetsByKey, manifest, assetManager, progress);
                ExpandEquipmentModelCompanions(assetsByKey, assetManager, progress, cancellationToken);
                ExpandEquipmentGfxDependencies(assetsByKey, assetManager, progress, cancellationToken);
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
            return ImportItemPackage(
                listCollection,
                database,
                assetManager,
                idGenerationService,
                packageFile,
                ItemTransferImportMode.FullStructure,
                null,
                CancellationToken.None);
        }

        public ItemTransferImportResult ImportItemPackage(
            eListCollection listCollection,
            CacheSave database,
            AssetManager assetManager,
            IdGenerationService idGenerationService,
            string packageFile,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            return ImportItemPackage(
                listCollection,
                database,
                assetManager,
                idGenerationService,
                packageFile,
                ItemTransferImportMode.FullStructure,
                progress,
                cancellationToken);
        }

        public ItemTransferImportResult ImportItemPackage(
            eListCollection listCollection,
            CacheSave database,
            AssetManager assetManager,
            IdGenerationService idGenerationService,
            string packageFile,
            ItemTransferImportMode mode,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            ItemTransferImportResult result = CreateImportResult(mode);
            try
            {
                ReportProgress(progress, "Preparing import", "Validating item package...", 0, 0, true);
                cancellationToken.ThrowIfCancellationRequested();

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
                    ItemTransferPackageManifest manifest = ReadImportManifest(archive);
                    if (manifest == null)
                    {
                        result.ErrorMessage = "Invalid or unsupported item package.";
                        return result;
                    }

                    int targetListIndex = ResolveTargetListIndex(listCollection, manifest);
                    if (targetListIndex < 0 && mode == ItemTransferImportMode.FullStructure)
                    {
                        result.ErrorMessage = "Target list was not found: " + (manifest.SourceListName ?? string.Empty);
                        return result;
                    }

                    Dictionary<string, string> packageRemap = BuildImportPackageRemap(manifest, assetManager, result);

                    ReportProgress(progress, "Importing path data", "Resolving path.data entries...", 0, 0, true);
                    cancellationToken.ThrowIfCancellationRequested();
                    Dictionary<int, int> pathIdRemap = ImportPathDataEntries(manifest, database, assetManager, result, packageRemap);

                    ImportAssets(archive, manifest, assetManager, result, progress, cancellationToken, packageRemap);

                    result.ImportedModelPaths = BuildImportedModelPathSummary(manifest, database, pathIdRemap);

                    if (mode == ItemTransferImportMode.ModelsOnly)
                    {
                        result.Success = true;
                        result.TargetListIndex = targetListIndex;
                        result.RemappedPathIdCount = pathIdRemap.Count(pair => pair.Key != pair.Value);
                        ReportProgress(progress, "Import complete", "Model assets imported.", 1, 1, false);
                        return result;
                    }

                    ReportProgress(progress, "Creating item", "Adding imported equipment to the target list...", 0, 0, true);
                    cancellationToken.ThrowIfCancellationRequested();
                    int newIndex = AddManifestItem(listCollection, targetListIndex, manifest, pathIdRemap, packageRemap, idGenerationService, out int newId);
                    result.Success = true;
                    result.TargetListIndex = targetListIndex;
                    result.NewItemIndex = newIndex;
                    result.NewId = newId;
                    result.ImportedItemCount = 1;
                    result.ImportedItems.Add(new ItemTransferImportedItem
                    {
                        ListIndex = targetListIndex,
                        ItemIndex = newIndex,
                        Id = newId
                    });
                    result.RemappedPathIdCount = pathIdRemap.Count(pair => pair.Key != pair.Value);
                    ReportProgress(progress, "Import complete", "Item package imported.", 1, 1, false);
                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Import cancelled.";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private static ItemTransferImportResult CreateImportResult(ItemTransferImportMode mode)
        {
            return new ItemTransferImportResult
            {
                Mode = mode,
                TargetListIndex = -1,
                NewItemIndex = -1,
                ImportedItems = new List<ItemTransferImportedItem>(),
                ImportedModelPaths = new List<ItemTransferImportedPath>(),
                AssetSummaries = new List<ItemTransferPackageAssetSummary>(),
                DependencyAssetPaths = new List<string>()
            };
        }

        private sealed class PendingImportedPackage
        {
            public ItemTransferPackageManifest Manifest { get; set; }
            public int TargetListIndex { get; set; }
            public Dictionary<int, int> PathIdRemap { get; set; }
            public Dictionary<string, string> PackageRemap { get; set; }
        }

        private sealed class PackageFileSnapshot
        {
            public string PackageName { get; set; }
            public string PckPath { get; set; }
            public string PkxPath { get; set; }
            public string PckBackupPath { get; set; }
            public string PkxBackupPath { get; set; }
            public bool HadPck { get; set; }
            public bool HadPkx { get; set; }
        }

        public ItemTransferImportResult ImportItemPackages(
            eListCollection listCollection,
            CacheSave database,
            AssetManager assetManager,
            IdGenerationService idGenerationService,
            IEnumerable<string> packageFiles,
            ItemTransferImportMode mode,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            List<string> files = packageFiles == null
                ? new List<string>()
                : packageFiles.Where(path => !string.IsNullOrWhiteSpace(path)).ToList();
            if (files.Count <= 1)
            {
                return ImportItemPackage(
                    listCollection,
                    database,
                    assetManager,
                    idGenerationService,
                    files.Count == 1 ? files[0] : string.Empty,
                    mode,
                    progress,
                    cancellationToken);
            }

            ItemTransferImportResult result = CreateImportResult(mode);
            string stagingRoot = Path.Combine(Path.GetTempPath(), "FWEledit", "item-package-import", Guid.NewGuid().ToString("N"));
            Dictionary<string, int> stagedCountsByPackage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> stagedAssetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ItemTransferPackageAssetSummary> summaryByPackage = BuildAssetSummaryLookup(result);
            List<PendingImportedPackage> pendingPackages = new List<PendingImportedPackage>();
            AssetManager.PathDataImportState pathDataState = null;
            Dictionary<int, object[][]> listValueSnapshots = null;
            List<PackageFileSnapshot> packageFileSnapshots = null;
            bool packageUpdatesStarted = false;

            try
            {
                ReportProgress(progress, "Preparing batch import", files.Count.ToString(CultureInfo.InvariantCulture) + " item packages", 0, files.Count, false);
                cancellationToken.ThrowIfCancellationRequested();

                if (listCollection == null || database == null || assetManager == null || idGenerationService == null)
                {
                    result.ErrorMessage = "No loaded elements data.";
                    return result;
                }

                pathDataState = assetManager.CapturePathDataImportState();
                listValueSnapshots = new Dictionary<int, object[][]>();

                for (int fileIndex = 0; fileIndex < files.Count; fileIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string packageFile = files[fileIndex];
                    if (!File.Exists(packageFile))
                    {
                        throw new FileNotFoundException("Item package was not found: " + packageFile, packageFile);
                    }

                    ReportProgress(
                        progress,
                        "Reading package",
                        Path.GetFileName(packageFile),
                        fileIndex + 1,
                        files.Count,
                        false);

                    using (ZipArchive archive = ZipFile.OpenRead(packageFile))
                    {
                        ItemTransferPackageManifest manifest = ReadImportManifest(archive);
                        if (manifest == null)
                        {
                            throw new InvalidOperationException("Invalid or unsupported item package: " + Path.GetFileName(packageFile));
                        }

                        int targetListIndex = ResolveTargetListIndex(listCollection, manifest);
                        if (targetListIndex < 0 && mode == ItemTransferImportMode.FullStructure)
                        {
                            throw new InvalidOperationException("Target list was not found: " + (manifest.SourceListName ?? string.Empty));
                        }
                        if (targetListIndex >= 0 && !listValueSnapshots.ContainsKey(targetListIndex))
                        {
                            listValueSnapshots[targetListIndex] = CloneElementValues(listCollection.Lists[targetListIndex]);
                        }

                        Dictionary<string, string> packageRemap = BuildImportPackageRemap(manifest, assetManager, result);

                        ReportProgress(progress, "Importing path data", Path.GetFileName(packageFile), fileIndex + 1, files.Count, false);
                        Dictionary<int, int> pathIdRemap = ImportPathDataEntries(manifest, database, assetManager, result, packageRemap);

                        StagePackageAssets(
                            archive,
                            manifest,
                            assetManager,
                            result,
                            progress,
                            cancellationToken,
                            packageRemap,
                            stagingRoot,
                            stagedCountsByPackage,
                            stagedAssetKeys,
                            summaryByPackage,
                            fileIndex + 1,
                            files.Count);

                        pendingPackages.Add(new PendingImportedPackage
                        {
                            Manifest = manifest,
                            TargetListIndex = targetListIndex,
                            PathIdRemap = pathIdRemap,
                            PackageRemap = packageRemap
                        });
                        result.ImportedModelPaths.AddRange(BuildImportedModelPathSummary(manifest, database, pathIdRemap));
                    }
                }

                packageFileSnapshots = CreatePackageFileSnapshots(stagedCountsByPackage.Keys);
                packageUpdatesStarted = true;
                UpdateStagedPackages(stagingRoot, stagedCountsByPackage, assetManager, result, progress, cancellationToken);

                if (mode == ItemTransferImportMode.ModelsOnly)
                {
                    result.Success = true;
                    result.RemappedPathIdCount = pendingPackages.Sum(p => p.PathIdRemap.Count(pair => pair.Key != pair.Value));
                    ReportProgress(progress, "Import complete", "Model assets imported.", 1, 1, false);
                    return result;
                }

                for (int i = 0; i < pendingPackages.Count; i++)
                {
                    PendingImportedPackage pending = pendingPackages[i];
                    ReportProgress(
                        progress,
                        "Creating items",
                        pending.Manifest.OriginalName ?? pending.Manifest.SourceListName ?? string.Empty,
                        i + 1,
                        pendingPackages.Count,
                        false);
                    int newIndex = AddManifestItem(
                        listCollection,
                        pending.TargetListIndex,
                        pending.Manifest,
                        pending.PathIdRemap,
                        pending.PackageRemap,
                        idGenerationService,
                        out int newId);
                    result.TargetListIndex = pending.TargetListIndex;
                    result.NewItemIndex = newIndex;
                    result.NewId = newId;
                    result.ImportedItemCount++;
                    result.ImportedItems.Add(new ItemTransferImportedItem
                    {
                        ListIndex = pending.TargetListIndex,
                        ItemIndex = newIndex,
                        Id = newId
                    });
                    result.RemappedPathIdCount += pending.PathIdRemap.Count(pair => pair.Key != pair.Value);
                }

                result.Success = true;
                ReportProgress(progress, "Import complete", "Item packages imported.", 1, 1, false);
                return result;
            }
            catch (OperationCanceledException)
            {
                RollbackBatchImport(listCollection, assetManager, listValueSnapshots, pathDataState, packageFileSnapshots, packageUpdatesStarted);
                result.ErrorMessage = "Import cancelled.";
                return result;
            }
            catch (Exception ex)
            {
                RollbackBatchImport(listCollection, assetManager, listValueSnapshots, pathDataState, packageFileSnapshots, packageUpdatesStarted);
                result.ErrorMessage = ex.Message;
                return result;
            }
            finally
            {
                TryDeleteDirectory(stagingRoot);
                DeletePackageFileSnapshots(packageFileSnapshots);
            }
        }

        public ItemTransferPackageManifest BuildEquipmentExportManifest(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            int itemIndex)
        {
            if (listCollection == null
                || database == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || itemIndex < 0
                || itemIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return null;
            }

            ItemTransferPackageManifest manifest = BuildManifest(listCollection, database, listIndex, itemIndex);
            CollectDirectPathDataEntries(manifest, database);
            return manifest;
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
                        if (IsIconAssetPathIdField(fieldName))
                        {
                            ItemTransferAssetEntry ignored;
                            TryAddExistingAsset(assets, mapped, assetManager, out ignored);
                        }
                        else
                        {
                            AddAsset(assets, mapped, assetManager);
                        }
                    }
                }

                if (LooksLikeAssetPath(value))
                {
                    AddAsset(assets, value, assetManager);
                }
            }

            return assets;
        }

        private static void CollectDirectPathDataEntries(ItemTransferPackageManifest manifest, CacheSave database)
        {
            if (manifest == null || manifest.Fields == null || database == null || database.pathById == null)
            {
                return;
            }

            if (manifest.PathDataEntries == null)
            {
                manifest.PathDataEntries = new List<ItemTransferPathDataEntry>();
            }

            HashSet<int> addedPathIds = new HashSet<int>(manifest.PathDataEntries.Select(p => p.PathId));
            foreach (ItemTransferFieldValue field in manifest.Fields)
            {
                if (field == null || !IsPackageAssetPathIdField(field.Name))
                {
                    continue;
                }

                int pathId = TryParseInt(field.Value);
                string mapped;
                if (pathId > 0
                    && database.pathById.TryGetValue(pathId, out mapped)
                    && !string.IsNullOrWhiteSpace(mapped)
                    && addedPathIds.Add(pathId))
                {
                    manifest.PathDataEntries.Add(new ItemTransferPathDataEntry { PathId = pathId, MappedPath = NormalizePath(mapped) });
                }
            }
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

        private static void AddPreviewResolvedDependencies(
            Dictionary<string, ItemTransferAssetEntry> assets,
            ItemTransferPackageManifest manifest,
            AssetManager assetManager,
            Action<ItemTransferProgressInfo> progress)
        {
            if (assets == null || manifest == null || assetManager == null)
            {
                return;
            }

            EmbeddedModelPreviewLoaderService previewLoader = new EmbeddedModelPreviewLoaderService(new PckEntryReaderService());
            HashSet<string> roots = CollectPreviewRootPaths(manifest);
            foreach (string root in roots)
            {
                ReportProgress(progress, "Mapping preview dependencies", root, 0, 0, true);
                List<string> dependencies;
                string error;
                if (!previewLoader.TryCollectDependencyPaths(assetManager, root, out dependencies, out error)
                    || dependencies == null)
                {
                    continue;
                }

                int addedCount = 0;
                for (int i = 0; i < dependencies.Count; i++)
                {
                    if (AddAsset(assets, dependencies[i], assetManager))
                    {
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    ReportProgress(progress, "Mapped preview dependencies", root + " (" + addedCount.ToString(CultureInfo.InvariantCulture) + " files)", 0, 0, true);
                }
            }
        }

        private static HashSet<string> CollectPreviewRootPaths(ItemTransferPackageManifest manifest)
        {
            HashSet<string> roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (manifest == null || manifest.Fields == null)
            {
                return roots;
            }

            Dictionary<int, string> pathDataById = (manifest.PathDataEntries ?? new List<ItemTransferPathDataEntry>())
                .Where(p => p != null && p.PathId > 0 && !string.IsNullOrWhiteSpace(p.MappedPath))
                .GroupBy(p => p.PathId)
                .ToDictionary(g => g.Key, g => g.First().MappedPath);

            foreach (ItemTransferFieldValue field in manifest.Fields)
            {
                string name = field != null ? field.Name ?? string.Empty : string.Empty;
                string value = field != null ? field.Value ?? string.Empty : string.Empty;
                if (IsPackageAssetPathIdField(name))
                {
                    int pathId;
                    string mapped;
                    if (int.TryParse(value, out pathId)
                        && pathDataById.TryGetValue(pathId, out mapped)
                        && IsPreviewableModelPath(mapped))
                    {
                        roots.Add(NormalizePath(mapped));
                    }
                }
                else if (LooksLikeAssetPath(value) && IsPreviewableModelPath(value))
                {
                    roots.Add(NormalizePath(value));
                }
            }

            return roots;
        }

        private static bool IsPreviewableModelPath(string mappedPath)
        {
            string package;
            string relative;
            if (!TrySplitPackagePath(mappedPath, out package, out relative))
            {
                return false;
            }

            string extension = Path.GetExtension(relative);
            return IsModelLikeExtension(extension)
                || string.Equals(extension, ".gfx", StringComparison.OrdinalIgnoreCase);
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
            foreach (string rawFieldReference in CollectNamedPathReferenceCandidates(text))
            {
                foreach (string candidate in ResolveReferenceCandidates(currentMappedPath, rawFieldReference))
                {
                    if (yielded.Add(candidate))
                    {
                        yield return candidate;
                    }
                }
            }

            MatchCollection matches = Regex.Matches(
                text,
                @"[^\0\r\n\t""'<>|:*?]{1,220}\.(?:dds|tga|bmp|png|jpg|jpeg|ski|smd|ecm|gfx|att|sgc|bon|stck|sdr)",
                RegexOptions.IgnoreCase);

            for (int i = 0; i < matches.Count; i++)
            {
                string raw = NormalizeExtractedPathCandidate(matches[i].Value.Trim().Trim('\0').TrimStart('.', '\\', '/'));
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

            string[] extensions = new string[] { ".ecm", ".smd", ".ski", ".gfx", ".att", ".sgc" };
            for (int i = 0; i < extensions.Length; i++)
            {
                foreach (string raw in ExtractPathsByRawByteScan(payload, extensions[i]))
                {
                    foreach (string candidate in ResolveReferenceCandidates(currentMappedPath, NormalizeExtractedPathCandidate(raw)))
                    {
                        if (yielded.Add(candidate))
                        {
                            yield return candidate;
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> CollectNamedPathReferenceCandidates(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            string[] directFields = new string[]
            {
                "SkinModelPath",
                "ModelPath",
                "ModelFile",
                "FileModel",
                "FilePath",
                "EcmPath",
                "SmdPath",
                "SkiPath",
                "FxFilePath",
                "GfxPath",
                "GfxFile",
                "AttPath",
                "AttFile",
                "SgcPath",
                "SgcFile",
                "Path"
            };

            string[] lines = text.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = (lines[lineIndex] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                for (int fieldIndex = 0; fieldIndex < directFields.Length; fieldIndex++)
                {
                    string field = directFields[fieldIndex];
                    if (!line.StartsWith(field, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    int separator = line.IndexOf(':');
                    if (separator < 0)
                    {
                        separator = line.IndexOf('=');
                    }
                    if (separator < 0 || separator >= line.Length - 1)
                    {
                        continue;
                    }

                    string candidate = NormalizeExtractedPathCandidate(line.Substring(separator + 1));
                    if (!string.IsNullOrWhiteSpace(candidate) && AssetExtensionPattern.IsMatch(candidate))
                    {
                        yield return candidate;
                    }
                }
            }
        }

        private static IEnumerable<string> ExtractPathsByRawByteScan(byte[] bytes, string extension)
        {
            if (bytes == null || bytes.Length == 0 || string.IsNullOrWhiteSpace(extension))
            {
                yield break;
            }

            string marker = extension.ToLowerInvariant();
            byte[] markerBytes = Encoding.ASCII.GetBytes(marker);
            if (markerBytes.Length == 0)
            {
                yield break;
            }

            HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i <= bytes.Length - markerBytes.Length; i++)
            {
                bool match = true;
                for (int m = 0; m < markerBytes.Length; m++)
                {
                    byte lower = (byte)char.ToLowerInvariant((char)bytes[i + m]);
                    if (lower != markerBytes[m])
                    {
                        match = false;
                        break;
                    }
                }
                if (!match)
                {
                    continue;
                }

                int start = i - 1;
                while (start >= 0 && IsPathByte(bytes[start]))
                {
                    start--;
                }
                start++;

                int end = i + markerBytes.Length;
                while (end < bytes.Length && IsPathByte(bytes[end]))
                {
                    end++;
                }

                int len = end - start;
                if (len <= 0 || len > 1024)
                {
                    continue;
                }

                string candidate;
                try
                {
                    candidate = Encoding.GetEncoding("GBK").GetString(bytes, start, len).Trim('\0', ' ', '\t', '\r', '\n');
                }
                catch
                {
                    continue;
                }

                candidate = NormalizeExtractedPathCandidate(candidate);
                if (!string.IsNullOrWhiteSpace(candidate)
                    && candidate.EndsWith(marker, StringComparison.OrdinalIgnoreCase)
                    && yielded.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }

        private static bool IsPathByte(byte b)
        {
            return b != 0 && b != 0xFF && b >= 0x20;
        }

        private static string NormalizeExtractedPathCandidate(string value)
        {
            string candidate = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return string.Empty;
            }

            candidate = candidate.Trim('"', '\'').Trim();
            char[] separators = new char[] { ':', '：', '=' };
            int separatorIndex = -1;
            for (int i = 0; i < separators.Length; i++)
            {
                int idx = candidate.IndexOf(separators[i]);
                if (idx > 0 && (separatorIndex < 0 || idx < separatorIndex))
                {
                    separatorIndex = idx;
                }
            }

            if (separatorIndex > 0 && separatorIndex < candidate.Length - 1)
            {
                string left = candidate.Substring(0, separatorIndex).Trim();
                string right = candidate.Substring(separatorIndex + 1).Trim().Trim('"', '\'').Trim();
                if (!string.IsNullOrWhiteSpace(right)
                    && left.IndexOf('\\') < 0
                    && left.IndexOf('/') < 0
                    && left.IndexOf('.') < 0)
                {
                    candidate = right;
                }
            }

            return NormalizePath(candidate);
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
                @"[^\0\r\n\t""'<>|:*?]{1,220}\.(?:dds|tga|bmp|png|jpg|jpeg|ski|smd|ecm|gfx|att|sgc|bon|stck|sdr)",
                RegexOptions.IgnoreCase);

            for (int i = 0; i < matches.Count; i++)
            {
                string raw = NormalizeExtractedPathCandidate(matches[i].Value.Trim().Trim('\0').TrimStart('.', '\\', '/'));
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

            string[] extensions = new string[] { ".ecm", ".smd", ".ski", ".gfx", ".att", ".sgc" };
            for (int i = 0; i < extensions.Length; i++)
            {
                foreach (string raw in ExtractPathsByRawByteScan(payload, extensions[i]))
                {
                    foreach (string candidate in ResolveGfxDependencyCandidates(current, NormalizeExtractedPathCandidate(raw)))
                    {
                        if (yielded.Add(candidate))
                        {
                            yield return candidate;
                        }
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
                string referenceExtension = Path.GetExtension(normalizedReference);
                if (IsEffectDescriptorExtension(referenceExtension))
                {
                    yield return "gfx\\" + normalizedReference;
                }

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

        private static bool IsEffectDescriptorExtension(string ext)
        {
            return string.Equals(ext, ".gfx", StringComparison.OrdinalIgnoreCase)
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

        private static void TryDeleteDirectory(string directory)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch
            {
            }
        }

        private static object[][] CloneElementValues(eList list)
        {
            if (list == null || list.elementValues == null)
            {
                return null;
            }

            object[][] clone = new object[list.elementValues.Length][];
            for (int i = 0; i < list.elementValues.Length; i++)
            {
                object[] row = list.elementValues[i];
                if (row == null)
                {
                    continue;
                }

                object[] rowClone = new object[row.Length];
                for (int fieldIndex = 0; fieldIndex < row.Length; fieldIndex++)
                {
                    byte[] bytes = row[fieldIndex] as byte[];
                    rowClone[fieldIndex] = bytes != null ? (object)((byte[])bytes.Clone()) : row[fieldIndex];
                }

                clone[i] = rowClone;
            }

            return clone;
        }

        private static void RollbackBatchImport(
            eListCollection listCollection,
            AssetManager assetManager,
            Dictionary<int, object[][]> listValueSnapshots,
            AssetManager.PathDataImportState pathDataState,
            List<PackageFileSnapshot> packageFileSnapshots,
            bool packageUpdatesStarted)
        {
            if (listCollection != null && listValueSnapshots != null)
            {
                foreach (KeyValuePair<int, object[][]> snapshot in listValueSnapshots)
                {
                    if (snapshot.Key >= 0
                        && snapshot.Key < listCollection.Lists.Length
                        && listCollection.Lists[snapshot.Key] != null)
                    {
                        listCollection.Lists[snapshot.Key].elementValues = CloneElementValues(snapshot.Value);
                    }
                }
            }

            if (assetManager != null)
            {
                assetManager.RestorePathDataImportState(pathDataState);
            }

            if (packageUpdatesStarted)
            {
                RestorePackageFileSnapshots(packageFileSnapshots);
            }
        }

        private static object[][] CloneElementValues(object[][] values)
        {
            if (values == null)
            {
                return null;
            }

            object[][] clone = new object[values.Length][];
            for (int i = 0; i < values.Length; i++)
            {
                object[] row = values[i];
                if (row == null)
                {
                    continue;
                }

                object[] rowClone = new object[row.Length];
                for (int fieldIndex = 0; fieldIndex < row.Length; fieldIndex++)
                {
                    byte[] bytes = row[fieldIndex] as byte[];
                    rowClone[fieldIndex] = bytes != null ? (object)((byte[])bytes.Clone()) : row[fieldIndex];
                }

                clone[i] = rowClone;
            }

            return clone;
        }

        private static List<PackageFileSnapshot> CreatePackageFileSnapshots(IEnumerable<string> packageNames)
        {
            List<PackageFileSnapshot> snapshots = new List<PackageFileSnapshot>();
            if (packageNames == null)
            {
                return snapshots;
            }

            if (string.IsNullOrWhiteSpace(AssetManager.GameRootPath) || !Directory.Exists(AssetManager.GameRootPath))
            {
                throw new InvalidOperationException("Game root was not found for transactional package import.");
            }

            string resourcesRoot = Path.Combine(AssetManager.GameRootPath, "resources");
            string backupRoot = Path.Combine(Path.GetTempPath(), "FWEledit", "item-package-import-backup", Guid.NewGuid().ToString("N"));
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string packageName in packageNames)
            {
                string normalizedPackage = NormalizePackageName(packageName);
                if (string.IsNullOrWhiteSpace(normalizedPackage) || !seen.Add(normalizedPackage))
                {
                    continue;
                }

                string pckPath = Path.Combine(resourcesRoot, normalizedPackage + ".pck");
                string pkxPath = Path.Combine(resourcesRoot, normalizedPackage + ".pkx");
                string pckBackupPath = Path.Combine(backupRoot, normalizedPackage + ".pck");
                string pkxBackupPath = Path.Combine(backupRoot, normalizedPackage + ".pkx");
                PackageFileSnapshot snapshot = new PackageFileSnapshot
                {
                    PackageName = normalizedPackage,
                    PckPath = pckPath,
                    PkxPath = pkxPath,
                    PckBackupPath = pckBackupPath,
                    PkxBackupPath = pkxBackupPath,
                    HadPck = File.Exists(pckPath),
                    HadPkx = File.Exists(pkxPath)
                };

                try
                {
                    Directory.CreateDirectory(backupRoot);
                    if (snapshot.HadPck)
                    {
                        File.Copy(pckPath, pckBackupPath, true);
                    }
                    if (snapshot.HadPkx)
                    {
                        File.Copy(pkxPath, pkxBackupPath, true);
                    }
                }
                catch (Exception ex)
                {
                    DeletePackageFileSnapshots(snapshots);
                    TryDeleteDirectory(backupRoot);
                    throw new InvalidOperationException("Could not prepare transactional backup for " + normalizedPackage + ".pck: " + ex.Message, ex);
                }

                snapshots.Add(snapshot);
            }

            return snapshots;
        }

        private static void RestorePackageFileSnapshots(List<PackageFileSnapshot> snapshots)
        {
            if (snapshots == null)
            {
                return;
            }

            foreach (PackageFileSnapshot snapshot in snapshots)
            {
                if (snapshot == null)
                {
                    continue;
                }

                RestorePackageFile(snapshot.PckPath, snapshot.PckBackupPath, snapshot.HadPck);
                RestorePackageFile(snapshot.PkxPath, snapshot.PkxBackupPath, snapshot.HadPkx);
                PckEntryReaderService.InvalidatePackageGlobally(snapshot.PackageName);
            }
        }

        private static void RestorePackageFile(string targetPath, string backupPath, bool existedBefore)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return;
            }

            if (existedBefore)
            {
                if (!string.IsNullOrWhiteSpace(backupPath) && File.Exists(backupPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    File.Copy(backupPath, targetPath, true);
                }
            }
            else if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }

        private static void DeletePackageFileSnapshots(List<PackageFileSnapshot> snapshots)
        {
            if (snapshots == null)
            {
                return;
            }

            HashSet<string> backupRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PackageFileSnapshot snapshot in snapshots)
            {
                if (snapshot == null)
                {
                    continue;
                }

                AddBackupRoot(backupRoots, snapshot.PckBackupPath);
                AddBackupRoot(backupRoots, snapshot.PkxBackupPath);
            }

            foreach (string backupRoot in backupRoots)
            {
                TryDeleteDirectory(backupRoot);
            }
        }

        private static void AddBackupRoot(HashSet<string> backupRoots, string backupFilePath)
        {
            if (backupRoots == null || string.IsNullOrWhiteSpace(backupFilePath))
            {
                return;
            }

            string packageBackupDirectory = Path.GetDirectoryName(backupFilePath);
            if (!string.IsNullOrWhiteSpace(packageBackupDirectory))
            {
                backupRoots.Add(packageBackupDirectory);
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

        private static Dictionary<string, string> BuildImportPackageRemap(
            ItemTransferPackageManifest manifest,
            AssetManager assetManager,
            ItemTransferImportResult result)
        {
            Dictionary<string, string> remap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (manifest == null)
            {
                return remap;
            }

            HashSet<string> sourcePackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (manifest.Assets != null)
            {
                foreach (ItemTransferAssetEntry asset in manifest.Assets)
                {
                    if (asset == null)
                    {
                        continue;
                    }

                    string package = NormalizePackageName(asset.Package);
                    if (!string.IsNullOrWhiteSpace(package))
                    {
                        sourcePackages.Add(package);
                    }
                }
            }

            if (manifest.PathDataEntries != null)
            {
                foreach (ItemTransferPathDataEntry entry in manifest.PathDataEntries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    string package;
                    string relative;
                    if (TrySplitMappedPackagePrefix(entry.MappedPath, out package, out relative))
                    {
                        sourcePackages.Add(package);
                    }
                }
            }

            foreach (string sourcePackage in sourcePackages)
            {
                if (string.Equals(sourcePackage, ImportFallbackPackage, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!CoreRuntimePackages.Contains(sourcePackage) || !ResourcePackageExists(sourcePackage))
                {
                    remap[sourcePackage] = ResolveFallbackPackageForSourcePackage(manifest, sourcePackage);
                }
            }

            if (result != null)
            {
                result.FallbackPackageCount += remap.Count;
            }

            return remap;
        }

        private static string ResolveFallbackPackageForSourcePackage(ItemTransferPackageManifest manifest, string sourcePackage)
        {
            if (SourcePackageContainsRelativePrefix(manifest, sourcePackage, "player\\")
                || SourcePackageContainsRelativePrefix(manifest, sourcePackage, "players\\"))
            {
                return PlayerAssetFallbackPackage;
            }

            return ImportFallbackPackage;
        }

        private static bool SourcePackageContainsRelativePrefix(
            ItemTransferPackageManifest manifest,
            string sourcePackage,
            string relativePrefix)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(sourcePackage) || string.IsNullOrWhiteSpace(relativePrefix))
            {
                return false;
            }

            string normalizedPackage = NormalizePackageName(sourcePackage);
            string normalizedPrefix = NormalizePath(relativePrefix).TrimStart('\\');

            if (manifest.Assets != null)
            {
                foreach (ItemTransferAssetEntry asset in manifest.Assets)
                {
                    if (asset == null
                        || !string.Equals(NormalizePackageName(asset.Package), normalizedPackage, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (PathStartsWithPrefix(asset.RelativePath, normalizedPrefix))
                    {
                        return true;
                    }
                }
            }

            if (manifest.PathDataEntries != null)
            {
                foreach (ItemTransferPathDataEntry entry in manifest.PathDataEntries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    string package;
                    string relative;
                    if (TrySplitMappedPackagePrefix(entry.MappedPath, out package, out relative)
                        && string.Equals(package, normalizedPackage, StringComparison.OrdinalIgnoreCase)
                        && PathStartsWithPrefix(relative, normalizedPrefix))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool PathStartsWithPrefix(string path, string prefix)
        {
            string normalizedPath = NormalizePath(path).TrimStart('\\');
            string normalizedPrefix = NormalizePath(prefix).TrimStart('\\');
            return normalizedPath.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveImportPackage(string packageName, Dictionary<string, string> packageRemap)
        {
            string sourcePackage = NormalizePackageName(packageName);
            string targetPackage;
            return packageRemap != null && packageRemap.TryGetValue(sourcePackage, out targetPackage)
                ? targetPackage
                : sourcePackage;
        }

        private static string RemapMappedPackage(string mappedPath, Dictionary<string, string> packageRemap)
        {
            string packageName;
            string relativePath;
            if (packageRemap == null
                || packageRemap.Count == 0
                || !TrySplitMappedPackagePrefix(mappedPath, out packageName, out relativePath))
            {
                return mappedPath;
            }

            string targetPackage;
            return packageRemap.TryGetValue(packageName, out targetPackage)
                ? targetPackage + "\\" + relativePath
                : mappedPath;
        }

        private static bool ShouldPreserveLogicalPackage(string packageName)
        {
            return string.Equals(
                NormalizePackageName(packageName),
                "moxing",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool TrySplitMappedPackagePrefix(string mappedPath, out string package, out string relative)
        {
            package = string.Empty;
            relative = string.Empty;
            string normalized = NormalizePath(mappedPath);
            int separator = normalized.IndexOf('\\');
            if (separator <= 0 || separator >= normalized.Length - 1)
            {
                return false;
            }

            package = NormalizePackageName(normalized.Substring(0, separator));
            relative = normalized.Substring(separator + 1).TrimStart('\\');
            return !string.IsNullOrWhiteSpace(package) && !string.IsNullOrWhiteSpace(relative);
        }

        private static string NormalizePackageName(string packageName)
        {
            return (packageName ?? string.Empty).Trim().TrimEnd('.', '\\', '/');
        }

        private static bool ResourcePackageExists(string packageName)
        {
            string normalizedPackage = NormalizePackageName(packageName);
            if (string.IsNullOrWhiteSpace(normalizedPackage)
                || normalizedPackage.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || normalizedPackage.Contains("\\")
                || normalizedPackage.Contains("/"))
            {
                return false;
            }

            return ResourcePackageExistsInRoot(AssetManager.GameRootPath, normalizedPackage)
                || ResourcePackageExistsInRoot(AssetManager.WorkspaceRootPath, normalizedPackage);
        }

        private static bool ResourcePackageExistsInRoot(string root, string packageName)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            string resources = Path.Combine(root, "resources");
            return File.Exists(Path.Combine(resources, packageName + ".pck"))
                || File.Exists(Path.Combine(resources, packageName + ".pkx"));
        }

        private static bool ResourcePackageRootExists(AssetManager assetManager, string packageName, string relativePath)
        {
            string root = GetTopLevelRoot(relativePath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return true;
            }

            List<string> entries;
            if (assetManager == null || !assetManager.TryEnumeratePckIndexEntries(packageName, out entries) || entries == null)
            {
                return false;
            }

            string prefix = root + "\\";
            return entries.Any(entry =>
                string.Equals(GetTopLevelRoot(entry), root, StringComparison.OrdinalIgnoreCase)
                || NormalizePath(entry).StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetTopLevelRoot(string relativePath)
        {
            string normalized = NormalizePath(relativePath);
            int slash = normalized.IndexOf('\\');
            return slash > 0 ? normalized.Substring(0, slash) : string.Empty;
        }

        private static Dictionary<int, int> ImportPathDataEntries(
            ItemTransferPackageManifest manifest,
            CacheSave database,
            AssetManager assetManager,
            ItemTransferImportResult result,
            Dictionary<string, string> packageRemap)
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

                string targetMappedPath = RemapMappedPackage(entry.MappedPath, packageRemap);
                int existingPathId;
                if (assetManager.TryFindPathIdByMappedPath(targetMappedPath, out existingPathId) && existingPathId > 0)
                {
                    remap[entry.PathId] = existingPathId;
                    if (!database.pathById.ContainsKey(existingPathId))
                    {
                        database.pathById[existingPathId] = NormalizePath(targetMappedPath);
                    }
                    continue;
                }

                int targetPathId = entry.PathId;
                string existingMapped;
                if (database.pathById.TryGetValue(targetPathId, out existingMapped)
                    && !PathsEqual(existingMapped, targetMappedPath))
                {
                    while (database.pathById.ContainsKey(nextPathId))
                    {
                        nextPathId++;
                    }
                    targetPathId = nextPathId++;
                }

                string canonicalPath = NormalizePath(targetMappedPath);
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
            ItemTransferImportResult result,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken,
            Dictionary<string, string> packageRemap)
        {
            if (manifest.Assets == null)
            {
                return;
            }

            string stagingRoot = Path.Combine(Path.GetTempPath(), "FWEledit", "item-package-import", Guid.NewGuid().ToString("N"));
            Dictionary<string, int> stagedCountsByPackage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> stagedAssetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ItemTransferPackageAssetSummary> summaryByPackage = BuildAssetSummaryLookup(result);
            try
            {
                StagePackageAssets(
                    archive,
                    manifest,
                    assetManager,
                    result,
                    progress,
                    cancellationToken,
                    packageRemap,
                    stagingRoot,
                    stagedCountsByPackage,
                    stagedAssetKeys,
                    summaryByPackage,
                    1,
                    1);
                UpdateStagedPackages(stagingRoot, stagedCountsByPackage, assetManager, result, progress, cancellationToken);
            }
            finally
            {
                TryDeleteDirectory(stagingRoot);
            }
        }

        private static ItemTransferPackageManifest ReadImportManifest(ZipArchive archive)
        {
            if (archive == null)
            {
                return null;
            }

            ZipArchiveEntry manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry == null)
            {
                return null;
            }

            using (StreamReader reader = new StreamReader(manifestEntry.Open()))
            {
                ItemTransferPackageManifest manifest = JsonConvert.DeserializeObject<ItemTransferPackageManifest>(reader.ReadToEnd());
                return manifest != null
                    && string.Equals(manifest.Format, PackageFormat, StringComparison.Ordinal)
                    && manifest.FormatVersion >= 1
                    ? manifest
                    : null;
            }
        }

        private static void StagePackageAssets(
            ZipArchive archive,
            ItemTransferPackageManifest manifest,
            AssetManager assetManager,
            ItemTransferImportResult result,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken,
            Dictionary<string, string> packageRemap,
            string stagingRoot,
            Dictionary<string, int> stagedCountsByPackage,
            HashSet<string> stagedAssetKeys,
            Dictionary<string, ItemTransferPackageAssetSummary> summaryByPackage,
            int packageNumber,
            int packageCount)
        {
            if (archive == null || manifest == null || manifest.Assets == null)
            {
                return;
            }

            int checkedAssets = 0;
            Dictionary<string, HashSet<string>> exactTargetEntriesByPackage = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            List<ItemTransferAssetEntry> assetsToProcess = manifest.Assets
                .OrderByDescending(asset => asset != null && IsImportPackageRemapped(asset.Package, packageRemap) ? 1 : 0)
                .ToList();
            int totalAssets = assetsToProcess.Count;
            foreach (ItemTransferAssetEntry asset in assetsToProcess)
            {
                cancellationToken.ThrowIfCancellationRequested();
                checkedAssets++;
                if (asset == null || string.IsNullOrWhiteSpace(asset.Package) || string.IsNullOrWhiteSpace(asset.RelativePath))
                {
                    continue;
                }

                ReportProgress(
                    progress,
                    packageCount > 1 ? "Checking assets (" + packageNumber.ToString(CultureInfo.InvariantCulture) + "/" + packageCount.ToString(CultureInfo.InvariantCulture) + ")" : "Checking assets",
                    asset.MappedPath ?? asset.RelativePath ?? string.Empty,
                    checkedAssets,
                    totalAssets,
                    false);

                string targetPackage = ResolveImportPackage(asset.Package, packageRemap);
                string targetRelativePath = NormalizeImportAssetRelativePath(asset, targetPackage, packageRemap);
                string stagedKey = targetPackage + "|" + targetRelativePath;
                if (stagedAssetKeys != null && stagedAssetKeys.Contains(stagedKey))
                {
                    AddDependencyAssetPath(result, targetPackage, targetRelativePath);
                    continue;
                }

                byte[] existingPayload;
                string existingError;
                bool needsReferenceRewrite = NeedsPackageReferenceRewrite(asset, packageRemap);
                bool targetEntryExists = false;
                HashSet<string> exactEntries;
                if (!exactTargetEntriesByPackage.TryGetValue(targetPackage, out exactEntries))
                {
                    string exactError;
                    if (assetManager.TryGetPackageEntryKeysExact(targetPackage, out exactEntries, out exactError))
                    {
                        exactTargetEntriesByPackage[targetPackage] = exactEntries;
                    }
                    else
                    {
                        exactEntries = null;
                        existingError = exactError;
                    }
                }
                if (exactEntries != null)
                {
                    existingError = string.Empty;
                    targetEntryExists = exactEntries.Contains(NormalizePath(targetRelativePath));
                    if (targetEntryExists)
                    {
                        targetEntryExists = assetManager.TryReadPackageEntry(targetPackage, targetRelativePath, out existingPayload, out existingError)
                            && existingPayload != null;
                    }
                }
                else
                {
                    targetEntryExists = assetManager.TryReadPackageEntry(targetPackage, targetRelativePath, out existingPayload, out existingError)
                        && existingPayload != null;
                }
                if (targetEntryExists)
                {
                    result.ExistingAssetCount++;
                    IncrementAssetSummary(result, summaryByPackage, targetPackage, false);
                    AddDependencyAssetPath(result, targetPackage, targetRelativePath);
                    continue;
                }
                if (IsFatalPackageReadError(existingError))
                {
                    throw new InvalidOperationException("Target " + targetPackage + ".pck could not be read before import. Restore a clean backup before importing more assets. " + existingError);
                }

                ZipArchiveEntry entry = archive.GetEntry(asset.ZipPath);
                if (entry == null)
                {
                    result.MissingAssetCount++;
                    continue;
                }

                string packageStagingRoot = Path.Combine(stagingRoot, targetPackage);
                string outputPath = Path.Combine(packageStagingRoot, targetRelativePath);
                string outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                byte[] payload;
                using (Stream input = entry.Open())
                using (MemoryStream memory = new MemoryStream())
                {
                    input.CopyTo(memory);
                    payload = memory.ToArray();
                }
                if (needsReferenceRewrite)
                {
                    payload = RewritePackageReferences(payload, packageRemap);
                }

                using (FileStream output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    output.Write(payload, 0, payload.Length);
                }

                if (stagedAssetKeys != null)
                {
                    stagedAssetKeys.Add(stagedKey);
                }

                int stagedCount;
                stagedCountsByPackage.TryGetValue(targetPackage, out stagedCount);
                stagedCountsByPackage[targetPackage] = stagedCount + 1;
                IncrementAssetSummary(result, summaryByPackage, targetPackage, true);
                AddDependencyAssetPath(result, targetPackage, targetRelativePath);
            }
        }

        private static string NormalizeImportAssetRelativePath(ItemTransferAssetEntry asset, string targetPackage, Dictionary<string, string> packageRemap)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            string mappedPackage;
            string mappedRelative;
            if (!string.IsNullOrWhiteSpace(asset.MappedPath)
                && TrySplitMappedPackagePrefix(RemapMappedPackage(asset.MappedPath, packageRemap), out mappedPackage, out mappedRelative)
                && string.Equals(NormalizePackageName(mappedPackage), NormalizePackageName(targetPackage), StringComparison.OrdinalIgnoreCase))
            {
                return CanonicalizePackageRelativePath(targetPackage, mappedRelative);
            }

            return NormalizeImportAssetRelativePath(asset.RelativePath, targetPackage);
        }

        private static string NormalizeImportAssetRelativePath(string relativePath, string targetPackage)
        {
            string normalized = NormalizePath(relativePath);
            string packagePrefix = NormalizePath(targetPackage) + "\\";
            string relative = normalized.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(packagePrefix.Length)
                : normalized;
            return CanonicalizePackageRelativePath(targetPackage, relative);
        }

        private static string CanonicalizePackageRelativePath(string packageName, string relativePath)
        {
            string normalized = NormalizePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalized)
                || !string.Equals(NormalizePackageName(packageName), "models", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            string[] parts = normalized.Split('\\');
            if (parts.Length == 0)
            {
                return normalized;
            }

            if (string.Equals(parts[0], "Weapons", StringComparison.OrdinalIgnoreCase))
            {
                parts[0] = "weapons";
            }
            else if (string.Equals(parts[0], "NPCS", StringComparison.OrdinalIgnoreCase))
            {
                parts[0] = "npcs";
            }

            return string.Join("\\", parts);
        }

        private static Dictionary<string, ItemTransferPackageAssetSummary> BuildAssetSummaryLookup(ItemTransferImportResult result)
        {
            Dictionary<string, ItemTransferPackageAssetSummary> lookup = new Dictionary<string, ItemTransferPackageAssetSummary>(StringComparer.OrdinalIgnoreCase);
            if (result == null)
            {
                return lookup;
            }

            if (result.AssetSummaries == null)
            {
                result.AssetSummaries = new List<ItemTransferPackageAssetSummary>();
            }

            return lookup;
        }

        private static bool IsFatalPackageReadError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return false;
            }

            return error.IndexOf("decode package footer", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("Package is too short", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("could not be read", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void IncrementAssetSummary(
            ItemTransferImportResult result,
            Dictionary<string, ItemTransferPackageAssetSummary> lookup,
            string packageName,
            bool imported)
        {
            if (result == null || lookup == null || string.IsNullOrWhiteSpace(packageName))
            {
                return;
            }

            string package = NormalizePackageName(packageName);
            ItemTransferPackageAssetSummary summary;
            if (!lookup.TryGetValue(package, out summary))
            {
                summary = new ItemTransferPackageAssetSummary { Package = package };
                lookup[package] = summary;
                if (result.AssetSummaries == null)
                {
                    result.AssetSummaries = new List<ItemTransferPackageAssetSummary>();
                }
                result.AssetSummaries.Add(summary);
            }

            if (imported)
            {
                summary.ImportedCount++;
            }
            else
            {
                summary.ExistingCount++;
            }
        }

        private static void AddDependencyAssetPath(ItemTransferImportResult result, string packageName, string relativePath)
        {
            if (result == null || string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            if (result.DependencyAssetPaths == null)
            {
                result.DependencyAssetPaths = new List<string>();
            }

            string mappedPath = NormalizePackageName(packageName) + "\\" + NormalizePath(relativePath);
            string extension = Path.GetExtension(mappedPath);
            bool isInterestingDependency =
                string.Equals(extension, ".gfx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".att", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".sgc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);

            if (!isInterestingDependency || result.DependencyAssetPaths.Contains(mappedPath, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            if (result.DependencyAssetPaths.Count < 200)
            {
                result.DependencyAssetPaths.Add(mappedPath);
            }
        }

        private static bool IsImportPackageRemapped(string packageName, Dictionary<string, string> packageRemap)
        {
            return packageRemap != null && packageRemap.ContainsKey(NormalizePackageName(packageName));
        }

        private static bool NeedsPackageReferenceRewrite(ItemTransferAssetEntry asset, Dictionary<string, string> packageRemap)
        {
            if (asset == null || packageRemap == null || packageRemap.Count == 0)
            {
                return false;
            }

            string extension = Path.GetExtension(asset.RelativePath ?? string.Empty);
            return string.Equals(extension, ".gfx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".ecm", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".ini", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".cfg", StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] RewritePackageReferences(byte[] payload, Dictionary<string, string> packageRemap)
        {
            if (payload == null || payload.Length == 0 || packageRemap == null || packageRemap.Count == 0)
            {
                return payload;
            }

            Encoding encoding = Encoding.GetEncoding("GBK");
            string text = encoding.GetString(payload);
            string rewritten = text;
            foreach (KeyValuePair<string, string> pair in packageRemap)
            {
                string source = NormalizePackageName(pair.Key);
                string target = NormalizePackageName(pair.Value);
                if (string.IsNullOrWhiteSpace(source)
                    || string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                rewritten = ReplacePackagePrefix(rewritten, source, target);
            }

            return string.Equals(text, rewritten, StringComparison.Ordinal)
                ? payload
                : encoding.GetBytes(rewritten);
        }

        private static string ReplacePackagePrefix(string text, string sourcePackage, string targetPackage)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            string pattern = Regex.Escape(sourcePackage) + "([\\\\/])";
            return Regex.Replace(
                text,
                pattern,
                match => targetPackage + match.Groups[1].Value,
                RegexOptions.IgnoreCase);
        }

        private static void UpdateStagedPackages(
            string stagingRoot,
            Dictionary<string, int> stagedCountsByPackage,
            AssetManager assetManager,
            ItemTransferImportResult result,
            Action<ItemTransferProgressInfo> progress,
            CancellationToken cancellationToken)
        {
            if (stagedCountsByPackage == null || stagedCountsByPackage.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, int> pair in stagedCountsByPackage)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ReportProgress(
                    progress,
                    "Updating PCK",
                    pair.Key + ".pck (" + pair.Value.ToString(CultureInfo.InvariantCulture) + " files)",
                    0,
                    0,
                    true);

                string packageStagingRoot = Path.Combine(stagingRoot, pair.Key);
                string updateError = string.Empty;
                if (pair.Value > 0 && assetManager.ImportStagedPackageAssets(pair.Key, packageStagingRoot, out updateError))
                {
                    result.ImportedAssetCount += pair.Value;
                    result.UpdatedPackageCount++;
                }
                else
                {
                    result.MissingAssetCount += Math.Max(1, pair.Value);
                    if (!string.IsNullOrWhiteSpace(updateError))
                    {
                        throw new InvalidOperationException(updateError);
                    }
                }
            }
        }

        private static int AddManifestItem(
            eListCollection listCollection,
            int targetListIndex,
            ItemTransferPackageManifest manifest,
            Dictionary<int, int> pathIdRemap,
            Dictionary<string, string> packageRemap,
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
                else if (LooksLikeAssetPath(value))
                {
                    value = RemapMappedPackage(value, packageRemap);
                }
                value = CoerceManifestValueForTargetType(value, list.elementTypes[fieldIndex]);
                list.SetValue(newIndex, fieldIndex, value);
            }

            int idFieldIndex = idGenerationService.GetIdFieldIndex(listCollection, targetListIndex);
            newId = 0;
            if (idFieldIndex >= 0)
            {
                HashSet<int> used = idGenerationService.BuildUsedIds(listCollection, targetListIndex, idFieldIndex);
                int startCandidate = GetSequentialImportIdCandidate(list, newIndex, idFieldIndex);
                newId = idGenerationService.GetNextUniqueId(used, startCandidate);
                list.SetValue(newIndex, idFieldIndex, newId.ToString(CultureInfo.InvariantCulture));
            }

            return newIndex;
        }

        private static List<ItemTransferImportedPath> BuildImportedModelPathSummary(
            ItemTransferPackageManifest manifest,
            CacheSave database,
            Dictionary<int, int> pathIdRemap)
        {
            List<ItemTransferImportedPath> summary = new List<ItemTransferImportedPath>();
            if (manifest == null || manifest.Fields == null || database == null || database.pathById == null)
            {
                return summary;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ItemTransferFieldValue field in manifest.Fields)
            {
                if (field == null || !IsModelAssetPathIdField(field.Name))
                {
                    continue;
                }

                int originalPathId = TryParseInt(field.Value);
                if (originalPathId <= 0)
                {
                    continue;
                }

                int targetPathId;
                if (pathIdRemap == null || !pathIdRemap.TryGetValue(originalPathId, out targetPathId))
                {
                    targetPathId = originalPathId;
                }

                string mappedPath;
                database.pathById.TryGetValue(targetPathId, out mappedPath);
                string key = (field.Name ?? string.Empty) + "|" + targetPathId.ToString(CultureInfo.InvariantCulture);
                if (!seen.Add(key))
                {
                    continue;
                }

                summary.Add(new ItemTransferImportedPath
                {
                    FieldName = field.Name ?? string.Empty,
                    OriginalPathId = originalPathId,
                    TargetPathId = targetPathId,
                    MappedPath = mappedPath ?? string.Empty
                });
            }

            return summary;
        }

        private static string CoerceManifestValueForTargetType(string value, string targetType)
        {
            string type = targetType ?? string.Empty;
            string text = value ?? string.Empty;
            if (type == "int16" || type == "int32" || type == "int64")
            {
                long whole;
                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out whole)
                    || long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
                {
                    return whole.ToString(CultureInfo.InvariantCulture);
                }

                double numeric;
                if (TryParseFlexibleDouble(text, out numeric))
                {
                    return ((long)numeric).ToString(CultureInfo.InvariantCulture);
                }
            }

            if (type == "float" || type == "double")
            {
                double numeric;
                if (TryParseFlexibleDouble(text, out numeric))
                {
                    return numeric.ToString(CultureInfo.CurrentCulture);
                }
            }

            return text;
        }

        private static bool TryParseFlexibleDouble(string value, out double result)
        {
            string text = (value ?? string.Empty).Trim();
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out result)
                || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }

            string invariantDecimal = text.Replace(',', '.');
            if (double.TryParse(invariantDecimal, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }

            string currentDecimal = text.Replace('.', CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);
            return double.TryParse(currentDecimal, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
        }

        private static int GetSequentialImportIdCandidate(eList list, int newIndex, int idFieldIndex)
        {
            if (list == null || idFieldIndex < 0)
            {
                return 1;
            }

            for (int rowIndex = Math.Min(newIndex - 1, list.elementValues.Length - 2); rowIndex >= 0; rowIndex--)
            {
                int previousId;
                if (int.TryParse(list.GetValue(rowIndex, idFieldIndex), out previousId) && previousId > 0)
                {
                    return previousId + 1;
                }
            }

            return 1;
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

            if (IsEquipmentEssenceListName(manifest.SourceListName))
            {
                for (int i = 0; i < listCollection.Lists.Length; i++)
                {
                    if (IsEquipmentEssenceList(listCollection.Lists[i]))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public static bool IsEquipmentEssenceList(eList list)
        {
            string listName = list != null ? list.listName : string.Empty;
            if (IsEquipmentEssenceListName(listName))
            {
                return true;
            }

            return HasEquipmentEssenceFields(list);
        }

        private static bool IsEquipmentEssenceListName(string listName)
        {
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

            return false;
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

        private static bool IsIconAssetPathIdField(string fieldName)
        {
            return string.Equals(fieldName, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "file_icon1", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsModelAssetPathIdField(string fieldName)
        {
            string normalized = (fieldName ?? string.Empty).ToLowerInvariant();
            return normalized.StartsWith("file_model", StringComparison.OrdinalIgnoreCase)
                || (normalized.StartsWith("models_", StringComparison.OrdinalIgnoreCase)
                    && normalized.Contains("_file_model"))
                || string.Equals(normalized, "id_change_model", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsModelProfessionFieldName(string fieldName)
        {
            string normalized = (fieldName ?? string.Empty).Trim();
            return normalized.StartsWith("models_", StringComparison.OrdinalIgnoreCase)
                && normalized.EndsWith("_profession", StringComparison.OrdinalIgnoreCase);
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
