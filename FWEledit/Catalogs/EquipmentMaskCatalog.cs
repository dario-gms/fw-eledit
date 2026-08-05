using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public sealed class EquipmentMaskOption
    {
        public uint Mask { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Label ?? string.Empty;
        }
    }

    public static class EquipmentMaskCatalog
    {
        private static readonly List<EquipmentMaskOption> options = new List<EquipmentMaskOption>
        {
            new EquipmentMaskOption { Mask = 1u, Label = "Weapon", Description = "Main weapon slot." },
            new EquipmentMaskOption { Mask = 2u, Label = "Bracelet", Description = "Bracelet / off-hand slot." },
            new EquipmentMaskOption { Mask = 4u, Label = "Shoulder", Description = "Shoulder equipment slot." },
            new EquipmentMaskOption { Mask = 8u, Label = "Body", Description = "Body armor slot." },
            new EquipmentMaskOption { Mask = 16u, Label = "Belt", Description = "Belt equipment slot." },
            new EquipmentMaskOption { Mask = 32u, Label = "Legs", Description = "Leg armor slot." },
            new EquipmentMaskOption { Mask = 64u, Label = "Helmet", Description = "Helmet equipment slot." },
            new EquipmentMaskOption { Mask = 128u, Label = "Shoes", Description = "Shoe equipment slot." },
            new EquipmentMaskOption { Mask = 256u, Label = "Necklace", Description = "Necklace equipment slot." },
            new EquipmentMaskOption { Mask = 512u, Label = "Soulforce Trinket", Description = "Soulforce trinket slot." },
            new EquipmentMaskOption { Mask = 1024u, Label = "Ring 1", Description = "First ring slot." },
            new EquipmentMaskOption { Mask = 2048u, Label = "Ring 2", Description = "Second ring slot." },
            new EquipmentMaskOption { Mask = 4096u, Label = "Quest Item", Description = "Quest item usage slot found in equipment data." },
            new EquipmentMaskOption { Mask = 8192u, Label = "Face Fashion", Description = "Face fashion slot found in equipment data." },
            new EquipmentMaskOption { Mask = 16384u, Label = "Usable Item", Description = "Usable item mask found in equipment data." },
            new EquipmentMaskOption { Mask = 32768u, Label = "Mount Assist", Description = "Mount Assist slot from the game interface resources." },
            new EquipmentMaskOption { Mask = 262144u, Label = "Fashion Accessory", Description = "Fashion Accessory slot from the game interface resources." },
            new EquipmentMaskOption { Mask = 524288u, Label = "Handhold", Description = "Handhold fashion slot from the game interface resources." },
            new EquipmentMaskOption { Mask = 1048576u, Label = "Fashion Hat", Description = "Fashion Hat slot from the game interface resources." },
            new EquipmentMaskOption { Mask = 2097152u, Label = "Fashion Coat", Description = "Fashion Coat slot from the game interface resources." },
            new EquipmentMaskOption { Mask = 4194304u, Label = "Fashion Pants", Description = "Fashion Pants slot from the game interface resources." },
            new EquipmentMaskOption { Mask = 8388608u, Label = "Shoes Fashion", Description = "Shoes Fashion slot from the game interface resources." },
            new EquipmentMaskOption { Mask = 16777216u, Label = "Fashion Ring", Description = "Fashion Ring slot from the game interface resources." },
            new EquipmentMaskOption { Mask = 33554432u, Label = "Wings of Radiance Fashion", Description = "Wings of Radiance Fashion slot from the game interface resources." }
        };

        public static IList<EquipmentMaskOption> Options
        {
            get { return options; }
        }

        public static bool IsEquipmentMaskFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return string.Equals(normalized, "equip_mask", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "hide_equip_mask", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "show_mask", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "disable_show_mask", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "equip_usingtype_mask", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("_equip_mask", StringComparison.OrdinalIgnoreCase);
        }

        public static string FormatDisplay(string rawValue)
        {
            uint value;
            if (!TryParseValue(rawValue, out value))
            {
                return rawValue ?? string.Empty;
            }

            if (value == 0)
            {
                return "None";
            }

            List<string> labels = new List<string>();
            uint remaining = value;
            for (int i = 0; i < options.Count; i++)
            {
                EquipmentMaskOption option = options[i];
                if (option == null || (value & option.Mask) != option.Mask)
                {
                    continue;
                }

                labels.Add(option.Label ?? string.Empty);
                remaining &= ~option.Mask;
            }

            if (remaining != 0)
            {
                labels.Add("Unknown (0x" + remaining.ToString("X", CultureInfo.InvariantCulture) + ")");
            }

            return labels.Count == 0 ? rawValue ?? string.Empty : string.Join(", ", labels.ToArray());
        }

        public static string NormalizeInput(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            uint parsed;
            if (TryParseValue(value, out parsed))
            {
                return parsed.ToString(CultureInfo.InvariantCulture);
            }

            return value.Trim();
        }

        public static bool TryParseValue(string text, out uint value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return uint.TryParse(trimmed.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
            }

            if (uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                || uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            return TryParseLabels(trimmed, out value);
        }

        private static bool TryParseLabels(string text, out uint value)
        {
            value = 0;
            string[] tokens = text.Split(new[] { ',', ';', '|', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens == null || tokens.Length == 0)
            {
                return false;
            }

            bool matchedAny = false;
            for (int i = 0; i < tokens.Length; i++)
            {
                EquipmentMaskOption option = FindOption(tokens[i]);
                if (option == null)
                {
                    return false;
                }

                value |= option.Mask;
                matchedAny = true;
            }

            return matchedAny;
        }

        private static EquipmentMaskOption FindOption(string text)
        {
            string normalized = NormalizeAlias(text);
            if (normalized == "none" || normalized == "no")
            {
                return new EquipmentMaskOption { Mask = 0u, Label = "None" };
            }

            for (int i = 0; i < options.Count; i++)
            {
                EquipmentMaskOption option = options[i];
                if (option != null && string.Equals(NormalizeAlias(option.Label), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return option;
                }
            }

            if (normalized == "head" || normalized == "hat")
            {
                return FindOption("Helmet");
            }
            if (normalized == "pants")
            {
                return FindOption("Legs");
            }
            if (normalized == "boots" || normalized == "feet")
            {
                return FindOption("Shoes");
            }
            if (normalized == "offhand")
            {
                return FindOption("Bracelet");
            }

            return null;
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
