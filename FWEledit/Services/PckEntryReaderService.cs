using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FWEledit
{
    public sealed class PckEntryReaderService
    {
        private const uint Key1 = 566434367;
        private const uint Key2 = 408690725;
        private const int FooterSize = 272;
        private const int PathBytes = 260;
        private const int MinEntrySize = PathBytes + 12;
        private const int MaxEntrySize = 1024 * 1024;
        private const int GameFileEntrySize = 276;
        private const uint PackFlagEncrypt = 0x80000000;
        private const int DiskCacheVersion = 5;

        private static readonly object globalInvalidationSync = new object();
        private static readonly Dictionary<string, int> globalPackageVersions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly object syncRoot = new object();
        private readonly Dictionary<string, PckPackageIndex> packageCache = new Dictionary<string, PckPackageIndex>(StringComparer.OrdinalIgnoreCase);
        public static Action<string, string, TimeSpan, int> ProfileEvent;

        public static void InvalidatePackageGlobally(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return;
            }

            lock (globalInvalidationSync)
            {
                string normalized = packageName.Trim();
                int version;
                globalPackageVersions.TryGetValue(normalized, out version);
                globalPackageVersions[normalized] = version + 1;
            }

            DeletePackageIndexDiskCache(packageName);
        }

        public bool TryWarmPackageIndex(string packageName, out string error)
        {
            error = string.Empty;

            if (!TryGetPackagePaths(packageName, out string normalizedPackage, out string pckPath, out string pkxPath, out error))
            {
                return false;
            }

            PckPackageIndex index;
            return TryGetPackageIndex(normalizedPackage, pckPath, pkxPath, out index, out error) && index != null;
        }

        public bool TryReadFile(string packageName, string relativePath, out byte[] payload, out string error)
        {
            payload = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                error = "Invalid package entry path.";
                return false;
            }

            if (!TryGetPackagePaths(packageName, out string normalizedPackage, out string pckPath, out string pkxPath, out error))
            {
                return false;
            }

            string normalizedEntry = NormalizeLookupKey(relativePath);
            if (normalizedEntry.StartsWith(normalizedPackage.ToLowerInvariant() + "\\", StringComparison.OrdinalIgnoreCase))
            {
                normalizedEntry = normalizedEntry.Substring(normalizedPackage.Length + 1);
            }

            PckPackageIndex index;
            string managedError = string.Empty;
            if (TryGetPackageIndex(normalizedPackage, pckPath, pkxPath, out index, out managedError) && index != null)
            {
                if (TryResolveIndexedEntry(index, normalizedPackage, normalizedEntry, out PckFileEntry entry, out string resolvedRelativePath)
                    && TryReadIndexedEntry(normalizedPackage, pckPath, pkxPath, resolvedRelativePath, entry, out payload, out error))
                {
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                return false;
            }

            error = !string.IsNullOrWhiteSpace(managedError)
                ? managedError
                : "Entry not found in " + normalizedPackage + ".pck: " + relativePath;
            return false;
        }

        public bool TryReadFileFast(
            string packageName,
            string relativePath,
            out byte[] payload,
            out string resolvedRelativePath,
            out string error)
        {
            payload = null;
            resolvedRelativePath = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                error = "Invalid package entry path.";
                return false;
            }

            if (!TryGetPackagePaths(packageName, out string normalizedPackage, out string pckPath, out string pkxPath, out error))
            {
                return false;
            }

            PckPackageIndex index;
            if (!TryGetPackageIndex(normalizedPackage, pckPath, pkxPath, out index, out error) || index == null)
            {
                return false;
            }

            string normalizedEntry = NormalizeLookupKey(relativePath);
            if (normalizedEntry.StartsWith(normalizedPackage.ToLowerInvariant() + "\\", StringComparison.OrdinalIgnoreCase))
            {
                normalizedEntry = normalizedEntry.Substring(normalizedPackage.Length + 1);
            }

            if (!TryResolveIndexedEntry(index, normalizedPackage, normalizedEntry, out PckFileEntry entry, out resolvedRelativePath))
            {
                error = "Entry not found in " + normalizedPackage + ".pck: " + relativePath;
                return false;
            }

            return TryReadIndexedEntry(normalizedPackage, pckPath, pkxPath, resolvedRelativePath, entry, out payload, out error);
        }

        public bool TryResolveSiblingByExtension(
            string packageName,
            string anchorRelativePath,
            string desiredExtension,
            out string resolvedRelativePath,
            out string error)
        {
            resolvedRelativePath = string.Empty;
            error = string.Empty;

            if (!TryGetPackagePaths(packageName, out string normalizedPackage, out string pckPath, out string pkxPath, out error))
            {
                return false;
            }

            PckPackageIndex index;
            if (!TryGetPackageIndex(normalizedPackage, pckPath, pkxPath, out index, out error) || index == null)
            {
                return false;
            }

            string normalizedAnchor = NormalizeLookupKey(anchorRelativePath);
            string normalizedExtension = NormalizeExtension(desiredExtension);
            if (string.IsNullOrWhiteSpace(normalizedAnchor) || string.IsNullOrWhiteSpace(normalizedExtension))
            {
                error = "Invalid sibling preview lookup.";
                return false;
            }

            string exactCandidate = NormalizeLookupKey(Path.ChangeExtension(normalizedAnchor, normalizedExtension));
            if (TryResolveCanonicalEntry(index, exactCandidate, out resolvedRelativePath))
            {
                return true;
            }

            string anchorDirectory = NormalizeLookupKey(Path.GetDirectoryName(normalizedAnchor) ?? string.Empty);
            string anchorFileName = NormalizeLookupKey(Path.GetFileNameWithoutExtension(normalizedAnchor) ?? string.Empty);
            if (string.IsNullOrWhiteSpace(anchorFileName))
            {
                error = "Failed to resolve sibling preview path.";
                return false;
            }

            if (!index.EntriesByExtension.TryGetValue(normalizedExtension, out List<string> extensionEntries) || extensionEntries == null)
            {
                error = "No sibling entries found for " + normalizedExtension + ".";
                return false;
            }

            int anchorPosition = GetEntryPosition(index, normalizedPackage, normalizedAnchor);
            int bestDistance = int.MaxValue;
            int bestPosition = int.MaxValue;
            string bestEntry = string.Empty;
            for (int i = 0; i < extensionEntries.Count; i++)
            {
                string candidate = extensionEntries[i] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                string candidateFileName = NormalizeLookupKey(Path.GetFileNameWithoutExtension(candidate) ?? string.Empty);
                if (!string.Equals(candidateFileName, anchorFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string candidateDirectory = NormalizeLookupKey(Path.GetDirectoryName(candidate) ?? string.Empty);
                int directoryPenalty = string.Equals(candidateDirectory, anchorDirectory, StringComparison.OrdinalIgnoreCase) ? 0 : 1000000;
                int candidatePosition = GetEntryPosition(index, normalizedPackage, candidate);
                int distance = directoryPenalty + ComputeEntryDistance(anchorPosition, candidatePosition);
                if (distance < bestDistance || (distance == bestDistance && candidatePosition < bestPosition))
                {
                    bestDistance = distance;
                    bestPosition = candidatePosition;
                    bestEntry = candidate;
                }
            }

            if (!string.IsNullOrWhiteSpace(bestEntry))
            {
                resolvedRelativePath = bestEntry;
                return true;
            }

            error = "Failed to resolve sibling preview path.";
            return false;
        }

        public bool TryResolveNearestEntryByFileNames(
            string packageName,
            string anchorRelativePath,
            IEnumerable<string> candidateFileNames,
            out string resolvedRelativePath,
            out string error)
        {
            resolvedRelativePath = string.Empty;
            error = string.Empty;

            if (!TryGetPackagePaths(packageName, out string normalizedPackage, out string pckPath, out string pkxPath, out error))
            {
                return false;
            }

            PckPackageIndex index;
            if (!TryGetPackageIndex(normalizedPackage, pckPath, pkxPath, out index, out error) || index == null)
            {
                return false;
            }

            string normalizedAnchor = NormalizeLookupKey(anchorRelativePath);
            int anchorPosition = GetEntryPosition(index, normalizedPackage, normalizedAnchor);
            int bestDistance = int.MaxValue;
            int bestPosition = int.MaxValue;
            string bestEntry = string.Empty;
            HashSet<string> seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<string> safeFileNames = candidateFileNames ?? new string[0];
            foreach (string rawName in safeFileNames)
            {
                string fileName = NormalizeLookupKey(Path.GetFileName(rawName ?? string.Empty) ?? string.Empty);
                if (string.IsNullOrWhiteSpace(fileName) || !seenNames.Add(fileName))
                {
                    continue;
                }

                if (!index.EntriesByFileName.TryGetValue(fileName, out List<string> matches) || matches == null)
                {
                    continue;
                }

                for (int i = 0; i < matches.Count; i++)
                {
                    string candidate = matches[i] ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(candidate))
                    {
                        continue;
                    }

                    int candidatePosition = GetEntryPosition(index, normalizedPackage, candidate);
                    int distance = ComputeEntryDistance(anchorPosition, candidatePosition);
                    if (distance < bestDistance || (distance == bestDistance && candidatePosition < bestPosition))
                    {
                        bestDistance = distance;
                        bestPosition = candidatePosition;
                        bestEntry = candidate;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(bestEntry))
            {
                resolvedRelativePath = bestEntry;
                return true;
            }

            error = "Failed to resolve nearby package entry.";
            return false;
        }

        public bool TryEnumerateEntries(string packageName, out List<string> entries, out string error)
        {
            entries = new List<string>();
            error = string.Empty;

            if (!TryGetPackagePaths(packageName, out string normalizedPackage, out string pckPath, out string pkxPath, out error))
            {
                return false;
            }

            PckPackageIndex index;
            if (!TryGetPackageIndex(normalizedPackage, pckPath, pkxPath, out index, out error) || index == null)
            {
                return false;
            }

            HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in index.Entries.Keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                string normalizedKey = key.Trim().Replace('/', '\\').TrimStart('\\');
                string packagePrefix = normalizedPackage + "\\";
                if (normalizedKey.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedKey = normalizedKey.Substring(packagePrefix.Length);
                }

                if (string.IsNullOrWhiteSpace(normalizedKey) || !unique.Add(normalizedKey))
                {
                    continue;
                }

                entries.Add(normalizedKey);
            }

            return entries.Count > 0;
        }

        public bool TryEnumeratePackageFileEntries(
            string packageName,
            string pckPath,
            string pkxPath,
            out List<string> entries,
            out string error)
        {
            entries = new List<string>();
            error = string.Empty;

            string normalizedPackage = NormalizeExternalPackageName(packageName, pckPath);
            if (string.IsNullOrWhiteSpace(normalizedPackage))
            {
                error = "Invalid package name.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(pckPath) || !File.Exists(pckPath))
            {
                error = "PCK file was not found.";
                return false;
            }

            string safePkxPath = !string.IsNullOrWhiteSpace(pkxPath) && File.Exists(pkxPath)
                ? pkxPath
                : string.Empty;

            PckPackageIndex index;
            if (!TryGetPackageIndex(normalizedPackage, pckPath, safePkxPath, out index, out error) || index == null)
            {
                return false;
            }

            HashSet<string> unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> sourceEntries = index.OrderedEntries != null && index.OrderedEntries.Count > 0
                ? (IEnumerable<string>)index.OrderedEntries
                : index.Entries.Keys;
            foreach (string key in sourceEntries)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                string normalizedKey = NormalizeRelativeForPackage(normalizedPackage, key);
                if (string.IsNullOrWhiteSpace(normalizedKey) || !unique.Add(normalizedKey))
                {
                    continue;
                }

                entries.Add(normalizedKey);
            }

            return true;
        }

        public bool TryReadPackageFileEntry(
            string packageName,
            string pckPath,
            string pkxPath,
            string relativePath,
            out byte[] payload,
            out string resolvedRelativePath,
            out string error)
        {
            payload = null;
            resolvedRelativePath = string.Empty;
            error = string.Empty;

            string normalizedPackage = NormalizeExternalPackageName(packageName, pckPath);
            if (string.IsNullOrWhiteSpace(normalizedPackage))
            {
                error = "Invalid package name.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(pckPath) || !File.Exists(pckPath))
            {
                error = "PCK file was not found.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                error = "Invalid package entry path.";
                return false;
            }

            string safePkxPath = !string.IsNullOrWhiteSpace(pkxPath) && File.Exists(pkxPath)
                ? pkxPath
                : string.Empty;

            PckPackageIndex index;
            if (!TryGetPackageIndex(normalizedPackage, pckPath, safePkxPath, out index, out error) || index == null)
            {
                return false;
            }

            string normalizedEntry = NormalizeRelativeForPackage(normalizedPackage, relativePath);
            if (!TryResolveIndexedEntry(index, normalizedPackage, normalizedEntry, out PckFileEntry entry, out resolvedRelativePath))
            {
                error = "Entry not found in " + normalizedPackage + ".pck: " + relativePath;
                return false;
            }

            return TryReadIndexedEntry(normalizedPackage, pckPath, safePkxPath, resolvedRelativePath, entry, out payload, out error);
        }

        private static bool TryResolveIndexedEntry(
            PckPackageIndex index,
            string packageName,
            string normalizedEntry,
            out PckFileEntry entry,
            out string resolvedRelativePath)
        {
            entry = null;
            resolvedRelativePath = string.Empty;
            if (index == null || string.IsNullOrWhiteSpace(normalizedEntry))
            {
                return false;
            }

            if (index.Entries.TryGetValue(normalizedEntry, out entry))
            {
                resolvedRelativePath = NormalizeRelativeForPackage(packageName, normalizedEntry);
                return true;
            }

            string prefixed = NormalizeLookupKey((packageName ?? string.Empty).Trim() + "\\" + normalizedEntry);
            if (!string.Equals(prefixed, normalizedEntry, StringComparison.OrdinalIgnoreCase)
                && index.Entries.TryGetValue(prefixed, out entry))
            {
                resolvedRelativePath = NormalizeRelativeForPackage(packageName, prefixed);
                return true;
            }

            return false;
        }

        private static bool TryResolveCanonicalEntry(
            PckPackageIndex index,
            string normalizedRelativePath,
            out string resolvedRelativePath)
        {
            resolvedRelativePath = string.Empty;
            if (index == null || string.IsNullOrWhiteSpace(normalizedRelativePath))
            {
                return false;
            }

            if (!index.Entries.TryGetValue(normalizedRelativePath, out PckFileEntry entry) || entry == null)
            {
                return false;
            }

            resolvedRelativePath = !string.IsNullOrWhiteSpace(entry.CanonicalPath)
                ? entry.CanonicalPath
                : normalizedRelativePath;
            return !string.IsNullOrWhiteSpace(resolvedRelativePath);
        }

        private static int GetEntryPosition(PckPackageIndex index, string packageName, string normalizedRelativePath)
        {
            if (index == null)
            {
                return int.MaxValue;
            }

            string normalized = NormalizeLookupKey(normalizedRelativePath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return int.MaxValue;
            }

            if (index.EntryPositions.TryGetValue(normalized, out int position))
            {
                return position;
            }

            string normalizedPackage = NormalizeLookupKey(packageName);
            if (!string.IsNullOrWhiteSpace(normalizedPackage))
            {
                string prefixed = NormalizeLookupKey(normalizedPackage + "\\" + normalized);
                if (index.EntryPositions.TryGetValue(prefixed, out position))
                {
                    return position;
                }
            }

            return int.MaxValue;
        }

        private static int ComputeEntryDistance(int anchorPosition, int candidatePosition)
        {
            if (candidatePosition == int.MaxValue)
            {
                return int.MaxValue;
            }

            if (anchorPosition == int.MaxValue)
            {
                return candidatePosition;
            }

            long delta = (long)anchorPosition - candidatePosition;
            if (delta < 0)
            {
                delta = -delta;
            }

            return delta > int.MaxValue ? int.MaxValue : (int)delta;
        }

        private static string NormalizeExtension(string extension)
        {
            string normalized = (extension ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            if (!normalized.StartsWith("."))
            {
                normalized = "." + normalized;
            }

            return normalized.ToLowerInvariant();
        }

        private static bool TryGetPackagePaths(
            string packageName,
            out string normalizedPackage,
            out string pckPath,
            out string pkxPath,
            out string error)
        {
            normalizedPackage = string.Empty;
            pckPath = string.Empty;
            pkxPath = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(packageName))
            {
                error = "Invalid package name.";
                return false;
            }

            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gameRoot) || !Directory.Exists(gameRoot))
            {
                error = "Game root not configured.";
                return false;
            }

            normalizedPackage = packageName.Trim();
            string resourcesRoot = Path.Combine(gameRoot, "resources");
            string workspaceResources = string.IsNullOrWhiteSpace(AssetManager.WorkspaceRootPath)
                ? string.Empty
                : Path.Combine(AssetManager.WorkspaceRootPath, "resources");
            pckPath = Path.Combine(resourcesRoot, normalizedPackage + ".pck");
            if (!File.Exists(pckPath) && !string.IsNullOrWhiteSpace(workspaceResources))
            {
                pckPath = Path.Combine(workspaceResources, normalizedPackage + ".pck");
            }
            pkxPath = Path.Combine(resourcesRoot, normalizedPackage + ".pkx");
            if (!File.Exists(pkxPath) && !string.IsNullOrWhiteSpace(workspaceResources))
            {
                pkxPath = Path.Combine(workspaceResources, normalizedPackage + ".pkx");
            }
            if (!File.Exists(pckPath))
            {
                error = "Package not found: " + pckPath;
                return false;
            }
            if (!File.Exists(pkxPath))
            {
                pkxPath = string.Empty;
            }

            return true;
        }

        private static string NormalizeRelativeForPackage(string packageName, string relativePath)
        {
            string normalized = NormalizeLookupKey(relativePath);
            string normalizedPackage = NormalizeLookupKey(packageName);
            if (!string.IsNullOrWhiteSpace(normalizedPackage))
            {
                string prefix = normalizedPackage + "\\";
                if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized.Substring(prefix.Length);
                }
            }

            return normalized;
        }

        private static string NormalizeExternalPackageName(string packageName, string pckPath)
        {
            string normalizedPackage = NormalizeLookupKey(packageName);
            if (!string.IsNullOrWhiteSpace(normalizedPackage))
            {
                return normalizedPackage;
            }

            if (string.IsNullOrWhiteSpace(pckPath))
            {
                return string.Empty;
            }

            return NormalizeLookupKey(Path.GetFileNameWithoutExtension(pckPath) ?? string.Empty);
        }

        private static bool TryReadIndexedEntry(
            string packageName,
            string pckPath,
            string pkxPath,
            string relativePath,
            PckFileEntry entry,
            out byte[] payload,
            out string error)
        {
            payload = null;
            error = string.Empty;

            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                using (PckConcatStream stream = new PckConcatStream(pckPath, pkxPath))
                {
                    if (entry.Offset < 0 || entry.CompressedSize <= 0 || entry.Offset + entry.CompressedSize > stream.Length)
                    {
                        error = "Invalid package entry data span for: " + relativePath;
                        return false;
                    }

                    stream.Seek(entry.Offset, SeekOrigin.Begin);
                    byte[] compressed = new byte[entry.CompressedSize];
                    int read = stream.Read(compressed, 0, compressed.Length);
                    if (read != compressed.Length)
                    {
                        error = "Failed to read package entry data: " + relativePath;
                        return false;
                    }

                    if (entry.Encrypted)
                    {
                        DecryptGamePayload(compressed, compressed.Length);
                    }

                    byte[] inflated = TryInflateEntry(compressed);
                    payload = inflated ?? compressed;
                    stopwatch.Stop();
                    EmitProfileEvent("read-entry", packageName + ":" + relativePath, stopwatch.Elapsed, payload.Length);
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = "Failed reading package entry: " + ex.Message;
                return false;
            }
        }

        private bool TryGetPackageIndex(
            string packageName,
            string pckPath,
            string pkxPath,
            out PckPackageIndex index,
            out string error)
        {
            index = null;
            error = string.Empty;

            string signature = BuildPackageSignature(packageName, pckPath, pkxPath);
            string cacheKey = packageName + "|" + pckPath + "|" + pkxPath;

            lock (syncRoot)
            {
                PckPackageIndex cached;
                if (packageCache.TryGetValue(cacheKey, out cached)
                    && cached != null
                    && string.Equals(cached.Signature, signature, StringComparison.OrdinalIgnoreCase))
                {
                    index = cached;
                    return true;
                }
            }

            Dictionary<string, PckFileEntry> decodedEntries;
            List<string> orderedEntries;
            Dictionary<string, int> entryPositions;
            Dictionary<string, List<string>> entriesByExtension;
            Dictionary<string, List<string>> entriesByFileName;
            if (TryLoadPackageIndexFromDiskCache(
                packageName,
                pckPath,
                pkxPath,
                signature,
                out decodedEntries,
                out orderedEntries,
                out entryPositions,
                out entriesByExtension,
                out entriesByFileName))
            {
                PckPackageIndex cachedFromDisk = new PckPackageIndex
                {
                    Signature = signature,
                    Entries = decodedEntries,
                    OrderedEntries = orderedEntries,
                    EntryPositions = entryPositions,
                    EntriesByExtension = entriesByExtension,
                    EntriesByFileName = entriesByFileName
                };

                lock (syncRoot)
                {
                    packageCache[cacheKey] = cachedFromDisk;
                }

                index = cachedFromDisk;
                return true;
            }

            Stopwatch decodeStopwatch = Stopwatch.StartNew();
            if (!TryDecodeIndexEntries(
                packageName,
                pckPath,
                pkxPath,
                out decodedEntries,
                out orderedEntries,
                out entryPositions,
                out entriesByExtension,
                out entriesByFileName,
                out error))
            {
                decodeStopwatch.Stop();
                EmitProfileEvent("decode-index-failed", packageName, decodeStopwatch.Elapsed, 0);
                return false;
            }
            decodeStopwatch.Stop();
            EmitProfileEvent("decode-index", packageName, decodeStopwatch.Elapsed, decodedEntries.Count);
            SavePackageIndexToDiskCache(packageName, pckPath, pkxPath, signature, orderedEntries, decodedEntries);

            PckPackageIndex built = new PckPackageIndex
            {
                Signature = signature,
                Entries = decodedEntries,
                OrderedEntries = orderedEntries,
                EntryPositions = entryPositions,
                EntriesByExtension = entriesByExtension,
                EntriesByFileName = entriesByFileName
            };

            lock (syncRoot)
            {
                packageCache[cacheKey] = built;
            }

            index = built;
            return true;
        }

        private static bool TryLoadPackageIndexFromDiskCache(
            string packageName,
            string pckPath,
            string pkxPath,
            string signature,
            out Dictionary<string, PckFileEntry> entries,
            out List<string> orderedEntries,
            out Dictionary<string, int> entryPositions,
            out Dictionary<string, List<string>> entriesByExtension,
            out Dictionary<string, List<string>> entriesByFileName)
        {
            entries = new Dictionary<string, PckFileEntry>(StringComparer.OrdinalIgnoreCase);
            orderedEntries = new List<string>();
            entryPositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            entriesByExtension = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            entriesByFileName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            string cacheFile = BuildPackageIndexCacheFilePath(packageName, pckPath, pkxPath, signature);
            if (string.IsNullOrWhiteSpace(cacheFile) || !File.Exists(cacheFile))
            {
                return false;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                using (FileStream fs = File.OpenRead(cacheFile))
                using (BinaryReader reader = new BinaryReader(fs, Encoding.UTF8))
                {
                    string magic = reader.ReadString();
                    int version = reader.ReadInt32();
                    string cachedSignature = reader.ReadString();
                    if (!string.Equals(magic, "FWPCKIDX", StringComparison.Ordinal)
                        || version != DiskCacheVersion
                        || !string.Equals(cachedSignature, signature, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    int count = reader.ReadInt32();
                    if (count < 0 || count > 1000000)
                    {
                        return false;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        string normalized = reader.ReadString();
                        long offset = reader.ReadInt64();
                        int compressedSize = reader.ReadInt32();
                        int originalSize = reader.ReadInt32();
                        bool encrypted = reader.ReadBoolean();
                        string canonical = reader.ReadString();
                        if (string.IsNullOrWhiteSpace(normalized) || compressedSize <= 0)
                        {
                            continue;
                        }

                        PckFileEntry entry = new PckFileEntry
                        {
                            Offset = offset,
                            CompressedSize = compressedSize,
                            OriginalSize = originalSize,
                            Encrypted = encrypted,
                            CanonicalPath = string.IsNullOrWhiteSpace(canonical) ? normalized : canonical
                        };
                        entries[normalized] = entry;
                        if (!entryPositions.ContainsKey(normalized))
                        {
                            entryPositions[normalized] = orderedEntries.Count;
                            orderedEntries.Add(normalized);
                        }

                        AddEntryToLookupTables(normalized, entriesByExtension, entriesByFileName);
                    }
                }

                stopwatch.Stop();
                EmitProfileEvent("load-index-cache", packageName, stopwatch.Elapsed, entries.Count);
                return entries.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void SavePackageIndexToDiskCache(
            string packageName,
            string pckPath,
            string pkxPath,
            string signature,
            List<string> orderedEntries,
            Dictionary<string, PckFileEntry> entries)
        {
            if (orderedEntries == null || entries == null || entries.Count == 0)
            {
                return;
            }

            string cacheFile = BuildPackageIndexCacheFilePath(packageName, pckPath, pkxPath, signature);
            if (string.IsNullOrWhiteSpace(cacheFile))
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(cacheFile);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempFile = cacheFile + ".tmp";
                using (FileStream fs = File.Create(tempFile))
                using (BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8))
                {
                    writer.Write("FWPCKIDX");
                    writer.Write(DiskCacheVersion);
                    writer.Write(signature ?? string.Empty);
                    writer.Write(orderedEntries.Count);
                    for (int i = 0; i < orderedEntries.Count; i++)
                    {
                        string normalized = orderedEntries[i] ?? string.Empty;
                        PckFileEntry entry;
                        if (!entries.TryGetValue(normalized, out entry) || entry == null)
                        {
                            writer.Write(string.Empty);
                            writer.Write(0L);
                            writer.Write(0);
                            writer.Write(0);
                            writer.Write(false);
                            writer.Write(string.Empty);
                            continue;
                        }

                        writer.Write(normalized);
                        writer.Write(entry.Offset);
                        writer.Write(entry.CompressedSize);
                        writer.Write(entry.OriginalSize);
                        writer.Write(entry.Encrypted);
                        writer.Write(entry.CanonicalPath ?? normalized);
                    }
                }

                if (File.Exists(cacheFile))
                {
                    File.Delete(cacheFile);
                }
                File.Move(tempFile, cacheFile);
            }
            catch
            {
            }
        }

        private static string BuildPackageIndexCacheFilePath(string packageName, string pckPath, string pkxPath, string signature)
        {
            try
            {
                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FWEledit",
                    "pck-index-cache");
                string key = (packageName ?? string.Empty)
                    + "|"
                    + (pckPath ?? string.Empty)
                    + "|"
                    + (pkxPath ?? string.Empty)
                    + "|"
                    + (signature ?? string.Empty);
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(key));
                    StringBuilder builder = new StringBuilder(hash.Length * 2);
                    for (int i = 0; i < hash.Length; i++)
                    {
                        builder.Append(hash[i].ToString("x2"));
                    }

                    string safePackage = string.IsNullOrWhiteSpace(packageName)
                        ? "package"
                        : packageName.Trim().Replace('\\', '_').Replace('/', '_').Replace(':', '_');
                    return Path.Combine(baseDir, safePackage + "-" + builder.ToString() + ".idx");
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void DeletePackageIndexDiskCache(string packageName)
        {
            try
            {
                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FWEledit",
                    "pck-index-cache");
                if (!Directory.Exists(baseDir))
                {
                    return;
                }

                string safePackage = string.IsNullOrWhiteSpace(packageName)
                    ? "package"
                    : packageName.Trim().Replace('\\', '_').Replace('/', '_').Replace(':', '_');
                string[] files = Directory.GetFiles(baseDir, safePackage + "-*.idx");
                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        File.Delete(files[i]);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private static void AddEntryToLookupTables(
            string normalized,
            Dictionary<string, List<string>> entriesByExtension,
            Dictionary<string, List<string>> entriesByFileName)
        {
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            string normalizedExtension = NormalizeExtension(Path.GetExtension(normalized));
            if (!string.IsNullOrWhiteSpace(normalizedExtension))
            {
                List<string> extensionEntries;
                if (!entriesByExtension.TryGetValue(normalizedExtension, out extensionEntries))
                {
                    extensionEntries = new List<string>();
                    entriesByExtension[normalizedExtension] = extensionEntries;
                }

                extensionEntries.Add(normalized);
            }

            string fileName = NormalizeLookupKey(Path.GetFileName(normalized));
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                List<string> fileNameEntries;
                if (!entriesByFileName.TryGetValue(fileName, out fileNameEntries))
                {
                    fileNameEntries = new List<string>();
                    entriesByFileName[fileName] = fileNameEntries;
                }

                fileNameEntries.Add(normalized);
            }
        }

        private static void EmitProfileEvent(string operation, string target, TimeSpan elapsed, int count)
        {
            Action<string, string, TimeSpan, int> handler = ProfileEvent;
            if (handler == null)
            {
                return;
            }

            try
            {
                handler(operation, target, elapsed, count);
            }
            catch
            {
            }
        }

        private static string BuildPackageSignature(string packageName, string pckPath, string pkxPath)
        {
            try
            {
                int cacheVersion = GetGlobalPackageVersion(packageName);
                FileInfo pck = new FileInfo(pckPath);
                string pckSig = pck.Exists
                    ? pck.Length.ToString() + ":" + pck.LastWriteTimeUtc.Ticks.ToString()
                    : "none";

                if (!string.IsNullOrWhiteSpace(pkxPath))
                {
                    FileInfo pkx = new FileInfo(pkxPath);
                    string pkxSig = pkx.Exists
                        ? pkx.Length.ToString() + ":" + pkx.LastWriteTimeUtc.Ticks.ToString()
                        : "none";
                    return cacheVersion.ToString() + "|" + pckSig + "|" + pkxSig;
                }

                return cacheVersion.ToString() + "|" + pckSig + "|none";
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        private static int GetGlobalPackageVersion(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return 0;
            }

            lock (globalInvalidationSync)
            {
                int version;
                return globalPackageVersions.TryGetValue(packageName.Trim(), out version) ? version : 0;
            }
        }

        private static bool TryDecodeIndexEntries(
            string packageName,
            string pckPath,
            string pkxPath,
            out Dictionary<string, PckFileEntry> entries,
            out List<string> orderedEntries,
            out Dictionary<string, int> entryPositions,
            out Dictionary<string, List<string>> entriesByExtension,
            out Dictionary<string, List<string>> entriesByFileName,
            out string error)
        {
            entries = new Dictionary<string, PckFileEntry>(StringComparer.OrdinalIgnoreCase);
            orderedEntries = new List<string>();
            entryPositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            entriesByExtension = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            entriesByFileName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            error = string.Empty;

            try
            {
                using (PckConcatStream stream = new PckConcatStream(pckPath, pkxPath))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    long length = stream.Length;
                    if (length < FooterSize + 8)
                    {
                        error = "Package is too short.";
                        return false;
                    }

                    if (PackageHasGameHeader(stream, br, length))
                    {
                        return TryDecodeGameIndexEntries(
                            packageName,
                            pckPath,
                            pkxPath,
                            entries,
                            orderedEntries,
                            entryPositions,
                            entriesByExtension,
                            entriesByFileName,
                            out error);
                    }

                    uint entryCount;
                    long tableOffset;
                    if (!TryReadFooter(stream, br, length, out tableOffset, out entryCount))
                    {
                        return TryDecodeGameIndexEntries(
                            packageName,
                            pckPath,
                            pkxPath,
                            entries,
                            orderedEntries,
                            entryPositions,
                            entriesByExtension,
                            entriesByFileName,
                            out error);
                    }

                    Encoding enc = Encoding.GetEncoding("GBK");
                    stream.Seek(tableOffset, SeekOrigin.Begin);
                    for (uint i = 0; i < entryCount; i++)
                    {
                        int entrySize;
                        if (!TryReadEntrySize(br, length, out entrySize))
                        {
                            break;
                        }

                        byte[] entryData = br.ReadBytes(entrySize);
                        if (entryData.Length != entrySize)
                        {
                            break;
                        }

                        byte[] raw = entrySize == FooterSize ? entryData : TryInflateEntry(entryData);
                        if (raw == null || raw.Length < MinEntrySize)
                        {
                            continue;
                        }

                        string path = DecodeEntryPath(enc, raw);
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }

                        uint rawOffset = BitConverter.ToUInt32(raw, PathBytes + 0);
                        uint rawCompressedSize = BitConverter.ToUInt32(raw, PathBytes + 4);
                        if (rawCompressedSize == 0)
                        {
                            continue;
                        }

                        long offset = rawOffset;
                        long end = offset + rawCompressedSize;
                        if (offset < 0 || end > length)
                        {
                            continue;
                        }

                        string normalized = NormalizeLookupKey(path);
                        PckFileEntry value = new PckFileEntry
                        {
                            Offset = offset,
                            CompressedSize = (int)rawCompressedSize,
                            OriginalSize = (int)rawCompressedSize,
                            CanonicalPath = normalized
                        };
                        entries[normalized] = value;
                        if (!entryPositions.ContainsKey(normalized))
                        {
                            entryPositions[normalized] = orderedEntries.Count;
                            orderedEntries.Add(normalized);
                        }

                        string normalizedExtension = NormalizeExtension(Path.GetExtension(normalized));
                        if (!string.IsNullOrWhiteSpace(normalizedExtension))
                        {
                            if (!entriesByExtension.TryGetValue(normalizedExtension, out List<string> extensionEntries))
                            {
                                extensionEntries = new List<string>();
                                entriesByExtension[normalizedExtension] = extensionEntries;
                            }

                            if (!extensionEntries.Contains(normalized))
                            {
                                extensionEntries.Add(normalized);
                            }
                        }

                        string fileName = NormalizeLookupKey(Path.GetFileName(normalized) ?? string.Empty);
                        if (!string.IsNullOrWhiteSpace(fileName))
                        {
                            if (!entriesByFileName.TryGetValue(fileName, out List<string> fileEntries))
                            {
                                fileEntries = new List<string>();
                                entriesByFileName[fileName] = fileEntries;
                            }

                            if (!fileEntries.Contains(normalized))
                            {
                                fileEntries.Add(normalized);
                            }
                        }

                        string packagePrefix = packageName.Trim().ToLowerInvariant() + "\\";
                        if (normalized.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            string trimmed = normalized.Substring(packagePrefix.Length);
                            if (!entries.ContainsKey(trimmed))
                            {
                                entries[trimmed] = value;
                            }
                        }
                    }
                }

                if (entries.Count == 0)
                {
                    return TryDecodeGameIndexEntries(
                        packageName,
                        pckPath,
                        pkxPath,
                        entries,
                        orderedEntries,
                        entryPositions,
                        entriesByExtension,
                        entriesByFileName,
                        out error);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string NormalizeLookupKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalized = value.Replace('/', '\\').Trim().TrimStart('\\');
            while (normalized.Contains("\\\\"))
            {
                normalized = normalized.Replace("\\\\", "\\");
            }

            return normalized.ToLowerInvariant();
        }

        private static bool TryReadFooter(Stream stream, BinaryReader br, long length, out long tableOffset, out uint entryCount)
        {
            tableOffset = 0;
            entryCount = 0;
            if (stream == null || br == null || length < FooterSize + 8)
            {
                return false;
            }

            stream.Seek(length - FooterSize, SeekOrigin.Begin);
            uint rawOffset;
            if (!TryReadUInt32(br, out rawOffset))
            {
                return false;
            }

            tableOffset = rawOffset ^ Key1;
            if (tableOffset < 0 || tableOffset >= length)
            {
                return false;
            }

            stream.Seek(length - 8, SeekOrigin.Begin);
            if (!TryReadUInt32(br, out entryCount))
            {
                return false;
            }

            if (entryCount == 0 || entryCount > 1000000)
            {
                return false;
            }

            return true;
        }

        private static bool PackageHasGameHeader(Stream stream, BinaryReader br, long length)
        {
            if (stream == null || br == null || length < FooterSize + 8)
            {
                return false;
            }

            long originalPosition = stream.Position;
            try
            {
                stream.Seek(length - (FooterSize + 8), SeekOrigin.Begin);
                byte[] header = br.ReadBytes(FooterSize);
                PckGameAlgorithm algorithm;
                return TryResolveGameAlgorithm(header, out algorithm);
            }
            catch
            {
                return false;
            }
            finally
            {
                try
                {
                    stream.Seek(originalPosition, SeekOrigin.Begin);
                }
                catch
                {
                }
            }
        }

        private static bool TryDecodeGameIndexEntries(
            string packageName,
            string pckPath,
            string pkxPath,
            Dictionary<string, PckFileEntry> entries,
            List<string> orderedEntries,
            Dictionary<string, int> entryPositions,
            Dictionary<string, List<string>> entriesByExtension,
            Dictionary<string, List<string>> entriesByFileName,
            out string error)
        {
            error = string.Empty;
            if (entries == null || orderedEntries == null || entryPositions == null)
            {
                error = "Invalid package index target.";
                return false;
            }

            try
            {
                using (PckConcatStream stream = new PckConcatStream(pckPath, pkxPath))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    long length = stream.Length;
                    if (length < FooterSize + 8)
                    {
                        error = "Package is too short.";
                        return false;
                    }

                    stream.Seek(length - 4, SeekOrigin.Begin);
                    uint version = br.ReadUInt32();
                    if (version != 0x00020001 && version != 0x00020002)
                    {
                        error = "Unsupported package index footer.";
                        return false;
                    }

                    stream.Seek(length - 8, SeekOrigin.Begin);
                    int entryCount = br.ReadInt32();
                    if (entryCount <= 0 || entryCount > 1000000)
                    {
                        error = "Invalid package entry count.";
                        return false;
                    }

                    stream.Seek(length - (FooterSize + 8), SeekOrigin.Begin);
                    byte[] header = br.ReadBytes(FooterSize);
                    if (header == null || header.Length != FooterSize)
                    {
                        error = "Failed to read package header.";
                        return false;
                    }

                    PckGameAlgorithm algorithm;
                    if (!TryResolveGameAlgorithm(header, out algorithm))
                    {
                        error = "Failed to resolve package algorithm.";
                        return false;
                    }

                    uint rawEntryOffset = BitConverter.ToUInt32(header, 8);
                    uint flags = BitConverter.ToUInt32(header, 12);
                    long tableOffset = rawEntryOffset ^ algorithm.MaskDword;
                    bool encrypted = (flags & PackFlagEncrypt) != 0;
                    if (tableOffset < 0 || tableOffset >= length)
                    {
                        error = "Invalid package entry offset.";
                        return false;
                    }

                    Encoding enc = Encoding.GetEncoding("GBK");
                    stream.Seek(tableOffset, SeekOrigin.Begin);
                    for (int i = 0; i < entryCount; i++)
                    {
                        int compressedEntrySize;
                        if (!TryReadGameEntrySize(br, algorithm, length, out compressedEntrySize))
                        {
                            break;
                        }

                        byte[] entryData = br.ReadBytes(compressedEntrySize);
                        if (entryData == null || entryData.Length != compressedEntrySize)
                        {
                            break;
                        }

                        byte[] raw = compressedEntrySize == GameFileEntrySize || compressedEntrySize == MinEntrySize
                            ? entryData
                            : TryInflateEntry(entryData);
                        if (raw == null || raw.Length < MinEntrySize)
                        {
                            continue;
                        }

                        string decodedPath = DecodeEntryPath(enc, raw);
                        string normalized = NormalizeLookupKey(decodedPath);
                        if (string.IsNullOrWhiteSpace(normalized))
                        {
                            continue;
                        }

                        uint rawOffset = BitConverter.ToUInt32(raw, PathBytes + 0);
                        uint rawLength = BitConverter.ToUInt32(raw, PathBytes + 4);
                        uint rawCompressedSize = BitConverter.ToUInt32(raw, PathBytes + 8);
                        if (rawLength == 0 || rawCompressedSize == 0)
                        {
                            continue;
                        }

                        long offset = rawOffset;
                        long end = offset + rawCompressedSize;
                        if (offset < 0 || end > length)
                        {
                            continue;
                        }

                        PckFileEntry value = new PckFileEntry
                        {
                            Offset = offset,
                            CompressedSize = (int)rawCompressedSize,
                            OriginalSize = (int)rawLength,
                            Encrypted = encrypted,
                            CanonicalPath = normalized
                        };

                        AddDecodedEntry(packageName, normalized, value, entries, orderedEntries, entryPositions, entriesByExtension, entriesByFileName);
                    }
                }

                if (entries.Count == 0)
                {
                    error = "No package index entries decoded.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void AddDecodedEntry(
            string packageName,
            string normalized,
            PckFileEntry value,
            Dictionary<string, PckFileEntry> entries,
            List<string> orderedEntries,
            Dictionary<string, int> entryPositions,
            Dictionary<string, List<string>> entriesByExtension,
            Dictionary<string, List<string>> entriesByFileName)
        {
            entries[normalized] = value;
            if (!entryPositions.ContainsKey(normalized))
            {
                entryPositions[normalized] = orderedEntries.Count;
                orderedEntries.Add(normalized);
            }
            AddEntryToLookupTables(normalized, entriesByExtension, entriesByFileName);

            string packagePrefix = packageName.Trim().ToLowerInvariant() + "\\";
            if (normalized.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase))
            {
                string trimmed = normalized.Substring(packagePrefix.Length);
                if (!entries.ContainsKey(trimmed))
                {
                    entries[trimmed] = value;
                }
                if (!entryPositions.ContainsKey(trimmed))
                {
                    entryPositions[trimmed] = orderedEntries.Count;
                    orderedEntries.Add(trimmed);
                }
                AddEntryToLookupTables(trimmed, entriesByExtension, entriesByFileName);
            }
        }

        private static bool TryResolveGameAlgorithm(byte[] header, out PckGameAlgorithm algorithm)
        {
            algorithm = default(PckGameAlgorithm);
            if (header == null || header.Length < FooterSize)
            {
                return false;
            }

            uint guard0 = BitConverter.ToUInt32(header, 0);
            uint guard1 = BitConverter.ToUInt32(header, 268);
            int[] algorithmIds = new int[] { 0, 1, 131 };
            for (int i = 0; i < algorithmIds.Length; i++)
            {
                PckGameAlgorithm candidate = BuildGameAlgorithm(algorithmIds[i]);
                if (guard0 == candidate.GuardByte0 && guard1 == candidate.GuardByte1)
                {
                    algorithm = candidate;
                    return true;
                }
            }

            return false;
        }

        private static PckGameAlgorithm BuildGameAlgorithm(int id)
        {
            if (id == 1)
            {
                return new PckGameAlgorithm(0xab12908f, 0xb3231902, 0x2a63810e, 0x18734563);
            }

            if (id == 0)
            {
                return new PckGameAlgorithm(0xfdfdfeee, 0xf00dbeef, 0xa8937462, 0x59374231);
            }

            return new PckGameAlgorithm(
                unchecked(0xfdfdfeeeu + (uint)id * 0x072341f2u),
                unchecked(0xf00dbeefu + (uint)id * 0x01237a73u),
                unchecked(0xa8937462u + (uint)id * 0x0ab2321fu),
                unchecked(0x59374231u + (uint)id * 0x0987a223u));
        }

        private static bool TryReadGameEntrySize(BinaryReader br, PckGameAlgorithm algorithm, long length, out int entrySize)
        {
            entrySize = 0;
            if (br == null || br.BaseStream == null || br.BaseStream.Position + 8 > length)
            {
                return false;
            }

            uint sizeX1;
            uint sizeX2;
            if (!TryReadUInt32(br, out sizeX1) || !TryReadUInt32(br, out sizeX2))
            {
                return false;
            }

            uint sizeA = sizeX1 ^ algorithm.MaskDword;
            uint sizeB = sizeX2 ^ algorithm.CheckMask ^ algorithm.MaskDword;
            if (sizeA != sizeB || sizeA == 0 || sizeA > MaxEntrySize)
            {
                return false;
            }

            entrySize = (int)sizeA;
            return true;
        }

        private static bool TryReadEntrySize(BinaryReader br, long length, out int entrySize)
        {
            entrySize = 0;
            if (br == null || br.BaseStream == null || br.BaseStream.Position + 8 > length)
            {
                return false;
            }

            uint sizeX1;
            uint sizeX2;
            if (!TryReadUInt32(br, out sizeX1) || !TryReadUInt32(br, out sizeX2))
            {
                return false;
            }

            uint sizeA = sizeX1 ^ Key1;
            uint sizeB = sizeX2 ^ Key2;
            uint resolvedSize = sizeA == sizeB ? sizeA : 0;
            if (resolvedSize == 0)
            {
                if (sizeB > 0 && sizeB <= MaxEntrySize)
                {
                    resolvedSize = sizeB;
                }
                else if (sizeA > 0 && sizeA <= MaxEntrySize)
                {
                    resolvedSize = sizeA;
                }
                else
                {
                    return false;
                }
            }

            if (resolvedSize == 0 || resolvedSize > MaxEntrySize)
            {
                return false;
            }

            entrySize = (int)resolvedSize;
            return true;
        }

        private static string DecodeEntryPath(Encoding encoding, byte[] raw)
        {
            if (encoding == null || raw == null || raw.Length < PathBytes)
            {
                return string.Empty;
            }

            int len = 0;
            while (len < PathBytes && raw[len] != 0)
            {
                len++;
            }
            if (len <= 0)
            {
                return string.Empty;
            }

            string path = encoding.GetString(raw, 0, len).Replace('/', '\\').Trim();
            return path;
        }

        private static byte[] TryInflateEntry(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            byte[] zlib = TryInflateZlib(data);
            if (zlib != null)
            {
                return zlib;
            }

            bool hasZlibHeader = data.Length >= 2 && IsZlibHeader(data[0], data[1]);
            if (hasZlibHeader)
            {
                int index = 2;
                if ((data[1] & 0x20) != 0)
                {
                    index += 4;
                }
                int len = data.Length - index - 4;
                if (len > 0)
                {
                    byte[] deflated = TryInflateDeflate(data, index, len);
                    if (deflated != null)
                    {
                        return deflated;
                    }
                }
            }
            else
            {
                byte[] deflated = TryInflateDeflate(data, 0, data.Length);
                if (deflated != null)
                {
                    return deflated;
                }
            }

            return null;
        }

        private static bool IsZlibHeader(byte cmf, byte flg)
        {
            if ((cmf & 0x0F) != 8)
            {
                return false;
            }
            int header = (cmf << 8) | flg;
            return (header % 31) == 0;
        }

        private static byte[] TryInflateZlib(byte[] data)
        {
            try
            {
                using (MemoryStream input = new MemoryStream(data))
                using (Ionic.Zlib.ZlibStream zlib = new Ionic.Zlib.ZlibStream(input, Ionic.Zlib.CompressionMode.Decompress))
                using (MemoryStream output = new MemoryStream())
                {
                    zlib.CopyTo(output);
                    return output.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        private static byte[] TryInflateDeflate(byte[] data, int index, int len)
        {
            try
            {
                using (MemoryStream input = new MemoryStream(data, index, len))
                using (Ionic.Zlib.DeflateStream deflate = new Ionic.Zlib.DeflateStream(input, Ionic.Zlib.CompressionMode.Decompress))
                using (MemoryStream output = new MemoryStream())
                {
                    deflate.CopyTo(output);
                    return output.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        private static void DecryptGamePayload(byte[] buffer, int length)
        {
            if (buffer == null || length <= 0)
            {
                return;
            }

            uint mask = unchecked((uint)(length + 0x739802ab));
            for (int i = 0; i + 3 < length; i += 4)
            {
                uint data = ((uint)buffer[i] << 24)
                    | ((uint)buffer[i + 1] << 16)
                    | ((uint)buffer[i + 2] << 8)
                    | buffer[i + 3];
                data = (data << 16) | (data >> 16);
                data ^= mask;
                buffer[i] = (byte)((data >> 24) & 0xff);
                buffer[i + 1] = (byte)((data >> 16) & 0xff);
                buffer[i + 2] = (byte)((data >> 8) & 0xff);
                buffer[i + 3] = (byte)(data & 0xff);
            }
        }

        private static bool TryReadUInt32(BinaryReader br, out uint value)
        {
            value = 0;
            if (br == null)
            {
                return false;
            }

            byte[] data = br.ReadBytes(4);
            if (data == null || data.Length < 4)
            {
                return false;
            }

            value = BitConverter.ToUInt32(data, 0);
            return true;
        }

        private sealed class PckPackageIndex
        {
            public string Signature { get; set; } = string.Empty;
            public Dictionary<string, PckFileEntry> Entries { get; set; } = new Dictionary<string, PckFileEntry>(StringComparer.OrdinalIgnoreCase);
            public List<string> OrderedEntries { get; set; } = new List<string>();
            public Dictionary<string, int> EntryPositions { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, List<string>> EntriesByExtension { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, List<string>> EntriesByFileName { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class PckFileEntry
        {
            public long Offset { get; set; }
            public int CompressedSize { get; set; }
            public int OriginalSize { get; set; }
            public bool Encrypted { get; set; }
            public string CanonicalPath { get; set; } = string.Empty;
        }

        private struct PckGameAlgorithm
        {
            public readonly uint GuardByte0;
            public readonly uint GuardByte1;
            public readonly uint MaskDword;
            public readonly uint CheckMask;

            public PckGameAlgorithm(uint guardByte0, uint guardByte1, uint maskDword, uint checkMask)
            {
                GuardByte0 = guardByte0;
                GuardByte1 = guardByte1;
                MaskDword = maskDword;
                CheckMask = checkMask;
            }
        }

        private sealed class PckConcatStream : Stream
        {
            private readonly FileStream pck;
            private readonly FileStream pkx;
            private readonly long pckLength;
            private readonly long pkxLength;
            private long position;

            public PckConcatStream(string pckPath, string pkxPath)
            {
                pck = new FileStream(pckPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                pckLength = pck.Length;

                if (!string.IsNullOrWhiteSpace(pkxPath) && File.Exists(pkxPath))
                {
                    pkx = new FileStream(pkxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    pkxLength = pkx.Length;
                }
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return true; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override long Length
            {
                get { return pckLength + pkxLength; }
            }

            public override long Position
            {
                get { return position; }
                set { Seek(value, SeekOrigin.Begin); }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (buffer == null || count <= 0)
                {
                    return 0;
                }

                long length = Length;
                if (position >= length)
                {
                    return 0;
                }

                int totalRead = 0;
                if (position < pckLength)
                {
                    pck.Position = position;
                    int toRead = (int)Math.Min(count, pckLength - position);
                    int read = pck.Read(buffer, offset, toRead);
                    totalRead += read;
                    position += read;
                    offset += read;
                    count -= read;
                }

                if (count > 0 && pkx != null && position < length)
                {
                    pkx.Position = position - pckLength;
                    int read = pkx.Read(buffer, offset, count);
                    totalRead += read;
                    position += read;
                }

                return totalRead;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long newPos = position;
                switch (origin)
                {
                    case SeekOrigin.Begin:
                        newPos = offset;
                        break;
                    case SeekOrigin.Current:
                        newPos = position + offset;
                        break;
                    case SeekOrigin.End:
                        newPos = Length + offset;
                        break;
                }

                if (newPos < 0)
                {
                    newPos = 0;
                }
                if (newPos > Length)
                {
                    newPos = Length;
                }

                position = newPos;
                return position;
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    pck.Dispose();
                    if (pkx != null)
                    {
                        pkx.Dispose();
                    }
                }
                base.Dispose(disposing);
            }
        }
    }
}
