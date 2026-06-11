using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public static class BindFlagCatalog
    {
        private static readonly List<QualityOption> bindStateOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Unbound" },
            new QualityOption { Value = 1, Label = "Bound" }
        };

        private static readonly List<QualityOption> keepBindOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Do not keep bind" },
            new QualityOption { Value = 1, Label = "Keep bind" }
        };

        public static IList<QualityOption> GetOptions(string fieldName)
        {
            return IsKeepBindFieldName(fieldName)
                ? keepBindOptions
                : bindStateOptions;
        }

        public static bool IsBindFlagFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            if (normalized.IndexOf("bind_money", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("unbind", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("probability", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("ratio", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("service", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("bonus", StringComparison.OrdinalIgnoreCase) >= 0
                || string.Equals(normalized, "bind_return_town", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return IsKeepBindFieldName(normalized)
                || string.Equals(normalized, "gain_item_bind", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "bBind", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("_bind", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("_bBind", StringComparison.OrdinalIgnoreCase);
        }

        public static string FormatDisplay(string fieldName, string rawValue)
        {
            int value;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return rawValue ?? string.Empty;
            }

            string label;
            return TryGetLabel(fieldName, value, out label)
                ? value.ToString(CultureInfo.InvariantCulture) + " - " + label
                : rawValue ?? string.Empty;
        }

        public static string NormalizeInput(string fieldName, string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            int numericValue;
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue))
            {
                return numericValue.ToString(CultureInfo.InvariantCulture);
            }

            string normalized = trimmed
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();

            if (IsKeepBindFieldName(fieldName))
            {
                if (normalized == "keep" || normalized == "keepbind" || normalized == "preserve" || normalized == "preservebind")
                {
                    return "1";
                }

                if (normalized == "dontkeep" || normalized == "donotkeep" || normalized == "removebind" || normalized == "resetbind" || normalized == "clearbind")
                {
                    return "0";
                }
            }
            else
            {
                if (normalized == "bound" || normalized == "bind" || normalized == "yes" || normalized == "true")
                {
                    return "1";
                }

                if (normalized == "unbound" || normalized == "unbind" || normalized == "no" || normalized == "false")
                {
                    return "0";
                }
            }

            return trimmed;
        }

        public static bool TryGetLabel(string fieldName, int value, out string label)
        {
            IList<QualityOption> options = GetOptions(fieldName);
            for (int i = 0; i < options.Count; i++)
            {
                QualityOption option = options[i];
                if (option != null && option.Value == value)
                {
                    label = option.Label ?? string.Empty;
                    return true;
                }
            }

            label = string.Empty;
            return false;
        }

        private static bool IsKeepBindFieldName(string fieldName)
        {
            return string.Equals(fieldName ?? string.Empty, "keep_bind_prop", StringComparison.OrdinalIgnoreCase);
        }
    }
}
