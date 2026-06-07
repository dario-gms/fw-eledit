using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public static class GenderTypeCatalog
    {
        private static readonly List<QualityOption> GenderOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Male" },
            new QualityOption { Value = 1, Label = "Female" },
            new QualityOption { Value = 2, Label = "Female & Male" }
        };

        public static List<QualityOption> Options
        {
            get { return GenderOptions; }
        }

        public static bool IsGenderTypeFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return string.Equals(normalized, "gender_type", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "require_gender", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("_gender_type", StringComparison.OrdinalIgnoreCase);
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
            for (int i = 0; i < GenderOptions.Count; i++)
            {
                QualityOption option = GenderOptions[i];
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
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace("&", string.Empty)
                .Replace("+", string.Empty)
                .ToLowerInvariant();

            if (normalized == "male")
            {
                return "0";
            }
            if (normalized == "female")
            {
                return "1";
            }
            if (normalized == "both"
                || normalized == "all"
                || normalized == "femaleandmale"
                || normalized == "femalemale"
                || normalized == "malefemale")
            {
                return "2";
            }

            return trimmed;
        }
    }
}
