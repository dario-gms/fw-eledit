using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public sealed class TaskEditorData
    {
        public string GameRootPath { get; set; }
        public List<TaskEditorShard> Shards { get; private set; }
        public List<TaskEditorEntry> Entries { get; private set; }

        public TaskEditorData()
        {
            Shards = new List<TaskEditorShard>();
            Entries = new List<TaskEditorEntry>();
        }
    }

    public sealed class TaskEditorShard
    {
        public int Index { get; set; }
        public string FilePath { get; set; }
        public int Signature { get; set; }
        public int Version { get; set; }
        public int Count { get; set; }
    }

    public sealed class TaskEditorEntry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ShardIndex { get; set; }
        public string ShardName { get; set; }
        public int ChunkIndex { get; set; }
        public int ChunkStartOffset { get; set; }
        public int ChunkEndOffset { get; set; }
        public int LocalOffset { get; set; }
        public int AbsoluteOffset { get; set; }
        public int Size { get; set; }
        public byte[] Bytes { get; set; }
        public int ParentId { get; set; }
        public int PrevSiblingId { get; set; }
        public int NextSiblingId { get; set; }
        public int FirstChildId { get; set; }
        public bool HasChildren { get; set; }
        public int TreeDepth { get; set; }
        public List<TaskEditorTextValue> Texts { get; private set; }
        public List<TaskEditorRawValue> RawValues { get; private set; }

        public TaskEditorEntry()
        {
            Texts = new List<TaskEditorTextValue>();
            RawValues = new List<TaskEditorRawValue>();
            Bytes = new byte[0];
        }
    }

    public sealed class TaskEditorTextValue
    {
        public int Offset { get; set; }
        public string Text { get; set; }
    }

    public sealed class TaskEditorFieldValue
    {
        public string Section { get; set; }
        public string Field { get; set; }
        public string DisplayName { get; set; }
        public string Meaning { get; set; }
        public int Offset { get; set; }
        public string HexOffset { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public sealed class TaskEditorItemValue
    {
        public string Kind { get; set; }
        public string Source { get; set; }
        public int Offset { get; set; }
        public string HexOffset { get; set; }
        public int ItemId { get; set; }
        public int Count { get; set; }
        public bool CommonItem { get; set; }
        public bool Bind { get; set; }
        public string Name { get; set; }
        public Color? NameForeColor { get; set; }
        public string IconKey { get; set; }
        public int Quality { get; set; }
        public string AccentHex { get; set; }
    }

    public sealed class TaskEditorRawValue
    {
        public int Offset { get; set; }
        public string HexOffset { get; set; }
        public string Int32Value { get; set; }
        public string UInt32Value { get; set; }
        public string FloatValue { get; set; }
        public string HexBytes { get; set; }
        public string Hint { get; set; }
    }
}
