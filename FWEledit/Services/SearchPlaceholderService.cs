using System;

namespace FWEledit
{
    public sealed class SearchPlaceholderService
    {
        public string ResolvePlaceholderText(bool searchAllChecked, string currentText)
        {
            string text = currentText ?? string.Empty;

            if (searchAllChecked)
            {
                if (string.Equals(text, "ID or NAME", StringComparison.OrdinalIgnoreCase))
                {
                    return "VALUE";
                }
                return text;
            }

            if (string.Equals(text, "VALUE", StringComparison.OrdinalIgnoreCase))
            {
                return "ID or NAME";
            }

            return text;
        }
    }
}
