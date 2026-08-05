namespace FWEledit
{
    public sealed class GameShopEntry
    {
        public int RowIndex { get; set; }
        public int RecordOffset { get; set; }
        public int Id { get; set; }
        public int FileIcon { get; set; }
        public string Name { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public uint IsHidden { get; set; }
        public uint CanGetConsumeScore { get; set; }
        public uint Unknown0 { get; set; }
        public int GiftId { get; set; }
        public float GiftRate { get; set; }
        public uint BuyTypeMask { get; set; }
        public int InternalPrice { get; set; }
        public int Price
        {
            get { return (int)System.Math.Floor(InternalPrice * 0.4d); }
            set { InternalPrice = (int)System.Math.Ceiling(value * 2.5d); }
        }
        public uint ExpireDate { get; set; }
        public string ExtSearch { get; set; }
        public uint NewItemIndex { get; set; }
        public uint SellBeginTime { get; set; }
        public uint SellEndTime { get; set; }
        public uint DiscountBeginTime { get; set; }
        public uint DiscountEndTime { get; set; }
        public int DiscountPrice { get; set; }
        public int LogoIcon { get; set; }
        public int PreviewBackgroundPic { get; set; }
        public int PreviewPriority { get; set; }
        public uint DiscountItemShowIndex { get; set; }
        public int PriorityPreviewModel { get; set; }
        public uint LimitTimesTaskId { get; set; }
    }
}
