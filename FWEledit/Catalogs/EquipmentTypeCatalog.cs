using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public sealed class EquipmentTypeOption
    {
        public int Value { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture) + " - " + (Label ?? string.Empty);
        }
    }

    public static class EquipmentTypeCatalog
    {
        private static readonly List<EquipmentTypeOption> options = new List<EquipmentTypeOption>
        {
            new EquipmentTypeOption { Value = 0, Label = "Weapon", Description = "Observed with weapon equip masks in the equipment list." },
            new EquipmentTypeOption { Value = 1, Label = "Armor", Description = "Observed with armor slots such as Helmet, Body, Legs, Shoulder, Bracelet, Belt, and Shoes." },
            new EquipmentTypeOption { Value = 2, Label = "Fashion", Description = "Observed with fashion slots such as Fashion Hat, Fashion Coat, Fashion Pants, Shoes Fashion, Handhold, and Fashion Accessory." },
            new EquipmentTypeOption { Value = 3, Label = "Jewelry", Description = "Observed with Necklace and Ring masks." },
            new EquipmentTypeOption { Value = 6, Label = "Collecting Tool", Description = "Observed in client item descriptions as collecting tools." },
            new EquipmentTypeOption { Value = 7, Label = "Soulforce Trinket", Description = "Observed with the Soulforce Trinket mask." }
        };

        public static IList<EquipmentTypeOption> Options
        {
            get { return options; }
        }

        public static bool IsEquipmentTypeFieldName(string fieldName)
        {
            return string.Equals((fieldName ?? string.Empty).Trim(), "equip_type", StringComparison.OrdinalIgnoreCase);
        }

        public static string FormatDisplay(string rawValue)
        {
            int value;
            if (!TryParseValue(rawValue, out value))
            {
                return rawValue ?? string.Empty;
            }

            EquipmentTypeOption option = FindOption(value);
            return option != null
                ? option.Label
                : "Unknown (" + value.ToString(CultureInfo.InvariantCulture) + ")";
        }

        public static string NormalizeInput(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            int parsed;
            if (TryParseValue(value, out parsed))
            {
                return parsed.ToString(CultureInfo.InvariantCulture);
            }

            return value.Trim();
        }

        public static bool TryParseValue(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                || int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            EquipmentTypeOption option = FindOption(trimmed);
            if (option == null)
            {
                return false;
            }

            value = option.Value;
            return true;
        }

        public static EquipmentTypeOption FindOption(int value)
        {
            for (int i = 0; i < options.Count; i++)
            {
                EquipmentTypeOption option = options[i];
                if (option != null && option.Value == value)
                {
                    return option;
                }
            }

            return null;
        }

        private static EquipmentTypeOption FindOption(string label)
        {
            string normalized = NormalizeAlias(label);
            for (int i = 0; i < options.Count; i++)
            {
                EquipmentTypeOption option = options[i];
                if (option != null && string.Equals(NormalizeAlias(option.Label), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return option;
                }
            }

            if (normalized == "armour")
            {
                return FindOption(1);
            }
            if (normalized == "costume" || normalized == "clothes")
            {
                return FindOption(2);
            }
            if (normalized == "ring" || normalized == "necklace" || normalized == "accessory")
            {
                return FindOption(3);
            }
            if (normalized == "gatheringtool" || normalized == "tool" || normalized == "collectingtools")
            {
                return FindOption(6);
            }
            if (normalized == "trinket" || normalized == "soulforcetrinket")
            {
                return FindOption(7);
            }

            return null;
        }

        private static string NormalizeAlias(string value)
        {
            return (value ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }
    }
}
