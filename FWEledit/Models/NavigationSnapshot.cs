namespace FWEledit
{
    public sealed class NavigationSnapshot
    {
        public int ListIndex { get; set; }
        public int ItemId { get; set; }
        public int GridRowIndex { get; set; }
        public int FirstDisplayedRow { get; set; }
        public int ItemGridRowIndex { get; set; }
        public int ItemGridColumnIndex { get; set; }
        public int ItemGridFirstDisplayedRow { get; set; }
        public bool IsAutoListSelection { get; set; }
    }
}
