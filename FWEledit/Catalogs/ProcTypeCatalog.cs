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
            new ProcTypeOption { Value = 0x0002, Label = "Not droppable", Description = "The item cannot be thrown on the ground." },
            new ProcTypeOption { Value = 0x0004, Label = "Not sellable", Description = "The item cannot be sold to NPCs." },
            new ProcTypeOption { Value = 0x0008, Label = "Cash shop item", Description = "The item is treated as an RMB / cash-shop item." },
            new ProcTypeOption { Value = 0x0010, Label = "Not tradeable", Description = "The item cannot be traded between players." },
            new ProcTypeOption { Value = 0x0020, Label = "Quest item", Description = "The item is marked as quest-related." },
            new ProcTypeOption { Value = 0x0040, Label = "Bind on equip", Description = "Item becomes bound when equipped." },
            new ProcTypeOption { Value = 0x0080, Label = "Already bound", Description = "The item starts already bound." },
            new ProcTypeOption { Value = 0x0100, Label = "Generate unique instance", Description = "The item generates its own GUID / unique instance." },
            new ProcTypeOption { Value = 0x0200, Label = "No stack or split", Description = "The item cannot be stacked or split." },
            new ProcTypeOption { Value = 0x0400, Label = "Not destroyable", Description = "The item cannot be destroyed." },
            new ProcTypeOption { Value = 0x0800, Label = "Reserved flag 1", Description = "Reserved internal flag." },
            new ProcTypeOption { Value = 0x1000, Label = "Reserved flag 2", Description = "Reserved internal flag." },
            new ProcTypeOption { Value = 0x2000, Label = "Reserved flag 3", Description = "Reserved internal flag." },
            new ProcTypeOption { Value = 0x4000, Label = "Keep after expiry", Description = "A timed item stays in place even after expiring." }
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
