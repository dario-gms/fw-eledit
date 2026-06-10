using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public sealed class ProcTypeOption
    {
        public uint Value { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Label ?? string.Empty;
        }
    }

    public static class ProcTypeCatalog
    {
        private static readonly List<ProcTypeOption> ProcTypeOptions = new List<ProcTypeOption>
        {
            new ProcTypeOption { Value = 0x0001, Label = "No drop on death", Description = "The item will not drop when the character dies." },
            new ProcTypeOption { Value = 0x0002, Label = "Unable to discard", Description = "The item cannot be thrown on the ground." },
            new ProcTypeOption { Value = 0x0004, Label = "Unable to sell", Description = "The item cannot be sold to NPCs." },
            new ProcTypeOption { Value = 0x0008, Label = "Cash shop item", Description = "The client treats this as an RMB / boutique item." },
            new ProcTypeOption { Value = 0x0010, Label = "Unable to trade", Description = "The item cannot be traded between players." },
            new ProcTypeOption { Value = 0x0020, Label = "Quest item", Description = "The item is marked as a quest item." },
            new ProcTypeOption { Value = 0x0040, Label = "Binds when equipped", Description = "The item becomes bound after it is equipped." },
            new ProcTypeOption { Value = 0x0080, Label = "Bound", Description = "The item starts already bound." },
            new ProcTypeOption { Value = 0x0100, Label = "Unique instance", Description = "The client generates a unique GUID for this item." },
            new ProcTypeOption { Value = 0x0200, Label = "Cannot split or stack", Description = "The item cannot be split into stacks and cannot be stacked." },
            new ProcTypeOption { Value = 0x0400, Label = "Unable to destroy", Description = "The item cannot be destroyed." },
            new ProcTypeOption { Value = 0x0800, Label = "Reserved flag 1", Description = "Internal client flag with unknown gameplay text." },
            new ProcTypeOption { Value = 0x1000, Label = "Reserved flag 2", Description = "Internal client flag with unknown gameplay text." },
            new ProcTypeOption { Value = 0x2000, Label = "Reserved flag 3", Description = "Internal client flag with unknown gameplay text." },
            new ProcTypeOption { Value = 0x4000, Label = "Does not disappear after expiry", Description = "A timed item stays in the inventory after its timer expires." }
        };

        public static List<ProcTypeOption> Options
        {
            get { return ProcTypeOptions; }
        }

        public static bool IsProcTypeFieldName(string fieldName)
        {
            return string.Equals(fieldName ?? string.Empty, "proc_type", StringComparison.OrdinalIgnoreCase);
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

            for (int i = 0; i < ProcTypeOptions.Count; i++)
            {
                ProcTypeOption option = ProcTypeOptions[i];
                if (option == null || (value & option.Value) != option.Value)
                {
                    continue;
                }

                remaining &= ~option.Value;
                labels.Add(option.Label ?? string.Empty);
            }

            if (remaining != 0)
            {
                labels.Add("Unknown (0x" + remaining.ToString("X", CultureInfo.InvariantCulture) + ")");
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

            string trimmed = value.Trim();
            uint numericValue;
            if (TryParseValue(trimmed, out numericValue))
            {
                return numericValue.ToString(CultureInfo.InvariantCulture);
            }

            return trimmed;
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
