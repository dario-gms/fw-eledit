using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public static class ModelProfessionCatalog
    {
        private const int AnyProfessionValue = 10;

        private static readonly List<QualityOption> professionOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Unset" },
            new QualityOption { Value = 1, Label = "Warrior" },
            new QualityOption { Value = 2, Label = "Protector" },
            new QualityOption { Value = 3, Label = "Assassin" },
            new QualityOption { Value = 4, Label = "Marksman" },
            new QualityOption { Value = 5, Label = "Mage" },
            new QualityOption { Value = 6, Label = "Priest" },
            new QualityOption { Value = 7, Label = "Vampire" },
            new QualityOption { Value = 8, Label = "Bard" },
            new QualityOption { Value = 9, Label = "Blood Raider" },
            new QualityOption { Value = AnyProfessionValue, Label = "Any profession" }
        };

        private static readonly Dictionary<string, int> aliases = BuildAliases();

        public static IList<QualityOption> Options
        {
            get { return professionOptions; }
        }

        public static bool IsModelProfessionFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return normalized.StartsWith("models_", StringComparison.OrdinalIgnoreCase)
                && normalized.EndsWith("_profession", StringComparison.OrdinalIgnoreCase);
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
            for (int i = 0; i < professionOptions.Count; i++)
            {
                QualityOption option = professionOptions[i];
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
            for (int i = 0; i < professionOptions.Count; i++)
            {
                QualityOption option = professionOptions[i];
                if (option == null || string.IsNullOrWhiteSpace(option.Label))
                {
                    continue;
                }

                result[NormalizeAlias(option.Label)] = option.Value;
            }

            result["guard"] = 2;
            result["thief"] = 3;
            result["gunman"] = 4;
            result["enchanter"] = 5;
            result["blood"] = 7;
            result["poet"] = 8;
            result["allprofessions"] = AnyProfessionValue;
            result["allclasses"] = AnyProfessionValue;
            result["anyclass"] = AnyProfessionValue;
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
