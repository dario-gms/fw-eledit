namespace FWEledit
{
    public sealed class IconEntryModel
    {
        public int PathId { get; set; }
        public int AtlasIndex { get; set; }
        public string Key { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
