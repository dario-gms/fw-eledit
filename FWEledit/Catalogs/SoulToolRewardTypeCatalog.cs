using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public static class SoulToolRewardTypeCatalog
    {
        private static readonly List<QualityOption> RewardTypeOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Attack" },
            new QualityOption { Value = 1, Label = "Defense" },
            new QualityOption { Value = 2, Label = "Health" },
            new QualityOption { Value = 3, Label = "Mana" },
            new QualityOption { Value = 4, Label = "Skill" }
        };

        public static List<QualityOption> Options
        {
            get { return RewardTypeOptions; }
        }

        public static bool IsRewardTypeFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return normalized.StartsWith("recast_standard_", StringComparison.OrdinalIgnoreCase)
                && normalized.EndsWith("_reward_type", StringComparison.OrdinalIgnoreCase);
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
            for (int i = 0; i < RewardTypeOptions.Count; i++)
            {
                QualityOption option = RewardTypeOptions[i];
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
                .Replace("+", string.Empty)
                .ToLowerInvariant();

            if (normalized == "attack" || normalized == "atk")
            {
                return "0";
            }
            if (normalized == "defense" || normalized == "defence" || normalized == "def")
            {
                return "1";
            }
            if (normalized == "health" || normalized == "hp")
            {
                return "2";
            }
            if (normalized == "mana" || normalized == "mp")
            {
                return "3";
            }
            if (normalized == "skill" || normalized == "skillreward")
            {
                return "4";
            }

            return trimmed;
        }
    }
}
