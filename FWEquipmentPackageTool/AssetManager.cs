using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FWEledit
{
    public sealed class AssetManager
    {
        private readonly PckEntryReaderService pckEntryReaderService = new PckEntryReaderService();

        public static string GameRootPath { get; set; }

        public static string WorkspaceRootPath { get; set; }

        public bool TryReadPackageEntry(string packageName, string relativePath, out byte[] payload, out string error)
        {
            payload = null;
            error = string.Empty;

            try
            {
                string resolvedRelativePath;
                if (pckEntryReaderService.TryReadFileFast(packageName, relativePath, out payload, out resolvedRelativePath, out error))
                {
                    return true;
                }

                string pckPath;
                string pkxPath;
                string pathError;
                if (!TryGetPackagePaths(packageName, out pckPath, out pkxPath, out pathError))
                {
                    error = string.IsNullOrWhiteSpace(error) ? pathError : error;
                    return false;
                }

                return PckIndexReader.TryReadEntry(packageName, pckPath, pkxPath, relativePath, out payload, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryEnumeratePckIndexEntries(string packageName, out List<string> entries)
        {
            entries = new List<string>();
            try
            {
                string error;
                return pckEntryReaderService.TryEnumerateEntries(packageName, out entries, out error);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetPackagePaths(string packageName, out string pckPath, out string pkxPath, out string error)
        {
            pckPath = string.Empty;
            pkxPath = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(GameRootPath))
            {
                error = "Invalid package entry request.";
                return false;
            }

            string normalizedPackage = packageName.Trim();
            string resourcesRoot = Path.Combine(GameRootPath, "resources");
            string workspaceResources = string.IsNullOrWhiteSpace(WorkspaceRootPath)
                ? string.Empty
                : Path.Combine(WorkspaceRootPath, "resources");

            pckPath = Path.Combine(resourcesRoot, normalizedPackage + ".pck");
            if (!File.Exists(pckPath) && !string.IsNullOrWhiteSpace(workspaceResources))
            {
                pckPath = Path.Combine(workspaceResources, normalizedPackage + ".pck");
            }
            if (!File.Exists(pckPath))
            {
                error = "Package file not found: " + pckPath;
                return false;
            }

            pkxPath = Path.Combine(resourcesRoot, normalizedPackage + ".pkx");
            if (!File.Exists(pkxPath) && !string.IsNullOrWhiteSpace(workspaceResources))
            {
                pkxPath = Path.Combine(workspaceResources, normalizedPackage + ".pkx");
            }
            if (!File.Exists(pkxPath))
            {
                pkxPath = string.Empty;
            }

            return true;
        }

        private static class PckIndexReader
        {
            private const uint Key1 = 566434367;
            private const uint Key2 = 408690725;
            private const uint AngelicaKey1 = 2828235874;
            private const uint AngelicaKey2 = 4054070867;
            private const int FooterSize = 272;
            private const int PathBytes = 260;
            private const int MinEntrySize = PathBytes + 12;
            private const int MaxEntrySize = 1024 * 1024;

            private sealed class PckKeySet
            {
                public uint EntrySizeKey1 { get; set; }
                public uint EntrySizeKey2 { get; set; }
                public uint FileTableOffsetKey { get; set; }
            }

            private sealed class ExtractablePckEntry
            {
                public long Offset { get; set; }
                public int CompressedSize { get; set; }
            }

            private sealed class CachedPckIndex
            {
                public long LogicalLength { get; set; }
                public Dictionary<string, ExtractablePckEntry> Entries { get; set; }
            }

            private static readonly object IndexCacheLock = new object();
            private static readonly Dictionary<string, CachedPckIndex> IndexCache = new Dictionary<string, CachedPckIndex>(StringComparer.OrdinalIgnoreCase);

            public static bool TryReadEntry(
                string packageName,
                string pckPath,
                string pkxPath,
                string relativePath,
                out byte[] payload,
                out string error)
            {
                payload = null;
                error = string.Empty;

                if (string.IsNullOrWhiteSpace(pckPath) || !File.Exists(pckPath))
                {
                    error = "Package file not found.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    error = "Package entry path not set.";
                    return false;
                }

                try
                {
                    CachedPckIndex index;
                    if (!TryGetIndex(packageName, pckPath, pkxPath, out index, out error))
                    {
                        return false;
                    }

                    string requested = NormalizeLookupKey(relativePath);
                    ExtractablePckEntry match;
                    if (!index.Entries.TryGetValue(requested, out match)
                        && !index.Entries.TryGetValue(NormalizeLookupKey(packageName + "\\" + requested), out match))
                    {
                        error = "Entry not found in " + packageName + ".pck: " + relativePath;
                        return false;
                    }

                    using (PckConcatStream stream = new PckConcatStream(pckPath, pkxPath))
                    using (BinaryReader br = new BinaryReader(stream))
                    {
                        stream.Seek(match.Offset, SeekOrigin.Begin);
                        byte[] compressed = br.ReadBytes(match.CompressedSize);
                        if (compressed.Length != match.CompressedSize)
                        {
                            error = "Failed to read package entry data: " + relativePath;
                            return false;
                        }

                        payload = InflateEntry(compressed) ?? compressed;
                        return payload != null && payload.Length > 0;
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }

            private static bool TryGetIndex(string packageName, string pckPath, string pkxPath, out CachedPckIndex index, out string error)
            {
                index = null;
                error = string.Empty;

                string cacheKey = BuildIndexCacheKey(pckPath, pkxPath);
                lock (IndexCacheLock)
                {
                    if (IndexCache.TryGetValue(cacheKey, out index))
                    {
                        return true;
                    }
                }

                Dictionary<string, ExtractablePckEntry> indexedEntries = new Dictionary<string, ExtractablePckEntry>(StringComparer.OrdinalIgnoreCase);
                using (PckConcatStream stream = new PckConcatStream(pckPath, pkxPath))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    long length = GetPackageLogicalLength(stream, br);
                    if (length < FooterSize + 8)
                    {
                        error = "Package is too short.";
                        return false;
                    }

                    uint entryCount;
                    long tableOffset;
                    PckKeySet keySet;
                    if (!TryReadFooter(stream, br, length, out tableOffset, out entryCount, out keySet))
                    {
                        error = "Failed to decode package footer.";
                        return false;
                    }

                    stream.Seek(tableOffset, SeekOrigin.Begin);
                    Encoding enc = Encoding.GetEncoding("GBK");
                    string packagePrefix = NormalizeLookupKey(packageName) + "\\";

                    for (uint i = 0; i < entryCount; i++)
                    {
                        int entrySize;
                        if (!TryReadEntrySize(br, length, keySet, out entrySize))
                        {
                            break;
                        }

                        byte[] entryData = br.ReadBytes(entrySize);
                        if (entryData.Length != entrySize)
                        {
                            break;
                        }

                        byte[] raw = entrySize == FooterSize ? entryData : InflateEntry(entryData);
                        if (raw == null || raw.Length < MinEntrySize)
                        {
                            continue;
                        }

                        string decodedPath = DecodeEntryPath(enc, raw);
                        string normalizedPath = NormalizeLookupKey(decodedPath);
                        if (string.IsNullOrWhiteSpace(normalizedPath))
                        {
                            continue;
                        }

                        uint rawOffset = BitConverter.ToUInt32(raw, PathBytes + 0);
                        uint rawCompressedSize = BitConverter.ToUInt32(raw, PathBytes + 8);
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

                        ExtractablePckEntry entry = new ExtractablePckEntry
                        {
                            Offset = offset,
                            CompressedSize = (int)rawCompressedSize
                        };

                        indexedEntries[normalizedPath] = entry;
                        if (normalizedPath.StartsWith(packagePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            string trimmed = normalizedPath.Substring(packagePrefix.Length);
                            if (!indexedEntries.ContainsKey(trimmed))
                            {
                                indexedEntries[trimmed] = entry;
                            }
                        }
                    }

                    index = new CachedPckIndex
                    {
                        LogicalLength = length,
                        Entries = indexedEntries
                    };
                }

                lock (IndexCacheLock)
                {
                    IndexCache[cacheKey] = index;
                }

                return true;
            }

            private static string BuildIndexCacheKey(string pckPath, string pkxPath)
            {
                return BuildFileCachePart(pckPath) + "|" + BuildFileCachePart(pkxPath);
            }

            private static string BuildFileCachePart(string path)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return string.Empty;
                }

                FileInfo info = new FileInfo(path);
                return info.FullName + ":" + info.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + ":" + info.LastWriteTimeUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            private static long GetPackageLogicalLength(Stream stream, BinaryReader br)
            {
                long physicalLength = stream != null ? stream.Length : 0;
                if (stream == null || br == null || physicalLength < 12)
                {
                    return physicalLength;
                }

                long previous = stream.Position;
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    uint signature = br.ReadUInt32();
                    uint logicalLength = br.ReadUInt32();
                    uint signature2 = br.ReadUInt32();
                    if (signature == 1305093103 && signature2 == 1453361591 && logicalLength >= 12 && logicalLength <= physicalLength)
                    {
                        return logicalLength;
                    }
                }
                catch
                {
                }
                finally
                {
                    try { stream.Seek(previous, SeekOrigin.Begin); } catch { }
                }

                return physicalLength;
            }

            private static bool TryReadFooter(Stream stream, BinaryReader br, long length, out long tableOffset, out uint entryCount, out PckKeySet keySet)
            {
                tableOffset = 0;
                entryCount = 0;
                keySet = null;
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

                stream.Seek(length - 8, SeekOrigin.Begin);
                if (!TryReadUInt32(br, out entryCount) || entryCount == 0 || entryCount > 1000000)
                {
                    return false;
                }

                PckKeySet[] keySets = new PckKeySet[]
                {
                    new PckKeySet { FileTableOffsetKey = Key1, EntrySizeKey1 = Key1, EntrySizeKey2 = Key2 },
                    new PckKeySet { FileTableOffsetKey = AngelicaKey1, EntrySizeKey1 = AngelicaKey1, EntrySizeKey2 = AngelicaKey2 },
                    new PckKeySet { FileTableOffsetKey = 0, EntrySizeKey1 = 0, EntrySizeKey2 = 0 }
                };

                for (int i = 0; i < keySets.Length; i++)
                {
                    long candidateOffset = rawOffset ^ keySets[i].FileTableOffsetKey;
                    if (candidateOffset >= 12 && candidateOffset < length)
                    {
                        tableOffset = candidateOffset;
                        keySet = keySets[i];
                        return true;
                    }
                }

                return false;
            }

            private static bool TryReadEntrySize(BinaryReader br, long length, PckKeySet keySet, out int entrySize)
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

                uint sizeA = sizeX1 ^ (keySet != null ? keySet.EntrySizeKey1 : Key1);
                uint sizeB = sizeX2 ^ (keySet != null ? keySet.EntrySizeKey2 : Key2);
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

            private static string DecodeEntryPath(Encoding enc, byte[] raw)
            {
                if (raw == null || raw.Length < PathBytes)
                {
                    return string.Empty;
                }

                int len = 0;
                while (len < PathBytes && raw[len] != 0)
                {
                    len++;
                }
                return len <= 0 ? string.Empty : enc.GetString(raw, 0, len).Replace('/', '\\').Trim();
            }

            private static string NormalizeLookupKey(string value)
            {
                return string.IsNullOrWhiteSpace(value)
                    ? string.Empty
                    : value.Replace('/', '\\').Trim().TrimStart('\\').ToLowerInvariant();
            }

            private static byte[] InflateEntry(byte[] data)
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
                        return TryInflateDeflate(data, index, len);
                    }
                }

                return TryInflateDeflate(data, 0, data.Length);
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

                public override bool CanRead { get { return true; } }
                public override bool CanSeek { get { return true; } }
                public override bool CanWrite { get { return false; } }
                public override long Length { get { return pckLength + pkxLength; } }

                public override long Position
                {
                    get { return position; }
                    set { Seek(value, SeekOrigin.Begin); }
                }

                public override void Flush() { }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    if (buffer == null || count <= 0 || position >= Length)
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

                    if (count > 0 && pkx != null && position < Length)
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
}
