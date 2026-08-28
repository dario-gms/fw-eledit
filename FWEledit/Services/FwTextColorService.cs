using System;
using System.Drawing;
using System.Globalization;

namespace FWEledit
{
    public static class FwTextColorService
    {
        public static bool TryParseLeadingColor(string value, out Color color, out string text)
        {
            color = Color.Empty;
            text = value ?? string.Empty;

            if (string.IsNullOrEmpty(value) || value.Length < 7 || value[0] != '^')
            {
                return false;
            }

            for (int i = 1; i <= 6; i++)
            {
                if (!Uri.IsHexDigit(value[i]))
                {
                    return false;
                }
            }

            int rgb;
            if (!int.TryParse(value.Substring(1, 6), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
            {
                return false;
            }

            color = Color.FromArgb((rgb >> 16) & 0xff, (rgb >> 8) & 0xff, rgb & 0xff);
            text = value.Substring(7);
            return true;
        }

        public static bool HasLeadingColor(string value)
        {
            Color color;
            string text;
            return TryParseLeadingColor(value, out color, out text);
        }

        public static string StripLeadingColor(string value)
        {
            Color color;
            string text;
            return TryParseLeadingColor(value, out color, out text) ? text : (value ?? string.Empty);
        }

        public static string PreserveLeadingColor(string previousRawValue, string newVisibleValue)
        {
            string safeNewValue = newVisibleValue ?? string.Empty;
            if (HasLeadingColor(safeNewValue))
            {
                return safeNewValue;
            }

            Color color;
            string ignored;
            if (!TryParseLeadingColor(previousRawValue, out color, out ignored))
            {
                return safeNewValue;
            }

            return ToPrefix(color) + safeNewValue;
        }

        public static string SetLeadingColor(string rawValue, Color color)
        {
            return ToPrefix(color) + StripLeadingColor(rawValue);
        }

        public static string ToPrefix(Color color)
        {
            return "^"
                + color.R.ToString("x2", CultureInfo.InvariantCulture)
                + color.G.ToString("x2", CultureInfo.InvariantCulture)
                + color.B.ToString("x2", CultureInfo.InvariantCulture);
        }
    }
}
