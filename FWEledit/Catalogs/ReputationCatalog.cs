using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public static class ReputationCatalog
    {
        private static readonly List<QualityOption> reputationOptions = new List<QualityOption>
        {
            new QualityOption { Value = -1, Label = "None / Disabled" },
            new QualityOption { Value = 0, Label = "Slayer Reputation" },
            new QualityOption { Value = 1, Label = "Triumph Reputation" },
            new QualityOption { Value = 2, Label = "Call of Dawn" },
            new QualityOption { Value = 3, Label = "Bounty Reputation" },
            new QualityOption { Value = 4, Label = "Champion Reputation" },
            new QualityOption { Value = 5, Label = "Arena" },
            new QualityOption { Value = 6, Label = "Arena Dedication" },
            new QualityOption { Value = 7, Label = "3v3 Arena Score" },
            new QualityOption { Value = 8, Label = "6v6 Arena Score" },
            new QualityOption { Value = 9, Label = "Arena Reputation" },
            new QualityOption { Value = 10, Label = "Luck" },
            new QualityOption { Value = 11, Label = "Mentor" },
            new QualityOption { Value = 12, Label = "Social Contribution" },
            new QualityOption { Value = 13, Label = "Cruelty" },
            new QualityOption { Value = 14, Label = "Bounty Glory" },
            new QualityOption { Value = 15, Label = "Kindness" },
            new QualityOption { Value = 16, Label = "Courage" },
            new QualityOption { Value = 17, Label = "Master Reputation" },
            new QualityOption { Value = 18, Label = "Frostgale Fjord" },
            new QualityOption { Value = 19, Label = "Frostgale Fjord Battlefield Comment" },
            new QualityOption { Value = 20, Label = "Lionheart Champions Reputation" },
            new QualityOption { Value = 21, Label = "Frostgale Fjord Total Points" },
            new QualityOption { Value = 22, Label = "Sanguine Circle Reputation" },
            new QualityOption { Value = 23, Label = "Union of the Woods Reputation" },
            new QualityOption { Value = 24, Label = "Mercury Union Reputation" },
            new QualityOption { Value = 25, Label = "Hell Acclaim" },
            new QualityOption { Value = 26, Label = "Rose" },
            new QualityOption { Value = 27, Label = "Season 1 3v3 Score" },
            new QualityOption { Value = 28, Label = "Season 1 6v6 Score" },
            new QualityOption { Value = 29, Label = "Companion Points" },
            new QualityOption { Value = 30, Label = "Blessing of Antus" },
            new QualityOption { Value = 31, Label = "Unused / Empty" },
            new QualityOption { Value = 32, Label = "Promoter Points" },
            new QualityOption { Value = 33, Label = "Valor" },
            new QualityOption { Value = 34, Label = "Warlord Points" },
            new QualityOption { Value = 35, Label = "Hell Points" },
            new QualityOption { Value = 36, Label = "Fealty" },
            new QualityOption { Value = 37, Label = "Touch Points" },
            new QualityOption { Value = 38, Label = "Flower Score" }
        };

        private static readonly Dictionary<string, int> aliases = BuildAliases();

        public static IList<QualityOption> Options
        {
            get { return reputationOptions; }
        }

        public static bool IsReputationIdFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            if (normalized.IndexOf("repu", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (normalized.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (normalized.EndsWith("_value_1", StringComparison.OrdinalIgnoreCase))
                {
                    return normalized.IndexOf("repu_required", StringComparison.OrdinalIgnoreCase) >= 0
                        || normalized.IndexOf("repu_min", StringComparison.OrdinalIgnoreCase) >= 0
                        || normalized.IndexOf("repu_reduced", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }

            return normalized.IndexOf("reputation", StringComparison.OrdinalIgnoreCase) >= 0
                && normalized.EndsWith("_id", StringComparison.OrdinalIgnoreCase);
        }

        public static string FormatDisplay(string rawValue)
        {
            int value;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return rawValue ?? string.Empty;
            }

            string label;
            if (TryGetLabel(value, out label))
            {
                return value.ToString(CultureInfo.InvariantCulture) + " - " + label;
            }

            return rawValue ?? string.Empty;
        }

        public static bool TryGetLabel(int value, out string label)
        {
            for (int i = 0; i < reputationOptions.Count; i++)
            {
                QualityOption option = reputationOptions[i];
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

            int parsedValue;
            if (aliases.TryGetValue(NormalizeAlias(trimmed), out parsedValue))
            {
                return parsedValue.ToString(CultureInfo.InvariantCulture);
            }

            return trimmed;
        }

        private static Dictionary<string, int> BuildAliases()
        {
            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reputationOptions.Count; i++)
            {
                QualityOption option = reputationOptions[i];
                if (option == null || string.IsNullOrWhiteSpace(option.Label))
                {
                    continue;
                }

                AddAlias(result, option.Label, option.Value);
            }

            AddAlias(result, "Champion", 4);
            AddAlias(result, "Lionheart Champions", 20);
            AddAlias(result, "Sanguine Circle", 22);
            AddAlias(result, "Union of the Woods", 23);
            AddAlias(result, "War God Points", 34);
            AddAlias(result, "Warlord Points", 34);
            AddAlias(result, "None", -1);
            AddAlias(result, "Disabled", -1);
            return result;
        }

        private static void AddAlias(Dictionary<string, int> aliasesByName, string label, int value)
        {
            string normalized = NormalizeAlias(label);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            aliasesByName[normalized] = value;
        }

        private static string NormalizeAlias(string value)
        {
            return (value ?? string.Empty)
                .Replace("[Reputation]", string.Empty)
                .Replace("Reputation", string.Empty)
                .Replace("Points", " Points")
                .Replace("&", "and")
                .Replace("-", " ")
                .Replace("_", " ")
                .Replace(".", " ")
                .Trim()
                .ToLowerInvariant();
        }
    }
}
