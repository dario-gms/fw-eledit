using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FWEledit
{
    public sealed class PckRawCopyImportItem
    {
        public string SourceRelativePath { get; set; }
        public string TargetRelativePath { get; set; }
    }

    public sealed class PckRawCopyImportService
    {
        private const int PathBytes = 260;
        private const int HeaderSize = 272;
        private const int FooterSize = 8;
        private const int EntrySize = 276;
        private const int MinEntrySize = PathBytes + 12;
        private const int MaxEntrySize = 1024 * 1024;
        private const uint SafeHeaderTag1 = 0x4DCA23EF;
        private const uint SafeHeaderTag2 = 0x56A089B7;
        private const uint PackFlagEncrypt = 0x80000000;

        public bool TryCopyEntries(
            string sourcePck,
            string sourcePkx,
            string targetPck,
            string targetPkx,
            IEnumerable<PckRawCopyImportItem> items,
            out int copied,
            out string error)
        {
            copied = 0;
            error = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(sourcePck) || !File.Exists(sourcePck))
                {
                    error = "Source PCK not found.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(targetPck) || !File.Exists(targetPck))
                {
                    error = "Target PCK not found.";
                    return false;
                }

                PackageIndex source = ReadIndex(sourcePck, sourcePkx);
                PackageIndex target = ReadIndex(targetPck, targetPkx);
                if ((source.Flags & PackFlagEncrypt) != (target.Flags & PackFlagEncrypt))
                {
                    error = "Source and target package encryption flags differ.";
                    return false;
                }

                List<Entry> selected = new List<Entry>();
                HashSet<string> selectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                Encoding gbk = Encoding.GetEncoding("GBK");
                foreach (PckRawCopyImportItem item in items ?? new PckRawCopyImportItem[0])
                {
                    string sourcePath = CleanPath(item == null ? null : item.SourceRelativePath);
                    string targetPath = CleanPath(item == null ? null : item.TargetRelativePath);
                    string sourceKey = Normalize(sourcePath);
                    string targetKey = Normalize(targetPath);
                    if (string.IsNullOrWhiteSpace(sourceKey) || string.IsNullOrWhiteSpace(targetKey))
                    {
                        continue;
                    }

                    Entry sourceEntry;
                    if (!source.ByPath.TryGetValue(sourceKey, out sourceEntry))
                    {
                        error = "Source entry not found: " + sourceKey;
                        return false;
                    }

                    if (!selectedKeys.Add(targetKey))
                    {
                        continue;
                    }

                    string entryPath = string.Equals(sourcePath, targetPath, StringComparison.Ordinal)
                        ? sourceEntry.Path
                        : targetKey;
                    if (!string.Equals(sourcePath, targetPath, StringComparison.Ordinal))
                    {
                        entryPath = targetPath;
                    }

                    Entry clone = sourceEntry.CloneForTarget(entryPath, gbk);
                    selected.Add(clone);
                }

                if (selected.Count == 0)
                {
                    error = "No entries selected for raw PCK import.";
                    return false;
                }

                using (ConcatReader sourceReader = new ConcatReader(sourcePck, sourcePkx))
                {
                    foreach (Entry entry in selected)
                    {
                        sourceReader.Position = entry.SourceOffset;
                        entry.PackedBytes = sourceReader.ReadBytes(checked((int)entry.Packed));
                        if (entry.PackedBytes.Length != entry.Packed)
                        {
                            error = "Failed to read source payload: " + entry.Path;
                            return false;
                        }
                    }
                }

                Dictionary<string, Entry> merged = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
                foreach (Entry entry in target.Entries)
                {
                    merged[Normalize(entry.Path)] = entry;
                }

                foreach (Entry entry in selected)
                {
                    merged[Normalize(entry.Path)] = entry;
                }

                WriteMerged(targetPck, targetPkx, target, new List<Entry>(merged.Values), selected);
                PackageIndex verify = ReadIndex(targetPck, targetPkx);
                foreach (Entry entry in selected)
                {
                    if (!verify.ByPath.ContainsKey(Normalize(entry.Path)))
                    {
                        error = "Copied entry was not found after update: " + entry.Path;
                        return false;
                    }
                }

                copied = selected.Count;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private sealed class Algorithm
        {
            public uint Guard0;
            public uint Guard1;
            public uint Mask;
            public uint Check;
        }

        private sealed class Entry
        {
            public byte[] PathRaw;
            public string Path;
            public uint SourceOffset;
            public uint Offset;
            public uint Length;
            public uint Packed;
            public byte[] Tail;
            public byte[] PackedBytes;
            public byte[] IndexBytes;

            public Entry CloneForTarget(string targetPath, Encoding encoding)
            {
                return new Entry
                {
                    PathRaw = EncodePath(encoding, targetPath),
                    Path = targetPath,
                    SourceOffset = SourceOffset,
                    Offset = Offset,
                    Length = Length,
                    Packed = Packed,
                    Tail = Tail == null ? new byte[0] : (byte[])Tail.Clone(),
                    IndexBytes = null
                };
            }
        }

        private sealed class PackageIndex
        {
            public Algorithm Algorithm;
            public uint Flags;
            public byte[] HeaderRaw;
            public bool HasSafeHeader;
            public uint SafeHeaderOffset;
            public long TableOffset;
            public uint Version;
            public List<Entry> Entries = new List<Entry>();
            public Dictionary<string, Entry> ByPath = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        }

        private static PackageIndex ReadIndex(string pck, string pkx)
        {
            using (ConcatReader reader = new ConcatReader(pck, pkx))
            {
                long length = reader.Length;
                if (length < HeaderSize + FooterSize)
                {
                    throw new InvalidOperationException("Package is too short.");
                }

                reader.Position = 0;
                byte[] safeHeader = reader.ReadBytes(12);
                bool hasSafeHeader = safeHeader.Length == 12
                    && BitConverter.ToUInt32(safeHeader, 0) == SafeHeaderTag1
                    && BitConverter.ToUInt32(safeHeader, 8) == SafeHeaderTag2;
                uint safeHeaderOffset = hasSafeHeader ? BitConverter.ToUInt32(safeHeader, 4) : 0;
                long logicalLength = hasSafeHeader && safeHeaderOffset > 0 && safeHeaderOffset <= length
                    ? safeHeaderOffset
                    : length;

                reader.Position = logicalLength - 4;
                uint version = reader.ReadUInt32();
                if (version != 0x00020001 && version != 0x00020002)
                {
                    throw new InvalidOperationException("Unsupported PCK version.");
                }

                reader.Position = logicalLength - 8;
                int count = reader.ReadInt32();
                if (count <= 0 || count > 1000000)
                {
                    throw new InvalidOperationException("Invalid package entry count.");
                }

                reader.Position = logicalLength - (HeaderSize + FooterSize);
                byte[] header = reader.ReadBytes(HeaderSize);
                Algorithm algorithm = ResolveAlgorithm(header);
                uint tableRaw = BitConverter.ToUInt32(header, 8);
                uint flags = BitConverter.ToUInt32(header, 12);
                long tableOffset = tableRaw ^ algorithm.Mask;
                if (tableOffset < 0 || tableOffset >= logicalLength)
                {
                    throw new InvalidOperationException("Invalid package entry table offset.");
                }

                PackageIndex index = new PackageIndex
                {
                    Algorithm = algorithm,
                    Flags = flags,
                    HeaderRaw = header,
                    HasSafeHeader = hasSafeHeader,
                    SafeHeaderOffset = safeHeaderOffset,
                    TableOffset = tableOffset,
                    Version = version
                };

                Encoding gbk = Encoding.GetEncoding("GBK");
                reader.Position = tableOffset;
                for (int i = 0; i < count; i++)
                {
                    uint sx1 = reader.ReadUInt32();
                    uint sx2 = reader.ReadUInt32();
                    uint sizeA = sx1 ^ algorithm.Mask;
                    uint sizeB = sx2 ^ algorithm.Check ^ algorithm.Mask;
                    if (sizeA != sizeB || sizeA == 0 || sizeA > MaxEntrySize)
                    {
                        throw new InvalidOperationException("Invalid package index entry size.");
                    }

                    byte[] indexBytes = reader.ReadBytes((int)sizeA);
                    if (indexBytes.Length != sizeA)
                    {
                        throw new InvalidOperationException("Unexpected package index end.");
                    }

                    byte[] raw = indexBytes.Length == EntrySize || indexBytes.Length == MinEntrySize
                        ? indexBytes
                        : Inflate(indexBytes);
                    if (raw.Length < MinEntrySize)
                    {
                        throw new InvalidOperationException("Invalid package index entry.");
                    }

                    byte[] pathRaw = new byte[PathBytes];
                    Buffer.BlockCopy(raw, 0, pathRaw, 0, PathBytes);
                    string path = DecodePath(gbk, pathRaw);
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    byte[] tail = new byte[Math.Max(0, raw.Length - MinEntrySize)];
                    if (tail.Length > 0)
                    {
                        Buffer.BlockCopy(raw, MinEntrySize, tail, 0, tail.Length);
                    }

                    Entry entry = new Entry
                    {
                        PathRaw = pathRaw,
                        Path = path,
                        SourceOffset = BitConverter.ToUInt32(raw, PathBytes),
                        Offset = BitConverter.ToUInt32(raw, PathBytes),
                        Length = BitConverter.ToUInt32(raw, PathBytes + 4),
                        Packed = BitConverter.ToUInt32(raw, PathBytes + 8),
                        Tail = tail,
                        IndexBytes = indexBytes
                    };
                    index.Entries.Add(entry);
                    index.ByPath[Normalize(path)] = entry;
                }

                return index;
            }
        }

        private static void WriteMerged(string pck, string pkx, PackageIndex target, List<Entry> entries, List<Entry> copied)
        {
            long pckLength = new FileInfo(pck).Length;
            bool writeToPkx = target.TableOffset >= pckLength && !string.IsNullOrWhiteSpace(pkx) && File.Exists(pkx);
            string outputPath = writeToPkx ? pkx : pck;
            string temp = outputPath + ".rawcopy.tmp";
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }

            long localDataEnd = writeToPkx ? target.TableOffset - pckLength : target.TableOffset;
            using (FileStream input = new FileStream(outputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream output = new FileStream(temp, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                CopyRange(input, output, localDataEnd);
                foreach (Entry entry in copied)
                {
                    entry.Offset = checked((uint)(writeToPkx ? pckLength + output.Position : output.Position));
                    output.Write(entry.PackedBytes, 0, entry.PackedBytes.Length);
                }

                long tableOffset = writeToPkx ? pckLength + output.Position : output.Position;
                entries.Sort(CompareEntryPath);
                foreach (Entry entry in entries)
                {
                    byte[] indexBytes = BuildIndexEntry(entry);
                    uint size = (uint)indexBytes.Length;
                    output.Write(BitConverter.GetBytes(size ^ target.Algorithm.Mask), 0, 4);
                    output.Write(BitConverter.GetBytes(size ^ target.Algorithm.Check ^ target.Algorithm.Mask), 0, 4);
                    output.Write(indexBytes, 0, indexBytes.Length);
                }

                byte[] header = target.HeaderRaw != null && target.HeaderRaw.Length == HeaderSize
                    ? (byte[])target.HeaderRaw.Clone()
                    : new byte[HeaderSize];
                Buffer.BlockCopy(BitConverter.GetBytes(target.Algorithm.Guard0), 0, header, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(((uint)tableOffset) ^ target.Algorithm.Mask), 0, header, 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(target.Flags), 0, header, 12, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(target.Algorithm.Guard1), 0, header, 268, 4);
                output.Write(header, 0, header.Length);
                output.Write(BitConverter.GetBytes(entries.Count), 0, 4);
                output.Write(BitConverter.GetBytes(target.Version), 0, 4);
            }

            File.Copy(temp, outputPath, true);
            File.Delete(temp);

            if (target.HasSafeHeader)
            {
                using (FileStream pckHeader = new FileStream(pck, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
                {
                    pckHeader.Position = 4;
                    long pkxLength = !string.IsNullOrWhiteSpace(pkx) && File.Exists(pkx)
                        ? new FileInfo(pkx).Length
                        : 0;
                    uint logicalLength = checked((uint)(new FileInfo(pck).Length + pkxLength));
                    pckHeader.Write(BitConverter.GetBytes(logicalLength), 0, 4);
                }
            }
        }

        private static byte[] BuildRawEntry(Entry entry)
        {
            byte[] raw = new byte[MinEntrySize + (entry.Tail == null ? 0 : entry.Tail.Length)];
            Buffer.BlockCopy(entry.PathRaw, 0, raw, 0, Math.Min(PathBytes, entry.PathRaw.Length));
            Buffer.BlockCopy(BitConverter.GetBytes(entry.Offset), 0, raw, PathBytes, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(entry.Length), 0, raw, PathBytes + 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(entry.Packed), 0, raw, PathBytes + 8, 4);
            if (entry.Tail != null && entry.Tail.Length > 0)
            {
                Buffer.BlockCopy(entry.Tail, 0, raw, MinEntrySize, entry.Tail.Length);
            }

            return raw;
        }

        private static byte[] BuildIndexEntry(Entry entry)
        {
            byte[] raw = BuildRawEntry(entry);
            if (entry.IndexBytes != null && entry.IndexBytes.Length > 0)
            {
                byte[] existingRaw = entry.IndexBytes.Length == EntrySize || entry.IndexBytes.Length == MinEntrySize
                    ? entry.IndexBytes
                    : Inflate(entry.IndexBytes);
                if (existingRaw.Length == raw.Length && AreBytesEqual(existingRaw, raw))
                {
                    return entry.IndexBytes;
                }
            }

            byte[] compressed = Compress(raw);
            return compressed != null && compressed.Length > 0 && compressed.Length < raw.Length
                ? compressed
                : raw;
        }

        private static Algorithm ResolveAlgorithm(byte[] header)
        {
            if (header == null || header.Length < HeaderSize)
            {
                throw new InvalidOperationException("Invalid package header.");
            }

            uint guard0 = BitConverter.ToUInt32(header, 0);
            uint guard1 = BitConverter.ToUInt32(header, 268);
            int[] ids = new int[] { 0, 1, 131 };
            for (int i = 0; i < ids.Length; i++)
            {
                Algorithm algorithm = BuildAlgorithm(ids[i]);
                if (algorithm.Guard0 == guard0 && algorithm.Guard1 == guard1)
                {
                    return algorithm;
                }
            }

            throw new InvalidOperationException("Unknown PCK algorithm.");
        }

        private static Algorithm BuildAlgorithm(int id)
        {
            if (id == 1)
            {
                return new Algorithm { Guard0 = 0xab12908f, Guard1 = 0xb3231902, Mask = 0x2a63810e, Check = 0x18734563 };
            }

            if (id == 0)
            {
                return new Algorithm { Guard0 = 0xfdfdfeee, Guard1 = 0xf00dbeef, Mask = 0xa8937462, Check = 0x59374231 };
            }

            return new Algorithm
            {
                Guard0 = unchecked(0xfdfdfeeeu + (uint)id * 0x072341f2u),
                Guard1 = unchecked(0xf00dbeefu + (uint)id * 0x01237a73u),
                Mask = unchecked(0xa8937462u + (uint)id * 0x0ab2321fu),
                Check = unchecked(0x59374231u + (uint)id * 0x0987a223u)
            };
        }

        private static string DecodePath(Encoding encoding, byte[] pathRaw)
        {
            int len = 0;
            while (len < pathRaw.Length && pathRaw[len] != 0)
            {
                len++;
            }

            return encoding.GetString(pathRaw, 0, len).Replace('/', '\\').Trim();
        }

        private static byte[] EncodePath(Encoding encoding, string path)
        {
            byte[] output = new byte[PathBytes];
            byte[] bytes = encoding.GetBytes((path ?? string.Empty).Replace('/', '\\').Trim().TrimStart('\\'));
            if (bytes.Length >= PathBytes)
            {
                throw new InvalidOperationException("Package path is too long: " + path);
            }

            Buffer.BlockCopy(bytes, 0, output, 0, bytes.Length);
            return output;
        }

        private static string Normalize(string path)
        {
            return CleanPath(path).ToLowerInvariant();
        }

        private static string CleanPath(string path)
        {
            string value = (path ?? string.Empty).Replace('/', '\\').Trim().TrimStart('\\');
            while (value.Contains("\\\\"))
            {
                value = value.Replace("\\\\", "\\");
            }

            return value;
        }

        private static byte[] Inflate(byte[] data)
        {
            byte[] zlib = TryInflateZlib(data);
            if (zlib != null)
            {
                return zlib;
            }

            byte[] deflate = TryInflateDeflate(data, 0, data.Length);
            if (deflate != null)
            {
                return deflate;
            }

            throw new InvalidOperationException("Unsupported compressed index entry.");
        }

        private static byte[] Compress(byte[] data)
        {
            try
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (ZlibStream zlib = new ZlibStream(output, CompressionMode.Compress, CompressionLevel.BestSpeed, true))
                    {
                        zlib.Write(data, 0, data.Length);
                    }

                    return output.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool AreBytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static byte[] TryInflateZlib(byte[] data)
        {
            try
            {
                using (MemoryStream input = new MemoryStream(data))
                using (ZlibStream zlib = new ZlibStream(input, CompressionMode.Decompress))
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

        private static byte[] TryInflateDeflate(byte[] data, int index, int length)
        {
            try
            {
                using (MemoryStream input = new MemoryStream(data, index, length))
                using (DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress))
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

        private static void CopyRange(Stream input, Stream output, long bytes)
        {
            byte[] buffer = new byte[1024 * 1024];
            long remaining = bytes;
            while (remaining > 0)
            {
                int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (read <= 0)
                {
                    throw new EndOfStreamException();
                }

                output.Write(buffer, 0, read);
                remaining -= read;
            }
        }

        private static int CompareEntryPath(Entry left, Entry right)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(Normalize(left == null ? null : left.Path), Normalize(right == null ? null : right.Path));
        }

        private sealed class ConcatReader : IDisposable
        {
            private readonly FileStream pck;
            private readonly FileStream pkx;
            private readonly long pckLength;

            public long Length { get; private set; }
            public long Position { get; set; }

            public ConcatReader(string pckPath, string pkxPath)
            {
                pck = new FileStream(pckPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                pckLength = pck.Length;
                if (!string.IsNullOrWhiteSpace(pkxPath) && File.Exists(pkxPath))
                {
                    pkx = new FileStream(pkxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                }

                Length = pckLength + (pkx == null ? 0 : pkx.Length);
            }

            public uint ReadUInt32()
            {
                byte[] bytes = ReadBytes(4);
                if (bytes.Length != 4)
                {
                    throw new EndOfStreamException();
                }

                return BitConverter.ToUInt32(bytes, 0);
            }

            public int ReadInt32()
            {
                byte[] bytes = ReadBytes(4);
                if (bytes.Length != 4)
                {
                    throw new EndOfStreamException();
                }

                return BitConverter.ToInt32(bytes, 0);
            }

            public byte[] ReadBytes(int count)
            {
                byte[] output = new byte[count];
                int offset = 0;
                while (offset < count)
                {
                    Stream stream = Position < pckLength ? (Stream)pck : (Stream)pkx;
                    if (stream == null)
                    {
                        break;
                    }

                    long local = Position < pckLength ? Position : Position - pckLength;
                    stream.Position = local;
                    int max = (int)Math.Min(count - offset, (Position < pckLength ? pckLength : Length) - Position);
                    int read = stream.Read(output, offset, max);
                    if (read <= 0)
                    {
                        break;
                    }

                    offset += read;
                    Position += read;
                }

                if (offset == count)
                {
                    return output;
                }

                byte[] shortOutput = new byte[offset];
                Buffer.BlockCopy(output, 0, shortOutput, 0, offset);
                return shortOutput;
            }

            public void Dispose()
            {
                if (pck != null)
                {
                    pck.Dispose();
                }

                if (pkx != null)
                {
                    pkx.Dispose();
                }
            }
        }
    }
}
