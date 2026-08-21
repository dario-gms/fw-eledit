using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public static class PetFoodUsageCatalog
    {
        private static readonly List<QualityOption> options = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "HP" },
            new QualityOption { Value = 1, Label = "MP" },
            new QualityOption { Value = 2, Label = "Repletion" },
            new QualityOption { Value = 3, Label = "Body Size Change" }
        };

        public static IList<QualityOption> Options
        {
            get { return options; }
        }

        public static bool IsPetFoodUsageFieldName(string fieldName)
        {
            return string.Equals(fieldName ?? string.Empty, "food_usage", StringComparison.OrdinalIgnoreCase);
        }

        public static string FormatDisplay(string rawValue)
        {
            int value;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return rawValue ?? string.Empty;
            }

            string label;
            return TryGetLabel(value, out label)
                ? value.ToString(CultureInfo.InvariantCulture) + " - " + label
                : rawValue ?? string.Empty;
        }

        public static bool TryGetLabel(int value, out string label)
        {
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

        public static string NormalizeInput(string value)
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

            if (normalized == "hp" || normalized == "health")
            {
                return "0";
            }
            if (normalized == "mp" || normalized == "mana")
            {
                return "1";
            }
            if (normalized == "repletion" || normalized == "satiety" || normalized == "gorge" || normalized == "food")
            {
                return "2";
            }
            if (normalized == "bodysizechange" || normalized == "bodychange" || normalized == "sizechange" || normalized == "potion")
            {
                return "3";
            }

            return trimmed;
        }
    }
}
