using System.Globalization;

namespace FWEledit
{
    public sealed class AddonParamFloatService
    {
        public bool IsAddonParamFloat(int addonType, int paramIndex)
        {
            switch (addonType)
            {
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                case 48:
                case 49:
                case 50:
                case 51:
                case 52:
                case 53:
                case 54:
                case 55:
                case 56:
                case 57:
                case 58:
                case 59:
                case 60:
                case 61:
                case 62:
                case 63:
                case 64:
                case 65:
                case 66:
                case 67:
                case 68:
                case 69:
                case 70:
                case 71:
                case 72:
                case 84:
                case 85:
                case 86:
                case 87:
                case 88:
                case 93:
                case 94:
                case 95:
                case 96:
                case 99:
                case 121:
                case 143:
                    return true;
                case 142:
                    return paramIndex == 1;
                default:
                    return false;
            }
        }

        public bool TryParseFloatFlexible(string raw, out float value)
        {
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }
    }
}
