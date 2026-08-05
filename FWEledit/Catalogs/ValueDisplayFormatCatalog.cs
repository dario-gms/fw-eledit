using System;
using System.Globalization;

namespace FWEledit
{
    public static class ValueDisplayFormatCatalog
    {
        public static string FormatFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            return fieldName.Trim().Replace('_', ' ');
        }

        public static string FormatLargeNumber(string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || ShouldKeepRawNumber(fieldName))
            {
                return value ?? string.Empty;
            }

            string trimmed = value.Trim();
            long parsed;
            if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                && !long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
            {
                return value;
            }

            if (Math.Abs(parsed) < 10000)
            {
                return value;
            }

            NumberFormatInfo format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            format.NumberGroupSeparator = ".";
            format.NumberDecimalDigits = 0;
            return parsed.ToString("N0", format);
        }

        public static string NormalizeLargeNumberInput(string fieldType, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value ?? string.Empty;
            }
            if (!IsIntegerFieldType(fieldType))
            {
                return value.Trim();
            }

            string trimmed = value.Trim();
            bool hasDot = false;
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (c == '.')
                {
                    hasDot = true;
                    continue;
                }
                if (i == 0 && (c == '-' || c == '+'))
                {
                    continue;
                }
                if (!char.IsDigit(c))
                {
                    return trimmed;
                }
            }

            return hasDot ? trimmed.Replace(".", string.Empty) : trimmed;
        }

        public static string FormatDisplayFieldName(string fieldName, string preferredDisplayName)
        {
            if (string.IsNullOrWhiteSpace(preferredDisplayName) || string.Equals(preferredDisplayName, fieldName, StringComparison.Ordinal))
            {
                return FormatFieldName(fieldName);
            }

            return preferredDisplayName.Replace('_', ' ');
        }

        private static bool ShouldKeepRawNumber(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return string.Equals(normalized, "id", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("id_", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("_id", StringComparison.OrdinalIgnoreCase)
                || normalized.IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("mask", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("color", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsIntegerFieldType(string fieldType)
        {
            if (string.IsNullOrWhiteSpace(fieldType))
            {
                return false;
            }

            string normalized = fieldType.Trim();
            return normalized.IndexOf("int", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("long", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("byte", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
