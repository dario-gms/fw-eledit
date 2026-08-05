using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public sealed class EquipmentLocationOption
    {
        public int Value { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture) + " - " + (Label ?? string.Empty);
        }
    }

    public static class EquipmentLocationCatalog
    {
        private static readonly List<EquipmentLocationOption> options = new List<EquipmentLocationOption>
        {
            new EquipmentLocationOption { Value = 0, Label = "None", Description = "No character equipment location." },
            new EquipmentLocationOption { Value = 1, Label = "Helmet / Fashion Hat", Description = "Observed in equipment data for Helmet and Fashion Hat masks." },
            new EquipmentLocationOption { Value = 2, Label = "Body", Description = "Observed in equipment data for Body armor." },
            new EquipmentLocationOption { Value = 3, Label = "Legs / Fashion Pants", Description = "Observed in equipment data for Leg armor and Fashion Pants masks." },
            new EquipmentLocationOption { Value = 4, Label = "Fashion Coat / Accessory", Description = "Observed in equipment data for Fashion Coat, dress-style fashion, and Fashion Accessory masks." },
            new EquipmentLocationOption { Value = 5, Label = "Shoes / Shoes Fashion", Description = "Observed in equipment data for Shoes and Shoes Fashion masks." },
            new EquipmentLocationOption { Value = 6, Label = "Shoulder", Description = "Observed in equipment data for Shoulder equipment." },
            new EquipmentLocationOption { Value = 7, Label = "Bracelet / Off-Hand", Description = "Observed in equipment data for Bracelet / off-hand equipment." },
            new EquipmentLocationOption { Value = 8, Label = "Belt", Description = "Observed in equipment data for Belt equipment." },
            new EquipmentLocationOption { Value = 10, Label = "Fashion Accessory Set", Description = "Observed in equipment data for combined fashion accessory masks." }
        };

        public static IList<EquipmentLocationOption> Options
        {
            get { return options; }
        }

        public static bool IsEquipmentLocationFieldName(string fieldName)
        {
            return string.Equals((fieldName ?? string.Empty).Trim(), "equip_location", StringComparison.OrdinalIgnoreCase);
        }

        public static string FormatDisplay(string rawValue)
        {
            int value;
            if (!int.TryParse((rawValue ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                && !int.TryParse((rawValue ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
            {
                return rawValue ?? string.Empty;
            }

            EquipmentLocationOption option = FindOption(value);
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

            int numericValue;
            if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue)
                || int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out numericValue))
            {
                return numericValue.ToString(CultureInfo.InvariantCulture);
            }

            EquipmentLocationOption option = FindOption(value);
            return option != null
                ? option.Value.ToString(CultureInfo.InvariantCulture)
                : value.Trim();
        }

        public static EquipmentLocationOption FindOption(int value)
        {
            for (int i = 0; i < options.Count; i++)
            {
                EquipmentLocationOption option = options[i];
                if (option != null && option.Value == value)
                {
                    return option;
                }
            }

            return null;
        }

        private static EquipmentLocationOption FindOption(string label)
        {
            string normalized = NormalizeAlias(label);
            for (int i = 0; i < options.Count; i++)
            {
                EquipmentLocationOption option = options[i];
                if (option != null && NormalizeAlias(option.Label).Contains(normalized))
                {
                    return option;
                }
            }

            if (normalized == "head" || normalized == "hat" || normalized == "fashionhat")
            {
                return FindOption(1);
            }
            if (normalized == "coat" || normalized == "fashioncoat" || normalized == "accessory" || normalized == "fashionaccessory")
            {
                return FindOption(4);
            }
            if (normalized == "pants" || normalized == "fashionpants")
            {
                return FindOption(3);
            }
            if (normalized == "boots" || normalized == "feet" || normalized == "shoesfashion")
            {
                return FindOption(5);
            }
            if (normalized == "offhand")
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
                .Replace("/", string.Empty)
                .ToLowerInvariant();
        }
    }
}
