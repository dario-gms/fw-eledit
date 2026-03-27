using System;
using System.Globalization;

namespace FWEledit
{
    public sealed class AddonParamService
    {
        public bool IsAddonParamField(string fieldName)
        {
            return string.Equals(fieldName, "param1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "param2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "param3", StringComparison.OrdinalIgnoreCase);
        }

        public string FormatAddonParamValueForUi(eListCollection listCollection, int listIndex, int entryIndex, string fieldName, string rawValue)
        {
            if (listCollection == null || listIndex != 0 || !IsAddonParamField(fieldName))
            {
                return rawValue;
            }
            int addonType = GetAddonTypeForEntry(listCollection, listIndex, entryIndex);
            if (addonType < 0)
            {
                return rawValue;
            }
            int paramIndex = string.Equals(fieldName, "param1", StringComparison.OrdinalIgnoreCase) ? 1
                : string.Equals(fieldName, "param2", StringComparison.OrdinalIgnoreCase) ? 2 : 3;
            if (!IsAddonParamFloat(addonType, paramIndex))
            {
                return rawValue;
            }
            int rawInt;
            if (!int.TryParse(rawValue, out rawInt))
            {
                return rawValue;
            }
            float decoded = BitConverter.ToSingle(BitConverter.GetBytes(rawInt), 0);
            return decoded.ToString("0.######", CultureInfo.InvariantCulture);
        }

        public bool TryNormalizeAddonParamValueForStorage(
            eListCollection listCollection,
            int listIndex,
            int entryIndex,
            string fieldName,
            string rawValue,
            out string normalized)
        {
            normalized = rawValue;
            if (listCollection == null || listIndex != 0 || !IsAddonParamField(fieldName))
            {
                return false;
            }

            int addonType = GetAddonTypeForEntry(listCollection, listIndex, entryIndex);
            if (addonType < 0)
            {
                return false;
            }

            int paramIndex = string.Equals(fieldName, "param1", StringComparison.OrdinalIgnoreCase) ? 1
                : string.Equals(fieldName, "param2", StringComparison.OrdinalIgnoreCase) ? 2 : 3;
            if (!IsAddonParamFloat(addonType, paramIndex))
            {
                return false;
            }

            float parsed;
            if (!TryParseFloatFlexible(rawValue, out parsed))
            {
                return false;
            }

            int raw = BitConverter.ToInt32(BitConverter.GetBytes(parsed), 0);
            normalized = raw.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private int GetAddonTypeForEntry(eListCollection listCollection, int listIndex, int entryIndex)
        {
            if (listCollection == null || listIndex != 0 || entryIndex < 0 || entryIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return -1;
            }
            int typeFieldIndex = -1;
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "type", StringComparison.OrdinalIgnoreCase))
                {
                    typeFieldIndex = i;
                    break;
                }
            }
            if (typeFieldIndex < 0)
            {
                return -1;
            }
            int addonType;
            return int.TryParse(listCollection.GetValue(listIndex, entryIndex, typeFieldIndex), out addonType) ? addonType : -1;
        }

        private bool IsAddonParamFloat(int addonType, int paramIndex)
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

        private bool TryParseFloatFlexible(string raw, out float value)
        {
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }
    }
}
