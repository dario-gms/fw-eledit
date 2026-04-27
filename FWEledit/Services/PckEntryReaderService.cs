using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly object syncRoot = new object();
        private readonly Dictionary<string, PckPackageIndex> packageCache = new Dictionary<string, PckPackageIndex>(StringComparer.OrdinalIgnoreCase);

        public bool TryReadFile(string packageName, string relativePath, out byte[] payload, out string error)
        {
            payload = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(packageName))
            {
                error = "Invalid package name.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                error = "Invalid package entry path.";
                return false;
            }

            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gameRoot) || !Directory.Exists(gameRoot))
            {
                error = "Game root not configured.";
                return false;
            }

            string normalizedPackage = packageName.Trim();
            string normalizedEntry = NormalizeLookupKey(relativePath);
            if (normalizedEntry.StartsWith(normalizedPackage.ToLowerInvariant() + "\\", StringComparison.OrdinalIgnoreCase))
            {
                normalizedEntry = normalizedEntry.Substring(normalizedPackage.Length + 1);
            }

            string resourcesRoot = Path.Combine(gameRoot, "resources");
            string pckPath = Path.Combine(resourcesRoot, normalizedPackage + ".pck");
            string pkxPath = Path.Combine(resourcesRoot, normalizedPackage + ".pkx");
            if (!File.Exists(pckPath))
            {
                error = "Package not found: " + pckPath;
                return false;
            }
            if (!File.Exists(pkxPath))
            {
                pkxPath = string.Empty;
            }

            PckPackageIndex index;
            if (!TryGetPackageIndex(normalizedPackage, pckPath, pkxPath, out index, out error) || index == null)
            {
                return false;
            }

            PckFileEntry entry;
            if (!index.Entries.TryGetValue(normalizedEntry, out entry))
            {
                string fallback = NormalizeLookupKey(normalizedPackage + "\\" + normalizedEntry);
                if (!index.Entries.TryGetValue(fallback, out entry))
                {
                    error = "Entry not found in " + normalizedPackage + ".pck: " + relativePath;
                    return false;
                }
            }

            try
            {
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

                    byte[] inflated = TryInflateEntry(compressed);
                    payload = inflated ?? compressed;
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

            string signature = BuildPackageSignature(pckPath, pkxPath);
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
            if (!TryDecodeIndexEntries(packageName, pckPath, pkxPath, out decodedEntries, out error))
            {
                return false;
            }

            PckPackageIndex built = new PckPackageIndex
            {
                Signature = signature,
                Entries = decodedEntries
            };

            lock (syncRoot)
            {
                packageCache[cacheKey] = built;
            }

            index = built;
            return true;
        }

        private static string BuildPackageSignature(string pckPath, string pkxPath)
        {
            try
            {
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
                    return pckSig + "|" + pkxSig;
                }

                return pckSig + "|none";
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        private static bool TryDecodeIndexEntries(
            string packageName,
            string pckPath,
            string pkxPath,
            out Dictionary<string, PckFileEntry> entries,
            out string error)
        {
            entries = new Dictionary<string, PckFileEntry>(StringComparer.OrdinalIgnoreCase);
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

                    uint entryCount;
                    long tableOffset;
                    if (!TryReadFooter(stream, br, length, out tableOffset, out entryCount))
                    {
                        error = "Failed to decode package footer.";
                        return false;
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

                        PckFileEntry value = new PckFileEntry
                        {
                            Offset = offset,
                            CompressedSize = (int)rawCompressedSize
                        };

                        string normalized = NormalizeLookupKey(path);
                        entries[normalized] = value;

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

        private static string NormalizeLookupKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace('/', '\\').Trim().TrimStart('\\').ToLowerInvariant();
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
        }

        private sealed class PckFileEntry
        {
            public long Offset { get; set; }
            public int CompressedSize { get; set; }
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
                pck = new FileStream(pckPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                pckLength = pck.Length;

                if (!string.IsNullOrWhiteSpace(pkxPath) && File.Exists(pkxPath))
                {
                    pkx = new FileStream(pkxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
