namespace FWEledit
{
    public sealed class ElementsFileInfo
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string FilePath { get; set; }
        public short Version { get; set; }
        public short Signature { get; set; }
        public int Timestamp { get; set; }
        public string TimestampText { get; set; }
    }
}
