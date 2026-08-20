param(
    [Parameter(Mandatory = $true)]
    [string] $SourcePck,

    [string] $SourcePkx,

    [Parameter(Mandatory = $true)]
    [string] $TargetPck,

    [string] $TargetPkx,

    [Parameter(Mandatory = $true)]
    [string[]] $Prefix,

    [string] $PackageName = "models",

    [string] $WorkRoot = ".tmp-pck-raw-probe",

    [switch] $UseOriginalTarget
)

$ErrorActionPreference = "Stop"

$code = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public static class PckRawCopyProbe
{
    private const int PathBytes = 260;
    private const int HeaderSize = 272;
    private const int FooterSize = 8;
    private const int EntrySize = 276;
    private const uint SafeHeaderTag1 = 0x4DCA23EF;
    private const uint SafeHeaderTag2 = 0x56A089B7;
    private const uint PackFlagEncrypt = 0x80000000;

    private sealed class Algo
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
        public uint Offset;
        public uint Length;
        public uint Packed;
        public byte[] Tail;
        public byte[] PackedBytes;
    }

    public static void CopyPrefixes(string sourcePck, string sourcePkx, string targetPck, string targetPkx, string packageName, string[] prefixes, string workRoot, bool useOriginalTarget)
    {
        if (!File.Exists(sourcePck)) throw new FileNotFoundException("Source PCK not found.", sourcePck);
        if (!File.Exists(targetPck)) throw new FileNotFoundException("Target PCK not found.", targetPck);

        string actualTarget = targetPck;
        string actualTargetPkx = targetPkx;
        if (!useOriginalTarget)
        {
            Directory.CreateDirectory(workRoot);
            string runRoot = Path.Combine(workRoot, "run-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(runRoot);
            actualTarget = Path.Combine(runRoot, Path.GetFileName(targetPck));
            File.Copy(targetPck, actualTarget, true);
            if (!string.IsNullOrWhiteSpace(targetPkx) && File.Exists(targetPkx))
            {
                actualTargetPkx = Path.Combine(runRoot, Path.GetFileName(targetPkx));
                File.Copy(targetPkx, actualTargetPkx, true);
            }
            else
            {
                actualTargetPkx = string.Empty;
            }
            Console.WriteLine("Target copy: " + actualTarget);
        }

        PackageIndex source = ReadIndex(sourcePck, sourcePkx);
        PackageIndex target = ReadIndex(actualTarget, actualTargetPkx);
        if ((source.Flags & PackFlagEncrypt) != (target.Flags & PackFlagEncrypt))
        {
            throw new InvalidOperationException("Source and target encryption flags differ; raw payload copy is not safe.");
        }

        List<Entry> selected = new List<Entry>();
        foreach (Entry entry in source.Entries)
        {
            string path = Normalize(entry.Path);
            foreach (string prefix in prefixes ?? new string[0])
            {
                string normalizedPrefix = Normalize(prefix);
                if (path.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    selected.Add(entry);
                    break;
                }
            }
        }

        if (selected.Count == 0)
        {
            throw new InvalidOperationException("No source entries matched the requested prefix.");
        }

        using (ConcatReader sourceReader = new ConcatReader(sourcePck, sourcePkx))
        {
            foreach (Entry entry in selected)
            {
                sourceReader.Position = entry.Offset;
                entry.PackedBytes = sourceReader.ReadBytes((int)entry.Packed);
                if (entry.PackedBytes.Length != entry.Packed)
                {
                    throw new InvalidOperationException("Failed to read source payload: " + entry.Path);
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

        WriteMerged(actualTarget, actualTargetPkx, target, new List<Entry>(merged.Values), selected);
        PackageIndex verify = ReadIndex(actualTarget, actualTargetPkx);
        int found = 0;
        foreach (Entry entry in selected)
        {
            if (verify.ByPath.ContainsKey(Normalize(entry.Path))) found++;
        }
        Console.WriteLine("Selected: " + selected.Count);
        Console.WriteLine("Verified: " + found);
        if (found != selected.Count)
        {
            throw new InvalidOperationException("Not every copied entry was found after update.");
        }
    }

    private sealed class PackageIndex
    {
        public Algo Algorithm;
        public uint Flags;
        public byte[] HeaderRaw;
        public bool HasSafeHeader;
        public long TableOffset;
        public long DataEnd;
        public uint Version;
        public List<Entry> Entries = new List<Entry>();
        public Dictionary<string, Entry> ByPath = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
    }

    private static PackageIndex ReadIndex(string pck, string pkx)
    {
        using (ConcatReader reader = new ConcatReader(pck, pkx))
        {
            long length = reader.Length;
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
            if (version != 0x00020001 && version != 0x00020002) throw new InvalidOperationException("Unsupported PCK version.");
            reader.Position = logicalLength - 8;
            int count = reader.ReadInt32();
            if (count <= 0 || count > 1000000) throw new InvalidOperationException("Invalid entry count.");
            reader.Position = logicalLength - (HeaderSize + FooterSize);
            byte[] header = reader.ReadBytes(HeaderSize);
            Algo algo = ResolveAlgo(header);
            uint tableRaw = BitConverter.ToUInt32(header, 8);
            uint flags = BitConverter.ToUInt32(header, 12);
            long tableOffset = tableRaw ^ algo.Mask;

            PackageIndex index = new PackageIndex { Algorithm = algo, Flags = flags, HeaderRaw = header, HasSafeHeader = hasSafeHeader, TableOffset = tableOffset, DataEnd = tableOffset, Version = version };
            Encoding gbk = Encoding.GetEncoding("GBK");
            reader.Position = tableOffset;
            for (int i = 0; i < count; i++)
            {
                uint sx1 = reader.ReadUInt32();
                uint sx2 = reader.ReadUInt32();
                uint sizeA = sx1 ^ algo.Mask;
                uint sizeB = sx2 ^ algo.Check ^ algo.Mask;
                if (sizeA != sizeB || sizeA == 0 || sizeA > 1024 * 1024) throw new InvalidOperationException("Invalid entry size.");
                byte[] raw = reader.ReadBytes((int)sizeA);
                if (raw.Length != sizeA) throw new InvalidOperationException("Unexpected index EOF.");
                if (raw.Length != EntrySize && raw.Length != PathBytes + 12) raw = Inflate(raw);
                if (raw.Length < PathBytes + 12) throw new InvalidOperationException("Invalid index entry.");

                byte[] pathRaw = new byte[PathBytes];
                Buffer.BlockCopy(raw, 0, pathRaw, 0, PathBytes);
                string path = DecodePath(gbk, pathRaw);
                byte[] tail = new byte[Math.Max(0, raw.Length - (PathBytes + 12))];
                if (tail.Length > 0) Buffer.BlockCopy(raw, PathBytes + 12, tail, 0, tail.Length);
                Entry entry = new Entry
                {
                    PathRaw = pathRaw,
                    Path = path,
                    Offset = BitConverter.ToUInt32(raw, PathBytes),
                    Length = BitConverter.ToUInt32(raw, PathBytes + 4),
                    Packed = BitConverter.ToUInt32(raw, PathBytes + 8),
                    Tail = tail
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
        bool writeToPkx = target.TableOffset >= pckLength && !string.IsNullOrWhiteSpace(pkx);
        string outputPath = writeToPkx ? pkx : pck;
        string temp = outputPath + ".rawcopy.tmp";
        if (File.Exists(temp)) File.Delete(temp);

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
                byte[] raw = BuildRawEntry(entry);
                uint size = (uint)raw.Length;
                output.Write(BitConverter.GetBytes(size ^ target.Algorithm.Mask), 0, 4);
                output.Write(BitConverter.GetBytes(size ^ target.Algorithm.Check ^ target.Algorithm.Mask), 0, 4);
                output.Write(raw, 0, raw.Length);
            }
            byte[] header = target.HeaderRaw != null && target.HeaderRaw.Length == HeaderSize
                ? (byte[])target.HeaderRaw.Clone()
                : new byte[HeaderSize];
            Buffer.BlockCopy(BitConverter.GetBytes(target.Algorithm.Guard0), 0, header, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(target.Algorithm.Guard1), 0, header, 268, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(((uint)tableOffset) ^ target.Algorithm.Mask), 0, header, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(target.Flags), 0, header, 12, 4);
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
        byte[] raw = new byte[PathBytes + 12 + (entry.Tail == null ? 0 : entry.Tail.Length)];
        Buffer.BlockCopy(entry.PathRaw, 0, raw, 0, Math.Min(PathBytes, entry.PathRaw.Length));
        Buffer.BlockCopy(BitConverter.GetBytes(entry.Offset), 0, raw, PathBytes, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(entry.Length), 0, raw, PathBytes + 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(entry.Packed), 0, raw, PathBytes + 8, 4);
        if (entry.Tail != null && entry.Tail.Length > 0) Buffer.BlockCopy(entry.Tail, 0, raw, PathBytes + 12, entry.Tail.Length);
        return raw;
    }

    private static int CompareEntryPath(Entry left, Entry right)
    {
        return StringComparer.OrdinalIgnoreCase.Compare(Normalize(left == null ? null : left.Path), Normalize(right == null ? null : right.Path));
    }

    private static Algo ResolveAlgo(byte[] header)
    {
        uint g0 = BitConverter.ToUInt32(header, 0);
        uint g1 = BitConverter.ToUInt32(header, 268);
        foreach (int id in new int[] { 0, 1, 131 })
        {
            Algo a = BuildAlgo(id);
            if (a.Guard0 == g0 && a.Guard1 == g1) return a;
        }
        throw new InvalidOperationException("Unknown PCK algorithm.");
    }

    private static Algo BuildAlgo(int id)
    {
        if (id == 1) return new Algo { Guard0 = 0xab12908f, Guard1 = 0xb3231902, Mask = 0x2a63810e, Check = 0x18734563 };
        if (id == 0) return new Algo { Guard0 = 0xfdfdfeee, Guard1 = 0xf00dbeef, Mask = 0xa8937462, Check = 0x59374231 };
        return new Algo
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
        while (len < pathRaw.Length && pathRaw[len] != 0) len++;
        return encoding.GetString(pathRaw, 0, len).Replace('/', '\\').Trim();
    }

    private static string Normalize(string path)
    {
        string value = (path ?? string.Empty).Replace('/', '\\').Trim().TrimStart('\\');
        while (value.Contains("\\\\")) value = value.Replace("\\\\", "\\");
        return value.ToLowerInvariant();
    }

    private static byte[] Inflate(byte[] data)
    {
        byte[] zlib = TryInflateZlib(data);
        if (zlib != null) return zlib;
        byte[] deflate = TryInflateDeflate(data, 0, data.Length);
        if (deflate != null) return deflate;
        throw new InvalidOperationException("Unsupported compressed index entry.");
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

    private static byte[] TryInflateDeflate(byte[] data, int index, int length)
    {
        try
        {
            using (MemoryStream input = new MemoryStream(data, index, length))
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

    private static void CopyRange(Stream input, Stream output, long bytes)
    {
        byte[] buffer = new byte[1024 * 1024];
        long remaining = bytes;
        while (remaining > 0)
        {
            int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read <= 0) throw new EndOfStreamException();
            output.Write(buffer, 0, read);
            remaining -= read;
        }
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
            byte[] b = ReadBytes(4);
            return BitConverter.ToUInt32(b, 0);
        }

        public int ReadInt32()
        {
            byte[] b = ReadBytes(4);
            return BitConverter.ToInt32(b, 0);
        }

        public byte[] ReadBytes(int count)
        {
            byte[] output = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                Stream stream = Position < pckLength ? (Stream)pck : (Stream)pkx;
                if (stream == null) break;
                long local = Position < pckLength ? Position : Position - pckLength;
                stream.Position = local;
                int max = (int)Math.Min(count - offset, (Position < pckLength ? pckLength : Length) - Position);
                int read = stream.Read(output, offset, max);
                if (read <= 0) break;
                offset += read;
                Position += read;
            }
            if (offset == count) return output;
            byte[] shortOutput = new byte[offset];
            Buffer.BlockCopy(output, 0, shortOutput, 0, offset);
            return shortOutput;
        }

        public void Dispose()
        {
            if (pck != null) pck.Dispose();
            if (pkx != null) pkx.Dispose();
        }
    }
}
'@

$zlibPath = [System.IO.Path]::GetFullPath("FWEledit\lib\Ionic.Zlib.dll")
[void][System.Reflection.Assembly]::LoadFrom($zlibPath)

Add-Type -TypeDefinition $code -ReferencedAssemblies @(
    "System.dll",
    "System.Core.dll",
    "System.Collections.dll",
    "System.Console.dll",
    "mscorlib.dll",
    $zlibPath
)

$targetPckFull = [System.IO.Path]::GetFullPath($TargetPck)
$targetPkxFull = if ([string]::IsNullOrWhiteSpace($TargetPkx)) { "" } else { [System.IO.Path]::GetFullPath($TargetPkx) }
$sourcePckFull = [System.IO.Path]::GetFullPath($SourcePck)
$sourcePkxFull = if ([string]::IsNullOrWhiteSpace($SourcePkx)) { "" } else { [System.IO.Path]::GetFullPath($SourcePkx) }
$workRootFull = [System.IO.Path]::GetFullPath($WorkRoot)

[PckRawCopyProbe]::CopyPrefixes($sourcePckFull, $sourcePkxFull, $targetPckFull, $targetPkxFull, $PackageName, $Prefix, $workRootFull, [bool]$UseOriginalTarget)
