using System.Drawing;

namespace FWEledit
{
    public sealed class ColorShadeService
    {
        public Color Darken(Color color, float factor)
        {
            float f = System.Math.Max(0.0f, System.Math.Min(1.0f, factor));
            int r = System.Math.Max(0, System.Math.Min(255, (int)(color.R * f)));
            int g = System.Math.Max(0, System.Math.Min(255, (int)(color.G * f)));
            int b = System.Math.Max(0, System.Math.Min(255, (int)(color.B * f)));
            return Color.FromArgb(r, g, b);
        }
    }
}
