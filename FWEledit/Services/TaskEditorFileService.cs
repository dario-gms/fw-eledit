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
        private const int FixedTaskDataSize = 0x1007;
        private const int AwardDataSize = 0x0710;
        private const int TaskTimeSize = 24;
        private const int TaskNameTextBytes = 60;
        private const int TaskMethodTextBytes = 128;
        private const int ItemWantedSize = 52;
        private const int MonsterWantedSize = 23;
        private const int TeamMemberWantedSize = 37;
        private const int MaxTimetableEntries = 12;
        private const int MaxItemWantedEntries = 32;
        private const int MaxItemSubmitEntries = 16;
        private const int MaxMonsterWantedEntries = 6;
        private const int MaxTeamMemberWantedEntries = 8;
        private const int MaxAwardItemCandidates = 8;
        private const int MaxAwardItems = 32;
        private const int HasSignOffset = 0x0042;
        private const int TimetableCountOffset = 0x0054;
        private const int PremItemsCountOffset = 0x01DC;
        private const int GivenItemsCountOffset = 0x01E9;
        private const int PremTitleCountOffset = 0x0205;
        private const int TeamworkOffset = 0x0A32;
        private const int TeamMemberWantedCountOffset = 0x0A4E;
        private const int MonsterWantedCountOffset = 0x0DEA;
        private const int HasGatherMonsterOffset = 0x0DFE;
        private const int ItemsWantedCountOffset = 0x0E11;
        private const int HasFinishTaskTimesOffset = 0x0E9F;
        private const int ItemsSubmitWantedCountOffset = 0x0EB5;
        private const int WelcomeWordsCountOffset = 0x0EC9;
        private const int AwardSideOccupationExpOffset = 0x05B2;
        private const int AwardSideOccupationExpCount = 64;
        private const int AwardSideOccupationPointsOffset = 0x06B2;
        private const int AwardItemCandidateCountOffset = 0x04AD;
        private const int ParentTaskIdOffset = FixedTaskDataSize - 16;
        private const int PrevSiblingTaskIdOffset = FixedTaskDataSize - 12;
        private const int NextSiblingTaskIdOffset = FixedTaskDataSize - 8;
        private const int FirstChildTaskIdOffset = FixedTaskDataSize - 4;

        public TaskEditorData LoadFromGameRoot(string gameRootPath)
        {
            TaskEditorData data = new TaskEditorData();
            data.GameRootPath = gameRootPath ?? string.Empty;

            List<string> files = EnumerateTaskDataShardFiles(gameRootPath);
            for (int i = 0; i < files.Count; i++)
            {
                LoadShard(files[i], i, data);
            }

            SanitizeTaskHierarchy(data.Entries);
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

                List<TaskEditorEntry> chunkEntries = new List<TaskEditorEntry>();
                for (int i = 0; i < candidates.Count; i++)
                {
                    TaskChunkCandidate candidate = candidates[i];
                    int candidateEnd = i < candidates.Count - 1 ? candidates[i + 1].Start : chunkBytes.Length;
                    if (candidateEnd <= candidate.Start || candidateEnd - candidate.Start < FixedTaskDataSize)
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
                    PopulateTaskHierarchy(entry);
                    chunkEntries.Add(entry);
                    data.Entries.Add(entry);
                }

                PopulateChunkHierarchyFallback(chunkEntries);
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

            int markerA = ReadInt32(chunkBytes, offset + 68);
            int markerB = ReadInt32(chunkBytes, offset + 72);
            return ReadInt32(chunkBytes, offset + 64) == 0
                && ReadInt32(chunkBytes, offset + 76) == 0
                && ((markerA == 0 && IsTaskHeaderMarker(markerB)) || (IsTaskHeaderMarker(markerA) && markerB == 0));
        }

        private static bool IsTaskHeaderMarker(int value)
        {
            if (value <= 0)
            {
                return false;
            }

            return value == 0x04000000 || value == 0x07000000;
        }

        private static void PopulateTaskHierarchy(TaskEditorEntry entry)
        {
            if (entry == null || entry.Bytes == null)
            {
                return;
            }

            entry.ParentId = ReadUInt32AsInt(entry.Bytes, ParentTaskIdOffset);
            entry.PrevSiblingId = ReadUInt32AsInt(entry.Bytes, PrevSiblingTaskIdOffset);
            entry.NextSiblingId = ReadUInt32AsInt(entry.Bytes, NextSiblingTaskIdOffset);
            entry.FirstChildId = ReadUInt32AsInt(entry.Bytes, FirstChildTaskIdOffset);
            entry.HasChildren = entry.FirstChildId > 0;
        }

        private static void PopulateChunkHierarchyFallback(List<TaskEditorEntry> entries)
        {
            if (entries == null || entries.Count <= 1)
            {
                return;
            }

            TaskEditorEntry root = entries[0];
            HashSet<int> chunkIds = new HashSet<int>(entries.Select(entry => entry.Id));
            if (!chunkIds.Contains(root.FirstChildId))
            {
                root.FirstChildId = entries[1].Id;
            }

            root.HasChildren = true;
            for (int i = 1; i < entries.Count; i++)
            {
                TaskEditorEntry entry = entries[i];
                if (!chunkIds.Contains(entry.ParentId))
                {
                    entry.ParentId = root.Id;
                }

                entry.PrevSiblingId = i > 1 ? entries[i - 1].Id : 0;
                entry.NextSiblingId = i < entries.Count - 1 ? entries[i + 1].Id : 0;
            }
        }

        private static void SanitizeTaskHierarchy(TaskEditorEntry entry, ISet<int> knownTaskIds)
        {
            if (entry == null || knownTaskIds == null)
            {
                return;
            }

            if (!knownTaskIds.Contains(entry.ParentId))
            {
                entry.ParentId = 0;
            }
            if (!knownTaskIds.Contains(entry.PrevSiblingId))
            {
                entry.PrevSiblingId = 0;
            }
            if (!knownTaskIds.Contains(entry.NextSiblingId))
            {
                entry.NextSiblingId = 0;
            }
            if (!knownTaskIds.Contains(entry.FirstChildId))
            {
                entry.FirstChildId = 0;
            }

            entry.HasChildren = entry.FirstChildId > 0;
        }

        private static void SanitizeTaskHierarchy(List<TaskEditorEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            HashSet<int> knownTaskIds = new HashSet<int>(entries.Select(entry => entry.Id));
            foreach (TaskEditorEntry entry in entries)
            {
                SanitizeTaskHierarchy(entry, knownTaskIds);
            }
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

                string text = NormalizeExtractedText(builder.ToString());
                if ((text.Length >= 3 || IsUsefulShortText(text)) && IsUsefulText(text))
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

        private static bool IsUsefulShortText(string text)
        {
            text = (text ?? string.Empty).Trim();
            return text.Length == 1 && IsAsciiLetterOrDigit(text[0]);
        }

        private static bool IsAsciiLetterOrDigit(char c)
        {
            return (c >= 'A' && c <= 'Z')
                || (c >= 'a' && c <= 'z')
                || (c >= '0' && c <= '9');
        }

        private static string NormalizeExtractedText(string text)
        {
            text = (text ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                return text;
            }

            string swapped = SwapUtf16Bytes(text).Trim();
            if (text.Length < 3 && IsUsefulShortText(swapped))
            {
                return swapped;
            }

            if (ShouldUseByteSwappedText(text, swapped))
            {
                return swapped;
            }

            return text;
        }

        private static string SwapUtf16Bytes(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                int code = text[i];
                builder.Append((char)(((code & 0x00FF) << 8) | ((code & 0xFF00) >> 8)));
            }

            return builder.ToString();
        }

        private static bool ShouldUseByteSwappedText(string original, string swapped)
        {
            if (string.IsNullOrWhiteSpace(swapped) || !IsUsefulText(swapped))
            {
                return false;
            }

            int originalCjk = CountCjkCharacters(original);
            int originalLatin = CountLatinCharacters(original);
            int swappedLatin = CountLatinCharacters(swapped);
            int swappedPrintable = CountSupportedCharacters(swapped);
            return originalCjk >= Math.Max(2, original.Length / 2)
                && swappedLatin > originalLatin
                && swappedLatin >= Math.Max(2, swapped.Length / 3)
                && swappedPrintable >= swapped.Length - 1;
        }

        private static int CountCjkCharacters(string text)
        {
            int count = 0;
            for (int i = 0; i < (text ?? string.Empty).Length; i++)
            {
                char c = text[i];
                if ((c >= 0x2E80 && c <= 0x9FFF) || (c >= 0xF900 && c <= 0xFAFF))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountLatinCharacters(string text)
        {
            int count = 0;
            for (int i = 0; i < (text ?? string.Empty).Length; i++)
            {
                char c = text[i];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSupportedCharacters(string text)
        {
            int count = 0;
            for (int i = 0; i < (text ?? string.Empty).Length; i++)
            {
                if (IsSupportedTextCharacter(text[i]))
                {
                    count++;
                }
            }

            return count;
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

        public static List<TaskEditorFieldValue> BuildKnownFields(byte[] bytes)
        {
            return BuildKnownFields(bytes, false);
        }

        public static List<TaskEditorFieldValue> BuildAllMappedFields(byte[] bytes)
        {
            return BuildKnownFields(bytes, true);
        }

        private static List<TaskEditorFieldValue> BuildKnownFields(byte[] bytes, bool includeEmptyArraySlots)
        {
            List<TaskEditorFieldValue> values = new List<TaskEditorFieldValue>();
            if (bytes == null)
            {
                return values;
            }

            List<FieldSpec> specs = new List<FieldSpec>
            {
                new FieldSpec("General", "m_ID", 0x0000, "uint"),
                new FieldSpec("General", "m_szName", 0x0004, "wstring:30"),
                new FieldSpec("General", "m_bHidden", 0x0040, "bool"),
                new FieldSpec("General", "m_bOffLineIsFail", 0x0041, "bool"),
                new FieldSpec("General", "m_bHasSign", 0x0042, "bool"),
                new FieldSpec("General", "m_ulType", 0x004B, "uint"),
                new FieldSpec("General", "m_ulTimeLimit", 0x004F, "uint"),
                new FieldSpec("General", "m_bAbsTime", 0x0053, "bool"),
                new FieldSpec("General", "m_ulTimetable", 0x0054, "uint"),
                new FieldSpec("Timetable", "m_tmType", 0x0058, "byte-array:20"),
                new FieldSpec("General", "m_lAvailFrequency", 0x0074, "int"),
                new FieldSpec("General", "m_lTimeInterval", 0x0078, "int"),
                new FieldSpec("Flags", "m_bBirthday", 0x007C, "bool"),
                new FieldSpec("Flags", "m_bBuild", 0x007D, "bool"),
                new FieldSpec("Flags", "m_bNoExpMakeUp", 0x007E, "bool"),
                new FieldSpec("Flags", "m_bChooseOne", 0x007F, "bool"),
                new FieldSpec("Flags", "m_bRandOne", 0x0080, "bool"),
                new FieldSpec("Flags", "m_bExeChildInOrder", 0x0081, "bool"),
                new FieldSpec("Flags", "m_bParentAlsoFail", 0x0082, "bool"),
                new FieldSpec("Flags", "m_bParentAlsoSucc", 0x0083, "bool"),
                new FieldSpec("Flags", "m_bCanGiveUp", 0x0084, "bool"),
                new FieldSpec("Flags", "m_bCanRedo", 0x0085, "bool"),
                new FieldSpec("Flags", "m_bCanRedoAfterFailure", 0x0086, "bool"),
                new FieldSpec("Flags", "m_bClearAsGiveUp", 0x0087, "bool"),
                new FieldSpec("Flags", "m_bUIButtonTask", 0x0088, "bool"),
                new FieldSpec("Flags", "m_bNeedRecord", 0x0089, "bool"),
                new FieldSpec("Flags", "m_bFailAsPlayerDie", 0x008A, "bool"),
                new FieldSpec("Delivery", "m_ulMaxReceiver", 0x008B, "uint"),
                new FieldSpec("Delivery", "m_bDelvInZone", 0x008F, "bool"),
                new FieldSpec("Delivery", "m_ulDelvWorld", 0x0090, "uint"),
                new FieldSpec("Delivery", "m_DelvMinVert", 0x0094, "zone"),
                new FieldSpec("Delivery", "m_DelvMaxVert", 0x00A0, "zone"),
                new FieldSpec("Delivery", "m_bOutZoneFail", 0x00AC, "bool"),
                new FieldSpec("Delivery", "m_ulOutZoneWorldID", 0x00AD, "uint"),
                new FieldSpec("Delivery", "m_OutZoneMinVert", 0x00B1, "zone"),
                new FieldSpec("Delivery", "m_OutZoneMaxVert", 0x00BD, "zone"),
                new FieldSpec("Transport", "m_bTransTo", 0x00C9, "bool"),
                new FieldSpec("Transport", "m_ulTransWldId", 0x00CA, "uint"),
                new FieldSpec("Transport", "m_TransPt", 0x00CE, "zone"),
                new FieldSpec("Flow", "m_bAutoDeliver", 0x00DA, "bool"),
                new FieldSpec("Flow", "m_bDeathTrig", 0x00DB, "bool"),
                new FieldSpec("Flow", "m_bClearAcquired", 0x00DC, "bool"),
                new FieldSpec("Flow", "m_ulSuitableLevel", 0x00DD, "uint"),
                new FieldSpec("Flow", "m_bSuitLevelEx", 0x00E1, "bool"),
                new FieldSpec("Flow", "m_bShowPrompt", 0x00E2, "bool"),
                new FieldSpec("Flow", "m_bKeyTask", 0x00E3, "bool"),
                new FieldSpec("Flow", "m_bLuaTask", 0x00E4, "bool"),
                new FieldSpec("NPC", "m_ulDelvNPC", 0x00E5, "uint"),
                new FieldSpec("NPC", "m_ulAwardNPC", 0x00E9, "uint"),
                new FieldSpec("Flow", "m_bSkillTask", 0x00ED, "bool"),
                new FieldSpec("Flow", "m_bCanSeekOut", 0x00EE, "bool"),
                new FieldSpec("Flow", "m_bShowDirection", 0x00EF, "bool"),
                new FieldSpec("General", "m_fStorageWeight", 0x00F0, "float"),
                new FieldSpec("General", "m_ulRank", 0x00F4, "uint"),
                new FieldSpec("Flags", "m_bMarriage", 0x00F8, "bool"),
                new FieldSpec("Flags", "m_bSharedByFamily", 0x00F9, "bool"),
                new FieldSpec("Finish Count", "m_bRecFinishCount", 0x00FA, "bool"),
                new FieldSpec("Finish Count", "m_bRecFinishCountGlobal", 0x00FB, "bool"),
                new FieldSpec("Finish Count", "m_ulMaxFinishCount", 0x00FC, "uint"),
                new FieldSpec("Finish Count", "m_FinishClearTime", 0x0100, "task_tm"),
                new FieldSpec("Finish Count", "m_nFinishTimeType", 0x0118, "int"),
                new FieldSpec("Trade", "m_bPursueTradeTask", 0x011C, "bool"),
                new FieldSpec("Trade", "m_ulPursueTradeTemplID", 0x011D, "uint"),
                new FieldSpec("General", "m_nTopic", 0x0121, "int"),
                new FieldSpec("General", "m_ulCameraMove", 0x0125, "uint"),
                new FieldSpec("Message", "m_bSendMsg", 0x0129, "bool"),
                new FieldSpec("Message", "m_nMsgChannel", 0x012A, "int"),
                new FieldSpec("Terminate", "m_ulTerminateCount", 0x012E, "uint"),
                new FieldSpec("Hierarchy", "m_ulParent", ParentTaskIdOffset, "uint"),
                new FieldSpec("Hierarchy", "m_ulPrevSibling", PrevSiblingTaskIdOffset, "uint"),
                new FieldSpec("Hierarchy", "m_ulNextSibling", NextSiblingTaskIdOffset, "uint"),
                new FieldSpec("Hierarchy", "m_ulFirstChild", FirstChildTaskIdOffset, "uint")
            };
            AddExtendedFixedFieldSpecs(specs);

            foreach (FieldSpec spec in specs)
            {
                string value;
                if (!TryReadField(bytes, spec, includeEmptyArraySlots, out value))
                {
                    continue;
                }

                values.Add(new TaskEditorFieldValue
                {
                    Section = spec.Section,
                    Field = spec.Name,
                    DisplayName = GetFieldDisplayName(spec.Name),
                    Meaning = GetFieldMeaning(spec.Name),
                    Offset = spec.Offset,
                    HexOffset = "0x" + spec.Offset.ToString("X4", CultureInfo.InvariantCulture),
                    Type = spec.Kind,
                    Value = value
                });
            }

            TaskEditorLayout layout;
            if (TryBuildTaskLayout(bytes, out layout))
            {
                AddAwardFields(bytes, values, "Success reward", layout.SuccessAwardOffset, "m_Award_S", includeEmptyArraySlots);
                AddAwardFields(bytes, values, "Fail reward", layout.FailAwardOffset, "m_Award_F", includeEmptyArraySlots);
                if (includeEmptyArraySlots)
                {
                    AddAllValueCollectionMarkers(bytes, values, layout);
                }
            }

            return values;
        }

        public static List<TaskEditorItemValue> BuildItemValues(byte[] bytes)
        {
            List<TaskEditorItemValue> values = new List<TaskEditorItemValue>();
            TaskEditorLayout layout;
            if (!TryBuildTaskLayout(bytes, out layout))
            {
                return values;
            }

            AddItemArray(bytes, values, "Required item", "m_PremItems", layout.PremItemsOffset, layout.PremItemsCount);
            AddItemArray(bytes, values, "Given item", "m_GivenItems", layout.GivenItemsOffset, layout.GivenItemsCount);
            AddItemArray(bytes, values, "Completion item", "m_ItemsWanted", layout.ItemsWantedOffset, layout.ItemsWantedCount);
            AddItemArray(bytes, values, "Submit item", "m_ItemsSubmitWanted", layout.ItemsSubmitWantedOffset, layout.ItemsSubmitWantedCount);
            AddAwardItemValues(bytes, values, "Success reward item", "m_Award_S", layout.SuccessAwardOffset);
            AddAwardItemValues(bytes, values, "Fail reward item", "m_Award_F", layout.FailAwardOffset);
            return values
                .Where(value => value != null && value.ItemId > 0 && value.Count > 0)
                .ToList();
        }

        private static bool TryBuildTaskLayout(byte[] bytes, out TaskEditorLayout layout)
        {
            layout = null;
            if (bytes == null || bytes.Length < FixedTaskDataSize)
            {
                return false;
            }

            int timetableCount = ClampCount(ReadUInt32AsInt(bytes, TimetableCountOffset), MaxTimetableEntries);
            int premItemsCount = ClampCount(ReadUInt32AsInt(bytes, PremItemsCountOffset), MaxItemWantedEntries);
            int givenItemsCount = ClampCount(ReadUInt32AsInt(bytes, GivenItemsCountOffset), MaxItemWantedEntries);
            int premTitleCount = ClampCount(ReadUInt32AsInt(bytes, PremTitleCountOffset), MaxItemWantedEntries);
            int teamMemberCount = ClampCount(ReadUInt32AsInt(bytes, TeamMemberWantedCountOffset), MaxTeamMemberWantedEntries);
            int monsterCount = ClampCount(ReadUInt32AsInt(bytes, MonsterWantedCountOffset), MaxMonsterWantedEntries);
            int itemsWantedCount = ClampCount(ReadUInt32AsInt(bytes, ItemsWantedCountOffset), MaxItemWantedEntries);
            int itemsSubmitWantedCount = ClampCount(ReadUInt32AsInt(bytes, ItemsSubmitWantedCountOffset), MaxItemSubmitEntries);
            int welcomeWordsCount = ClampCount(ReadUInt32AsInt(bytes, WelcomeWordsCountOffset), 65535);

            int cursor = FixedTaskDataSize;
            int signatureOffset = -1;
            if (ReadBool(bytes, HasSignOffset))
            {
                signatureOffset = cursor;
                cursor += TaskNameTextBytes;
            }

            int timetableOffset = cursor;
            cursor += timetableCount * TaskTimeSize * 2;

            int premItemsOffset = cursor;
            cursor += premItemsCount * ItemWantedSize;

            int premTitlesOffset = cursor;
            cursor += premTitleCount * 2;

            int givenItemsOffset = cursor;
            cursor += givenItemsCount * ItemWantedSize;

            int teamMemberOffset = cursor;
            if (ReadBool(bytes, TeamworkOffset))
            {
                cursor += teamMemberCount * TeamMemberWantedSize;
            }

            int monsterWantedOffset = cursor;
            cursor += monsterCount * MonsterWantedSize;

            int gatherMonsterOffset = -1;
            if (ReadBool(bytes, HasGatherMonsterOffset))
            {
                gatherMonsterOffset = cursor;
                cursor += TaskNameTextBytes;
            }

            int finishTaskTimesOffset = -1;
            if (ReadBool(bytes, HasFinishTaskTimesOffset))
            {
                finishTaskTimesOffset = cursor;
                cursor += TaskMethodTextBytes;
            }

            int itemsWantedOffset = cursor;
            cursor += itemsWantedCount * ItemWantedSize;

            int welcomeWordsOffset = cursor;
            cursor += welcomeWordsCount * 2;

            int itemsSubmitWantedOffset = cursor;
            cursor += itemsSubmitWantedCount * ItemWantedSize;

            if (cursor < FixedTaskDataSize || cursor + AwardDataSize * 2 > bytes.Length)
            {
                return false;
            }

            int successAwardOffset = cursor;
            int afterSuccessAward = SkipAwardData(bytes, successAwardOffset);
            if (afterSuccessAward <= successAwardOffset)
            {
                return false;
            }

            int failAwardOffset = afterSuccessAward;
            int afterFailAward = SkipAwardData(bytes, failAwardOffset);
            if (afterFailAward <= failAwardOffset)
            {
                return false;
            }

            layout = new TaskEditorLayout
            {
                PremItemsOffset = premItemsOffset,
                PremItemsCount = premItemsCount,
                PremTitlesOffset = premTitlesOffset,
                PremTitlesCount = premTitleCount,
                GivenItemsOffset = givenItemsOffset,
                GivenItemsCount = givenItemsCount,
                TeamMemberOffset = teamMemberOffset,
                TeamMemberCount = teamMemberCount,
                MonsterWantedOffset = monsterWantedOffset,
                MonsterWantedCount = monsterCount,
                ItemsWantedOffset = itemsWantedOffset,
                ItemsWantedCount = itemsWantedCount,
                ItemsSubmitWantedOffset = itemsSubmitWantedOffset,
                ItemsSubmitWantedCount = itemsSubmitWantedCount,
                SignatureOffset = signatureOffset,
                TimetableOffset = timetableOffset,
                TimetableCount = timetableCount,
                GatherMonsterOffset = gatherMonsterOffset,
                FinishTaskTimesOffset = finishTaskTimesOffset,
                WelcomeWordsOffset = welcomeWordsOffset,
                WelcomeWordsCount = welcomeWordsCount,
                SuccessAwardOffset = successAwardOffset,
                FailAwardOffset = failAwardOffset,
                AfterFailAwardOffset = afterFailAward
            };
            return true;
        }

        private static void AddAllValueCollectionMarkers(byte[] bytes, List<TaskEditorFieldValue> values, TaskEditorLayout layout)
        {
            if (bytes == null || values == null || layout == null)
            {
                return;
            }

            AddMappedField(values, "General", "m_pszSignature", layout.SignatureOffset, "dynamic-wstring", layout.SignatureOffset >= 0 ? ReadTaskName(bytes, layout.SignatureOffset, 30) : string.Empty, "Optional signature string stored after the fixed header.");
            AddMappedField(values, "Timetable", "m_TimeTable", layout.TimetableOffset, "collection:task_tm", layout.TimetableCount.ToString(CultureInfo.InvariantCulture) + " start/end pair(s)", "Dynamic timetable pairs stored after the signature block.");
            AddMappedField(values, "Premise", "m_PremItems", layout.PremItemsOffset, "collection:ITEM_WANTED", layout.PremItemsCount.ToString(CultureInfo.InvariantCulture) + " item(s)", "Dynamic required-item collection.");
            AddMappedField(values, "Premise", "m_PremTitles", layout.PremTitlesOffset, "collection:int16", layout.PremTitlesCount.ToString(CultureInfo.InvariantCulture) + " title(s)", "Dynamic required-title collection.");
            AddMappedField(values, "Given Items", "m_GivenItems", layout.GivenItemsOffset, "collection:ITEM_WANTED", layout.GivenItemsCount.ToString(CultureInfo.InvariantCulture) + " item(s)", "Dynamic item collection granted when the task is accepted.");
            AddMappedField(values, "Team", "m_TeamMemsWanted", layout.TeamMemberOffset, "collection:TEAM_MEM_WANTED", layout.TeamMemberCount.ToString(CultureInfo.InvariantCulture) + " team member rule(s)", "Dynamic team-member requirement collection.");
            AddMappedField(values, "Completion", "m_MonsterWanted", layout.MonsterWantedOffset, "collection:MONSTER_WANTED", layout.MonsterWantedCount.ToString(CultureInfo.InvariantCulture) + " monster rule(s)", "Dynamic monster-kill requirement collection.");
            AddMappedField(values, "Completion", "m_pszGatherMonster", layout.GatherMonsterOffset, "dynamic-wstring", layout.GatherMonsterOffset >= 0 ? ReadTaskName(bytes, layout.GatherMonsterOffset, 30) : string.Empty, "Optional gather-monster display text.");
            AddMappedField(values, "Completion", "m_pszFinishTaskTimes", layout.FinishTaskTimesOffset, "dynamic-wstring", layout.FinishTaskTimesOffset >= 0 ? ReadFixedWideText(bytes, layout.FinishTaskTimesOffset, 64) : string.Empty, "Optional finish-count display text.");
            AddMappedField(values, "Completion", "m_ItemsWanted", layout.ItemsWantedOffset, "collection:ITEM_WANTED", layout.ItemsWantedCount.ToString(CultureInfo.InvariantCulture) + " item(s)", "Dynamic completion-item collection.");
            AddMappedField(values, "Completion", "m_pszWelcomeWords", layout.WelcomeWordsOffset, "dynamic-wstring", layout.WelcomeWordsCount > 0 ? ReadFixedWideText(bytes, layout.WelcomeWordsOffset, layout.WelcomeWordsCount) : string.Empty, "Optional welcome text used by some task flows.");
            AddMappedField(values, "Completion", "m_ItemsSubmitWanted", layout.ItemsSubmitWantedOffset, "collection:ITEM_WANTED", layout.ItemsSubmitWantedCount.ToString(CultureInfo.InvariantCulture) + " item(s)", "Dynamic submit-item collection.");

            AddMappedField(values, "Rewards", "m_Award_S", layout.SuccessAwardOffset, "collection:AWARD_DATA", "(Collection)", "Normal success reward block.");
            AddMappedField(values, "Rewards", "m_Award_F", layout.FailAwardOffset, "collection:AWARD_DATA", "(Collection)", "Normal failure reward block.");
            AddMappedField(values, "Rewards", "m_AwByRatio_S", layout.AfterFailAwardOffset, "collection:AWARD_RATIO_SCALE", "Not expanded yet", "Success reward scale chosen by ratio/time.");
            AddMappedField(values, "Rewards", "m_AwByRatio_F", -1, "collection:AWARD_RATIO_SCALE", "Not expanded yet", "Failure reward scale chosen by ratio/time.");
            AddMappedField(values, "Rewards", "m_AwByItems_S", -1, "collection:AWARD_ITEMS_SCALE", "Not expanded yet", "Success reward scale chosen by item count.");
            AddMappedField(values, "Rewards", "m_AwByItems_F", -1, "collection:AWARD_ITEMS_SCALE", "Not expanded yet", "Failure reward scale chosen by item count.");
            AddMappedField(values, "Rewards", "m_AwByCount_S", -1, "collection:AWARD_COUNT_SCALE", "Not expanded yet", "Success reward scale chosen by finish count.");
            AddMappedField(values, "Rewards", "m_AwByCount_F", -1, "collection:AWARD_COUNT_SCALE", "Not expanded yet", "Failure reward scale chosen by finish count.");
            AddMappedField(values, "Rewards", "m_AwBySubmitItems_S", -1, "collection:AWARD_SUBMIT_ITEMS", "Not expanded yet", "Success reward scale chosen by submitted items.");
            AddMappedField(values, "Rewards", "m_AwBySubmitItems_F", -1, "collection:AWARD_SUBMIT_ITEMS", "Not expanded yet", "Failure reward scale chosen by submitted items.");

            List<TaskEditorTextValue> texts = ExtractUnicodeTexts(bytes);
            AddTextMarker(values, texts, "Texts", "m_pwstrDescript", 0, "Task description text.");
            AddTextMarker(values, texts, "Texts", "m_pwstrOkText", 1, "Positive/acceptance task text.");
            AddTextMarker(values, texts, "Texts", "m_pwstrNoText", 2, "Negative/refusal task text.");
            AddTextMarker(values, texts, "Texts", "m_pwstrTribute", 3, "Tribute or special task text.");
            AddTextMarker(values, texts, "Texts", "m_pwstrItemAwardBroadcast", 4, "Item award broadcast text.");
            AddMappedField(values, "Subtasks", "SubQuestCnt", -1, "dynamic-count", "See task tree", "Subtask count is represented by parent/child/sibling IDs in the task tree.");
            AddMappedField(values, "Subtasks", "SubQuests", -1, "collection:subtask", "See task tree", "Dynamic subtask collection derived from hierarchy links.");
            AddMappedField(values, "Appearance", "NameFontColor", -1, "collection:color", "Not expanded yet", "Name font color collection used by the paid editor.");
            AddMappedField(values, "Appearance", "NameCleanColor", -1, "collection:color", "Not expanded yet", "Clean name color collection used by the paid editor.");
            AddMappedField(values, "Appearance", "ParsedName", -1, "string", string.Empty, "Parsed display name cache used by the paid editor.");
            AddMappedField(values, "Appearance", "TextColor", -1, "collection:color", "Not expanded yet", "Text color collection used by the paid editor.");
        }

        private static void AddTextMarker(List<TaskEditorFieldValue> values, List<TaskEditorTextValue> texts, string section, string fieldName, int index, string meaning)
        {
            TaskEditorTextValue text = texts != null && index >= 0 && index < texts.Count ? texts[index] : null;
            AddMappedField(
                values,
                section,
                fieldName,
                text != null ? text.Offset : -1,
                "dynamic-wstring",
                text != null ? text.Text : string.Empty,
                meaning);
        }

        private static void AddMappedField(List<TaskEditorFieldValue> values, string section, string fieldName, int offset, string type, string value, string meaning)
        {
            values.Add(new TaskEditorFieldValue
            {
                Section = section ?? string.Empty,
                Field = fieldName ?? string.Empty,
                DisplayName = GetFieldDisplayName(fieldName),
                Meaning = meaning ?? GetFieldMeaning(fieldName),
                Offset = offset,
                HexOffset = offset >= 0 ? "0x" + offset.ToString("X4", CultureInfo.InvariantCulture) : string.Empty,
                Type = type ?? string.Empty,
                Value = value ?? string.Empty
            });
        }

        private static int SkipAwardData(byte[] bytes, int awardOffset)
        {
            if (bytes == null || awardOffset < 0 || awardOffset + AwardDataSize > bytes.Length)
            {
                return -1;
            }

            int cursor = awardOffset + AwardDataSize;
            int candidateCount = ClampCount(ReadUInt32AsInt(bytes, awardOffset + AwardItemCandidateCountOffset), MaxAwardItemCandidates);
            for (int i = 0; i < candidateCount; i++)
            {
                if (cursor + 5 > bytes.Length)
                {
                    return -1;
                }

                cursor++;
                int itemCount = ClampCount(ReadUInt32AsInt(bytes, cursor), MaxAwardItems);
                cursor += 4 + itemCount * ItemWantedSize;
            }

            return cursor <= bytes.Length ? cursor : -1;
        }

        private static void AddItemArray(byte[] bytes, List<TaskEditorItemValue> values, string kind, string source, int offset, int count)
        {
            if (bytes == null || values == null || count <= 0 || offset < 0 || offset + count * ItemWantedSize > bytes.Length)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int itemOffset = offset + i * ItemWantedSize;
                TaskEditorItemValue item;
                if (TryReadItemWanted(bytes, itemOffset, kind, source + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", out item))
                {
                    values.Add(item);
                }
            }
        }

        private static void AddAwardItemValues(byte[] bytes, List<TaskEditorItemValue> values, string kind, string source, int awardOffset)
        {
            if (bytes == null || values == null || awardOffset < 0 || awardOffset + AwardDataSize > bytes.Length)
            {
                return;
            }

            int cursor = awardOffset + AwardDataSize;
            int candidateCount = ClampCount(ReadUInt32AsInt(bytes, awardOffset + AwardItemCandidateCountOffset), MaxAwardItemCandidates);
            for (int group = 0; group < candidateCount; group++)
            {
                if (cursor + 5 > bytes.Length)
                {
                    return;
                }

                bool randomChoose = bytes[cursor] != 0;
                cursor++;
                int itemCount = ClampCount(ReadUInt32AsInt(bytes, cursor), MaxAwardItems);
                cursor += 4;
                for (int i = 0; i < itemCount; i++)
                {
                    int itemOffset = cursor + i * ItemWantedSize;
                    string itemSource = source
                        + ".m_CandItems["
                        + group.ToString(CultureInfo.InvariantCulture)
                        + "].m_AwardItems["
                        + i.ToString(CultureInfo.InvariantCulture)
                        + "]";
                    TaskEditorItemValue item;
                    if (TryReadItemWanted(bytes, itemOffset, kind + (randomChoose ? " (random group)" : string.Empty), itemSource, out item))
                    {
                        values.Add(item);
                    }
                }

                cursor += itemCount * ItemWantedSize;
            }
        }

        private static bool TryReadItemWanted(byte[] bytes, int offset, string kind, string source, out TaskEditorItemValue item)
        {
            item = null;
            if (bytes == null || offset < 0 || offset + ItemWantedSize > bytes.Length)
            {
                return false;
            }

            int itemId = ReadUInt32AsInt(bytes, offset);
            int count = ReadUInt32AsInt(bytes, offset + 5);
            if (itemId <= 0 || count <= 0)
            {
                return false;
            }

            item = new TaskEditorItemValue
            {
                Kind = kind,
                Source = source,
                Offset = offset,
                HexOffset = "0x" + offset.ToString("X4", CultureInfo.InvariantCulture),
                ItemId = itemId,
                Count = count,
                CommonItem = bytes[offset + 4] != 0,
                Bind = bytes[offset + 18] != 0,
                Quality = -1
            };
            return true;
        }

        private static void AddExtendedFixedFieldSpecs(List<FieldSpec> specs)
        {
            if (specs == null)
            {
                return;
            }

            specs.Add(new FieldSpec("Terminate", "m_TerminateTask", 0x0132, "uint-array:8"));
            specs.Add(new FieldSpec("Flow", "m_bFinishTask", 0x0152, "bool"));
            specs.Add(new FieldSpec("Monster Control", "m_ulMonCtrlCnt", 0x0153, "uint"));
            specs.Add(new FieldSpec("Monster Control", "m_MonCtrl", 0x0157, "monctrl-array:8"));
            specs.Add(new FieldSpec("Monster Control", "m_bRanMonCtrl", 0x019F, "bool"));
            specs.Add(new FieldSpec("Dungeon", "m_bCreateDungeon", 0x01A0, "bool"));
            specs.Add(new FieldSpec("Dungeon", "m_bEnterDungeon", 0x01A1, "bool"));
            specs.Add(new FieldSpec("Dungeon", "m_iDungeonTemplateID", 0x01A2, "int"));
            specs.Add(new FieldSpec("Emotion", "m_bEmotionTrig", 0x01A6, "bool"));
            specs.Add(new FieldSpec("Emotion", "m_iEmotionTrigID", 0x01A7, "int"));
            specs.Add(new FieldSpec("Message", "m_AIMsg", 0x01AB, "ai-msg"));
            specs.Add(new FieldSpec("Flow", "m_iRetrieveIdx", 0x01B7, "int"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_Lev_Min", 0x01BB, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_Lev_Max", 0x01BF, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bShowByLev", 0x01C3, "bool"));
            specs.Add(new FieldSpec("Premise", "m_i64RegionMask", 0x01C4, "int64"));
            specs.Add(new FieldSpec("Premise", "m_nTalismanValueMin", 0x01CC, "int"));
            specs.Add(new FieldSpec("Premise", "m_nTalismanValueMax", 0x01D0, "int"));
            specs.Add(new FieldSpec("Premise", "m_nIntimacyMin", 0x01D4, "int"));
            specs.Add(new FieldSpec("Premise", "m_nIntimacyMax", 0x01D8, "int"));
            specs.Add(new FieldSpec("Premise", "m_ulPremItems", 0x01DC, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bShowByItems", 0x01E8, "bool"));
            specs.Add(new FieldSpec("Given Items", "m_ulGivenItems", 0x01E9, "uint"));
            specs.Add(new FieldSpec("Given Items", "m_ulGivenCmnCount", 0x01ED, "uint"));
            specs.Add(new FieldSpec("Given Items", "m_ulGivenTskCount", 0x01F1, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulPremTitleCount", 0x0205, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bPremTitleCond", 0x0209, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_Deposit", 0x020A, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bShowByDeposit", 0x020E, "bool"));
            specs.Add(new FieldSpec("Premise", "m_lPremise_Reputation", 0x020F, "int"));
            specs.Add(new FieldSpec("Premise", "m_bRepuDeposit", 0x0213, "bool"));
            specs.Add(new FieldSpec("Premise", "m_bShowByRepu", 0x0214, "bool"));
            specs.Add(new FieldSpec("Premise", "m_nPremise_Vigour", 0x0215, "int"));
            specs.Add(new FieldSpec("Premise", "m_bVigourDeposit", 0x0219, "bool"));
            specs.Add(new FieldSpec("Premise", "m_nPremise_Vitality", 0x021A, "int"));
            specs.Add(new FieldSpec("Premise", "m_bVitalityDeposit", 0x021E, "bool"));
            specs.Add(new FieldSpec("Premise", "m_nPremise_InteractionPoints", 0x021F, "int"));
            specs.Add(new FieldSpec("Premise", "m_bInteractionPointsDeposit", 0x0223, "bool"));
            specs.Add(new FieldSpec("Premise", "m_lPremise_Contribution", 0x0224, "int"));
            specs.Add(new FieldSpec("Premise", "m_bDepositContribution", 0x0228, "bool"));
            specs.Add(new FieldSpec("Premise", "m_nPremise_FamContrib", 0x0229, "int"));
            specs.Add(new FieldSpec("Premise", "m_nPremFamContribMax", 0x022D, "int"));
            specs.Add(new FieldSpec("Premise", "m_bDepositFamContrib", 0x0231, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_FactionMoney", 0x0232, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bFactionMoneyDeposit", 0x0236, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_FacMelee", 0x0237, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bFacMeleeDeposit", 0x023B, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_FacMagic", 0x023C, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bFacMagicDeposit", 0x0240, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_FacEnconomy", 0x0241, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bFacEnconomyDeposit", 0x0245, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_FacCulture", 0x0246, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bFacCultureDeposit", 0x024A, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_FacBelief", 0x024B, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bFacBeliefDeposit", 0x024F, "bool"));
            specs.Add(new FieldSpec("Premise", "m_iPremise_FacResElse", 0x0250, "int-array:10"));
            specs.Add(new FieldSpec("Premise", "m_bFacResElseDeposit", 0x0278, "bool-array:10"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_FacCredit", 0x0282, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bFacCreditDeposit", 0x0286, "bool"));
            specs.Add(new FieldSpec("Premise", "m_bShowByFacCredit", 0x0287, "bool"));
            specs.Add(new FieldSpec("Premise", "m_i64Premise_GoldenValue", 0x0288, "int64"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_AchievementCount", 0x0290, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_AchievementPoint", 0x0294, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bAchievementPointDeposit", 0x0298, "bool"));
            specs.Add(new FieldSpec("Premise", "m_nPremBattleScoreMin", 0x0299, "int"));
            specs.Add(new FieldSpec("Premise", "m_nPremBattleScoreMax", 0x029D, "int"));
            specs.Add(new FieldSpec("Premise", "m_bDepositBattleScore", 0x02A1, "bool"));
            specs.Add(new FieldSpec("Premise", "m_Premise_FriendshipMin", 0x02A2, "int-array:64"));
            specs.Add(new FieldSpec("Premise", "m_Premise_FriendshipMax", 0x03A2, "int-array:64"));
            specs.Add(new FieldSpec("Premise", "m_bPremFriendshipCond", 0x04A2, "bool"));
            specs.Add(new FieldSpec("Premise", "m_bFriendshipDeposit", 0x04A3, "bool"));
            specs.Add(new FieldSpec("Premise", "m_Premise_Mastery", 0x04A4, "uint-array:8"));
            specs.Add(new FieldSpec("Premise", "m_Premise_Resistance", 0x04C4, "uint-array:8"));
            specs.Add(new FieldSpec("Premise", "m_bPremise_MRCond", 0x04E4, "bool"));
            specs.Add(new FieldSpec("Premise", "m_Premise_SpecialCounterMin", 0x04E5, "int-array:32"));
            specs.Add(new FieldSpec("Premise", "m_Premise_SpecialCounterMax", 0x0565, "int-array:32"));
            specs.Add(new FieldSpec("Premise", "m_bPremSpecialCounterCond", 0x05E5, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_Task_Count", 0x05E6, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_Tasks", 0x05EA, "uint-array:32"));
            specs.Add(new FieldSpec("Premise", "m_bShowByPreTask", 0x066A, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremFinishTaskCount", 0x066B, "uint"));
            specs.Add(new FieldSpec("Premise", "m_PremFinishTasks", 0x066F, "finish-task-count-array:32"));
            specs.Add(new FieldSpec("Premise", "m_ulPremGlobalCount", 0x072F, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulPremGlobalTask", 0x0733, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_Period", 0x0737, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bShowByPeriod", 0x073B, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_Faction", 0x073C, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_Faction_Max", 0x0740, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bShowByFaction", 0x0744, "bool"));
            specs.Add(new FieldSpec("Premise", "m_bPremise_Master", 0x0745, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulGender", 0x0746, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bShowByGender", 0x074A, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulOccupations", 0x074B, "uint"));
            specs.Add(new FieldSpec("Premise", "m_Occupations", 0x074F, "uint-array:32"));
            specs.Add(new FieldSpec("Premise", "m_bShowByOccup", 0x07CF, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulRaces", 0x07D0, "uint"));
            specs.Add(new FieldSpec("Premise", "m_Races", 0x07D4, "uint-array:8"));
            specs.Add(new FieldSpec("Premise", "m_bPremise_Spouse", 0x07F4, "bool"));
            specs.Add(new FieldSpec("Premise", "m_bShowBySpouse", 0x07F5, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_Cotask_Cnt", 0x07F6, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulPremise_Cotask", 0x07FA, "uint-array:4"));
            specs.Add(new FieldSpec("Premise", "m_ulCoTaskCond", 0x080A, "uint"));
            specs.Add(new FieldSpec("Premise", "m_bShowByCoTask", 0x080E, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulMutexTaskCount", 0x080F, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulMutexTasks", 0x0813, "uint-array:32"));
            specs.Add(new FieldSpec("Premise", "m_nMutexType", 0x0893, "int-array:32"));
            specs.Add(new FieldSpec("Premise", "m_lSkillLev", 0x0913, "int-array:32"));
            specs.Add(new FieldSpec("Premise", "m_lSkillPro", 0x0993, "int-array:32"));
            specs.Add(new FieldSpec("Premise", "m_DynTaskType", 0x0A13, "byte"));
            specs.Add(new FieldSpec("Premise", "m_ulSpecialAward", 0x0A14, "uint"));
            specs.Add(new FieldSpec("Premise", "m_lPKValueMin", 0x0A18, "int"));
            specs.Add(new FieldSpec("Premise", "m_lPKValueMax", 0x0A1C, "int"));
            specs.Add(new FieldSpec("Flags", "m_bPremise_GM", 0x0A20, "bool"));
            specs.Add(new FieldSpec("Flags", "m_bPremise_CallPet", 0x0A21, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulChangedRace", 0x0A22, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulChangedVisualize", 0x0A26, "uint"));
            specs.Add(new FieldSpec("Capture", "m_ulCapTaskID", 0x0A2A, "uint"));
            specs.Add(new FieldSpec("Capture", "m_ulCapTemplAddr", 0x0A2E, "uint"));
            specs.Add(new FieldSpec("Team", "m_bTeamwork", 0x0A32, "bool"));
            specs.Add(new FieldSpec("Team", "m_bRaidTeam", 0x0A33, "bool"));
            specs.Add(new FieldSpec("Team", "m_bRcvByTeam", 0x0A34, "bool"));
            specs.Add(new FieldSpec("Team", "m_bSharedTask", 0x0A35, "bool"));
            specs.Add(new FieldSpec("Team", "m_bSharedAchieved", 0x0A36, "bool"));
            specs.Add(new FieldSpec("Team", "m_bCheckTeammate", 0x0A37, "bool"));
            specs.Add(new FieldSpec("Team", "m_fTeammateDist", 0x0A38, "float"));
            specs.Add(new FieldSpec("Team", "m_bAllFail", 0x0A3C, "bool"));
            specs.Add(new FieldSpec("Team", "m_bCapFail", 0x0A3D, "bool"));
            specs.Add(new FieldSpec("Team", "m_bCapSucc", 0x0A3E, "bool"));
            specs.Add(new FieldSpec("Team", "m_fSuccDist", 0x0A3F, "float"));
            specs.Add(new FieldSpec("Team", "m_bDismAsSelfFail", 0x0A43, "bool"));
            specs.Add(new FieldSpec("Team", "m_bRcvChckMem", 0x0A44, "bool"));
            specs.Add(new FieldSpec("Team", "m_fRcvMemDist", 0x0A45, "float"));
            specs.Add(new FieldSpec("Team", "m_bCntByMemPos", 0x0A49, "bool"));
            specs.Add(new FieldSpec("Team", "m_fCntMemDist", 0x0A4A, "float"));
            specs.Add(new FieldSpec("Team", "m_ulTeamMemsWanted", 0x0A4E, "uint"));
            specs.Add(new FieldSpec("Team", "m_bShowByTeam", 0x0A5A, "bool"));
            specs.Add(new FieldSpec("Team", "m_bDeadShared", 0x0A5B, "bool"));
            specs.Add(new FieldSpec("Team", "m_bIsMaster", 0x0A5C, "bool"));
            specs.Add(new FieldSpec("Family", "m_bInFamily", 0x0A5D, "bool"));
            specs.Add(new FieldSpec("Family", "m_bFamilyHeader", 0x0A5E, "bool"));
            specs.Add(new FieldSpec("Family", "m_nFamilySkillLevelMin", 0x0A5F, "int"));
            specs.Add(new FieldSpec("Family", "m_nFamilySkillLevelMax", 0x0A63, "int"));
            specs.Add(new FieldSpec("Family", "m_nFamilySkillProficiencyMin", 0x0A67, "int"));
            specs.Add(new FieldSpec("Family", "m_nFamilySkillProficiencyMax", 0x0A6B, "int"));
            specs.Add(new FieldSpec("Family", "m_nFamilySkillIndex", 0x0A6F, "int"));
            specs.Add(new FieldSpec("Family", "m_nFamilyMonRecordIndex", 0x0A73, "int"));
            specs.Add(new FieldSpec("Family", "m_nFamilyMonRecordMin", 0x0A77, "int"));
            specs.Add(new FieldSpec("Family", "m_nFamilyMonRecordMax", 0x0A7B, "int"));
            specs.Add(new FieldSpec("Family", "m_nFamilyValueIndex", 0x0A7F, "int"));
            specs.Add(new FieldSpec("Family", "m_bDepositFamilyValue", 0x0A83, "bool"));
            specs.Add(new FieldSpec("Family", "m_nFamilyValueMin", 0x0A84, "int"));
            specs.Add(new FieldSpec("Family", "m_nFamilyValueMax", 0x0A88, "int"));
            specs.Add(new FieldSpec("Premise", "m_PremKeyValue", 0x0A8C, "compare-key-value"));
            specs.Add(new FieldSpec("Premise", "m_ulPremSkillCnt", 0x0AA1, "uint"));
            specs.Add(new FieldSpec("Premise", "m_PremSkill", 0x0AA5, "prem-skill-array:8"));
            specs.Add(new FieldSpec("Premise", "m_bPremSkillCond", 0x0AED, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulPremBuffCnt", 0x0AEE, "uint"));
            specs.Add(new FieldSpec("Premise", "m_PremBuff", 0x0AF2, "prem-skill-array:8"));
            specs.Add(new FieldSpec("Premise", "m_bPremBuffCond", 0x0B3A, "bool"));
            specs.Add(new FieldSpec("Premise", "m_ulSpecKnow", 0x0B3B, "uint"));
            specs.Add(new FieldSpec("Premise", "m_ulPremAccompCnt", 0x0B3F, "uint"));
            specs.Add(new FieldSpec("Premise", "m_aPremAccompID", 0x0B43, "uint-array:8"));
            specs.Add(new FieldSpec("Premise", "m_bPremAccompCond", 0x0B63, "bool"));
            specs.Add(new FieldSpec("Premise", "m_iBindMoney", 0x0B64, "int"));
            specs.Add(new FieldSpec("Premise", "m_bShowByBindMoney", 0x0B68, "bool"));
            specs.Add(new FieldSpec("Premise", "m_arrSideOccupation", 0x0B69, "int-array:64"));
            specs.Add(new FieldSpec("Premise", "m_arrSideOccupMax", 0x0C69, "int-array:64"));
            specs.Add(new FieldSpec("Premise", "m_arrSideOccupActive", 0x0D69, "bool-array:64"));
            specs.Add(new FieldSpec("Premise", "m_bDiscoverMap", 0x0DA9, "bool"));
            specs.Add(new FieldSpec("Premise", "m_lDiscoverMapId", 0x0DAA, "int"));
            specs.Add(new FieldSpec("Premise", "m_iPremImpression", 0x0DAE, "int"));
            specs.Add(new FieldSpec("Premise", "m_iPremPrayValue", 0x0DB2, "int"));
            specs.Add(new FieldSpec("Premise", "m_iFlyLevel", 0x0DB6, "int"));
            specs.Add(new FieldSpec("Premise", "m_bFlyTimeFull", 0x0DBA, "bool"));
            specs.Add(new FieldSpec("Premise", "m_iBelief", 0x0DBB, "int"));
            specs.Add(new FieldSpec("Premise", "m_bBeliefDeposit", 0x0DBF, "bool"));
            specs.Add(new FieldSpec("Premise", "m_iMinGodLevel", 0x0DC0, "int"));
            specs.Add(new FieldSpec("Premise", "m_iMaxGodLevel", 0x0DC4, "int"));
            specs.Add(new FieldSpec("Premise", "m_iMinEvilLevel", 0x0DC8, "int"));
            specs.Add(new FieldSpec("Premise", "m_iMaxEvilLevel", 0x0DCC, "int"));
            specs.Add(new FieldSpec("Premise", "m_bGodEvilCond", 0x0DD0, "bool"));
            specs.Add(new FieldSpec("Premise", "m_lCompletedUserAccountFlag", 0x0DD1, "int"));
            specs.Add(new FieldSpec("Premise", "m_cVipLevel", 0x0DD5, "byte"));
            specs.Add(new FieldSpec("Premise", "m_ulConsumeScore", 0x0DD6, "uint"));
            specs.Add(new FieldSpec("Premise", "m_iServerType", 0x0DDA, "int"));
            specs.Add(new FieldSpec("Premise", "m_iMarriage", 0x0DDE, "int"));
            specs.Add(new FieldSpec("Completion", "m_enumMethod", 0x0DE2, "int"));
            specs.Add(new FieldSpec("Completion", "m_enumFinishType", 0x0DE6, "int"));
            specs.Add(new FieldSpec("Completion", "m_ulMonsterWanted", 0x0DEA, "uint"));
            specs.Add(new FieldSpec("Completion", "m_bHasGatherMonster", 0x0DFE, "bool"));
            specs.Add(new FieldSpec("Completion", "m_ulGatherMonsterNum", 0x0DFF, "uint"));
            specs.Add(new FieldSpec("Completion", "m_bGatherItem", 0x0E03, "bool"));
            specs.Add(new FieldSpec("Completion", "m_ulGatherItemNum", 0x0E04, "uint"));
            specs.Add(new FieldSpec("Completion", "m_bKillMonsterPack", 0x0E08, "bool"));
            specs.Add(new FieldSpec("Completion", "m_iKillMonsterPackID", 0x0E09, "int"));
            specs.Add(new FieldSpec("Completion", "m_iKillMonsterPackNum", 0x0E0D, "int"));
            specs.Add(new FieldSpec("Completion", "m_ulItemsWanted", 0x0E11, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulGoldWanted", 0x0E1D, "uint"));
            specs.Add(new FieldSpec("Completion", "m_bNotGiveMine", 0x0E21, "bool"));
            specs.Add(new FieldSpec("Completion", "m_bNotClearCommonItem", 0x0E22, "bool"));
            specs.Add(new FieldSpec("Completion", "m_ulNPCToProtect", 0x0E23, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulProtectTimeLen", 0x0E27, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulNPCMoving", 0x0E2B, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulNPCDestSite", 0x0E2F, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ReachSiteMin", 0x0E33, "zone"));
            specs.Add(new FieldSpec("Completion", "m_ReachSiteMax", 0x0E3F, "zone"));
            specs.Add(new FieldSpec("Completion", "m_ulReachSiteId", 0x0E4B, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulWaitTime", 0x0E4F, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulTitleWantedNum", 0x0E53, "uint"));
            specs.Add(new FieldSpec("Completion", "m_TitleWanted", 0x0E57, "uint16-array:32"));
            specs.Add(new FieldSpec("Completion", "m_ulFinishTaskID", 0x0E97, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulFinishTaskTimes", 0x0E9B, "uint"));
            specs.Add(new FieldSpec("Completion", "m_bHasFinishTaskTimes", 0x0E9F, "bool"));
            specs.Add(new FieldSpec("Completion", "m_ulFinishLev", 0x0EA8, "uint"));
            specs.Add(new FieldSpec("Completion", "m_bShowByFinLev", 0x0EAC, "bool"));
            specs.Add(new FieldSpec("Completion", "m_ulFinChangedRace", 0x0EAD, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulFinChangedVisualize", 0x0EB1, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulItemsSubmitWanted", 0x0EB5, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulItemTypeFalse", 0x0EC1, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulItemCountFalse", 0x0EC5, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulWelcomeWords", 0x0EC9, "uint"));
            specs.Add(new FieldSpec("Completion", "m_ulFinBuffCnt", 0x0ED5, "uint"));
            specs.Add(new FieldSpec("Completion", "m_FinBuff", 0x0ED9, "prem-skill-array:8"));
            specs.Add(new FieldSpec("Completion", "m_bFinBuffCond", 0x0F21, "bool"));
            specs.Add(new FieldSpec("Completion", "m_FinKeyValue", 0x0F22, "compare-key-value"));
            specs.Add(new FieldSpec("Completion", "m_iFinServerType", 0x0F37, "int"));
            specs.Add(new FieldSpec("Completion", "m_iTMMineID", 0x0F3B, "int"));
            specs.Add(new FieldSpec("Completion", "m_iTMMiningTimes", 0x0F3F, "int"));
            specs.Add(new FieldSpec("Completion", "m_fTMMiningProb", 0x0F43, "float"));
            specs.Add(new FieldSpec("Completion", "m_iTMUseItemID", 0x0F47, "int"));
            specs.Add(new FieldSpec("Completion", "m_iTMUseItemTimes", 0x0F4B, "int"));
            specs.Add(new FieldSpec("Completion", "m_fTMUseItemProb", 0x0F4F, "float"));
            specs.Add(new FieldSpec("Completion", "m_iTMDungeonID", 0x0F53, "int"));
            specs.Add(new FieldSpec("Completion", "m_KillPlayerSiteMin", 0x0F57, "zone"));
            specs.Add(new FieldSpec("Completion", "m_KillPlayerSiteMax", 0x0F63, "zone"));
            specs.Add(new FieldSpec("Completion", "m_ulKillPlayerSiteId", 0x0F6F, "uint"));
            specs.Add(new FieldSpec("Completion", "m_iTMKillPlayerTimes", 0x0F73, "int"));
            specs.Add(new FieldSpec("Completion", "m_fTMKillPlayerProb", 0x0F77, "float"));
            specs.Add(new FieldSpec("Completion", "m_iTMAchievementID", 0x0F7B, "int"));
            specs.Add(new FieldSpec("Completion", "m_iTMAchievementTimes", 0x0F7F, "int"));
            specs.Add(new FieldSpec("Completion", "m_fTMAchievementProb", 0x0F83, "float"));
            specs.Add(new FieldSpec("Completion", "m_iTMDoEmotionID", 0x0F87, "int"));
            specs.Add(new FieldSpec("Completion", "m_iTMDoEmotionTimes", 0x0F8B, "int"));
            specs.Add(new FieldSpec("Completion", "m_fTMDoEmotionProb", 0x0F8F, "float"));
            specs.Add(new FieldSpec("Completion", "m_iPVPWinTimes", 0x0F93, "int"));
            specs.Add(new FieldSpec("Completion", "m_iPVPFailTimes", 0x0F97, "int"));
            specs.Add(new FieldSpec("Completion", "m_iPVPFinTimes", 0x0F9B, "int"));
            specs.Add(new FieldSpec("Rewards", "m_ulAwardType_S", 0x0F9F, "uint"));
            specs.Add(new FieldSpec("Rewards", "m_ulAwardType_F", 0x0FA3, "uint"));
        }

        private static void AddAwardFields(byte[] bytes, List<TaskEditorFieldValue> values, string section, int awardStartOffset, string sourcePrefix, bool includeEmptyAwardFields)
        {
            if (bytes == null || values == null || awardStartOffset < 0 || awardStartOffset + AwardDataSize > bytes.Length)
            {
                return;
            }

            AwardFieldSpec[] specs =
            {
                new AwardFieldSpec("m_ulGoldNum", "Gold", 0x0000, "uint", "Coins awarded when the task is completed."),
                new AwardFieldSpec("m_bGoldRevise", "m_bGoldRevise", 0x0004, "bool", ""),
                new AwardFieldSpec("m_lGoldReviseLev", "m_lGoldReviseLev", 0x0005, "int", ""),
                new AwardFieldSpec("m_ulExp", "Experience", 0x0009, "uint", "Character experience awarded on completion."),
                new AwardFieldSpec("m_bExpRevise", "m_bExpRevise", 0x000D, "bool", ""),
                new AwardFieldSpec("m_lExpReviseLev", "m_lExpReviseLev", 0x000E, "int", ""),
                new AwardFieldSpec("m_bExpFix", "m_bExpFix", 0x0012, "bool", ""),
                new AwardFieldSpec("m_ulPetExp", "Pet experience", 0x0013, "uint", "Pet experience awarded on completion."),
                new AwardFieldSpec("m_ulSecOccpExp", "Secondary occupation EXP", 0x0017, "uint", "Secondary occupation experience awarded on completion."),
                new AwardFieldSpec("m_ulNewTask", "Starts task", 0x001B, "uint", "Task ID started by this reward."),
                new AwardFieldSpec("m_ulTerminateTaskCnt", "m_ulTerminateTaskCnt", 0x001F, "uint", ""),
                new AwardFieldSpec("m_ulTerminateTask", "m_ulTerminateTask", 0x0023, "uint-array:8", ""),
                new AwardFieldSpec("m_ulSP", "Soul power", 0x0043, "uint", "SP/soul-power style reward."),
                new AwardFieldSpec("m_ulReputation", "Reputation", 0x0047, "uint", "Reputation awarded by the task."),
                new AwardFieldSpec("m_lContribution", "Faction contribution", 0x004B, "uint", "Faction contribution awarded by the task."),
                new AwardFieldSpec("m_ulFactionMoney", "Faction money", 0x004F, "uint", "Faction money resource awarded."),
                new AwardFieldSpec("m_ulFactionMelee", "Faction melee resource", 0x0053, "uint", "Faction melee resource awarded."),
                new AwardFieldSpec("m_ulFactionMagic", "Faction magic resource", 0x0057, "uint", "Faction magic resource awarded."),
                new AwardFieldSpec("m_ulFactionEnconomy", "Faction economy resource", 0x005B, "uint", "Faction economy resource awarded."),
                new AwardFieldSpec("m_ulFactionCulture", "Faction culture resource", 0x005F, "uint", "Faction culture resource awarded."),
                new AwardFieldSpec("m_ulFactionBelief", "Faction belief resource", 0x0063, "uint", "Faction belief resource awarded."),
                new AwardFieldSpec("m_ulFactionCredit", "Faction credit", 0x0067, "uint", "Faction credit awarded."),
                new AwardFieldSpec("m_ulFactionVitality", "Faction vitality", 0x006B, "uint", "Faction vitality awarded."),
                new AwardFieldSpec("m_iFactionResElse", "m_iFactionResElse", 0x006F, "int-array:10", ""),
                new AwardFieldSpec("m_iFactionReserveFund", "m_iFactionReserveFund", 0x0097, "int", ""),
                new AwardFieldSpec("m_ulMastery", "m_ulMastery", 0x009B, "uint-array:8", ""),
                new AwardFieldSpec("m_ulResistance", "m_ulResistance", 0x00BB, "uint-array:8", ""),
                new AwardFieldSpec("m_ulMasteryExp", "m_ulMasteryExp", 0x00DB, "uint-array:8", ""),
                new AwardFieldSpec("m_ulResistanceExp", "m_ulResistanceExp", 0x00FB, "uint-array:8", ""),
                new AwardFieldSpec("m_nFamContrib", "Family contribution", 0x011B, "int", "Family contribution awarded."),
                new AwardFieldSpec("m_ulProsperity", "Prosperity", 0x011F, "uint", "Prosperity awarded."),
                new AwardFieldSpec("m_ulTitleCnt", "m_ulTitleCnt", 0x0123, "uint", ""),
                new AwardFieldSpec("m_Title", "m_Title", 0x0127, "award-title-array:8", ""),
                new AwardFieldSpec("m_bRandomTitle", "m_bRandomTitle", 0x0167, "bool", ""),
                new AwardFieldSpec("m_lPKValue", "PK value", 0x0168, "int", "PK value adjustment awarded by the task."),
                new AwardFieldSpec("m_ulVitality", "Vitality", 0x016C, "uint", "Vitality awarded."),
                new AwardFieldSpec("m_ulVigour", "Vigour", 0x0170, "uint", "Vigour awarded."),
                new AwardFieldSpec("m_bResetPKValue", "m_bResetPKValue", 0x0174, "bool", ""),
                new AwardFieldSpec("m_bDivorce", "m_bDivorce", 0x0175, "bool", ""),
                new AwardFieldSpec("m_aFriendships", "m_aFriendships", 0x0176, "int-array:64", ""),
                new AwardFieldSpec("m_aSpecialCounter", "m_aSpecialCounter", 0x0276, "int-array:32", ""),
                new AwardFieldSpec("m_ulNewPeriod", "Unlocks period", 0x02F6, "uint", "New period unlocked by this reward."),
                new AwardFieldSpec("m_ulNewRelayStation", "Unlocks relay station", 0x02FA, "uint", "Relay station unlocked by this reward."),
                new AwardFieldSpec("m_ulStorehouseSize", "Storage slots", 0x02FE, "uint", "Personal storage slots awarded."),
                new AwardFieldSpec("m_ulFactionStorehouseSize", "Faction storage slots", 0x0302, "uint", "Faction storage slots awarded."),
                new AwardFieldSpec("m_lInventorySize", "Bag slots", 0x0306, "int", "Inventory slots awarded."),
                new AwardFieldSpec("m_ulPetInventorySize", "Pet bag slots", 0x030A, "uint", "Pet inventory slots awarded."),
                new AwardFieldSpec("m_ulPetCallInvSize", "Pet call slots", 0x030E, "uint", "Pet call slots awarded."),
                new AwardFieldSpec("m_ulPetCombInvSize", "Pet combine slots", 0x0312, "uint", "Pet combine slots awarded."),
                new AwardFieldSpec("m_ulPetHatchInvSize", "Pet hatch slots", 0x0316, "uint", "Pet hatch slots awarded."),
                new AwardFieldSpec("m_ulPetIncubatorInvSize", "Pet incubator slots", 0x031A, "uint", "Pet incubator slots awarded."),
                new AwardFieldSpec("m_ulBusinessInvSize", "Business bag slots", 0x031E, "uint", "Business inventory slots awarded."),
                new AwardFieldSpec("m_ulFuryULimit", "Fury limit", 0x0322, "uint", "Fury limit increase awarded."),
                new AwardFieldSpec("m_bSetProduceSkill", "m_bSetProduceSkill", 0x0326, "bool", ""),
                new AwardFieldSpec("m_nSkillType", "m_nSkillType", 0x0327, "int", ""),
                new AwardFieldSpec("m_ulProduceSkillExp", "m_ulProduceSkillExp", 0x032B, "uint-array:32", ""),
                new AwardFieldSpec("m_ulNewProfession", "m_ulNewProfession", 0x03AB, "uint", ""),
                new AwardFieldSpec("m_ulTransWldId", "m_ulTransWldId", 0x03AF, "uint", ""),
                new AwardFieldSpec("m_TransPt", "m_TransPt", 0x03B3, "zone", ""),
                new AwardFieldSpec("m_ulTransBackWldId", "m_ulTransBackWldId", 0x03BF, "uint", ""),
                new AwardFieldSpec("m_TransBackPt", "m_TransBackPt", 0x03C3, "zone", ""),
                new AwardFieldSpec("m_lMonsCtrl", "m_lMonsCtrl", 0x03CF, "int", ""),
                new AwardFieldSpec("m_bTrigCtrl", "m_bTrigCtrl", 0x03D3, "bool", ""),
                new AwardFieldSpec("m_lBuffId", "Buff ID", 0x03D4, "int", "Buff applied by this reward."),
                new AwardFieldSpec("m_lBuffLev", "Buff level", 0x03D8, "int", "Level of the buff applied by this reward."),
                new AwardFieldSpec("m_nFamilySkillProficiency", "m_nFamilySkillProficiency", 0x03DC, "int", ""),
                new AwardFieldSpec("m_nFamilySkillLevel", "m_nFamilySkillLevel", 0x03E0, "int", ""),
                new AwardFieldSpec("m_nFamilySkillIndex", "m_nFamilySkillIndex", 0x03E4, "int", ""),
                new AwardFieldSpec("m_nFamilyMonRecordIndex", "m_nFamilyMonRecordIndex", 0x03E8, "int", ""),
                new AwardFieldSpec("m_nFamilyValueIndex", "m_nFamilyValueIndex", 0x03EC, "int", ""),
                new AwardFieldSpec("m_nFamilyValue", "m_nFamilyValue", 0x03F0, "int", ""),
                new AwardFieldSpec("m_bSendMsg", "m_bSendMsg", 0x03F4, "bool", ""),
                new AwardFieldSpec("m_nMsgChannel", "m_nMsgChannel", 0x03F5, "int", ""),
                new AwardFieldSpec("m_ulClearCountTaskCnt", "m_ulClearCountTaskCnt", 0x03F9, "uint", ""),
                new AwardFieldSpec("m_ulClearCountTask", "m_ulClearCountTask", 0x03FD, "uint-array:8", ""),
                new AwardFieldSpec("m_ulClearTaskRecCnt", "m_ulClearTaskRecCnt", 0x041D, "uint", ""),
                new AwardFieldSpec("m_ulClearTaskRec", "m_ulClearTaskRec", 0x0421, "uint-array:8", ""),
                new AwardFieldSpec("m_ulDoubleExpTime", "m_ulDoubleExpTime", 0x0441, "uint", ""),
                new AwardFieldSpec("m_iModifyCountTaskCnt", "m_iModifyCountTaskCnt", 0x0445, "int", ""),
                new AwardFieldSpec("m_ulModifyCountTask", "m_ulModifyCountTask", 0x0449, "uint-array:8", ""),
                new AwardFieldSpec("m_iModifyCount", "m_iModifyCount", 0x0469, "int-array:8", ""),
                new AwardFieldSpec("m_ulClearDeliverTimeTaskCnt", "m_ulClearDeliverTimeTaskCnt", 0x0489, "uint", ""),
                new AwardFieldSpec("m_ulClearDeliverTimeTask", "m_ulClearDeliverTimeTask", 0x048D, "uint-array:8", ""),
                new AwardFieldSpec("m_ulCandItems", "Item reward groups", 0x04AD, "uint", "Number of item reward groups stored after AWARD_DATA."),
                new AwardFieldSpec("m_CandItems", "m_CandItems", 0x04B5, "collection:AWARD_ITEMS_CAND", ""),
                new AwardFieldSpec("m_ulChangeKeyCnt", "m_ulChangeKeyCnt", 0x04B9, "uint", ""),
                new AwardFieldSpec("m_plChangeKey", "m_plChangeKey", 0x04C1, "collection:int", ""),
                new AwardFieldSpec("m_plChangeKeyValue", "m_plChangeKeyValue", 0x04C9, "collection:int", ""),
                new AwardFieldSpec("m_pbChangeType", "m_pbChangeType", 0x04D1, "collection:bool", ""),
                new AwardFieldSpec("m_ulDisplayKeyCnt", "m_ulDisplayKeyCnt", 0x04D5, "uint", ""),
                new AwardFieldSpec("m_plDisplayKey", "m_plDisplayKey", 0x04DD, "collection:int", ""),
                new AwardFieldSpec("m_bMulti", "m_bMulti", 0x04E1, "bool", ""),
                new AwardFieldSpec("m_nNumType", "m_nNumType", 0x04E2, "int", ""),
                new AwardFieldSpec("m_lNum", "m_lNum", 0x04E6, "int", ""),
                new AwardFieldSpec("m_lSkillID", "m_lSkillID", 0x04EA, "int", ""),
                new AwardFieldSpec("m_lSkillLev", "m_lSkillLev", 0x04EE, "int", ""),
                new AwardFieldSpec("m_lDelSkillID", "m_lDelSkillID", 0x04F2, "int", ""),
                new AwardFieldSpec("m_ulMonCtrlCnt", "m_ulMonCtrlCnt", 0x04F6, "uint", ""),
                new AwardFieldSpec("m_MonCtrl", "m_MonCtrl", 0x04FA, "monctrl-array:8", ""),
                new AwardFieldSpec("m_bRanMonCtrl", "m_bRanMonCtrl", 0x0542, "bool", ""),
                new AwardFieldSpec("m_ulProSkillSel", "m_ulProSkillSel", 0x0543, "uint", ""),
                new AwardFieldSpec("m_bAwardSpecifyRole", "m_bAwardSpecifyRole", 0x0547, "bool", ""),
                new AwardFieldSpec("m_ulRoleSelected", "m_ulRoleSelected", 0x0548, "uint", ""),
                new AwardFieldSpec("m_fAwardSpecifyRoleDis", "m_fAwardSpecifyRoleDis", 0x054C, "float", ""),
                new AwardFieldSpec("m_ulExpAlgo", "m_ulExpAlgo", 0x0550, "int", ""),
                new AwardFieldSpec("m_ulFriendshipAlgo", "m_ulFriendshipAlgo", 0x0554, "int", ""),
                new AwardFieldSpec("m_ulBindMoneyAlgo", "m_ulBindMoneyAlgo", 0x0558, "int", ""),
                new AwardFieldSpec("m_bCheckIP", "m_bCheckIP", 0x055C, "bool", ""),
                new AwardFieldSpec("m_pAwardSpecifyRole", "m_pAwardSpecifyRole", 0x0561, "collection:AWARD_DATA", ""),
                new AwardFieldSpec("m_bTeamMulti", "m_bTeamMulti", 0x0565, "bool", ""),
                new AwardFieldSpec("m_ulTeamMultiCnt", "m_ulTeamMultiCnt", 0x0566, "uint", ""),
                new AwardFieldSpec("m_TeamMulti", "m_TeamMulti", 0x056A, "team-multi-array:6", ""),
                new AwardFieldSpec("m_ulTeamMultiAwardSel", "m_ulTeamMultiAwardSel", 0x059A, "uint", ""),
                new AwardFieldSpec("m_ulCameraMove", "m_ulCameraMove", 0x059E, "uint", ""),
                new AwardFieldSpec("m_ulFollowTask", "m_ulFollowTask", 0x05A2, "uint", ""),
                new AwardFieldSpec("m_iBindMoney", "Bound money", 0x05A6, "int", "Bound money awarded."),
                new AwardFieldSpec("m_64MaskLearnSideOccup", "m_64MaskLearnSideOccup", 0x05AA, "int64", ""),
                new AwardFieldSpec("m_iSideOccupExp", "m_iSideOccupExp", 0x05B2, "int-array:64", ""),
                new AwardFieldSpec("m_iSideOccupPoints", "m_iSideOccupPoints", 0x06B2, "int", ""),
                new AwardFieldSpec("m_bDiscoverMap", "m_bDiscoverMap", 0x06B6, "bool", ""),
                new AwardFieldSpec("m_lDiscoverMapId", "m_lDiscoverMapId", 0x06B7, "int", ""),
                new AwardFieldSpec("m_iMineType", "m_iMineType", 0x06BB, "int", ""),
                new AwardFieldSpec("m_bMineShow", "m_bMineShow", 0x06BF, "bool", ""),
                new AwardFieldSpec("m_iLearnEmotion", "m_iLearnEmotion", 0x06C0, "int", ""),
                new AwardFieldSpec("m_nIntimacy", "m_nIntimacy", 0x06C4, "int", ""),
                new AwardFieldSpec("m_nSectCap", "m_nSectCap", 0x06C8, "int", ""),
                new AwardFieldSpec("m_bIsGrad", "m_bIsGrad", 0x06CC, "bool", ""),
                new AwardFieldSpec("m_nImpression", "m_nImpression", 0x06CD, "int", ""),
                new AwardFieldSpec("m_AIMsg", "m_AIMsg", 0x06D1, "ai-msg", ""),
                new AwardFieldSpec("m_bMineProtect", "m_bMineProtect", 0x06DD, "bool", ""),
                new AwardFieldSpec("m_bMineDestroy", "m_bMineDestroy", 0x06DE, "bool", ""),
                new AwardFieldSpec("m_bUpFlyLevel", "m_bUpFlyLevel", 0x06DF, "bool", ""),
                new AwardFieldSpec("m_iBelief", "m_iBelief", 0x06E0, "int", ""),
                new AwardFieldSpec("m_bIgnoreBeliefLimit", "m_bIgnoreBeliefLimit", 0x06E4, "bool", ""),
                new AwardFieldSpec("m_ulBindCash", "Bound cash", 0x06E5, "uint", "Bound cash awarded."),
                new AwardFieldSpec("m_cAwardVipLevel", "VIP level", 0x06E9, "byte", "VIP level awarded or changed."),
                new AwardFieldSpec("m_ulVipBonusTemplid", "VIP bonus template", 0x06EA, "uint", "VIP bonus template applied by this reward."),
                new AwardFieldSpec("m_uiMasteryPoint", "Mastery point", 0x06EE, "uint", "Mastery points awarded."),
                new AwardFieldSpec("m_uiResistancePoint", "Resistance point", 0x06F2, "uint", "Resistance points awarded."),
                new AwardFieldSpec("m_bChangeDS", "m_bChangeDS", 0x06F6, "bool", ""),
                new AwardFieldSpec("m_i64GoldenValue", "Golden value", 0x06F7, "int64", "Golden-value reward."),
                new AwardFieldSpec("m_i64GoldenValueToKingdom", "Kingdom golden value", 0x06FF, "int64", "Golden value delivered to kingdom/guild context."),
                new AwardFieldSpec("m_iChariotID", "m_iChariotID", 0x0707, "int", ""),
                new AwardFieldSpec("m_bLeaveChariot", "m_bLeaveChariot", 0x070B, "bool", ""),
                new AwardFieldSpec("m_iRandomGift", "Random gift", 0x070C, "int", "Random gift template awarded.")
            };

            foreach (AwardFieldSpec spec in specs)
            {
                if (!includeEmptyAwardFields && (spec.Name == "m_iSideOccupExp" || spec.Name == "m_iSideOccupPoints"))
                {
                    continue;
                }

                string value;
                if (!TryReadAwardField(bytes, awardStartOffset, spec, includeEmptyAwardFields, out value) || (!includeEmptyAwardFields && IsZeroAwardValue(value)))
                {
                    continue;
                }

                int offset = awardStartOffset + spec.Offset;
                values.Add(new TaskEditorFieldValue
                {
                    Section = section,
                    Field = sourcePrefix + "." + spec.Name,
                    DisplayName = spec.DisplayName,
                    Meaning = spec.Meaning,
                    Offset = offset,
                    HexOffset = "0x" + offset.ToString("X4", CultureInfo.InvariantCulture),
                    Type = spec.Kind,
                    Value = value
                });
            }

            if (includeEmptyAwardFields)
            {
                return;
            }

            for (int i = 0; i < AwardSideOccupationExpCount; i++)
            {
                int offset = awardStartOffset + AwardSideOccupationExpOffset + i * 4;
                int points = BitConverter.ToInt32(bytes, offset);
                if (!includeEmptyAwardFields && points == 0)
                {
                    continue;
                }

                string pointName = GetReleasePointRewardName(i);
                values.Add(new TaskEditorFieldValue
                {
                    Section = section,
                    Field = sourcePrefix + ".m_iSideOccupExp[" + i.ToString(CultureInfo.InvariantCulture) + "]",
                    DisplayName = pointName,
                    Meaning = "Adds value to " + pointName + " when this reward is delivered.",
                    Offset = offset,
                    HexOffset = "0x" + offset.ToString("X4", CultureInfo.InvariantCulture),
                    Type = "int",
                    Value = points.ToString(CultureInfo.InvariantCulture)
                });
            }

            int sideOccupationPoints = BitConverter.ToInt32(bytes, awardStartOffset + AwardSideOccupationPointsOffset);
            if (includeEmptyAwardFields || sideOccupationPoints != 0)
            {
                int offset = awardStartOffset + AwardSideOccupationPointsOffset;
                values.Add(new TaskEditorFieldValue
                {
                    Section = section,
                    Field = sourcePrefix + ".m_iSideOccupPoints",
                    DisplayName = "Side occupation points",
                    Meaning = "Adds generic side-occupation points when this reward is delivered.",
                    Offset = offset,
                    HexOffset = "0x" + offset.ToString("X4", CultureInfo.InvariantCulture),
                    Type = "int",
                    Value = sideOccupationPoints.ToString(CultureInfo.InvariantCulture)
                });
            }
        }

        private static bool TryReadAwardField(byte[] bytes, int awardStartOffset, AwardFieldSpec spec, bool includeEmptyArraySlots, out string value)
        {
            value = string.Empty;
            if (bytes == null || spec == null)
            {
                return false;
            }

            int offset = awardStartOffset + spec.Offset;
            if (offset < 0 || offset >= bytes.Length)
            {
                return false;
            }

            if (spec.Kind.StartsWith("collection:", StringComparison.Ordinal))
            {
                value = "(Collection)";
                return true;
            }
            if (spec.Kind.StartsWith("award-title-array:", StringComparison.Ordinal))
            {
                return TryReadAwardTitleArray(bytes, offset, spec.Kind, includeEmptyArraySlots, out value);
            }
            if (spec.Kind.StartsWith("team-multi-array:", StringComparison.Ordinal))
            {
                return TryReadTeamMultiArray(bytes, offset, spec.Kind, includeEmptyArraySlots, out value);
            }

            FieldSpec fieldSpec = new FieldSpec(string.Empty, spec.Name, offset, spec.Kind);
            return TryReadField(bytes, fieldSpec, includeEmptyArraySlots, out value);
        }

        private static bool IsZeroAwardValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)
                || string.Equals(value, "No", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "None", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "(Collection)", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (value.StartsWith("Object 0, Param1 0, Param2 0", StringComparison.Ordinal))
            {
                return true;
            }

            if (value.StartsWith("X 0, Y 0, Z 0", StringComparison.Ordinal))
            {
                return true;
            }

            long number;
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) && number == 0;
        }

        private static string GetReleasePointRewardName(int index)
        {
            string[] names =
            {
                "Slayer Reputation",
                "Triumph Reputation",
                "Call of Dawn",
                "Bounty Reputation",
                "Champion Reputation",
                "Arena",
                "Arena Dedication",
                "3v3 Arena Score",
                "6v6 Arena Score",
                "Arena Reputation",
                "Luck",
                "Mentor",
                "Social Contribution",
                "Cruelty",
                "Bounty Glory",
                "Kindness",
                "Courage",
                "Master Reputation",
                "Frostgale Fjord",
                "Frostgale Fjord Battlefield Comment",
                "Lionheart Champions Reputation",
                "Frostgale Fjord Total Points",
                "Sanguine Circle Reputation",
                "Union of the Woods Reputation",
                "Mercury Union Reputation",
                "Hell Acclaim",
                "Rose",
                "Season 1 3v3 Score",
                "Season 1 6v6 Score",
                "Companion Points",
                "Blessing of Antus",
                "Unused / Empty",
                "Promoter Points",
                "Valor",
                "Warlord Points",
                "Hell",
                "Fealty",
                "Touch Points",
                "Flower Score"
            };

            return index >= 0 && index < names.Length
                ? names[index]
                : "Release slot " + index.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryReadField(byte[] bytes, FieldSpec spec, bool includeEmptyArraySlots, out string value)
        {
            value = string.Empty;
            if (bytes == null || spec == null || spec.Offset < 0 || spec.Offset >= bytes.Length)
            {
                return false;
            }

            if (spec.Kind == "bool")
            {
                value = bytes[spec.Offset] == 0 ? "No" : "Yes";
                return true;
            }
            if (spec.Kind == "uint")
            {
                if (spec.Offset + 4 > bytes.Length) return false;
                value = BitConverter.ToUInt32(bytes, spec.Offset).ToString(CultureInfo.InvariantCulture);
                return true;
            }
            if (spec.Kind == "int")
            {
                if (spec.Offset + 4 > bytes.Length) return false;
                value = BitConverter.ToInt32(bytes, spec.Offset).ToString(CultureInfo.InvariantCulture);
                return true;
            }
            if (spec.Kind == "int64")
            {
                if (spec.Offset + 8 > bytes.Length) return false;
                value = BitConverter.ToInt64(bytes, spec.Offset).ToString(CultureInfo.InvariantCulture);
                return true;
            }
            if (spec.Kind == "byte")
            {
                value = bytes[spec.Offset].ToString(CultureInfo.InvariantCulture);
                return true;
            }
            if (spec.Kind.StartsWith("byte-array:", StringComparison.Ordinal))
            {
                return TryReadByteArray(bytes, spec.Offset, spec.Kind, "byte-array:", includeEmptyArraySlots, out value);
            }
            if (spec.Kind == "uint16")
            {
                if (spec.Offset + 2 > bytes.Length) return false;
                value = BitConverter.ToUInt16(bytes, spec.Offset).ToString(CultureInfo.InvariantCulture);
                return true;
            }
            if (spec.Kind == "float")
            {
                if (spec.Offset + 4 > bytes.Length) return false;
                value = BitConverter.ToSingle(bytes, spec.Offset).ToString("R", CultureInfo.InvariantCulture);
                return true;
            }
            if (spec.Kind == "zone")
            {
                if (spec.Offset + 12 > bytes.Length) return false;
                value = string.Format(
                    CultureInfo.InvariantCulture,
                    "X {0:R}, Y {1:R}, Z {2:R}",
                    BitConverter.ToSingle(bytes, spec.Offset),
                    BitConverter.ToSingle(bytes, spec.Offset + 4),
                    BitConverter.ToSingle(bytes, spec.Offset + 8));
                return true;
            }
            if (spec.Kind == "task_tm")
            {
                if (spec.Offset + 24 > bytes.Length) return false;
                value = string.Format(
                    CultureInfo.InvariantCulture,
                    "Y {0}, M {1}, D {2}, H {3}, M {4}, W {5}",
                    BitConverter.ToInt32(bytes, spec.Offset),
                    BitConverter.ToInt32(bytes, spec.Offset + 4),
                    BitConverter.ToInt32(bytes, spec.Offset + 8),
                    BitConverter.ToInt32(bytes, spec.Offset + 12),
                    BitConverter.ToInt32(bytes, spec.Offset + 16),
                    BitConverter.ToInt32(bytes, spec.Offset + 20));
                return true;
            }
            if (spec.Kind.StartsWith("wstring:", StringComparison.Ordinal))
            {
                int charCount;
                if (!int.TryParse(spec.Kind.Substring("wstring:".Length), out charCount))
                {
                    return false;
                }
                value = ReadTaskName(bytes, spec.Offset, charCount);
                return true;
            }
            if (spec.Kind.StartsWith("uint-array:", StringComparison.Ordinal))
            {
                return TryReadUInt32Array(bytes, spec.Offset, spec.Kind, "uint-array:", includeEmptyArraySlots, out value);
            }
            if (spec.Kind.StartsWith("int-array:", StringComparison.Ordinal))
            {
                return TryReadInt32Array(bytes, spec.Offset, spec.Kind, "int-array:", includeEmptyArraySlots, out value);
            }
            if (spec.Kind.StartsWith("uint16-array:", StringComparison.Ordinal))
            {
                return TryReadUInt16Array(bytes, spec.Offset, spec.Kind, "uint16-array:", includeEmptyArraySlots, out value);
            }
            if (spec.Kind.StartsWith("bool-array:", StringComparison.Ordinal))
            {
                return TryReadBoolArray(bytes, spec.Offset, spec.Kind, "bool-array:", includeEmptyArraySlots, out value);
            }
            if (spec.Kind.StartsWith("finish-task-count-array:", StringComparison.Ordinal))
            {
                return TryReadFinishTaskCountArray(bytes, spec.Offset, spec.Kind, includeEmptyArraySlots, out value);
            }
            if (spec.Kind.StartsWith("prem-skill-array:", StringComparison.Ordinal))
            {
                return TryReadIdLevelFlagArray(bytes, spec.Offset, spec.Kind, "prem-skill-array:", includeEmptyArraySlots, out value);
            }
            if (spec.Kind.StartsWith("monctrl-array:", StringComparison.Ordinal))
            {
                return TryReadMonsterControlArray(bytes, spec.Offset, spec.Kind, includeEmptyArraySlots, out value);
            }
            if (spec.Kind == "ai-msg")
            {
                if (spec.Offset + 12 > bytes.Length) return false;
                value = string.Format(
                    CultureInfo.InvariantCulture,
                    "Object {0}, Param1 {1}, Param2 {2}",
                    BitConverter.ToInt32(bytes, spec.Offset),
                    BitConverter.ToInt32(bytes, spec.Offset + 4),
                    BitConverter.ToInt32(bytes, spec.Offset + 8));
                return true;
            }
            if (spec.Kind == "compare-key-value")
            {
                if (spec.Offset + 21 > bytes.Length) return false;
                value = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}; Left type {1} value {2}; Operator {3}; Right type {4} value {5}",
                    bytes[spec.Offset] == 0 ? "Disabled" : "Enabled",
                    BitConverter.ToInt32(bytes, spec.Offset + 1),
                    BitConverter.ToInt32(bytes, spec.Offset + 5),
                    BitConverter.ToInt32(bytes, spec.Offset + 9),
                    BitConverter.ToInt32(bytes, spec.Offset + 13),
                    BitConverter.ToInt32(bytes, spec.Offset + 17));
                return true;
            }

            return false;
        }

        private static bool TryReadUInt32Array(byte[] bytes, int offset, string kind, string prefix, bool includeEmptySlots, out string value)
        {
            value = string.Empty;
            int count;
            if (!TryParseArrayCount(kind, prefix, out count) || bytes == null || offset < 0 || offset + count * 4 > bytes.Length)
            {
                return false;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                uint item = BitConverter.ToUInt32(bytes, offset + i * 4);
                if (includeEmptySlots || item != 0)
                {
                    parts.Add("[" + i.ToString(CultureInfo.InvariantCulture) + "] " + item.ToString(CultureInfo.InvariantCulture));
                }
            }

            value = parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "None";
            return true;
        }

        private static bool TryReadByteArray(byte[] bytes, int offset, string kind, string prefix, bool includeEmptySlots, out string value)
        {
            value = string.Empty;
            int count;
            if (!TryParseArrayCount(kind, prefix, out count) || bytes == null || offset < 0 || offset + count > bytes.Length)
            {
                return false;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                byte item = bytes[offset + i];
                if (includeEmptySlots || item != 0)
                {
                    parts.Add("[" + i.ToString(CultureInfo.InvariantCulture) + "] " + item.ToString(CultureInfo.InvariantCulture));
                }
            }

            value = parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "None";
            return true;
        }

        private static bool TryReadInt32Array(byte[] bytes, int offset, string kind, string prefix, bool includeEmptySlots, out string value)
        {
            value = string.Empty;
            int count;
            if (!TryParseArrayCount(kind, prefix, out count) || bytes == null || offset < 0 || offset + count * 4 > bytes.Length)
            {
                return false;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int item = BitConverter.ToInt32(bytes, offset + i * 4);
                if (includeEmptySlots || item != 0)
                {
                    parts.Add("[" + i.ToString(CultureInfo.InvariantCulture) + "] " + item.ToString(CultureInfo.InvariantCulture));
                }
            }

            value = parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "None";
            return true;
        }

        private static bool TryReadUInt16Array(byte[] bytes, int offset, string kind, string prefix, bool includeEmptySlots, out string value)
        {
            value = string.Empty;
            int count;
            if (!TryParseArrayCount(kind, prefix, out count) || bytes == null || offset < 0 || offset + count * 2 > bytes.Length)
            {
                return false;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                ushort item = BitConverter.ToUInt16(bytes, offset + i * 2);
                if (includeEmptySlots || item != 0)
                {
                    parts.Add("[" + i.ToString(CultureInfo.InvariantCulture) + "] " + item.ToString(CultureInfo.InvariantCulture));
                }
            }

            value = parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "None";
            return true;
        }

        private static bool TryReadBoolArray(byte[] bytes, int offset, string kind, string prefix, bool includeEmptySlots, out string value)
        {
            value = string.Empty;
            int count;
            if (!TryParseArrayCount(kind, prefix, out count) || bytes == null || offset < 0 || offset + count > bytes.Length)
            {
                return false;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                bool item = bytes[offset + i] != 0;
                if (includeEmptySlots || item)
                {
                    parts.Add("[" + i.ToString(CultureInfo.InvariantCulture) + "] " + (item ? "Yes" : "No"));
                }
            }

            value = parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "None";
            return true;
        }

        private static bool TryReadFinishTaskCountArray(byte[] bytes, int offset, string kind, bool includeEmptySlots, out string value)
        {
            value = string.Empty;
            int count;
            if (!TryParseArrayCount(kind, "finish-task-count-array:", out count) || bytes == null || offset < 0 || offset + count * 6 > bytes.Length)
            {
                return false;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int itemOffset = offset + i * 6;
                uint taskId = BitConverter.ToUInt32(bytes, itemOffset);
                ushort finishCount = BitConverter.ToUInt16(bytes, itemOffset + 4);
                if (includeEmptySlots || taskId != 0 || finishCount != 0)
                {
                    parts.Add("[" + i.ToString(CultureInfo.InvariantCulture) + "] Task " + taskId.ToString(CultureInfo.InvariantCulture) + " x" + finishCount.ToString(CultureInfo.InvariantCulture));
                }
            }

            value = parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "None";
            return true;
        }

        private static bool TryReadIdLevelFlagArray(byte[] bytes, int offset, string kind, string prefix, bool includeEmptySlots, out string value)
        {
            value = string.Empty;
            int count;
            if (!TryParseArrayCount(kind, prefix, out count) || bytes == null || offset < 0 || offset + count * 9 > bytes.Length)
            {
                return false;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int itemOffset = offset + i * 9;
                int id = BitConverter.ToInt32(bytes, itemOffset);
                int level = BitConverter.ToInt32(bytes, itemOffset + 4);
                bool enabled = bytes[itemOffset + 8] != 0;
                if (includeEmptySlots || id != 0 || level != 0 || enabled)
                {
                    parts.Add("[" + i.ToString(CultureInfo.InvariantCulture) + "] ID " + id.ToString(CultureInfo.InvariantCulture) + ", level " + level.ToString(CultureInfo.InvariantCulture) + ", " + (enabled ? "enabled" : "disabled"));
                }
            }

            value = parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "None";
            return true;
        }

        private static bool TryReadMonsterControlArray(byte[] bytes, int offset, string kind, bool includeEmptySlots, out string value)
        {
            value = string.Empty;
            int count;
            if (!TryParseArrayCount(kind, "monctrl-array:", out count) || bytes == null || offset < 0 || offset + count * 9 > bytes.Length)
            {
                return false;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int itemOffset = offset + i * 9;
                int id = BitConverter.ToInt32(bytes, itemOffset);
                float probability = BitConverter.ToSingle(bytes, itemOffset + 4);
                bool open = bytes[itemOffset + 8] != 0;
                if (includeEmptySlots || id != 0 || probability != 0F || open)
                {
                    parts.Add("[" + i.ToString(CultureInfo.InvariantCulture) + "] ID " + id.ToString(CultureInfo.InvariantCulture) + ", prob " + probability.ToString("R", CultureInfo.InvariantCulture) + ", " + (open ? "open" : "closed"));
                }
            }

            value = parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "None";
            return true;
        }

        private static bool TryReadAwardTitleArray(byte[] bytes, int offset, string kind, bool includeEmptySlots, out string value)
        {
            value = string.Empty;
            int count;
            if (!TryParseArrayCount(kind, "award-title-array:", out count) || bytes == null || offset < 0 || offset + count * 8 > bytes.Length)
            {
                return false;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int itemOffset = offset + i * 8;
                int titleId = BitConverter.ToInt32(bytes, itemOffset);
                float probability = BitConverter.ToSingle(bytes, itemOffset + 4);
                if (includeEmptySlots || titleId != 0 || probability != 0F)
                {
                    parts.Add("[" + i.ToString(CultureInfo.InvariantCulture) + "] Title " + titleId.ToString(CultureInfo.InvariantCulture) + ", prob " + probability.ToString("R", CultureInfo.InvariantCulture));
                }
            }

            value = parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "None";
            return true;
        }

        private static bool TryReadTeamMultiArray(byte[] bytes, int offset, string kind, bool includeEmptySlots, out string value)
        {
            value = string.Empty;
            int count;
            if (!TryParseArrayCount(kind, "team-multi-array:", out count) || bytes == null || offset < 0 || offset + count * 8 > bytes.Length)
            {
                return false;
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int itemOffset = offset + i * 8;
                uint memberCount = BitConverter.ToUInt32(bytes, itemOffset);
                float probability = BitConverter.ToSingle(bytes, itemOffset + 4);
                if (includeEmptySlots || memberCount != 0 || probability != 0F)
                {
                    parts.Add("[" + i.ToString(CultureInfo.InvariantCulture) + "] Members " + memberCount.ToString(CultureInfo.InvariantCulture) + ", prob " + probability.ToString("R", CultureInfo.InvariantCulture));
                }
            }

            value = parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "None";
            return true;
        }

        private static bool TryParseArrayCount(string kind, string prefix, out int count)
        {
            count = 0;
            if (string.IsNullOrWhiteSpace(kind) || !kind.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            return int.TryParse(kind.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out count)
                && count >= 0
                && count < 1024;
        }

        private static string GetFieldDisplayName(string fieldName)
        {
            switch (fieldName)
            {
                case "m_ID": return "Task ID";
                case "m_szName": return "Task name";
                case "m_bHidden": return "Hidden";
                case "m_bOffLineIsFail": return "Fails on logout";
                case "m_bHasSign": return "Has signature";
                case "m_pszSignature": return "Signature text";
                case "m_ulType": return "Task type";
                case "m_ulTimeLimit": return "Time limit";
                case "m_bAbsTime": return "Absolute time";
                case "m_ulTimetable": return "Timetable";
                case "m_tmType": return "Timetable types";
                case "m_TimeTable": return "Timetable entries";
                case "m_lAvailFrequency": return "Available times";
                case "m_lTimeInterval": return "Cooldown";
                case "m_bBirthday": return "Birthday only";
                case "m_bBuild": return "Build task";
                case "m_bNoExpMakeUp": return "No EXP makeup";
                case "m_bChooseOne": return "Choose one child";
                case "m_bRandOne": return "Random child";
                case "m_bExeChildInOrder": return "Children in order";
                case "m_bParentAlsoFail": return "Parent fails with child";
                case "m_bParentAlsoSucc": return "Parent succeeds with child";
                case "m_bCanGiveUp": return "Can give up";
                case "m_bCanRedo": return "Repeatable";
                case "m_bCanRedoAfterFailure": return "Repeat after failure";
                case "m_bClearAsGiveUp": return "Clear as give up";
                case "m_bUIButtonTask": return "UI button task";
                case "m_bNeedRecord": return "Requires record";
                case "m_bFailAsPlayerDie": return "Fails on death";
                case "m_ulMaxReceiver": return "Receiver limit";
                case "m_bDelvInZone": return "Start in zone";
                case "m_ulDelvWorld": return "Start world";
                case "m_DelvMinVert": return "Start zone min";
                case "m_DelvMaxVert": return "Start zone max";
                case "m_bOutZoneFail": return "Fail outside zone";
                case "m_ulOutZoneWorldID": return "Outside-zone world";
                case "m_OutZoneMinVert": return "Outside-zone min";
                case "m_OutZoneMaxVert": return "Outside-zone max";
                case "m_bTransTo": return "Teleport on delivery";
                case "m_ulTransWldId": return "Teleport world";
                case "m_TransPt": return "Teleport point";
                case "m_bAutoDeliver": return "Auto deliver";
                case "m_bDeathTrig": return "Death trigger";
                case "m_bClearAcquired": return "Clear acquired items";
                case "m_ulSuitableLevel": return "Recommended level";
                case "m_bSuitLevelEx": return "Exact level gate";
                case "m_bShowPrompt": return "Show prompt";
                case "m_bKeyTask": return "Key task";
                case "m_bLuaTask": return "Lua task";
                case "m_ulDelvNPC": return "Start NPC";
                case "m_ulAwardNPC": return "Finish NPC";
                case "m_bSkillTask": return "Living skill task";
                case "m_bCanSeekOut": return "Can track/search";
                case "m_bShowDirection": return "Show direction";
                case "m_fStorageWeight": return "Storage weight";
                case "m_ulRank": return "Rank";
                case "m_bMarriage": return "Marriage task";
                case "m_bSharedByFamily": return "Shared by family";
                case "m_bRecFinishCount": return "Records finish count";
                case "m_bRecFinishCountGlobal": return "Global finish count";
                case "m_ulMaxFinishCount": return "Max finishes";
                case "m_FinishClearTime": return "Finish counter reset";
                case "m_nFinishTimeType": return "Finish reset type";
                case "m_bPursueTradeTask": return "Trade route task";
                case "m_ulPursueTradeTemplID": return "Trade route template";
                case "m_nTopic": return "Topic";
                case "m_ulCameraMove": return "Camera move";
                case "m_bSendMsg": return "Sends message";
                case "m_nMsgChannel": return "Message channel";
                case "m_ulTerminateCount": return "Terminate count";
                case "m_TerminateTask": return "Tasks terminated by this task";
                case "m_bFinishTask": return "Finish task flag";
                case "m_ulMonCtrlCnt": return "Monster controller count";
                case "m_MonCtrl": return "Monster controllers";
                case "m_bRanMonCtrl": return "Random monster controller";
                case "m_bCreateDungeon": return "Creates dungeon";
                case "m_bEnterDungeon": return "Enters dungeon";
                case "m_iDungeonTemplateID": return "Dungeon template";
                case "m_bEmotionTrig": return "Emotion trigger";
                case "m_iEmotionTrigID": return "Emotion trigger ID";
                case "m_AIMsg": return "AI message";
                case "m_iRetrieveIdx": return "Retrieve index";
                case "m_ulPremise_Lev_Min": return "Required level min";
                case "m_ulPremise_Lev_Max": return "Required level max";
                case "m_bShowByLev": return "Show by level";
                case "m_i64RegionMask": return "Region mask";
                case "m_nTalismanValueMin": return "Talisman value min";
                case "m_nTalismanValueMax": return "Talisman value max";
                case "m_nIntimacyMin": return "Intimacy min";
                case "m_nIntimacyMax": return "Intimacy max";
                case "m_ulPremItems": return "Required item count";
                case "m_bShowByItems": return "Show by items";
                case "m_ulGivenItems": return "Given item count";
                case "m_ulGivenCmnCount": return "Given common items";
                case "m_ulGivenTskCount": return "Given task items";
                case "m_ulPremTitleCount": return "Required title count";
                case "m_bPremTitleCond": return "Title condition";
                case "m_ulPremise_Deposit": return "Required deposited money";
                case "m_bShowByDeposit": return "Show by deposit";
                case "m_lPremise_Reputation": return "Required reputation";
                case "m_bRepuDeposit": return "Deposit reputation";
                case "m_bShowByRepu": return "Show by reputation";
                case "m_nPremise_Vigour": return "Required vigor";
                case "m_bVigourDeposit": return "Deposit vigor";
                case "m_nPremise_Vitality": return "Required vitality";
                case "m_bVitalityDeposit": return "Deposit vitality";
                case "m_nPremise_InteractionPoints": return "Required interaction points";
                case "m_bInteractionPointsDeposit": return "Deposit interaction points";
                case "m_lPremise_Contribution": return "Required contribution";
                case "m_bDepositContribution": return "Deposit contribution";
                case "m_nPremise_FamContrib": return "Family contribution min";
                case "m_nPremFamContribMax": return "Family contribution max";
                case "m_bDepositFamContrib": return "Deposit family contribution";
                case "m_ulPremise_FactionMoney": return "Required faction money";
                case "m_bFactionMoneyDeposit": return "Deposit faction money";
                case "m_ulPremise_FacMelee": return "Required faction melee";
                case "m_bFacMeleeDeposit": return "Deposit faction melee";
                case "m_ulPremise_FacMagic": return "Required faction magic";
                case "m_bFacMagicDeposit": return "Deposit faction magic";
                case "m_ulPremise_FacEnconomy": return "Required faction economy";
                case "m_bFacEnconomyDeposit": return "Deposit faction economy";
                case "m_ulPremise_FacCulture": return "Required faction culture";
                case "m_bFacCultureDeposit": return "Deposit faction culture";
                case "m_ulPremise_FacBelief": return "Required faction belief";
                case "m_bFacBeliefDeposit": return "Deposit faction belief";
                case "m_iPremise_FacResElse": return "Faction resource requirements";
                case "m_bFacResElseDeposit": return "Deposit faction resources";
                case "m_ulPremise_FacCredit": return "Required faction credit";
                case "m_bFacCreditDeposit": return "Deposit faction credit";
                case "m_bShowByFacCredit": return "Show by faction credit";
                case "m_i64Premise_GoldenValue": return "Required golden value";
                case "m_ulPremise_AchievementCount": return "Required achievement count";
                case "m_ulPremise_AchievementPoint": return "Required achievement points";
                case "m_bAchievementPointDeposit": return "Deposit achievement points";
                case "m_nPremBattleScoreMin": return "Battle score min";
                case "m_nPremBattleScoreMax": return "Battle score max";
                case "m_bDepositBattleScore": return "Deposit battle score";
                case "m_Premise_FriendshipMin": return "Friendship minimums";
                case "m_Premise_FriendshipMax": return "Friendship maximums";
                case "m_bPremFriendshipCond": return "Friendship condition";
                case "m_bFriendshipDeposit": return "Deposit friendship";
                case "m_Premise_Mastery": return "Required mastery";
                case "m_Premise_Resistance": return "Required resistance";
                case "m_bPremise_MRCond": return "Mastery/resistance condition";
                case "m_Premise_SpecialCounterMin": return "Special counter min";
                case "m_Premise_SpecialCounterMax": return "Special counter max";
                case "m_bPremSpecialCounterCond": return "Special counter condition";
                case "m_ulPremise_Task_Count": return "Prerequisite task count";
                case "m_ulPremise_Tasks": return "Prerequisite task IDs";
                case "m_bShowByTask": return "Show by prerequisite tasks";
                case "m_ulPremFinishTaskCount": return "Finish-count prerequisite count";
                case "m_PremFinishTasks": return "Finish-count prerequisites";
                case "m_ulPremGlobalCount": return "Global task count";
                case "m_ulPremGlobalTask": return "Global task ID";
                case "m_ulPremise_Period": return "Required period";
                case "m_bShowByPeriod": return "Show by period";
                case "m_ulPremise_Faction": return "Required faction";
                case "m_ulPremise_Faction_Max": return "Required faction max";
                case "m_bShowByFaction": return "Show by faction";
                case "m_bPremise_Master": return "Must be master";
                case "m_ulGender": return "Required gender";
                case "m_bShowByGender": return "Show by gender";
                case "m_ulOccupations": return "Required occupation count";
                case "m_Occupations": return "Required occupations";
                case "m_bShowByOccup": return "Show by occupation";
                case "m_ulRaces": return "Required race count";
                case "m_Races": return "Required races";
                case "m_bPremise_Spouse": return "Requires spouse";
                case "m_bShowBySpouse": return "Show by spouse";
                case "m_ulPremise_Cotask_Cnt": return "Co-task count";
                case "m_ulPremise_Cotask": return "Co-task IDs";
                case "m_ulCoTaskCond": return "Co-task condition";
                case "m_bShowByCoTask": return "Show by co-task";
                case "m_ulMutexTaskCount": return "Mutex task count";
                case "m_ulMutexTasks": return "Mutex task IDs";
                case "m_nMutexType": return "Mutex task types";
                case "m_lSkillLev": return "Required living skill levels";
                case "m_lSkillPro": return "Required living skill proficiency";
                case "m_DynTaskType": return "Dynamic task type";
                case "m_SpecialAward": return "Special award";
                case "m_lPKValueMin": return "PK value min";
                case "m_lPKValueMax": return "PK value max";
                case "m_bPremise_GM": return "GM only";
                case "m_bPremise_CallPet": return "Call pet premise";
                case "m_ulChangedRace": return "Changed race";
                case "m_ulChangedVisualize": return "Changed visual";
                case "m_ulCapTaskID": return "Capture task ID";
                case "m_ulCapTemplAddr": return "Capture task template address";
                case "m_bTeamwork": return "Teamwork";
                case "m_bRaidTeam": return "Raid team";
                case "m_bRcvByTeam": return "Receive by team";
                case "m_bSharedTask": return "Shared task";
                case "m_bSharedAchieved": return "Shared achievement";
                case "m_bCheckTeammate": return "Check teammate";
                case "m_fTeammateDist": return "Teammate distance";
                case "m_bAllFail": return "All fail";
                case "m_bCapFail": return "Captain fail";
                case "m_bCapSucc": return "Captain success";
                case "m_fSuccDist": return "Success distance";
                case "m_bDismAsSelfFail": return "Dismiss counts as self fail";
                case "m_bRcvChckMem": return "Check receiving member";
                case "m_fRcvMemDist": return "Receiving member distance";
                case "m_bCntByMemPos": return "Count by member position";
                case "m_fCntMemDist": return "Member count distance";
                case "m_ulTeamMemsWanted": return "Required team members";
                case "m_bShowByTeam": return "Show by team";
                case "m_bDeadShared": return "Death shared";
                case "m_bIsMaster": return "Must be leader";
                case "m_bInFamily": return "Must be in family";
                case "m_bFamilyHeader": return "Must be family leader";
                case "m_nFamilySkillLevelMin": return "Family skill level min";
                case "m_nFamilySkillLevelMax": return "Family skill level max";
                case "m_nFamilySkillProficiencyMin": return "Family skill proficiency min";
                case "m_nFamilySkillProficiencyMax": return "Family skill proficiency max";
                case "m_nFamilySkillIndex": return "Family skill index";
                case "m_nFamilyMonRecordIndex": return "Family monster record index";
                case "m_ulFamilyMonsterRecordMin": return "Family monster record min";
                case "m_ulFamilyMonsterRecordMax": return "Family monster record max";
                case "m_ulFamilyValueIndex": return "Family value index";
                case "m_bDepositFamilyValue": return "Deposit family value";
                case "m_iFamilyValueMin": return "Family value min";
                case "m_iFamilyValueMax": return "Family value max";
                case "m_PremKeyValue": return "Required key/value";
                case "m_ulPremSkillCount": return "Required skill count";
                case "m_PremSkills": return "Required skills";
                case "m_bShowBySkill": return "Show by skill";
                case "m_ulPremBuffCount": return "Required buff count";
                case "m_PremBuffs": return "Required buffs";
                case "m_bShowByBuff": return "Show by buff";
                case "m_ulSpecKnow": return "Required special knowledge";
                case "m_ulPremAccompCnt": return "Required accompany count";
                case "m_aPremAccompID": return "Required accompany IDs";
                case "m_bPremAccompCond": return "Accompany condition";
                case "m_iBindMoney": return "Required bound money";
                case "m_bShowByBindMoney": return "Show by bound money";
                case "m_arrSideOccupation": return "Side occupation min";
                case "m_arrSideOccupMax": return "Side occupation max";
                case "m_arrSideOccupActive": return "Side occupation active";
                case "m_bDiscoverMap": return "Requires discovered map";
                case "m_ulDiscoverMapID": return "Discover map ID";
                case "m_iPremImpression": return "Required impression";
                case "m_iPremPrayValue": return "Required pray value";
                case "m_iFlyLevel": return "Required fly level";
                case "m_bFlyTimeFull": return "Requires full fly time";
                case "m_iBelief": return "Required belief";
                case "m_bBeliefDeposit": return "Deposit belief";
                case "m_iMinGodLevel": return "God level min";
                case "m_iMaxGodLevel": return "God level max";
                case "m_iMinEvilLevel": return "Evil level min";
                case "m_iMaxEvilLevel": return "Evil level max";
                case "m_bGodEvilCond": return "God/evil condition";
                case "m_bShowByGodEvil": return "Show by god/evil";
                case "m_lCompletedUserAccountFlag": return "Account completion flag";
                case "m_cVipLevel": return "Required VIP level";
                case "m_ulConsumeScore": return "Required consume score";
                case "m_iServerType": return "Required server type";
                case "m_iMarriage": return "Required marriage state";
                case "m_enumMethod": return "Completion method";
                case "m_enumFinishType": return "Finish type";
                case "m_ulMonsterWanted": return "Wanted monster count";
                case "m_bHasGatherMonster": return "Has gathered monster";
                case "m_ulGatherMonsterNum": return "Gather monster count";
                case "m_bGatherItem": return "Gather item";
                case "m_ulGatherItemNum": return "Gather item count";
                case "m_bKillMonsterPack": return "Kill monster pack";
                case "m_iKillMonsterPackID": return "Monster pack ID";
                case "m_iKillMonsterPackNum": return "Monster pack count";
                case "m_ulItemsWanted": return "Required completion items";
                case "m_ulGoldWanted": return "Required gold";
                case "m_bNotGiveMine": return "Do not give mine";
                case "m_bNotClearCommonItem": return "Do not clear common item";
                case "m_ulNPCToProtect": return "NPC to protect";
                case "m_ulProtectTimeLen": return "Protection time";
                case "m_ulNPCMoving": return "Moving NPC";
                case "m_ulNPCDestSite": return "NPC destination site";
                case "m_ReachSiteMin": return "Reach site min";
                case "m_ReachSiteMax": return "Reach site max";
                case "m_ulReachSiteId": return "Reach site ID";
                case "m_ulWaitTime": return "Wait time";
                case "m_ulTitleWantedNum": return "Required title count";
                case "m_TitleWanted": return "Required title IDs";
                case "m_ulFinishTaskID": return "Task to finish";
                case "m_ulFinishTaskTimes": return "Required finish count";
                case "m_bHasFinishTaskTimes": return "Has finish-count requirement";
                case "m_ulFinishLev": return "Required finish level";
                case "m_bShowByFinLev": return "Show by finish level";
                case "m_ulFinChangedRace": return "Finish changed race";
                case "m_ulFinChangedVisualize": return "Finish changed visual";
                case "m_ulItemsSubmitWanted": return "Submit item count";
                case "m_ulItemTypeFalse": return "Wrong item type behavior";
                case "m_ulItemCountFalse": return "Wrong item count behavior";
                case "m_ulWelcomeWords": return "Welcome words count";
                case "m_ulFinBuffCount": return "Finish buff count";
                case "m_FinBuffs": return "Finish buffs";
                case "m_bFinBuffCond": return "Finish buff condition";
                case "m_FinKeyValue": return "Finish key/value";
                case "m_iFinServerType": return "Finish server type";
                case "m_iTMMineID": return "Timed mine ID";
                case "m_iTMMiningTimes": return "Timed mining count";
                case "m_fTMMiningProb": return "Timed mining probability";
                case "m_iTMUseItemID": return "Timed item ID";
                case "m_iTMUseItemTimes": return "Timed item uses";
                case "m_fTMUseItemProb": return "Timed item probability";
                case "m_iTMDungeonID": return "Timed dungeon ID";
                case "m_KillPlayerSiteMin": return "Player-kill site min";
                case "m_KillPlayerSiteMax": return "Player-kill site max";
                case "m_ulKillPlayerSiteId": return "Player-kill site ID";
                case "m_iTMKillPlayerTimes": return "Kill-player count";
                case "m_fTMKillPlayerProb": return "Kill-player probability";
                case "m_iTMAchievementID": return "Achievement ID";
                case "m_iTMAchievementTimes": return "Achievement count";
                case "m_fTMAchievementProb": return "Achievement probability";
                case "m_iTMDoEmotionID": return "Emotion ID";
                case "m_iTMDoEmotionTimes": return "Emotion count";
                case "m_fTMDoEmotionProb": return "Emotion probability";
                case "m_iPVPWinTimes": return "PVP wins";
                case "m_iPVPFailTimes": return "PVP failures";
                case "m_iPVPFinTimes": return "PVP finishes";
                case "m_ulAwardType_S": return "Success reward mode";
                case "m_ulAwardType_F": return "Failure reward mode";
                case "m_Award_S": return "Success award";
                case "m_Award_F": return "Failure award";
                case "m_AwByRatio_S": return "Success ratio awards";
                case "m_AwByRatio_F": return "Failure ratio awards";
                case "m_AwByItems_S": return "Success item-count awards";
                case "m_AwByItems_F": return "Failure item-count awards";
                case "m_AwByCount_S": return "Success finish-count awards";
                case "m_AwByCount_F": return "Failure finish-count awards";
                case "m_AwBySubmitItems_S": return "Success submit-item awards";
                case "m_AwBySubmitItems_F": return "Failure submit-item awards";
                case "m_pwstrDescript": return "Description text";
                case "m_pwstrOkText": return "OK text";
                case "m_pwstrNoText": return "No text";
                case "m_pwstrTribute": return "Tribute text";
                case "m_pwstrItemAwardBroadcast": return "Item award broadcast";
                case "m_ulParent": return "Parent task";
                case "m_ulPrevSibling": return "Previous sibling";
                case "m_ulNextSibling": return "Next sibling";
                case "m_ulFirstChild": return "First child";
                default: return MakeDisplayName(fieldName);
            }
        }

        private static string GetFieldMeaning(string fieldName)
        {
            switch (fieldName)
            {
                case "m_ID": return "Unique identifier used by task references and scripts.";
                case "m_szName": return "Name shown by editors and usually mirrored in task text.";
                case "m_bHidden": return "Keeps the task out of normal visible lists until something exposes it.";
                case "m_bOffLineIsFail": return "The task fails if the player logs out while it is active.";
                case "m_bHasSign": return "Marks the task as carrying a signature/special marker.";
                case "m_ulType": return "High-level task category used by the game logic.";
                case "m_ulTimeLimit": return "Countdown in seconds; zero means no countdown from this field.";
                case "m_bAbsTime": return "Uses calendar/absolute time instead of a relative countdown.";
                case "m_ulTimetable": return "Number of timetable entries stored after the fixed header.";
                case "m_lAvailFrequency": return "How many times the task can be taken in its availability window.";
                case "m_lTimeInterval": return "Delay in seconds before the task can be taken again.";
                case "m_bCanGiveUp": return "Player is allowed to abandon the task.";
                case "m_bCanRedo": return "Player can repeat the task after completing it.";
                case "m_bCanRedoAfterFailure": return "Player can try again after failing it.";
                case "m_bClearAsGiveUp": return "Completion cleanup behaves like giving up the task.";
                case "m_bFailAsPlayerDie": return "The task fails when the character dies.";
                case "m_ulMaxReceiver": return "Maximum number of players that can receive this task, when enforced.";
                case "m_bDelvInZone": return "Task can only be received inside the configured delivery area.";
                case "m_ulDelvWorld": return "World/map where the receive-area check happens.";
                case "m_DelvMinVert": return "Minimum corner of the receive-area bounding box.";
                case "m_DelvMaxVert": return "Maximum corner of the receive-area bounding box.";
                case "m_bOutZoneFail": return "Task fails when the player leaves the configured area.";
                case "m_bTransTo": return "Teleports the player when the task is delivered.";
                case "m_TransPt": return "Destination coordinate for delivery teleport.";
                case "m_bAutoDeliver": return "Task is automatically delivered when conditions are met.";
                case "m_bDeathTrig": return "Death can trigger progress or completion logic.";
                case "m_bClearAcquired": return "Acquired task items are removed during cleanup.";
                case "m_ulSuitableLevel": return "Level displayed/used as the recommended level.";
                case "m_bSuitLevelEx": return "Treats the recommended level as a strict level gate.";
                case "m_bShowPrompt": return "Shows task prompts to guide the player.";
                case "m_bKeyTask": return "Marks the task as important/mainline content.";
                case "m_bLuaTask": return "Task behavior depends on Lua-side logic.";
                case "m_ulDelvNPC": return "NPC that gives or starts the task.";
                case "m_ulAwardNPC": return "NPC that receives or finishes the task.";
                case "m_bCanSeekOut": return "Allows the client to show tracking/search help.";
                case "m_bShowDirection": return "Allows directional guidance in the client.";
                case "m_bRecFinishCount": return "Stores per-player completion count for limits or display.";
                case "m_ulMaxFinishCount": return "Maximum completion count when finish-count tracking is active.";
                case "m_bPursueTradeTask": return "Connects the task to trade-route gameplay.";
                case "m_bSendMsg": return "Sends a configured message when the task logic fires.";
                case "m_ulTerminateCount": return "Number of termination records stored after the fixed header.";
                case "m_ulParent": return "Task ID of the parent task when the task is part of a subtask tree.";
                case "m_ulPrevSibling": return "Task ID of the previous subtask at the same tree level.";
                case "m_ulNextSibling": return "Task ID of the next subtask at the same tree level.";
                case "m_ulFirstChild": return "Task ID of the first subtask under this task.";
                default: return string.Empty;
            }
        }

        private static string MakeDisplayName(string fieldName)
        {
            string clean = (fieldName ?? string.Empty)
                .Replace("m_ul", string.Empty)
                .Replace("m_b", string.Empty)
                .Replace("m_l", string.Empty)
                .Replace("m_n", string.Empty)
                .Replace("m_f", string.Empty)
                .Replace("m_sz", string.Empty)
                .Replace("m_", string.Empty);

            if (string.IsNullOrWhiteSpace(clean))
            {
                return fieldName ?? string.Empty;
            }

            return Regex.Replace(clean, "([a-z])([A-Z])", "$1 $2");
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

        private static string ReadFixedWideText(byte[] bytes, int offset, int charCount)
        {
            StringBuilder builder = new StringBuilder(Math.Max(charCount, 0));
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

                builder.Append((char)value);
            }

            return builder.ToString().Trim();
        }

        private static int ReadInt32(byte[] bytes, int offset)
        {
            return bytes != null && offset >= 0 && offset + 4 <= bytes.Length
                ? BitConverter.ToInt32(bytes, offset)
                : 0;
        }

        private static int ReadUInt32AsInt(byte[] bytes, int offset)
        {
            if (bytes == null || offset < 0 || offset + 4 > bytes.Length)
            {
                return 0;
            }

            uint value = BitConverter.ToUInt32(bytes, offset);
            return value <= int.MaxValue ? (int)value : 0;
        }

        private static bool ReadBool(byte[] bytes, int offset)
        {
            return bytes != null && offset >= 0 && offset < bytes.Length && bytes[offset] != 0;
        }

        private static int ClampCount(int count, int max)
        {
            if (count <= 0)
            {
                return 0;
            }

            return count > max ? max : count;
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

        private sealed class TaskEditorLayout
        {
            public int PremItemsOffset { get; set; }
            public int PremItemsCount { get; set; }
            public int PremTitlesOffset { get; set; }
            public int PremTitlesCount { get; set; }
            public int GivenItemsOffset { get; set; }
            public int GivenItemsCount { get; set; }
            public int TeamMemberOffset { get; set; }
            public int TeamMemberCount { get; set; }
            public int MonsterWantedOffset { get; set; }
            public int MonsterWantedCount { get; set; }
            public int ItemsWantedOffset { get; set; }
            public int ItemsWantedCount { get; set; }
            public int ItemsSubmitWantedOffset { get; set; }
            public int ItemsSubmitWantedCount { get; set; }
            public int SignatureOffset { get; set; }
            public int TimetableOffset { get; set; }
            public int TimetableCount { get; set; }
            public int GatherMonsterOffset { get; set; }
            public int FinishTaskTimesOffset { get; set; }
            public int WelcomeWordsOffset { get; set; }
            public int WelcomeWordsCount { get; set; }
            public int SuccessAwardOffset { get; set; }
            public int FailAwardOffset { get; set; }
            public int AfterFailAwardOffset { get; set; }
        }

        private sealed class FieldSpec
        {
            public FieldSpec(string section, string name, int offset, string kind)
            {
                Section = section;
                Name = name;
                Offset = offset;
                Kind = kind;
            }

            public string Section { get; private set; }
            public string Name { get; private set; }
            public int Offset { get; private set; }
            public string Kind { get; private set; }
        }

        private sealed class AwardFieldSpec
        {
            public AwardFieldSpec(string name, string displayName, int offset, string kind, string meaning)
            {
                Name = name;
                DisplayName = displayName;
                Offset = offset;
                Kind = kind;
                Meaning = meaning;
            }

            public string Name { get; private set; }
            public string DisplayName { get; private set; }
            public int Offset { get; private set; }
            public string Kind { get; private set; }
            public string Meaning { get; private set; }
        }
    }
}
