using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public static class ModelRaceCatalog
    {
        private const int AnyRaceValue = 7;

        private static readonly List<QualityOption> raceOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Unset" },
            new QualityOption { Value = 1, Label = "Human" },
            new QualityOption { Value = 2, Label = "Elf" },
            new QualityOption { Value = 3, Label = "Dwarf" },
            new QualityOption { Value = 4, Label = "Stoneman" },
            new QualityOption { Value = 5, Label = "Kindred" },
            new QualityOption { Value = 6, Label = "Lycan" },
            new QualityOption { Value = AnyRaceValue, Label = "Any race" }
        };

        private static readonly Dictionary<string, int> aliases = BuildAliases();

        public static IList<QualityOption> Options
        {
            get { return raceOptions; }
        }

        public static bool IsModelRaceFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return normalized.StartsWith("models_", StringComparison.OrdinalIgnoreCase)
                && normalized.EndsWith("_race", StringComparison.OrdinalIgnoreCase);
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

        public static bool TryGetLabel(int value, out string label)
        {
            for (int i = 0; i < raceOptions.Count; i++)
            {
                QualityOption option = raceOptions[i];
                if (option != null && option.Value == value)
                {
                    label = option.Label ?? string.Empty;
                    return true;
                }
            }

            label = string.Empty;
            return false;
        }

        private static Dictionary<string, int> BuildAliases()
        {
            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < raceOptions.Count; i++)
            {
                QualityOption option = raceOptions[i];
                if (option == null || string.IsNullOrWhiteSpace(option.Label))
                {
                    continue;
                }

                result[NormalizeAlias(option.Label)] = option.Value;
            }

            result["giant"] = 4;
            result["vampire"] = 5;
            result["werewolf"] = 6;
            result["allraces"] = AnyRaceValue;
            return result;
        }

        private static string NormalizeAlias(string value)
        {
            return (value ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace("&", string.Empty)
                .ToLowerInvariant();
        }
    }
}
