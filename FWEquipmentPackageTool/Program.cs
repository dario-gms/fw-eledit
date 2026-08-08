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

            Report("Collecting equipment assets", "Finding direct model, icon and file paths from this equipment item...", 0, 0, true);
            Dictionary<string, ItemTransferAssetEntry> assetsByKey = CollectDirectAssets(manifest, assetManager);
            ExpandEquipmentGfxDependencies(assetsByKey, assetManager);
            ExpandEquipmentModelCompanions(assetsByKey, assetManager);
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
            Report("Export complete", "Item package created.", result.AssetCount, result.AssetCount, false);
            return result;
        }

        private static Dictionary<string, ItemTransferAssetEntry> CollectDirectAssets(ItemTransferPackageManifest manifest, AssetManager assetManager)
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
                            AddAsset(assets, mapped);
                        }
                    }
                }

                if (LooksLikeAssetPath(value))
                {
                    AddAsset(assets, value);
                }
            }

            return assets;
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
            string text = DecodePrintablePayload(payload);
            MatchCollection matches = Regex.Matches(text, @"[A-Za-z0-9_\-./\\\u0080-\uFFFF ]{1,220}\.(?:dds|tga|bmp|png|jpg|jpeg|gfx)", RegexOptions.IgnoreCase);
            HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < matches.Count; i++)
            {
                string raw = NormalizePath(matches[i].Value.Trim().Trim('\0').TrimStart('.', '\\', '/'));
                foreach (string candidate in ResolveReferenceCandidates(current.MappedPath, raw))
                {
                    if (yielded.Add(candidate))
                    {
                        yield return candidate;
                    }
                }
            }
        }

        private static IEnumerable<string> CollectModelReferenceCandidates(string currentMappedPath, byte[] payload)
        {
            string text = DecodeGbkPayload(payload);
            MatchCollection matches = Regex.Matches(text, @"[^\0\r\n\t""'<>|:*?]{1,220}\.(?:dds|tga|bmp|png|jpg|jpeg|ski|smd|ecm|att|sgc|bon|stck|sdr)", RegexOptions.IgnoreCase);
            HashSet<string> yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < matches.Count; i++)
            {
                string raw = NormalizePath(matches[i].Value.Trim().Trim('\0').TrimStart('.', '\\', '/'));
                foreach (string candidate in ResolveReferenceCandidates(currentMappedPath, raw))
                {
                    if (yielded.Add(candidate))
                    {
                        yield return candidate;
                    }
                }
            }
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

        private static bool IsPackageAssetPathIdField(string fieldName)
        {
            string normalized = (fieldName ?? string.Empty).ToLowerInvariant();
            return string.Equals(normalized, "file_matter", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "file_icon1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "normal_attack_sfx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "music_pick", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "music_drop", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_change_model", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("file_model", StringComparison.OrdinalIgnoreCase)
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
