using System;
using System.Collections.Generic;

namespace FWEledit
{
    public static class NpcSellMoneyTypeCatalog
    {
        public static readonly IList<QualityOption> Options = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Gold coin" },
            new QualityOption { Value = 1, Label = "Soul coin" }
        };

        public static bool IsMoneyTypeField(eListCollection listCollection, int listIndex, int fieldIndex, string fieldName)
        {
            return NpcSellServiceCatalog.TryGetFieldLocation(listCollection, listIndex, fieldIndex, fieldName, out _, out NpcSellServiceFieldType fieldType, out _)
                && fieldType == NpcSellServiceFieldType.MoneyType;
        }

        public static string FormatDisplay(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (!int.TryParse(value.Trim(), out int numericValue))
            {
                return value;
            }

            switch (numericValue)
            {
                case 0:
                    return "0 - Gold coin";
                case 1:
                    return "1 - Soul coin";
                default:
                    return numericValue.ToString();
            }
        }

        public static string NormalizeInput(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "0";
            }

            string normalized = value.Trim();
            if (int.TryParse(normalized, out int numericValue))
            {
                return numericValue <= 0 ? "0" : "1";
            }

            if (normalized.IndexOf("soul", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "1";
            }

            if (normalized.IndexOf("gold", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "0";
            }

            return normalized;
        }
    }
}
