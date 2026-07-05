using System;
using System.Globalization;

namespace FWEledit
{
    public static class MovementSpeedDisplayService
    {
        public static bool IsSupportedField(eListCollection listCollection, int listIndex, string fieldName)
        {
            SpeedFieldKind kind;
            return TryResolveFieldKind(listCollection, listIndex, fieldName, out kind);
        }

        public static string FormatDisplay(eListCollection listCollection, int listIndex, string fieldName, string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            SpeedFieldKind kind;
            if (!TryResolveFieldKind(listCollection, listIndex, fieldName, out kind))
            {
                return rawValue;
            }

            double numericValue;
            if (!TryParseNumber(rawValue, out numericValue))
            {
                return rawValue;
            }

            double percentage = kind == SpeedFieldKind.VehicleMounted
                ? 100d + (numericValue * 25d)
                : numericValue * 25d;

            return percentage.ToString("0.##", CultureInfo.CurrentCulture) + "%";
        }

        public static string NormalizeInput(eListCollection listCollection, int listIndex, string fieldName, string fieldType, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            SpeedFieldKind kind;
            if (!TryResolveFieldKind(listCollection, listIndex, fieldName, out kind))
            {
                return value;
            }

            string trimmed = value.Trim();
            bool isPercent = trimmed.EndsWith("%", StringComparison.Ordinal);
            if (isPercent)
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1).Trim();
            }

            double parsed;
            if (!TryParseNumber(trimmed, out parsed))
            {
                return value;
            }

            double normalized;
            if (isPercent)
            {
                normalized = FromPercentage(kind, parsed);
            }
            else
            {
                normalized = NormalizeNonPercentInput(kind, parsed);
            }

            return IsIntegralFieldType(fieldType)
                ? Math.Round(normalized).ToString("0", CultureInfo.CurrentCulture)
                : normalized.ToString("0.000000", CultureInfo.CurrentCulture);
        }

        private static double FromPercentage(SpeedFieldKind kind, double percentage)
        {
            return kind == SpeedFieldKind.VehicleMounted
                ? (percentage - 100d) / 25d
                : percentage / 25d;
        }

        private static double NormalizeNonPercentInput(SpeedFieldKind kind, double parsed)
        {
            if (kind == SpeedFieldKind.VehicleMounted)
            {
                return Math.Abs(parsed) < 20d
                    ? parsed
                    : FromPercentage(kind, parsed);
            }

            return Math.Abs(parsed) <= 50d
                ? parsed
                : FromPercentage(kind, parsed);
        }

        private static bool TryResolveFieldKind(eListCollection listCollection, int listIndex, string fieldName, out SpeedFieldKind kind)
        {
            kind = SpeedFieldKind.None;
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string listName = listCollection.Lists[listIndex].listName ?? string.Empty;
            if (string.Equals(fieldName, "speed", StringComparison.OrdinalIgnoreCase)
                && listName.IndexOf("VEHICLE_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                kind = SpeedFieldKind.VehicleMounted;
                return true;
            }

            if ((string.Equals(fieldName, "cruise_speed", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fieldName, "sprint_speed", StringComparison.OrdinalIgnoreCase))
                && listName.IndexOf("AIRCRAFT_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                kind = SpeedFieldKind.Aircraft;
                return true;
            }

            return false;
        }

        private static bool TryParseNumber(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool IsIntegralFieldType(string fieldType)
        {
            return string.Equals(fieldType, "int16", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldType, "int32", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldType, "int64", StringComparison.OrdinalIgnoreCase);
        }

        private enum SpeedFieldKind
        {
            None,
            VehicleMounted,
            Aircraft
        }
    }
}
