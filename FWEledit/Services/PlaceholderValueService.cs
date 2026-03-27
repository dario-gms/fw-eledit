using System;

namespace FWEledit
{
    public sealed class PlaceholderValueService
    {
        public bool IsPlaceholderOrEmpty(string value, string placeholder)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(placeholder))
            {
                return false;
            }

            return string.Equals(value.Trim(), placeholder, StringComparison.OrdinalIgnoreCase);
        }
    }
}
