using System;
using System.Globalization;
using System.Collections.Generic;

namespace FWEledit
{
    public static class NpcSellPriceCatalog
    {
        private const int UnitScale = 1;
        private const int SilverScale = 100;
        private const int GoldScale = 10000;
        private const int SoulGoldScale = 1000000;

        public static int DecodeDisplayAmount(int rawPrice)
        {
            if (rawPrice <= 0)
            {
                return 0;
            }

            int scale = DetectScale(rawPrice);
            return rawPrice / scale;
        }

        public static int EncodeRawAmount(int displayAmount, int previousRawPrice)
        {
            int normalizedAmount = Math.Max(0, displayAmount);
            int scale = DetectScale(previousRawPrice);
            return normalizedAmount * scale;
        }

        public static int EncodeRawAmountWithScale(int displayAmount, int scale)
        {
            int normalizedAmount = Math.Max(0, displayAmount);
            int normalizedScale = scale > 0 ? scale : UnitScale;
            return normalizedAmount * normalizedScale;
        }

        public static int DetectScale(int rawPrice)
        {
            if (rawPrice >= SoulGoldScale && rawPrice % SoulGoldScale == 0)
            {
                return SoulGoldScale;
            }

            if (rawPrice >= GoldScale && rawPrice % GoldScale == 0)
            {
                return GoldScale;
            }

            if (rawPrice >= SilverScale && rawPrice % SilverScale == 0)
            {
                return SilverScale;
            }

            return UnitScale;
        }

        public static string BuildCurrencyDisplay(int moneyType, int rawPrice)
        {
            int scale = DetectScale(rawPrice);
            string family = moneyType <= 0 ? "Gold coin - Cooper" : "Soul coin - Cooper";

            switch (scale)
            {
                case SoulGoldScale:
                    return moneyType <= 0 ? "Gold coin - Diamond" : "Soul coin - Diamond";
                case GoldScale:
                    return moneyType <= 0 ? "Gold coin - Gold" : "Soul coin - Gold";
                case SilverScale:
                    return moneyType <= 0 ? "Gold coin - Silver" : "Soul coin - Silver";
                default:
                    return family;
            }
        }

        public static List<QualityOption> BuildTierOptions(int moneyType)
        {
            List<QualityOption> options = new List<QualityOption>();
            if (moneyType <= 0)
            {
                options.Add(new QualityOption { Value = UnitScale, Label = "Gold coin - Cooper" });
                options.Add(new QualityOption { Value = SilverScale, Label = "Gold coin - Silver" });
                options.Add(new QualityOption { Value = GoldScale, Label = "Gold coin - Gold" });
                options.Add(new QualityOption { Value = SoulGoldScale, Label = "Gold coin - Diamond" });
                return options;
            }

            options.Add(new QualityOption { Value = UnitScale, Label = "Soul coin - Cooper" });
            options.Add(new QualityOption { Value = SilverScale, Label = "Soul coin - Silver" });
            options.Add(new QualityOption { Value = GoldScale, Label = "Soul coin - Gold" });
            options.Add(new QualityOption { Value = SoulGoldScale, Label = "Soul coin - Diamond" });
            return options;
        }

        public static int ParseTierScale(int moneyType, string label, int fallbackScale)
        {
            List<QualityOption> options = BuildTierOptions(moneyType);
            string normalized = (label ?? string.Empty).Trim();
            for (int i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i].Label, normalized, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(options[i].ToString(), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return options[i].Value;
                }
            }

            return fallbackScale > 0 ? fallbackScale : UnitScale;
        }

        public static string FormatRawValue(int rawPrice)
        {
            return rawPrice.ToString(CultureInfo.InvariantCulture);
        }
    }
}
