using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public sealed class ImmuneTypeOption
    {
        public uint Value { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Label ?? string.Empty;
        }
    }

    public static class ImmuneTypeCatalog
    {
        private static readonly List<ImmuneTypeOption> options = new List<ImmuneTypeOption>
        {
            new ImmuneTypeOption { Value = 0x0001, Label = "Immune to fear", Description = "Prevents fear and panic-style control effects." },
            new ImmuneTypeOption { Value = 0x0002, Label = "Immune to blind", Description = "Prevents blindness and vision-loss effects." },
            new ImmuneTypeOption { Value = 0x0004, Label = "Immune to knockback", Description = "Prevents pushback and knockback displacement." },
            new ImmuneTypeOption { Value = 0x0008, Label = "Immune to MP burn", Description = "Prevents mana burn and MP drain effects." },
            new ImmuneTypeOption { Value = 0x0010, Label = "Immune to defense % reduction", Description = "Prevents percentage-based total defense reduction." },
            new ImmuneTypeOption { Value = 0x0020, Label = "Immune to flat defense reduction", Description = "Prevents flat defense-lowering effects." },
            new ImmuneTypeOption { Value = 0x0040, Label = "Immune to stun", Description = "Prevents stun and similar hard crowd control." },
            new ImmuneTypeOption { Value = 0x0080, Label = "Immune to weaken", Description = "Prevents weakening or weakening-style debuffs." },
            new ImmuneTypeOption { Value = 0x0100, Label = "Immune to slow", Description = "Prevents movement speed reduction effects." },
            new ImmuneTypeOption { Value = 0x0200, Label = "Immune to silence", Description = "Prevents silence and skill-lock effects." },
            new ImmuneTypeOption { Value = 0x0400, Label = "Immune to sleep", Description = "Prevents sleep and forced-sleep effects." },
            new ImmuneTypeOption { Value = 0x0800, Label = "Immune to ensnare", Description = "Prevents root, bind and snare effects." },
            new ImmuneTypeOption { Value = 0x1000, Label = "Immune to hunger", Description = "Prevents forbidden-eat / hunger style effects." },
            new ImmuneTypeOption { Value = 0x2000, Label = "Immune to disarm", Description = "Prevents disarm and empty-hand effects." },
            new ImmuneTypeOption { Value = 0x4000, Label = "Immune to damage over time", Description = "Prevents continuous damage and bleed-like damage over time effects." }
        };

        public static IList<ImmuneTypeOption> Options
        {
            get { return options; }
        }

        public static bool IsImmuneTypeFieldName(string fieldName)
        {
            return string.Equals(fieldName ?? string.Empty, "immune_type", StringComparison.OrdinalIgnoreCase);
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

            List<string> labels = new List<string>();
            uint remaining = value;

            for (int i = 0; i < options.Count; i++)
            {
                ImmuneTypeOption option = options[i];
                if (option == null || (value & option.Value) != option.Value)
                {
                    continue;
                }

                remaining &= ~option.Value;
                labels.Add(option.Label ?? string.Empty);
            }

            if (remaining != 0)
            {
                labels.Add("Unknown flags (0x" + remaining.ToString("X", CultureInfo.InvariantCulture) + ")");
            }

            if (labels.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", labels.ToArray());
        }

        public static string NormalizeInput(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            uint numericValue;
            if (TryParseValue(value, out numericValue))
            {
                return numericValue.ToString(CultureInfo.InvariantCulture);
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

            return uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                || uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
        }
    }
}
