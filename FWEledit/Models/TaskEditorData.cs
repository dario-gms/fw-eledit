using System.Collections.Generic;

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
        public int Offset { get; set; }
        public string HexOffset { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
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
