using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public static class MonsterFieldCatalog
    {
        private static readonly List<QualityOption> ShowLevelOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Normal" },
            new QualityOption { Value = 1, Label = "Elite" },
            new QualityOption { Value = 2, Label = "Strong monster" },
            new QualityOption { Value = 3, Label = "Boss" },
            new QualityOption { Value = 4, Label = "Special" }
        };

        private static readonly List<QualityOption> StrategyOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Physical" },
            new QualityOption { Value = 1, Label = "Magical" },
            new QualityOption { Value = 2, Label = "Ranged" },
            new QualityOption { Value = 3, Label = "Support" }
        };

        private static readonly List<QualityOption> StandModeOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Default" },
            new QualityOption { Value = 1, Label = "Stand" },
            new QualityOption { Value = 2, Label = "Patrol" },
            new QualityOption { Value = 3, Label = "Wander" }
        };

        private static readonly List<QualityOption> NameColorOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Default" },
            new QualityOption { Value = 1, Label = "Color 1" },
            new QualityOption { Value = 2, Label = "Color 2" },
            new QualityOption { Value = 3, Label = "Color 3" },
            new QualityOption { Value = 4, Label = "Color 4" },
            new QualityOption { Value = 5, Label = "Color 5" },
            new QualityOption { Value = 6, Label = "Color 6" },
            new QualityOption { Value = 7, Label = "Color 7" },
            new QualityOption { Value = 8, Label = "Color 8" },
            new QualityOption { Value = 9, Label = "Color 9" },
            new QualityOption { Value = 10, Label = "Color 10" },
            new QualityOption { Value = 11, Label = "Color 11" },
            new QualityOption { Value = 12, Label = "Color 12" },
            new QualityOption { Value = 13, Label = "Color 13" },
            new QualityOption { Value = 14, Label = "Color 14" },
            new QualityOption { Value = 15, Label = "Color 15" }
        };

        public static bool IsMonsterEssenceList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }

            return string.Equals(NormalizeListName(listCollection.Lists[listIndex].listName), "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsOptionFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return string.Equals(normalized, "show_level", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_strategy", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "stand_mode", StringComparison.OrdinalIgnoreCase)
                || IsNameColorFieldName(normalized);
        }

        public static bool IsNameColorFieldName(string fieldName)
        {
            return string.Equals(fieldName, "name_color", StringComparison.OrdinalIgnoreCase);
        }

        public static IList<QualityOption> GetOptions(string fieldName)
        {
            if (IsNameColorFieldName(fieldName))
            {
                return NameColorOptions;
            }
            if (string.Equals(fieldName, "show_level", StringComparison.OrdinalIgnoreCase))
            {
                return ShowLevelOptions;
            }
            if (string.Equals(fieldName, "id_strategy", StringComparison.OrdinalIgnoreCase))
            {
                return StrategyOptions;
            }
            if (string.Equals(fieldName, "stand_mode", StringComparison.OrdinalIgnoreCase))
            {
                return StandModeOptions;
            }

            return new List<QualityOption>();
        }

        public static string FormatDisplay(string fieldName, string rawValue)
        {
            int value;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return rawValue ?? string.Empty;
            }

            IList<QualityOption> options = GetOptions(fieldName);
            for (int i = 0; i < options.Count; i++)
            {
                QualityOption option = options[i];
                if (option.Value == value)
                {
                    return option.Label;
                }
            }

            if (IsNameColorFieldName(fieldName))
            {
                return value == 0
                    ? "Default"
                    : "Color " + value.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string NormalizeInput(string fieldName, string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            int parsed;
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed.ToString(CultureInfo.InvariantCulture);
            }

            IList<QualityOption> options = GetOptions(fieldName);
            for (int i = 0; i < options.Count; i++)
            {
                QualityOption option = options[i];
                if (string.Equals(option.Label, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return option.Value.ToString(CultureInfo.InvariantCulture);
                }
            }

            return trimmed;
        }

        private static string NormalizeListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return string.Empty;
            }

            string trimmed = listName.Trim();
            string[] split = trimmed.Split(new string[] { " - " }, StringSplitOptions.None);
            if (split.Length > 1)
            {
                return split[1].Trim();
            }

            return trimmed;
        }
    }
}
