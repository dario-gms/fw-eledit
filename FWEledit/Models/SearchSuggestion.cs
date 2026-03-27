namespace FWEledit
{
    public sealed class SearchSuggestion
    {
        public int ListIndex { get; set; }
        public int ElementIndex { get; set; }
        public string IdText { get; set; }
        public string NameText { get; set; }

        public override string ToString()
        {
            string listTag = "[" + ListIndex.ToString("D3") + "]: ";
            if (string.IsNullOrWhiteSpace(NameText))
            {
                return listTag + (IdText ?? string.Empty);
            }
            if (string.IsNullOrWhiteSpace(IdText))
            {
                return listTag + NameText;
            }
            return listTag + IdText + " - " + NameText;
        }
    }
}
