using System;
using System.Globalization;

namespace FWEledit
{
    public static class ProbabilityDisplayService
    {
        private enum PercentageStorageScale
        {
            UnitFraction,
            WholePercent,
            BasisPoints,
            MillionthFraction,
            EncodedFloatFraction
        }

        public static bool IsProbabilityFieldName(string fieldName)
        {
            PercentageStorageScale scale;
            return TryResolveScale(fieldName, null, null, out scale);
        }

        public static string FormatDisplay(string fieldName, string fieldType, string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            PercentageStorageScale scale;
            if (!TryResolveScale(fieldName, fieldType, rawValue, out scale))
            {
                return rawValue;
            }

            double value;
            if (!TryParseNumber(rawValue, out value))
            {
                return rawValue;
            }

            double percentage = ToPercentage(value, scale);
            return percentage.ToString("0.##", CultureInfo.CurrentCulture) + "%";
        }

        public static string NormalizeInput(string fieldName, string fieldType, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            PercentageStorageScale scale;
            if (!TryResolveScale(fieldName, fieldType, value, out scale))
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
                normalized = FromPercentage(parsed, scale);
            }
            else
            {
                normalized = NormalizeNonPercentInput(parsed, scale);
            }

            if (IsIntegralFieldType(fieldType))
            {
                return Math.Round(normalized).ToString("0", CultureInfo.CurrentCulture);
            }

            return normalized.ToString("0.000000", CultureInfo.CurrentCulture);
        }

        private static bool TryParseNumber(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryResolveScale(string fieldName, string fieldType, string rawValue, out PercentageStorageScale scale)
        {
            scale = PercentageStorageScale.UnitFraction;
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            if (string.Equals(normalized, "catch_pet_success_factor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "catch_pet_hp_limit", StringComparison.OrdinalIgnoreCase))
            {
                scale = PercentageStorageScale.UnitFraction;
                return true;
            }

            if (normalized.StartsWith("catch_pet_reward_item_", StringComparison.OrdinalIgnoreCase)
                && normalized.EndsWith("_probability", StringComparison.OrdinalIgnoreCase))
            {
                scale = PercentageStorageScale.EncodedFloatFraction;
                return true;
            }

            if (normalized.StartsWith("decompose_sub_result_", StringComparison.OrdinalIgnoreCase)
                && normalized.EndsWith("_ratio", StringComparison.OrdinalIgnoreCase))
            {
                scale = PercentageStorageScale.EncodedFloatFraction;
                return true;
            }

            bool looksLikeProbability = normalized.IndexOf("probability", StringComparison.OrdinalIgnoreCase) >= 0;
            bool looksLikePercent = normalized.IndexOf("percent", StringComparison.OrdinalIgnoreCase) >= 0;
            bool looksLikeRatio = normalized.EndsWith("_ratio", StringComparison.OrdinalIgnoreCase)
                || normalized.IndexOf("_ratio_", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.StartsWith("quality_ratio_", StringComparison.OrdinalIgnoreCase);

            if (!looksLikeProbability && !looksLikePercent && !looksLikeRatio)
            {
                return false;
            }

            if (looksLikePercent)
            {
                scale = PercentageStorageScale.WholePercent;
                return true;
            }

            if (looksLikeRatio)
            {
                scale = IsIntegralFieldType(fieldType)
                    ? PercentageStorageScale.BasisPoints
                    : PercentageStorageScale.UnitFraction;
                return true;
            }

            if (looksLikeProbability)
            {
                if (IsIntegralFieldType(fieldType))
                {
                    scale = PercentageStorageScale.BasisPoints;
                    return true;
                }

                if (IsFloatingFieldType(fieldType))
                {
                    scale = PercentageStorageScale.UnitFraction;
                    return true;
                }

                double numericValue;
                if (TryParseNumber(rawValue ?? string.Empty, out numericValue))
                {
                    scale = Math.Abs(numericValue) > 1d
                        ? PercentageStorageScale.BasisPoints
                        : PercentageStorageScale.UnitFraction;
                    return true;
                }

                scale = PercentageStorageScale.UnitFraction;
                return true;
            }

            return false;
        }

        private static bool IsIntegralFieldType(string fieldType)
        {
            return string.Equals(fieldType, "int16", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldType, "int32", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldType, "int64", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFloatingFieldType(string fieldType)
        {
            return string.Equals(fieldType, "float", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldType, "double", StringComparison.OrdinalIgnoreCase);
        }

        private static double ToPercentage(double value, PercentageStorageScale scale)
        {
            switch (scale)
            {
                case PercentageStorageScale.WholePercent:
                    return value;
                case PercentageStorageScale.BasisPoints:
                    return value / 100d;
                case PercentageStorageScale.MillionthFraction:
                    return value / 10000d;
                case PercentageStorageScale.EncodedFloatFraction:
                    return DecodeEncodedFloatFraction(value) * 100d;
                default:
                    return value * 100d;
            }
        }

        private static double FromPercentage(double percentage, PercentageStorageScale scale)
        {
            switch (scale)
            {
                case PercentageStorageScale.WholePercent:
                    return percentage;
                case PercentageStorageScale.BasisPoints:
                    return percentage * 100d;
                case PercentageStorageScale.MillionthFraction:
                    return percentage * 10000d;
                case PercentageStorageScale.EncodedFloatFraction:
                    return EncodeEncodedFloatFraction(percentage / 100d);
                default:
                    return percentage / 100d;
            }
        }

        private static double NormalizeNonPercentInput(double parsed, PercentageStorageScale scale)
        {
            switch (scale)
            {
                case PercentageStorageScale.WholePercent:
                    return parsed;
                case PercentageStorageScale.BasisPoints:
                    return Math.Abs(parsed) <= 1d ? parsed * 10000d : parsed * 100d;
                case PercentageStorageScale.MillionthFraction:
                    return Math.Abs(parsed) <= 1d ? parsed * 1000000d : parsed * 10000d;
                case PercentageStorageScale.EncodedFloatFraction:
                    return EncodeEncodedFloatFraction(Math.Abs(parsed) > 1d ? parsed / 100d : parsed);
                default:
                    return Math.Abs(parsed) > 1d ? parsed / 100d : parsed;
            }
        }

        private static float DecodeEncodedFloatFraction(double encoded)
        {
            int bits = Convert.ToInt32(Math.Round(encoded));
            return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        }

        private static int EncodeEncodedFloatFraction(double fraction)
        {
            float bounded = (float)fraction;
            return BitConverter.ToInt32(BitConverter.GetBytes(bounded), 0);
        }
    }
}
