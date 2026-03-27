using System.Drawing;

namespace FWEledit
{
    public sealed class ItemQualityColorService
    {
        public Color? GetQualityColor(int quality)
        {
            Color color;
            if (ItemQualityCatalog.TryGetColor(quality, out color))
            {
                return color;
            }
            return null;
        }
    }
}
