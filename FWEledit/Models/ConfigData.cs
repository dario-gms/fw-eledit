namespace FWEledit
{
    public sealed class ConfigData
    {
        public int ConversationListIndex { get; set; }
        public string LoadedFileName { get; set; }
        public string[] ListNames { get; set; }
        public string[] ListOffsets { get; set; }
        public string[][] FieldNames { get; set; }
        public string[][] FieldTypes { get; set; }

        public int ListCount
        {
            get { return ListNames == null ? 0 : ListNames.Length; }
        }
    }
}
