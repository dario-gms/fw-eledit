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

        public static List<TaskEditorFieldValue> BuildKnownFields(byte[] bytes)
        {
            List<TaskEditorFieldValue> values = new List<TaskEditorFieldValue>();
            if (bytes == null)
            {
                return values;
            }

            FieldSpec[] specs =
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
                new FieldSpec("Terminate", "m_ulTerminateCount", 0x012E, "uint")
            };

            foreach (FieldSpec spec in specs)
            {
                string value;
                if (!TryReadField(bytes, spec, out value))
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

            return values;
        }

        private static bool TryReadField(byte[] bytes, FieldSpec spec, out string value)
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

            return false;
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
                case "m_ulType": return "Task type";
                case "m_ulTimeLimit": return "Time limit";
                case "m_bAbsTime": return "Absolute time";
                case "m_ulTimetable": return "Timetable";
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
    }
}
