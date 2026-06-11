using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public static class PetFoodTypeCatalog
    {
        private static readonly List<QualityOption> options = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "General" },
            new QualityOption { Value = 1, Label = "Meat" },
            new QualityOption { Value = 2, Label = "Snacks" },
            new QualityOption { Value = 3, Label = "Element" },
            new QualityOption { Value = 4, Label = "Fruit" },
            new QualityOption { Value = 5, Label = "Reserve 1" },
            new QualityOption { Value = 6, Label = "Reserve 2" },
            new QualityOption { Value = 7, Label = "Reserve 3" }
        };

        public static IList<QualityOption> Options
        {
            get { return options; }
        }

        public static bool IsPetFoodTypeFieldName(string fieldName)
        {
            return string.Equals(fieldName ?? string.Empty, "pet_food_type", StringComparison.OrdinalIgnoreCase);
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

            if (normalized == "general")
            {
                return "0";
            }
            if (normalized == "meat")
            {
                return "1";
            }
            if (normalized == "snacks" || normalized == "snack")
            {
                return "2";
            }
            if (normalized == "element" || normalized == "elemental")
            {
                return "3";
            }
            if (normalized == "fruit")
            {
                return "4";
            }
            if (normalized == "reserve1")
            {
                return "5";
            }
            if (normalized == "reserve2")
            {
                return "6";
            }
            if (normalized == "reserve3")
            {
                return "7";
            }

            return trimmed;
        }
    }
}
