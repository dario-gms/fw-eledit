using FWEledit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace FWEquipmentPackageTool
{
    internal static class Program
    {
        private const string PackageFormat = "FWEledit.ItemTransfer";
        private const int PackageFormatVersion = 1;
        private static readonly string[] KnownPackages = new string[]
        {
            "building", "configs", "gfx", "grasses", "interfaces", "litmodels", "loddata", "models", "models2",
            "moxing", "script", "sfx", "shaders", "surfaces", "textures", "music"
        };
        private static readonly string[] ModelReferencePackageFallbacks = new string[]
        {
            "models", "models2", "moxing", "litmodels", "shaders", "grasses", "surfaces"
        };

        private static readonly Regex AssetExtensionPattern = new Regex(
            "\\.(dds|tga|bmp|png|jpg|jpeg|ski|smd|ecm|gfx|att|sgc|bon|stck|txt|ini|cfg|sdr)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.InputEncoding = Encoding.UTF8;

                if (args.Length < 1 || !string.Equals(args[0], "export-equipment", StringComparison.OrdinalIgnoreCase))
                {
                    PrintUsage();
                    return 2;
                }

                string requestFile = GetArgument(args, "--request");
                if (string.IsNullOrWhiteSpace(requestFile) || !File.Exists(requestFile))
                {
                    Console.Error.WriteLine("Request file not found.");
                    return 2;
                }

                JavaScriptSerializer serializer = CreateSerializer();
                EquipmentExportRequest request = serializer.Deserialize<EquipmentExportRequest>(File.ReadAllText(requestFile, Encoding.UTF8));
                EquipmentExportResult result = ExportEquipment(request);
                if (!string.IsNullOrWhiteSpace(request.ResultFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(request.ResultFile));
                    File.WriteAllText(request.ResultFile, serializer.Serialize(result), Encoding.UTF8);
                }

                Console.WriteLine("RESULT|" + serializer.Serialize(result));
                return result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static EquipmentExportResult ExportEquipment(EquipmentExportRequest request)
        {
            EquipmentExportResult result = new EquipmentExportResult { MissingAssets = new List<string>() };
            if (request == null || request.Manifest == null)
            {
                result.ErrorMessage = "Invalid export request.";
                return result;
            }
            if (string.IsNullOrWhiteSpace(request.GameRootPath) || !Directory.Exists(request.GameRootPath))
            {
                result.ErrorMessage = "Invalid game root path.";
                return result;
            }
            if (string.IsNullOrWhiteSpace(request.OutputFile))
            {
                result.ErrorMessage = "Invalid output file.";
                return result;
            }

            AssetManager.GameRootPath = request.GameRootPath;
            AssetManager.WorkspaceRootPath = request.WorkspaceRootPath ?? string.Empty;
            AssetManager assetManager = new AssetManager();

            ItemTransferPackageManifest manifest = request.Manifest;
            manifest.Format = PackageFormat;
            manifest.FormatVersion = PackageFormatVersion;
            if (manifest.Assets == null)
            {
                manifest.Assets = new List<ItemTransferAssetEntry>();
            }
            if (manifest.PathDataEntries == null)
            {
                manifest.PathDataEntries = new List<ItemTransferPathDataEntry>();
            }
            EnsureManifestPathDataEntries(manifest);

            Report("Collecting item assets", "Finding direct model, icon and file paths from this item...", 0, 0, true);
            Dictionary<string, List<string>> entriesByPackage = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ItemTransferAssetEntry> assetsByKey = CollectDirectAssets(manifest, assetManager, entriesByPackage);
            AddManifestPathDataAssets(manifest, assetsByKey, assetManager, entriesByPackage);
            ExpandEquipmentModelCompanions(assetsByKey, assetManager);
            ExpandEquipmentGfxDependencies(assetsByKey, assetManager);
            manifest.Assets = assetsByKey.Values.OrderBy(a => a.Package).ThenBy(a => a.RelativePath).ToList();

            string folder = Path.GetDirectoryName(request.OutputFile);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                Directory.CreateDirectory(folder);
            }
            if (File.Exists(request.OutputFile))
            {
                File.Delete(request.OutputFile);
            }

            Report("Creating package", "Writing manifest and assets...", 0, manifest.Assets.Count, false);
            using (ZipArchive archive = ZipFile.Open(request.OutputFile, ZipArchiveMode.Create))
            {
                WriteTextEntry(archive, "manifest.json", CreateSerializer().Serialize(manifest));
                int written = 0;
                foreach (ItemTransferAssetEntry asset in manifest.Assets)
                {
                    Report("Writing package", asset.MappedPath ?? asset.RelativePath ?? string.Empty, written, manifest.Assets.Count, false);

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
            result.AssetCount = CountPackageAssets(request.OutputFile);
            result.MissingAssetCount = result.MissingAssets.Count;

            if (HasCriticalMissingAssets(result.MissingAssets))
            {
                TryDeletePartialPackage(request.OutputFile);
                result.Success = false;
                result.ErrorMessage = BuildMissingAssetsErrorMessage(result.MissingAssets);
                Report("Export failed", result.ErrorMessage, result.AssetCount, manifest.Assets.Count, false);
                return result;
            }

            if (result.AssetCount == 0 && HasVisualPathDataEntries(manifest))
            {
                TryDeletePartialPackage(request.OutputFile);
                result.Success = false;
                result.ErrorMessage = BuildNoVisualAssetsErrorMessage(manifest);
                Report("Export failed", result.ErrorMessage, result.AssetCount, manifest.Assets.Count, false);
                return result;
            }

            Report("Export complete", "Item package created.", result.AssetCount, result.AssetCount, false);
            return result;
        }

        private static void EnsureManifestPathDataEntries(ItemTransferPackageManifest manifest)
        {
            if (manifest == null || manifest.Fields == null)
            {
                return;
            }

            Dictionary<int, string> loadedPathData = null;
            HashSet<int> knownPathIds = new HashSet<int>(
                (manifest.PathDataEntries ?? new List<ItemTransferPathDataEntry>())
                    .Where(p => p != null && p.PathId > 0)
                    .Select(p => p.PathId));

            foreach (ItemTransferFieldValue field in manifest.Fields)
            {
                if (field == null || !IsPackageAssetPathIdField(field.Name))
                {
                    continue;
                }

                int pathId = TryParseInt(field.Value);
                if (pathId <= 0 || knownPathIds.Contains(pathId))
                {
                    continue;
                }

                if (loadedPathData == null)
                {
                    loadedPathData = LoadPathDataById();
                }

                string mappedPath;
                if (loadedPathData != null
                    && loadedPathData.TryGetValue(pathId, out mappedPath)
                    && !string.IsNullOrWhiteSpace(mappedPath))
                {
                    manifest.PathDataEntries.Add(new ItemTransferPathDataEntry
                    {
                        PathId = pathId,
                        MappedPath = NormalizePath(mappedPath)
                    });
                    knownPathIds.Add(pathId);
                }
            }
        }

        private static Dictionary<int, string> LoadPathDataById()
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            string pathDataFile = ResolvePathDataFile();
            if (string.IsNullOrWhiteSpace(pathDataFile) || !File.Exists(pathDataFile))
            {
                return result;
            }

            try
            {
                Encoding encoding = Encoding.GetEncoding("GBK");
                using (FileStream stream = File.OpenRead(pathDataFile))
                using (BinaryReader reader = new BinaryReader(stream, encoding))
                {
                    if (reader.BaseStream.Length < 8)
                    {
                        return result;
                    }

                    string magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    if (!string.Equals(magic, "DIMP", StringComparison.Ordinal))
                    {
                        return result;
                    }

                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        if (reader.BaseStream.Position + 8 > reader.BaseStream.Length)
                        {
                            break;
                        }

                        int id = reader.ReadInt32();
                        int length = reader.ReadInt32();
                        if (id < 0 || length < 0 || length > 8192 || reader.BaseStream.Position + length > reader.BaseStream.Length)
                        {
                            break;
                        }

                        string mappedPath = encoding.GetString(reader.ReadBytes(length)).Replace('/', '\\');
                        if (!result.ContainsKey(id))
                        {
                            result.Add(id, mappedPath);
                        }
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        private static string ResolvePathDataFile()
        {
            if (!string.IsNullOrWhiteSpace(AssetManager.WorkspaceRootPath))
            {
                string workspacePathData = Path.Combine(AssetManager.WorkspaceRootPath, "data", "path.data");
                if (File.Exists(workspacePathData))
                {
                    return workspacePathData;
                }
            }

            if (!string.IsNullOrWhiteSpace(AssetManager.GameRootPath))
            {
                string gamePathData = Path.Combine(AssetManager.GameRootPath, "data", "path.data");
                if (File.Exists(gamePathData))
                {
                    return gamePathData;
                }

                string felEditPathData = Path.Combine(AssetManager.GameRootPath, "fELedit", "resources", "data", "path.data");
                if (File.Exists(felEditPathData))
                {
                    return felEditPathData;
                }
            }

            return string.Empty;
        }

        private static Dictionary<string, ItemTransferAssetEntry> CollectDirectAssets(
            ItemTransferPackageManifest manifest,
            AssetManager assetManager,
            Dictionary<string, List<string>> entriesByPackage)
        {
            Dictionary<string, ItemTransferAssetEntry> assets = new Dictionary<string, ItemTransferAssetEntry>(StringComparer.OrdinalIgnoreCase);
            Dictionary<int, string> pathDataById = (manifest.PathDataEntries ?? new List<ItemTransferPathDataEntry>())
                .Where(p => p != null && p.PathId > 0 && !string.IsNullOrWhiteSpace(p.MappedPath))
                .GroupBy(p => p.PathId)
                .ToDictionary(g => g.Key, g => NormalizePath(g.First().MappedPath));

            foreach (ItemTransferFieldValue field in manifest.Fields ?? new List<ItemTransferFieldValue>())
            {
                string fieldName = field.Name ?? string.Empty;
                string value = field.Value ?? string.Empty;
                if (IsPackageAssetPathIdField(fieldName))
                {
                    int pathId = TryParseInt(value);
                    string mapped;
                    if (pathId > 0 && pathDataById.TryGetValue(pathId, out mapped) && !string.IsNullOrWhiteSpace(mapped))
                    {
                        if (IsIconAssetPathIdField(fieldName))
                        {
                            ItemTransferAssetEntry ignored;
                            TryAddExistingAsset(assets, mapped, assetManager, out ignored);
                        }
                        else
                        {
                            ItemTransferAssetEntry added;
                            if (!ContainsAsset(assets, mapped)
                                && !TryAddExistingAsset(assets, mapped, assetManager, out added))
                            {
                                string fallback;
                                if (TryResolveFallbackAssetPath(mapped, assetManager, entriesByPackage, out fallback))
                                {
                                    RemapPathDataEntry(manifest, pathId, fallback);
                                    TryAddExistingAsset(assets, fallback, assetManager, out added);
                                }
                                else
                                {
                                    AddAsset(assets, mapped);
                                }
                            }
                        }
                    }
                }

                if (LooksLikeAssetPath(value))
                {
                    ItemTransferAssetEntry added;
                    if (!ContainsAsset(assets, value)
                        && !TryAddExistingAsset(assets, value, assetManager, out added))
                    {
                        string fallback;
                        AddAsset(assets, TryResolveFallbackAssetPath(value, assetManager, entriesByPackage, out fallback) ? fallback : value);
                    }
                }
            }

            return assets;
        }

        private static void AddManifestPathDataAssets(
            ItemTransferPackageManifest manifest,
            Dictionary<string, ItemTransferAssetEntry> assets,
            AssetManager assetManager,
            Dictionary<string, List<string>> entriesByPackage)
        {
            if (manifest == null || manifest.PathDataEntries == null || assets == null)
            {
                return;
            }

            foreach (ItemTransferPathDataEntry entry in manifest.PathDataEntries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.MappedPath))
                {
                    continue;
                }

                string mapped = NormalizePath(entry.MappedPath);
                string package;
                string relative;
                if (!TrySplitPackagePath(mapped, out package, out relative))
                {
                    continue;
                }

                string extension = Path.GetExtension(relative);
                if (!IsVisualAssetExtension(extension))
                {
                    continue;
                }

                ItemTransferAssetEntry added;
                if (ContainsAsset(assets, mapped) || TryAddExistingAsset(assets, mapped, assetManager, out added))
                {
                    continue;
                }

                string fallback;
                AddAsset(assets, TryResolveFallbackAssetPath(mapped, assetManager, entriesByPackage, out fallback) ? fallback : mapped);
            }
        }

        private static bool AddAsset(Dictionary<string, ItemTransferAssetEntry> assets, string mappedPath)
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

        private static void ExpandEquipmentGfxDependencies(Dictionary<string, ItemTransferAssetEntry> assets, AssetManager assetManager)
        {
            Queue<ItemTransferAssetEntry> pending = new Queue<ItemTransferAssetEntry>(assets.Values.Where(IsGfxAsset).ToArray());
            HashSet<string> processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const int MaxProcessedGfx = 256;

            while (pending.Count > 0 && processed.Count < MaxProcessedGfx)
            {
                ItemTransferAssetEntry current = pending.Dequeue();
                if (current == null || !processed.Add(current.Package + "|" + current.RelativePath))
                {
                    continue;
                }

                Report("Resolving GFX dependencies", current.MappedPath ?? current.RelativePath ?? string.Empty, processed.Count, Math.Max(processed.Count + pending.Count, assets.Count), false);
                byte[] payload;
                string error;
                if (!assetManager.TryReadPackageEntry(current.Package, current.RelativePath, out payload, out error) || payload == null)
                {
                    continue;
                }

                foreach (string dependency in CollectGfxReferenceCandidates(current, payload))
                {
                    ItemTransferAssetEntry addedAsset;
                    if (TryAddExistingAsset(assets, dependency, assetManager, out addedAsset) && IsGfxAsset(addedAsset))
                    {
                        pending.Enqueue(addedAsset);
                    }
                }
            }
        }

        private static void ExpandEquipmentModelCompanions(Dictionary<string, ItemTransferAssetEntry> assets, AssetManager assetManager)
        {
            Queue<ItemTransferAssetEntry> pending = new Queue<ItemTransferAssetEntry>(assets.Values.Where(a => a != null && IsModelLikeExtension(Path.GetExtension(a.RelativePath))).ToArray());
            HashSet<string> processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            const int MaxProcessedModels = 512;

            while (pending.Count > 0 && processed.Count < MaxProcessedModels)
            {
                ItemTransferAssetEntry current = pending.Dequeue();
                if (current == null || !processed.Add(current.Package + "|" + current.RelativePath))
                {
                    continue;
                }

                Report("Collecting model companions", current.MappedPath ?? current.RelativePath ?? string.Empty, processed.Count, Math.Max(processed.Count + pending.Count, assets.Count), false);
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

        private static IEnumerable<string> CollectGfxReferenceCandidates(ItemTransferAssetEntry current, byte[] payload)
        {
            string text = DecodeGbkPayload(payload);
            HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            MatchCollection matches = Regex.Matches(
                text,
                @"[A-Za-z0-9_\-./\\\u0080-\uFFFF ]{1,220}\.(?:dds|tga|bmp|png|jpg|jpeg|ski|smd|ecm|gfx|att|sgc|bon|stck|sdr)",
                RegexOptions.IgnoreCase);
            for (int i = 0; i < matches.Count; i++)
            {
                string raw = NormalizeExtractedPathCandidate(matches[i].Value.Trim().Trim('\0').TrimStart('.', '\\', '/'));
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

                candidate = NormalizePath(candidate);
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

        private static IEnumerable<string> CollectModelReferenceCandidates(string currentMappedPath, byte[] payload)
        {
            string text = DecodeGbkPayload(payload);
            HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

            MatchCollection matches = Regex.Matches(text, @"[^\0\r\n\t""'<>|:*?]{1,220}\.(?:dds|tga|bmp|png|jpg|jpeg|ski|smd|ecm|gfx|att|sgc|bon|stck|sdr)", RegexOptions.IgnoreCase);
            for (int i = 0; i < matches.Count; i++)
            {
                string raw = NormalizeExtractedPathCandidate(matches[i].Value.Trim().Trim('\0').TrimStart('.', '\\', '/'));
                foreach (string candidate in ResolveReferenceCandidates(currentMappedPath, raw))
                {
                    if (yielded.Add(candidate))
                    {
                        yield return candidate;
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

        private static IEnumerable<string> ResolveGfxDependencyCandidates(ItemTransferAssetEntry current, string reference)
        {
            if (current == null)
            {
                yield break;
            }

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

        private static bool TryAddExistingAsset(Dictionary<string, ItemTransferAssetEntry> assets, string mappedPath, AssetManager assetManager, out ItemTransferAssetEntry addedAsset)
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

            AddAsset(assets, package + "\\" + relative);
            addedAsset = assets[key];
            return true;
        }

        private static bool ContainsAsset(Dictionary<string, ItemTransferAssetEntry> assets, string mappedPath)
        {
            string package;
            string relative;
            return TrySplitPackagePath(mappedPath, out package, out relative)
                && assets.ContainsKey(package + "|" + relative);
        }

        private static bool TryResolveFallbackAssetPath(
            string mappedPath,
            AssetManager assetManager,
            Dictionary<string, List<string>> entriesByPackage,
            out string fallbackMappedPath)
        {
            fallbackMappedPath = string.Empty;

            string package;
            string relative;
            if (!TrySplitPackagePath(mappedPath, out package, out relative))
            {
                return false;
            }

            string extension = Path.GetExtension(relative);
            if (string.IsNullOrWhiteSpace(extension) || !IsModelLikeExtension(extension))
            {
                return false;
            }

            foreach (string candidatePackage in ModelReferencePackageFallbacks)
            {
                if (string.Equals(candidatePackage, package, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                byte[] candidatePayload;
                string candidateError;
                if (assetManager.TryReadPackageEntry(candidatePackage, relative, out candidatePayload, out candidateError)
                    && candidatePayload != null)
                {
                    fallbackMappedPath = candidatePackage + "\\" + NormalizePath(relative);
                    Report("Resolved cross-package fallback", NormalizePath(mappedPath) + " -> " + fallbackMappedPath, 0, 0, true);
                    return true;
                }
            }

            List<string> entries;
            if (!entriesByPackage.TryGetValue(package, out entries))
            {
                if (!assetManager.TryEnumeratePckIndexEntries(package, out entries))
                {
                    entries = new List<string>();
                }
                entriesByPackage[package] = entries;
            }

            if (entries == null || entries.Count == 0)
            {
                return false;
            }

            string normalizedRelative = NormalizePath(relative);
            string directory = NormalizePath(Path.GetDirectoryName(normalizedRelative) ?? string.Empty);
            string fileName = Path.GetFileNameWithoutExtension(normalizedRelative) ?? string.Empty;
            string looseFileName = NormalizeModelVariantName(fileName);

            string best = string.Empty;
            int bestScore = 0;
            foreach (string entry in entries)
            {
                string normalizedEntry = NormalizePath(entry);
                if (!string.Equals(Path.GetExtension(normalizedEntry), extension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string entryDirectory = NormalizePath(Path.GetDirectoryName(normalizedEntry) ?? string.Empty);
                if (!string.Equals(entryDirectory, directory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string entryFileName = Path.GetFileNameWithoutExtension(normalizedEntry) ?? string.Empty;
                int score = ScoreFallbackName(fileName, looseFileName, entryFileName);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = normalizedEntry;
                }
            }

            if (bestScore < 50 || string.IsNullOrWhiteSpace(best))
            {
                return false;
            }

            fallbackMappedPath = package + "\\" + best;
            Report("Resolved missing model fallback", NormalizePath(mappedPath) + " -> " + fallbackMappedPath, 0, 0, true);
            return true;
        }

        private static int ScoreFallbackName(string missingFileName, string normalizedMissingFileName, string candidateFileName)
        {
            if (string.IsNullOrWhiteSpace(missingFileName) || string.IsNullOrWhiteSpace(candidateFileName))
            {
                return 0;
            }

            string normalizedCandidate = NormalizeModelVariantName(candidateFileName);
            int score = LongestCommonSubsequenceLength(missingFileName, candidateFileName);
            if (string.Equals(normalizedMissingFileName, normalizedCandidate, StringComparison.OrdinalIgnoreCase))
            {
                score += 100;
            }
            if (StartsWithSameToken(missingFileName, candidateFileName))
            {
                score += 20;
            }
            if (EndsWithSameToken(missingFileName, candidateFileName))
            {
                score += 20;
            }

            return score;
        }

        private static string NormalizeModelVariantName(string value)
        {
            string normalized = (value ?? string.Empty).ToLowerInvariant();
            normalized = normalized.Replace("female", string.Empty)
                .Replace("male", string.Empty)
                .Replace("女", string.Empty)
                .Replace("男", string.Empty);
            return normalized.Trim();
        }

        private static bool StartsWithSameToken(string left, string right)
        {
            string leftToken = FirstToken(left);
            string rightToken = FirstToken(right);
            return !string.IsNullOrWhiteSpace(leftToken)
                && string.Equals(leftToken, rightToken, StringComparison.OrdinalIgnoreCase);
        }

        private static bool EndsWithSameToken(string left, string right)
        {
            string leftToken = LastToken(left);
            string rightToken = LastToken(right);
            return !string.IsNullOrWhiteSpace(leftToken)
                && string.Equals(leftToken, rightToken, StringComparison.OrdinalIgnoreCase);
        }

        private static string FirstToken(string value)
        {
            string[] parts = (value ?? string.Empty).Split(new char[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : string.Empty;
        }

        private static string LastToken(string value)
        {
            string[] parts = (value ?? string.Empty).Split(new char[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[parts.Length - 1] : string.Empty;
        }

        private static int LongestCommonSubsequenceLength(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                return 0;
            }

            int[] previous = new int[right.Length + 1];
            int[] current = new int[right.Length + 1];
            for (int i = 1; i <= left.Length; i++)
            {
                for (int j = 1; j <= right.Length; j++)
                {
                    current[j] = char.ToLowerInvariant(left[i - 1]) == char.ToLowerInvariant(right[j - 1])
                        ? previous[j - 1] + 1
                        : Math.Max(previous[j], current[j - 1]);
                }

                int[] swap = previous;
                previous = current;
                current = swap;
                Array.Clear(current, 0, current.Length);
            }

            return previous[right.Length];
        }

        private static void RemapPathDataEntry(ItemTransferPackageManifest manifest, int pathId, string mappedPath)
        {
            if (manifest == null || manifest.PathDataEntries == null || pathId <= 0 || string.IsNullOrWhiteSpace(mappedPath))
            {
                return;
            }

            for (int i = 0; i < manifest.PathDataEntries.Count; i++)
            {
                ItemTransferPathDataEntry entry = manifest.PathDataEntries[i];
                if (entry != null && entry.PathId == pathId)
                {
                    entry.MappedPath = NormalizePath(mappedPath);
                    return;
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
                foreach (string fallback in ResolvePackageFallbackReferenceCandidates(currentPackage, normalizedReference))
                {
                    yield return fallback;
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

        private static IEnumerable<string> ResolvePackageFallbackReferenceCandidates(string currentPackage, string normalizedReference)
        {
            if (string.IsNullOrWhiteSpace(normalizedReference) || normalizedReference.IndexOf('\\') < 0)
            {
                yield break;
            }

            for (int i = 0; i < ModelReferencePackageFallbacks.Length; i++)
            {
                string package = ModelReferencePackageFallbacks[i];
                if (string.Equals(package, currentPackage, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return package + "\\" + normalizedReference;
            }
        }

        private static bool IsPackageAssetPathIdField(string fieldName)
        {
            string normalized = NormalizeFieldNameKey(fieldName);
            return string.Equals(normalized, "file_matter", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "file_icon1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "normal_attack_sfx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "music_pick", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "music_drop", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_change_model", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("file_model", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("file_models", StringComparison.OrdinalIgnoreCase)
                || (normalized.StartsWith("models_", StringComparison.OrdinalIgnoreCase) && normalized.Contains("_file_model"))
                || normalized.StartsWith("gfx_", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("_gfx_")
                || normalized.EndsWith("_gfx", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsIconAssetPathIdField(string fieldName)
        {
            return string.Equals(fieldName, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "file_icon1", StringComparison.OrdinalIgnoreCase);
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

        private static string NormalizeFieldNameKey(string fieldName)
        {
            return (fieldName ?? string.Empty)
                .Trim()
                .Replace(' ', '_')
                .Replace('-', '_')
                .ToLowerInvariant();
        }

        private static bool IsGfxAsset(ItemTransferAssetEntry asset)
        {
            return asset != null
                && string.Equals(asset.Package, "gfx", StringComparison.OrdinalIgnoreCase)
                && string.Equals(Path.GetExtension(asset.RelativePath), ".gfx", StringComparison.OrdinalIgnoreCase);
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

        private static int TryParseInt(string value)
        {
            int result;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0;
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
            try
            {
                return Encoding.GetEncoding("GBK").GetString(payload).Replace('\0', '\n');
            }
            catch
            {
                return DecodePrintablePayload(payload);
            }
        }

        private static void WriteTextEntry(ZipArchive archive, string name, string text)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (StreamWriter writer = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                writer.Write(text ?? string.Empty);
            }
        }

        private static int CountPackageAssets(string packageFile)
        {
            using (ZipArchive archive = ZipFile.OpenRead(packageFile))
            {
                return archive.Entries.Count(entry => entry.FullName.StartsWith("assets/", StringComparison.OrdinalIgnoreCase));
            }
        }

        private static bool HasCriticalMissingAssets(IEnumerable<string> missingAssets)
        {
            if (missingAssets == null)
            {
                return false;
            }

            foreach (string missing in missingAssets)
            {
                string normalized = NormalizePath(missing ?? string.Empty);
                if (IsNonCriticalDisplayAsset(normalized))
                {
                    continue;
                }

                string extension = Path.GetExtension(normalized);
                if (IsModelLikeExtension(extension) || IsTextureLikeExtension(extension))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNonCriticalDisplayAsset(string normalizedPath)
        {
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return false;
            }

            string path = normalizedPath.Replace('/', '\\').Trim().TrimStart('\\');
            return path.StartsWith("surfaces\\", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("sfx\\", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("interface\\", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("interfaces\\", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTextureLikeExtension(string ext)
        {
            return string.Equals(ext, ".dds", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".tga", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".bmp", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildMissingAssetsErrorMessage(IEnumerable<string> missingAssets)
        {
            List<string> missing = (missingAssets ?? Enumerable.Empty<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Take(8)
                .ToList();

            string message = "Export failed because required model assets are missing from the source client.";
            if (missing.Count > 0)
            {
                message += "\nMissing assets:\n- " + string.Join("\n- ", missing);
            }

            return message;
        }

        private static bool HasVisualPathDataEntries(ItemTransferPackageManifest manifest)
        {
            return manifest != null
                && manifest.PathDataEntries != null
                && manifest.PathDataEntries.Any(entry => entry != null && IsVisualAssetExtension(Path.GetExtension(entry.MappedPath ?? string.Empty)));
        }

        private static bool IsVisualAssetExtension(string ext)
        {
            return IsModelLikeExtension(ext)
                || IsTextureLikeExtension(ext)
                || string.Equals(ext, ".gfx", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildNoVisualAssetsErrorMessage(ItemTransferPackageManifest manifest)
        {
            List<string> visualPaths = (manifest != null && manifest.PathDataEntries != null
                    ? manifest.PathDataEntries
                    : Enumerable.Empty<ItemTransferPathDataEntry>())
                .Where(entry => entry != null && IsVisualAssetExtension(Path.GetExtension(entry.MappedPath ?? string.Empty)))
                .Select(entry => entry.MappedPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Take(8)
                .ToList();

            string message = "Export failed because no visual assets were written to the item package.";
            if (visualPaths.Count > 0)
            {
                message += "\nExpected visual assets:\n- " + string.Join("\n- ", visualPaths);
            }

            return message;
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

        private static void Report(string stage, string detail, int current, int total, bool indeterminate)
        {
            Console.WriteLine("PROGRESS|{0}|{1}|{2}|{3}|{4}",
                EscapeProgress(stage),
                EscapeProgress(detail),
                current,
                total,
                indeterminate ? "1" : "0");
        }

        private static string EscapeProgress(string value)
        {
            return (value ?? string.Empty).Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
        }

        private static string GetArgument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }

        private static JavaScriptSerializer CreateSerializer()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            serializer.RecursionLimit = 128;
            return serializer;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  FWEquipmentPackageTool export-equipment --request request.json");
        }
    }

    public sealed class EquipmentExportRequest
    {
        public string GameRootPath { get; set; }
        public string WorkspaceRootPath { get; set; }
        public string OutputFile { get; set; }
        public string ResultFile { get; set; }
        public ItemTransferPackageManifest Manifest { get; set; }
    }

    public sealed class EquipmentExportResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int AssetCount { get; set; }
        public int MissingAssetCount { get; set; }
        public List<string> MissingAssets { get; set; }
    }
}
