namespace FWEledit
{
    public sealed class DescriptionFlushResult
    {
        public bool Success { get; set; }
        public bool HadPendingChanges { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
