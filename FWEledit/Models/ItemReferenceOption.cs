namespace FWEledit
{
    public sealed class ItemReferenceOption
    {
        public int ListIndex { get; set; }
        public int ElementIndex { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string ListName { get; set; }
        public string IconKey { get; set; }
        public int Quality { get; set; }
        public string Description { get; set; }
        public string SecondaryText { get; set; }
        public string AccentHex { get; set; }
        public string Kind { get; set; }

        public override string ToString()
        {
            return (Name ?? string.Empty) + "  [" + Id.ToString() + "]";
        }
    }
}
