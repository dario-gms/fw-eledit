using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FWEledit
{
    public sealed class ClientResourceMapService
    {
        private const int CurrentFormatVersion = 2;
        private const string MapFileName = "client-map-v2.json.gz";
        private static readonly object MemoryCacheSync = new object();
        private static readonly Dictionary<string, ClientResourceMap> MemoryCache =
            new Dictionary<string, ClientResourceMap>(StringComparer.OrdinalIgnoreCase);

        public ClientResourceMap LoadOrBuild(string gameRoot, string workspaceRoot, PckEntryReaderService pckReader)
        {
            if (string.IsNullOrWhiteSpace(gameRoot) || !Directory.Exists(gameRoot))
            {
                return null;
            }

            try
            {
                Dictionary<string, ClientResourceFileSignature> signatures = BuildSignatures(gameRoot, workspaceRoot);
                string clientId = BuildClientId(gameRoot);
                string canonicalRoot = GetCanonicalRoot(gameRoot);
                string mapPath = GetMapPath(gameRoot);
                ClientResourceMap loaded;
                if (TryLoadFromMemory(mapPath, clientId, canonicalRoot, signatures, out loaded))
                {
                    return loaded;
                }

                if (TryLoad(mapPath, clientId, canonicalRoot, signatures, out loaded))
                {
                    StoreInMemory(mapPath, loaded);
                    return loaded;
                }

                ClientResourceMap built = Build(gameRoot, workspaceRoot, clientId, canonicalRoot, signatures, pckReader);
                Save(mapPath, built);
                StoreInMemory(mapPath, built);
                return built;
            }
            catch
            {
                return null;
            }
        }

        public bool HasValidMap(string gameRoot, string workspaceRoot)
        {
            if (string.IsNullOrWhiteSpace(gameRoot) || !Directory.Exists(gameRoot))
            {
                return false;
            }

            try
            {
                Dictionary<string, ClientResourceFileSignature> signatures = BuildSignatures(gameRoot, workspaceRoot);
                string clientId = BuildClientId(gameRoot);
                string canonicalRoot = GetCanonicalRoot(gameRoot);
                string mapPath = GetMapPath(gameRoot);
                ClientResourceMap loaded;
                if (TryLoadFromMemory(mapPath, clientId, canonicalRoot, signatures, out loaded))
                {
                    return true;
                }

                if (TryLoad(mapPath, clientId, canonicalRoot, signatures, out loaded))
                {
                    StoreInMemory(mapPath, loaded);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public void Invalidate(string gameRoot)
        {
            try
            {
                string mapPath = GetMapPath(gameRoot);
                if (!string.IsNullOrWhiteSpace(mapPath))
                {
                    lock (MemoryCacheSync)
                    {
                        MemoryCache.Remove(mapPath);
                    }
                }

                if (!string.IsNullOrWhiteSpace(mapPath) && File.Exists(mapPath))
                {
                    File.Delete(mapPath);
                }
            }
            catch
            {
            }
        }

        private static bool TryLoadFromMemory(
            string mapPath,
            string clientId,
            string canonicalRoot,
            Dictionary<string, ClientResourceFileSignature> currentSignatures,
            out ClientResourceMap map)
        {
            map = null;
            if (string.IsNullOrWhiteSpace(mapPath))
            {
                return false;
            }

            lock (MemoryCacheSync)
            {
                ClientResourceMap cached;
                if (!MemoryCache.TryGetValue(mapPath, out cached)
                    || cached == null
                    || cached.FormatVersion != CurrentFormatVersion
                    || !string.Equals(cached.ClientId ?? string.Empty, clientId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(cached.CanonicalClientRoot ?? string.Empty, canonicalRoot ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                    || !SignaturesMatch(cached.Files, currentSignatures))
                {
                    return false;
                }

                map = cached;
                return true;
            }
        }

        private static void StoreInMemory(string mapPath, ClientResourceMap map)
        {
            if (string.IsNullOrWhiteSpace(mapPath) || map == null)
            {
                return;
            }

            lock (MemoryCacheSync)
            {
                MemoryCache[mapPath] = map;
            }
        }

        public static string GetMapRoot()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FWEledit",
                "client-maps");
        }

        private static string GetMapPath(string gameRoot)
        {
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return string.Empty;
            }

            return Path.Combine(GetMapRoot(), BuildClientId(gameRoot), MapFileName);
        }

        private static string BuildClientId(string gameRoot)
        {
            string normalized = GetCanonicalRoot(gameRoot).ToLowerInvariant();
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        private static string GetCanonicalRoot(string gameRoot)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gameRoot))
                {
                    return string.Empty;
                }

                return Path.GetFullPath(gameRoot.Trim())
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return (gameRoot ?? string.Empty).Trim();
            }
        }

        private static bool TryLoad(
            string mapPath,
            string clientId,
            string canonicalRoot,
            Dictionary<string, ClientResourceFileSignature> currentSignatures,
            out ClientResourceMap map)
        {
            map = null;
            if (string.IsNullOrWhiteSpace(mapPath) || !File.Exists(mapPath))
            {
                return false;
            }

            try
            {
                using (FileStream fs = File.OpenRead(mapPath))
                using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(gzip, Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    ClientResourceMap loaded = JsonConvert.DeserializeObject<ClientResourceMap>(json);
                    if (loaded == null || loaded.FormatVersion != CurrentFormatVersion)
                    {
                        return false;
                    }

                    if (!string.Equals(loaded.ClientId ?? string.Empty, clientId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(loaded.CanonicalClientRoot ?? string.Empty, canonicalRoot ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (!SignaturesMatch(loaded.Files, currentSignatures))
                    {
                        return false;
                    }

                    map = loaded;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void Save(string mapPath, ClientResourceMap map)
        {
            if (string.IsNullOrWhiteSpace(mapPath) || map == null)
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(mapPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string tempPath = mapPath + ".tmp";
                string json = JsonConvert.SerializeObject(map, Formatting.None);
                using (FileStream fs = File.Create(tempPath))
                using (GZipStream gzip = new GZipStream(fs, CompressionMode.Compress))
                using (StreamWriter writer = new StreamWriter(gzip, Encoding.UTF8))
                {
                    writer.Write(json);
                }

                if (File.Exists(mapPath))
                {
                    File.Delete(mapPath);
                }
                File.Move(tempPath, mapPath);
            }
            catch
            {
            }
        }

        private static ClientResourceMap Build(
            string gameRoot,
            string workspaceRoot,
            string clientId,
            string canonicalRoot,
            Dictionary<string, ClientResourceFileSignature> signatures,
            PckEntryReaderService pckReader)
        {
            ClientResourceMap map = new ClientResourceMap
            {
                FormatVersion = CurrentFormatVersion,
                AppVersion = typeof(ClientResourceMapService).Assembly.GetName().Version.ToString(),
                ClientId = clientId,
                ClientRoot = gameRoot,
                CanonicalClientRoot = canonicalRoot,
                BuiltUtc = DateTime.UtcNow,
                Files = signatures ?? new Dictionary<string, ClientResourceFileSignature>(StringComparer.OrdinalIgnoreCase),
                PathById = LoadPathById(gameRoot, workspaceRoot),
                Packages = BuildPackageSummaries(signatures),
                TextResources = new Dictionary<string, ClientResourcePayload>(StringComparer.OrdinalIgnoreCase)
            };

            if (pckReader != null)
            {
                TryLoadItemDescriptionPayload(pckReader, map);
                TryLoadTextResourcePayloads(pckReader, map);
                TryLoadIconsetPayload(pckReader, map);
            }

            return map;
        }

        private static List<ClientResourcePackageSummary> BuildPackageSummaries(
            Dictionary<string, ClientResourceFileSignature> signatures)
        {
            List<ClientResourcePackageSummary> packages = new List<ClientResourcePackageSummary>();
            if (signatures == null)
            {
                return packages;
            }

            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string key in signatures.Keys)
            {
                if (string.IsNullOrWhiteSpace(key) || !key.StartsWith("resources/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string fileName = Path.GetFileName(key.Replace('/', Path.DirectorySeparatorChar));
                string extension = Path.GetExtension(fileName);
                if (!string.Equals(extension, ".pck", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(extension, ".pkx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string packageName = Path.GetFileNameWithoutExtension(fileName);
                if (!string.IsNullOrWhiteSpace(packageName))
                {
                    names.Add(packageName);
                }
            }

            foreach (string name in names.OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
            {
                ClientResourceFileSignature pck;
                ClientResourceFileSignature pkx;
                signatures.TryGetValue("resources/" + name + ".pck", out pck);
                signatures.TryGetValue("resources/" + name + ".pkx", out pkx);
                packages.Add(new ClientResourcePackageSummary
                {
                    Name = name,
                    Pck = pck,
                    Pkx = pkx
                });
            }

            return packages;
        }

        private static void TryLoadItemDescriptionPayload(PckEntryReaderService pckReader, ClientResourceMap map)
        {
            string[] candidates =
            {
                "item_ext_desc.txt",
                Path.Combine("data", "item_ext_desc.txt"),
                Path.Combine("configs", "item_ext_desc.txt")
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                byte[] payload;
                string resolvedRelativePath;
                string error;
                if (pckReader.TryReadFileFast("configs", candidates[i], out payload, out resolvedRelativePath, out error)
                    && payload != null
                    && payload.Length > 0)
                {
                    map.ItemExtDescription = new ClientResourcePayload
                    {
                        Package = "configs",
                        RelativePath = string.IsNullOrWhiteSpace(resolvedRelativePath) ? candidates[i] : resolvedRelativePath,
                        Payload = payload
                    };
                    return;
                }
            }
        }

        private static void TryLoadTextResourcePayloads(PckEntryReaderService pckReader, ClientResourceMap map)
        {
            if (pckReader == null || map == null || map.TextResources == null)
            {
                return;
            }

            string[] resourceNames =
            {
                "skillstr.txt",
                "buff_str.txt",
                "language_en.txt",
                "addon_table.txt",
                "addon_table_en.txt",
                "addon_table_pt.txt",
                "item_ext_prop.txt",
                "item_color.txt",
                "theme.txt"
            };

            for (int i = 0; i < resourceNames.Length; i++)
            {
                string name = resourceNames[i];
                ClientResourcePayload payload;
                if (TryReadConfigTextPayload(pckReader, name, out payload))
                {
                    map.TextResources[name] = payload;
                }
            }
        }

        private static bool TryReadConfigTextPayload(
            PckEntryReaderService pckReader,
            string fileName,
            out ClientResourcePayload result)
        {
            result = null;
            if (pckReader == null || string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            string[] candidates =
            {
                fileName,
                Path.Combine("data", fileName),
                Path.Combine("configs", fileName)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                byte[] payload;
                string resolvedRelativePath;
                string error;
                if (pckReader.TryReadFileFast("configs", candidates[i], out payload, out resolvedRelativePath, out error)
                    && payload != null
                    && payload.Length > 0)
                {
                    result = new ClientResourcePayload
                    {
                        Package = "configs",
                        RelativePath = string.IsNullOrWhiteSpace(resolvedRelativePath) ? candidates[i] : resolvedRelativePath,
                        Payload = payload
                    };
                    return true;
                }
            }

            return false;
        }

        private static void TryLoadIconsetPayload(PckEntryReaderService pckReader, ClientResourceMap map)
        {
            if (pckReader == null || map == null)
            {
                return;
            }

            string[] variants =
            {
                "iconlist_ivtr0",
                "iconlist_ivtrm"
            };
            string[] roots =
            {
                Path.Combine("surfaces", "iconset"),
                "iconset"
            };
            string[] imageExtensions =
            {
                ".dds",
                ".png"
            };

            for (int r = 0; r < roots.Length; r++)
            {
                for (int v = 0; v < variants.Length; v++)
                {
                    ClientResourcePayload textPayload;
                    if (!TryReadPackagePayload(pckReader, "surfaces", Path.Combine(roots[r], variants[v] + ".txt"), out textPayload))
                    {
                        continue;
                    }

                    for (int e = 0; e < imageExtensions.Length; e++)
                    {
                        ClientResourcePayload imagePayload;
                        if (TryReadPackagePayload(pckReader, "surfaces", Path.Combine(roots[r], variants[v] + imageExtensions[e]), out imagePayload))
                        {
                            map.Iconset = new ClientResourceIconset
                            {
                                Text = textPayload,
                                Image = imagePayload
                            };
                            return;
                        }
                    }
                }
            }
        }

        private static bool TryReadPackagePayload(
            PckEntryReaderService pckReader,
            string packageName,
            string relativePath,
            out ClientResourcePayload result)
        {
            result = null;
            if (pckReader == null || string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(relativePath))
            {
                return false;
            }

            byte[] payload;
            string resolvedRelativePath;
            string error;
            if (pckReader.TryReadFileFast(packageName, relativePath, out payload, out resolvedRelativePath, out error)
                && payload != null
                && payload.Length > 0)
            {
                result = new ClientResourcePayload
                {
                    Package = packageName,
                    RelativePath = string.IsNullOrWhiteSpace(resolvedRelativePath) ? relativePath : resolvedRelativePath,
                    Payload = payload
                };
                return true;
            }

            return false;
        }

        private static Dictionary<int, string> LoadPathById(string gameRoot, string workspaceRoot)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            string pathDataFile = ResolvePathDataFile(gameRoot, workspaceRoot);
            if (string.IsNullOrWhiteSpace(pathDataFile) || !File.Exists(pathDataFile))
            {
                return result;
            }

            try
            {
                Encoding enc = Encoding.GetEncoding("GBK");
                using (FileStream fs = File.OpenRead(pathDataFile))
                using (BinaryReader br = new BinaryReader(fs, enc))
                {
                    if (br.BaseStream.Length < 8)
                    {
                        return result;
                    }

                    string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                    if (!string.Equals(magic, "DIMP", StringComparison.Ordinal))
                    {
                        return result;
                    }

                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        if (br.BaseStream.Position + 8 > br.BaseStream.Length)
                        {
                            break;
                        }

                        int id = br.ReadInt32();
                        int len = br.ReadInt32();
                        if (id < 0 || len < 0 || len > 8192 || br.BaseStream.Position + len > br.BaseStream.Length)
                        {
                            break;
                        }

                        string mappedPath = enc.GetString(br.ReadBytes(len)).Replace('/', '\\');
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

        private static string ResolvePathDataFile(string gameRoot, string workspaceRoot)
        {
            if (!string.IsNullOrWhiteSpace(workspaceRoot))
            {
                string workspacePathData = Path.Combine(workspaceRoot, "data", "path.data");
                if (File.Exists(workspacePathData))
                {
                    return workspacePathData;
                }
            }

            if (!string.IsNullOrWhiteSpace(gameRoot))
            {
                string gamePathData = Path.Combine(gameRoot, "data", "path.data");
                if (File.Exists(gamePathData))
                {
                    return gamePathData;
                }

                string legacyPathData = Path.Combine(gameRoot, "fELedit", "resources", "data", "path.data");
                if (File.Exists(legacyPathData))
                {
                    return legacyPathData;
                }
            }

            return string.Empty;
        }

        private static Dictionary<string, ClientResourceFileSignature> BuildSignatures(string gameRoot, string workspaceRoot)
        {
            Dictionary<string, ClientResourceFileSignature> signatures =
                new Dictionary<string, ClientResourceFileSignature>(StringComparer.OrdinalIgnoreCase);

            AddSignature(signatures, "data/elements.data", Path.Combine(gameRoot, "data", "elements.data"));
            AddSignature(signatures, "data/path.data", ResolvePathDataFile(gameRoot, workspaceRoot));

            string resourcesRoot = Path.Combine(gameRoot, "resources");
            if (Directory.Exists(resourcesRoot))
            {
                foreach (string file in Directory.EnumerateFiles(resourcesRoot, "*.*", SearchOption.TopDirectoryOnly))
                {
                    string extension = Path.GetExtension(file);
                    if (!string.Equals(extension, ".pck", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(extension, ".pkx", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AddSignature(signatures, "resources/" + Path.GetFileName(file), file);
                }
            }

            return signatures;
        }

        private static void AddSignature(
            Dictionary<string, ClientResourceFileSignature> signatures,
            string key,
            string path)
        {
            if (signatures == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            ClientResourceFileSignature signature = new ClientResourceFileSignature
            {
                Path = path ?? string.Empty,
                Exists = !string.IsNullOrWhiteSpace(path) && File.Exists(path)
            };

            if (signature.Exists)
            {
                FileInfo info = new FileInfo(path);
                signature.Length = info.Length;
                signature.LastWriteTimeUtcTicks = info.LastWriteTimeUtc.Ticks;
            }

            signatures[key.Replace('\\', '/').ToLowerInvariant()] = signature;
        }

        private static bool SignaturesMatch(
            Dictionary<string, ClientResourceFileSignature> stored,
            Dictionary<string, ClientResourceFileSignature> current)
        {
            if (stored == null || current == null || stored.Count != current.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, ClientResourceFileSignature> pair in current)
            {
                ClientResourceFileSignature previous;
                if (!stored.TryGetValue(pair.Key, out previous) || !ClientResourceFileSignature.Equals(previous, pair.Value))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class ClientResourceMap
    {
        public int FormatVersion { get; set; }
        public string AppVersion { get; set; }
        public string ClientId { get; set; }
        public string ClientRoot { get; set; }
        public string CanonicalClientRoot { get; set; }
        public DateTime BuiltUtc { get; set; }
        public Dictionary<string, ClientResourceFileSignature> Files { get; set; }
        public Dictionary<int, string> PathById { get; set; }
        public ClientResourcePayload ItemExtDescription { get; set; }
        public ClientResourceIconset Iconset { get; set; }
        public Dictionary<string, ClientResourcePayload> TextResources { get; set; }
        public List<ClientResourcePackageSummary> Packages { get; set; }
    }

    public sealed class ClientResourceIconset
    {
        public ClientResourcePayload Text { get; set; }
        public ClientResourcePayload Image { get; set; }
    }

    public sealed class ClientResourcePackageSummary
    {
        public string Name { get; set; }
        public ClientResourceFileSignature Pck { get; set; }
        public ClientResourceFileSignature Pkx { get; set; }
    }

    public sealed class ClientResourcePayload
    {
        public string Package { get; set; }
        public string RelativePath { get; set; }
        public byte[] Payload { get; set; }
    }

    public sealed class ClientResourceFileSignature
    {
        public string Path { get; set; }
        public bool Exists { get; set; }
        public long Length { get; set; }
        public long LastWriteTimeUtcTicks { get; set; }

        public static bool Equals(ClientResourceFileSignature left, ClientResourceFileSignature right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (left == null || right == null)
            {
                return false;
            }

            return left.Exists == right.Exists
                && left.Length == right.Length
                && left.LastWriteTimeUtcTicks == right.LastWriteTimeUtcTicks
                && string.Equals(left.Path ?? string.Empty, right.Path ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
