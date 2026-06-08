using System;
using System.Globalization;

namespace FWEledit
{
    public static class ProbabilityDisplayService
    {
        public static bool IsProbabilityFieldName(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.IndexOf("probability", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string FormatDisplay(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            double value;
            if (!TryParseNumber(rawValue, out value))
            {
                return rawValue;
            }

            double percentage = value * 100d;
            return percentage.ToString("0.##", CultureInfo.CurrentCulture) + "%";
        }

        public static string NormalizeInput(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            bool isPercent = trimmed.EndsWith("%", StringComparison.Ordinal);
            if (isPercent)
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1).Trim();
            }

            double parsed;
            if (!TryParseNumber(trimmed, out parsed))
            {
                return value;
            }

            double normalized = isPercent
                ? parsed / 100d
                : (Math.Abs(parsed) > 1d ? parsed / 100d : parsed);
            return normalized.ToString("0.000000", CultureInfo.CurrentCulture);
        }

        private static bool TryParseNumber(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
