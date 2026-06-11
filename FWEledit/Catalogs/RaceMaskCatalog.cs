using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public sealed class RaceMaskOption
    {
        public uint Mask { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Label ?? string.Empty;
        }
    }

    public static class RaceMaskCatalog
    {
        private const uint MaskHuman = 1u << 1;
        private const uint MaskElf = 1u << 2;
        private const uint MaskDwarf = 1u << 3;
        private const uint MaskStoneman = 1u << 4;
        private const uint MaskKindred = 1u << 5;
        private const uint MaskLycan = 1u << 6;

        private static readonly List<RaceMaskOption> raceOptions = new List<RaceMaskOption>
        {
            new RaceMaskOption { Mask = MaskHuman, Label = "Human", Description = "Allow Human characters to use this." },
            new RaceMaskOption { Mask = MaskElf, Label = "Elf", Description = "Allow Elf characters to use this." },
            new RaceMaskOption { Mask = MaskDwarf, Label = "Dwarf", Description = "Allow Dwarf characters to use this." },
            new RaceMaskOption { Mask = MaskStoneman, Label = "Stoneman", Description = "Allow Stoneman characters to use this." },
            new RaceMaskOption { Mask = MaskKindred, Label = "Kindred", Description = "Allow Kindred characters to use this." },
            new RaceMaskOption { Mask = MaskLycan, Label = "Lycan", Description = "Allow Lycan characters to use this." }
        };

        private const uint AllKnownRacesMask =
            MaskHuman
            | MaskElf
            | MaskDwarf
            | MaskStoneman
            | MaskKindred
            | MaskLycan;

        public static IList<RaceMaskOption> Options
        {
            get { return raceOptions; }
        }

        public static bool IsRaceMaskFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return normalized.IndexOf("mask_race", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("race_mask", StringComparison.OrdinalIgnoreCase) >= 0;
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

        public static string FormatDisplay(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            uint value;
            if (!TryParseValue(rawValue, out value))
            {
                return rawValue ?? string.Empty;
            }

            if (value == 0)
            {
                return "No races";
            }

            if ((value & AllKnownRacesMask) == AllKnownRacesMask)
            {
                return "All races";
            }

            List<string> labels = new List<string>();
            uint remaining = value;
            for (int i = 0; i < raceOptions.Count; i++)
            {
                RaceMaskOption option = raceOptions[i];
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

            return labels.Count == 0
                ? "No races"
                : string.Join(", ", labels.ToArray());
        }

        private static bool TryParseLabels(string value, out uint parsedValue)
        {
            parsedValue = 0;
            string[] tokens = value.Split(new[] { ',', ';', '|', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens == null || tokens.Length == 0)
            {
                return false;
            }

            bool matchedAny = false;
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                RaceMaskOption option = FindOption(token);
                if (option == null)
                {
                    return false;
                }

                parsedValue |= option.Mask;
                matchedAny = true;
            }

            return matchedAny;
        }

        private static RaceMaskOption FindOption(string token)
        {
            string normalized = NormalizeAlias(token);
            for (int i = 0; i < raceOptions.Count; i++)
            {
                RaceMaskOption option = raceOptions[i];
                if (option == null)
                {
                    continue;
                }

                if (string.Equals(NormalizeAlias(option.Label), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return option;
                }
            }

            if (normalized == "giant")
            {
                return FindOption("Stoneman");
            }

            if (normalized == "vampire")
            {
                return FindOption("Kindred");
            }

            if (normalized == "werewolf")
            {
                return FindOption("Lycan");
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
