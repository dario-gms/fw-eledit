using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public sealed class TaskEditorFileService
    {
        public TaskEditorData LoadFromGameRoot(string gameRootPath)
        {
            TaskEditorData data = new TaskEditorData();
            data.GameRootPath = gameRootPath ?? string.Empty;

            List<string> files = EnumerateTaskDataShardFiles(gameRootPath);
            for (int i = 0; i < files.Count; i++)
            {
                LoadShard(files[i], i, data);
            }

            data.Entries.Sort(CompareEntries);
            return data;
        }

        public List<string> EnumerateTaskDataShardFiles(string gameRootPath)
        {
            List<string> files = new List<string>();
            if (string.IsNullOrWhiteSpace(gameRootPath))
            {
                return files;
            }

            string dataDirectory = Path.Combine(gameRootPath, "data");
            if (Directory.Exists(dataDirectory))
            {
                files.AddRange(Directory.GetFiles(dataDirectory, "tasks.data*"));
            }

            if (files.Count == 0)
            {
                string backupDirectory = Path.Combine(gameRootPath, "bak");
                if (Directory.Exists(backupDirectory))
                {
                    files.AddRange(Directory.GetFiles(backupDirectory, "tasks.data*"));
                }
            }

            return files
                .Where(path => IsTaskDataShardFile(Path.GetFileName(path)))
                .OrderBy(path => GetTaskDataShardOrder(Path.GetFileName(path)))
                .ToList();
        }

        private void LoadShard(string filePath, int shardIndex, TaskEditorData data)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.Length < 16)
            {
                return;
            }

            int signature = ReadInt32(fileBytes, 0);
            int version = ReadInt32(fileBytes, 4);
            int count = ReadInt32(fileBytes, 8);
            if (version <= 0 || count <= 0 || count > 200000)
            {
                return;
            }

            long tableLength = 12L + count * 4L;
            if (tableLength >= fileBytes.Length)
            {
                return;
            }

            TaskEditorShard shard = new TaskEditorShard
            {
                Index = shardIndex,
                FilePath = filePath,
                Signature = signature,
                Version = version,
                Count = count
            };
            data.Shards.Add(shard);

            int[] offsets = new int[count];
            for (int i = 0; i < count; i++)
            {
                offsets[i] = ReadInt32(fileBytes, 12 + i * 4);
            }

            HashSet<string> emitted = new HashSet<string>(StringComparer.Ordinal);
            for (int chunkIndex = 0; chunkIndex < offsets.Length; chunkIndex++)
            {
                int startOffset = offsets[chunkIndex];
                int endOffset = chunkIndex < offsets.Length - 1 ? offsets[chunkIndex + 1] : fileBytes.Length;
                if (startOffset <= 0 || endOffset <= startOffset || endOffset > fileBytes.Length)
                {
                    continue;
                }

                byte[] chunkBytes = new byte[endOffset - startOffset];
                Buffer.BlockCopy(fileBytes, startOffset, chunkBytes, 0, chunkBytes.Length);

                List<TaskChunkCandidate> candidates = FindTaskChunkCandidates(chunkBytes);
                if (candidates.Count == 0)
                {
                    TaskChunkCandidate fallback;
                    if (TryReadFallbackTaskChunkCandidate(chunkBytes, out fallback))
                    {
                        candidates.Add(fallback);
                    }
                }

                for (int i = 0; i < candidates.Count; i++)
                {
                    TaskChunkCandidate candidate = candidates[i];
                    int candidateEnd = i < candidates.Count - 1 ? candidates[i + 1].Start : chunkBytes.Length;
                    if (candidateEnd <= candidate.Start)
                    {
                        continue;
                    }

                    string key = shardIndex.ToString(CultureInfo.InvariantCulture)
                        + "|"
                        + (startOffset + candidate.Start).ToString(CultureInfo.InvariantCulture);
                    if (!emitted.Add(key))
                    {
                        continue;
                    }

                    byte[] entryBytes = new byte[candidateEnd - candidate.Start];
                    Buffer.BlockCopy(chunkBytes, candidate.Start, entryBytes, 0, entryBytes.Length);
                    TaskEditorEntry entry = new TaskEditorEntry
                    {
                        Id = candidate.Id,
                        Name = candidate.Name,
                        ShardIndex = shardIndex,
                        ShardName = Path.GetFileName(filePath),
                        ChunkIndex = chunkIndex,
                        ChunkStartOffset = startOffset,
                        ChunkEndOffset = endOffset,
                        LocalOffset = candidate.Start,
                        AbsoluteOffset = startOffset + candidate.Start,
                        Size = entryBytes.Length,
                        Bytes = entryBytes
                    };
                    data.Entries.Add(entry);
                }
            }
        }

        private List<TaskChunkCandidate> FindTaskChunkCandidates(byte[] chunkBytes)
        {
            List<TaskChunkCandidate> candidates = new List<TaskChunkCandidate>();
            if (chunkBytes == null || chunkBytes.Length < 80)
            {
                return candidates;
            }

            for (int offset = 0; offset <= chunkBytes.Length - 80; offset++)
            {
                TaskChunkCandidate candidate;
                if (TryReadTaskChunkCandidate(chunkBytes, offset, out candidate))
                {
                    candidates.Add(candidate);
                }
            }

            return FilterCloseCandidates(candidates);
        }

        private bool TryReadTaskChunkCandidate(byte[] chunkBytes, int offset, out TaskChunkCandidate candidate)
        {
            candidate = null;
            if (chunkBytes == null || offset < 0 || offset + 76 > chunkBytes.Length)
            {
                return false;
            }

            int id = ReadInt32(chunkBytes, offset);
            if (id <= 0 || id > 20000000)
            {
                return false;
            }

            string name = ReadTaskName(chunkBytes, offset + 4, 30);
            if (string.IsNullOrWhiteSpace(name) || !HasTaskChunkHeaderSignature(chunkBytes, offset))
            {
                return false;
            }

            candidate = new TaskChunkCandidate { Start = offset, Id = id, Name = name };
            return true;
        }

        private bool TryReadFallbackTaskChunkCandidate(byte[] chunkBytes, out TaskChunkCandidate candidate)
        {
            candidate = null;
            if (chunkBytes == null || chunkBytes.Length < 64)
            {
                return false;
            }

            int id = ReadInt32(chunkBytes, 0);
            if (id <= 0 || id > 20000000)
            {
                return false;
            }

            string name = ReadTaskName(chunkBytes, 4, 30);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Task " + id.ToString(CultureInfo.InvariantCulture);
            }

            candidate = new TaskChunkCandidate { Start = 0, Id = id, Name = name };
            return true;
        }

        private static bool HasTaskChunkHeaderSignature(byte[] chunkBytes, int offset)
        {
            bool signatureA = ReadInt32(chunkBytes, offset + 64) == 0
                && ReadInt32(chunkBytes, offset + 68) == 67108864
                && ReadInt32(chunkBytes, offset + 72) == 0;
            if (signatureA)
            {
                return true;
            }

            return ReadInt32(chunkBytes, offset + 64) == 0
                && ReadInt32(chunkBytes, offset + 68) == 0
                && ReadInt32(chunkBytes, offset + 72) == 67108864
                && ReadInt32(chunkBytes, offset + 76) == 0;
        }

        private static List<TaskChunkCandidate> FilterCloseCandidates(List<TaskChunkCandidate> candidates)
        {
            if (candidates == null || candidates.Count <= 1)
            {
                return candidates ?? new List<TaskChunkCandidate>();
            }

            List<TaskChunkCandidate> filtered = new List<TaskChunkCandidate>();
            for (int i = 0; i < candidates.Count; i++)
            {
                TaskChunkCandidate candidate = candidates[i];
                if (filtered.Count == 0)
                {
                    filtered.Add(candidate);
                    continue;
                }

                TaskChunkCandidate previous = filtered[filtered.Count - 1];
                if (candidate.Start - previous.Start <= 4)
                {
                    if ((candidate.Name ?? string.Empty).Length > (previous.Name ?? string.Empty).Length)
                    {
                        filtered[filtered.Count - 1] = candidate;
                    }
                    continue;
                }

                filtered.Add(candidate);
            }

            return filtered;
        }

        public static List<TaskEditorTextValue> ExtractUnicodeTexts(byte[] bytes)
        {
            List<TaskEditorTextValue> values = new List<TaskEditorTextValue>();
            if (bytes == null || bytes.Length < 2)
            {
                return values;
            }

            for (int offset = 0; offset < bytes.Length - 2; offset += 2)
            {
                int start = offset;
                StringBuilder builder = new StringBuilder();
                while (offset + 1 < bytes.Length)
                {
                    ushort code = BitConverter.ToUInt16(bytes, offset);
                    if (code == 0)
                    {
                        break;
                    }
                    char c = (char)code;
                    if (!IsSupportedTextCharacter(c))
                    {
                        break;
                    }
                    builder.Append(c);
                    offset += 2;
                }

                string text = builder.ToString().Trim();
                if (text.Length >= 3 && IsUsefulText(text))
                {
                    values.Add(new TaskEditorTextValue { Offset = start, Text = text });
                }
            }

            return values
                .GroupBy(value => value.Offset.ToString(CultureInfo.InvariantCulture) + "|" + value.Text)
                .Select(group => group.First())
                .ToList();
        }

        private static bool IsSupportedTextCharacter(char c)
        {
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            {
                return true;
            }

            if ("-_'()[]{}:,.!?/&+;\"%#=*<>".IndexOf(c) >= 0)
            {
                return true;
            }

            return (c >= 0x2E80 && c <= 0x9FFF)
                || (c >= 0xAC00 && c <= 0xD7AF)
                || (c >= 0xF900 && c <= 0xFAFF)
                || (c >= 0xFF00 && c <= 0xFFEF);
        }

        private static bool IsUsefulText(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsLetterOrDigit(text[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static List<TaskEditorRawValue> BuildRawValues(byte[] bytes)
        {
            List<TaskEditorRawValue> values = new List<TaskEditorRawValue>();
            if (bytes == null)
            {
                return values;
            }

            for (int offset = 0; offset + 4 <= bytes.Length; offset += 4)
            {
                int intValue = BitConverter.ToInt32(bytes, offset);
                uint uintValue = BitConverter.ToUInt32(bytes, offset);
                float floatValue = BitConverter.ToSingle(bytes, offset);
                values.Add(new TaskEditorRawValue
                {
                    Offset = offset,
                    HexOffset = "0x" + offset.ToString("X4", CultureInfo.InvariantCulture),
                    Int32Value = intValue.ToString(CultureInfo.InvariantCulture),
                    UInt32Value = uintValue.ToString(CultureInfo.InvariantCulture),
                    FloatValue = IsReadableFloat(floatValue) ? floatValue.ToString("R", CultureInfo.InvariantCulture) : string.Empty,
                    HexBytes = BitConverter.ToString(bytes, offset, 4).Replace("-", " "),
                    Hint = GetFieldHint(offset)
                });
            }

            return values;
        }

        private static string GetFieldHint(int offset)
        {
            if (offset == 0)
            {
                return "m_ID";
            }
            if (offset == 4)
            {
                return "m_szName[0]";
            }
            if (offset > 4 && offset < 64)
            {
                return "m_szName";
            }
            return string.Empty;
        }

        private static bool IsReadableFloat(float value)
        {
            return !float.IsNaN(value)
                && !float.IsInfinity(value)
                && Math.Abs(value) < 100000000F
                && (value == 0F || Math.Abs(value) >= 0.000001F);
        }

        private static bool IsTaskDataShardFile(string fileName)
        {
            return !string.IsNullOrWhiteSpace(fileName)
                && Regex.IsMatch(fileName.Trim(), @"^tasks\.data\d+$", RegexOptions.IgnoreCase);
        }

        private static int GetTaskDataShardOrder(string fileName)
        {
            Match match = Regex.Match((fileName ?? string.Empty).Trim(), @"^tasks\.data(\d+)$", RegexOptions.IgnoreCase);
            int order;
            return match.Success && int.TryParse(match.Groups[1].Value, out order)
                ? order
                : int.MaxValue;
        }

        private static string ReadTaskName(byte[] bytes, int offset, int charCount)
        {
            StringBuilder builder = new StringBuilder(charCount);
            for (int i = 0; i < charCount; i++)
            {
                int charOffset = offset + i * 2;
                if (bytes == null || charOffset < 0 || charOffset + 2 > bytes.Length)
                {
                    break;
                }

                ushort value = BitConverter.ToUInt16(bytes, charOffset);
                if (value == 0)
                {
                    break;
                }

                char c = (char)value;
                if (!IsSupportedTextCharacter(c))
                {
                    break;
                }

                builder.Append(c);
            }

            return builder.ToString().Trim();
        }

        private static int ReadInt32(byte[] bytes, int offset)
        {
            return bytes != null && offset >= 0 && offset + 4 <= bytes.Length
                ? BitConverter.ToInt32(bytes, offset)
                : 0;
        }

        private static int CompareEntries(TaskEditorEntry left, TaskEditorEntry right)
        {
            if (left == null && right == null)
            {
                return 0;
            }
            if (left == null)
            {
                return -1;
            }
            if (right == null)
            {
                return 1;
            }

            int shard = left.ShardIndex.CompareTo(right.ShardIndex);
            if (shard != 0)
            {
                return shard;
            }

            int chunk = left.ChunkIndex.CompareTo(right.ChunkIndex);
            if (chunk != 0)
            {
                return chunk;
            }

            return left.LocalOffset.CompareTo(right.LocalOffset);
        }

        private sealed class TaskChunkCandidate
        {
            public int Start { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
