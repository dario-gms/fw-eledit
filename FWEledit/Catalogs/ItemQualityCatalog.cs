using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public static class ItemQualityCatalog
    {
        private static readonly List<QualityOption> ItemQualityOptions = new List<QualityOption>
        {
            new QualityOption { Value = 0, Label = "Gray" },
            new QualityOption { Value = 1, Label = "White" },
            new QualityOption { Value = 2, Label = "Green" },
            new QualityOption { Value = 3, Label = "Blue" },
            new QualityOption { Value = 4, Label = "Purple" },
            new QualityOption { Value = 5, Label = "Gold" },
            new QualityOption { Value = 6, Label = "Red" },
            new QualityOption { Value = 7, Label = "Gold Red" },
            new QualityOption { Value = 8, Label = "Light Blue" },
            new QualityOption { Value = 9, Label = "Light Purple" }
        };

        private static readonly Dictionary<int, Color> ItemQualityColors = new Dictionary<int, Color>
        {
            { 0, Color.FromArgb(160, 160, 160) }, // Gray
            { 1, Color.White }, // White
            { 2, Color.FromArgb(0, 200, 80) }, // Green
            { 3, Color.RoyalBlue }, // Blue
            { 4, Color.DarkViolet }, // Purple
            { 5, Color.Gold }, // Gold
            { 6, Color.Red }, // Red
            { 7, Color.OrangeRed }, // Gold Red
            { 8, Color.LightSkyBlue }, // Light Blue
            { 9, Color.MediumPurple } // Light Purple
        };

        public static List<QualityOption> Options
        {
            get { return ItemQualityOptions; }
        }

        public static bool TryGetColor(int quality, out Color color)
        {
            return ItemQualityColors.TryGetValue(quality, out color);
        }
    }
}
