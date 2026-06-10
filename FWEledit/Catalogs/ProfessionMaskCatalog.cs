using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public sealed class ProfessionMaskOption
    {
        public uint Mask { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Label ?? string.Empty;
        }
    }

    public static class ProfessionMaskCatalog
    {
        private const uint MaskWarrior = 1u << 1;
        private const uint MaskProtector = 1u << 2;
        private const uint MaskAssassin = 1u << 3;
        private const uint MaskMarksman = 1u << 4;
        private const uint MaskMage = 1u << 5;
        private const uint MaskPriest = 1u << 6;
        private const uint MaskVampire = 1u << 7;
        private const uint MaskBard = 1u << 8;
        private const uint MaskBloodRaider = 1u << 9;

        private static readonly List<ProfessionMaskOption> professionOptions = new List<ProfessionMaskOption>
        {
            new ProfessionMaskOption { Mask = MaskWarrior, Label = "Warrior", Description = "Show this page to Warriors." },
            new ProfessionMaskOption { Mask = MaskProtector, Label = "Protector", Description = "Show this page to Protectors." },
            new ProfessionMaskOption { Mask = MaskAssassin, Label = "Assassin", Description = "Show this page to Assassins." },
            new ProfessionMaskOption { Mask = MaskMarksman, Label = "Marksman", Description = "Show this page to Marksmen." },
            new ProfessionMaskOption { Mask = MaskMage, Label = "Mage", Description = "Show this page to Mages." },
            new ProfessionMaskOption { Mask = MaskPriest, Label = "Priest", Description = "Show this page to Priests." },
            new ProfessionMaskOption { Mask = MaskVampire, Label = "Vampire", Description = "Show this page to Vampires." },
            new ProfessionMaskOption { Mask = MaskBard, Label = "Bard", Description = "Show this page to Bards." },
            new ProfessionMaskOption { Mask = MaskBloodRaider, Label = "Blood Raider", Description = "Show this page to Blood Raiders." }
        };

        private const uint AllProfessionsMask =
            MaskWarrior
            | MaskProtector
            | MaskAssassin
            | MaskMarksman
            | MaskMage
            | MaskPriest
            | MaskVampire
            | MaskBard
            | MaskBloodRaider;

        public static IList<ProfessionMaskOption> Options
        {
            get { return professionOptions; }
        }

        public static bool IsProfessionMaskFieldName(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.IndexOf("mask_profession", StringComparison.OrdinalIgnoreCase) >= 0;
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
                return "No professions";
            }

            if ((value & AllProfessionsMask) == AllProfessionsMask)
            {
                return "All professions";
            }

            List<string> labels = new List<string>();
            uint remaining = value;
            for (int i = 0; i < professionOptions.Count; i++)
            {
                ProfessionMaskOption option = professionOptions[i];
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
                ? "No professions"
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

                ProfessionMaskOption option = FindOption(token);
                if (option == null)
                {
                    return false;
                }

                parsedValue |= option.Mask;
                matchedAny = true;
            }

            return matchedAny;
        }

        private static ProfessionMaskOption FindOption(string token)
        {
            for (int i = 0; i < professionOptions.Count; i++)
            {
                ProfessionMaskOption option = professionOptions[i];
                if (option == null)
                {
                    continue;
                }

                if (string.Equals(option.Label, token, StringComparison.OrdinalIgnoreCase))
                {
                    return option;
                }
            }

            return null;
        }
    }
}
