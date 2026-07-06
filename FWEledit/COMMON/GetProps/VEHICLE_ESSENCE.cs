using System;
using System.Globalization;

namespace FWEledit
{
    class VEHICLE_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = string.Empty;
            try
            {
                string requireLevel = GetFieldValue(sessionService, 77, pos_item, "require_level");
                if (requireLevel != "0")
                {
                    line += "\nLv" + requireLevel;
                }

                string speed = GetFieldValue(sessionService, 77, pos_item, "speed");
                if (speed != "0")
                {
                    line += "\nMount Speed: " + FormatMountedSpeedPercent(speed);
                }

                string canSwim = GetFieldValue(sessionService, 77, pos_item, "can_swim");
                if (canSwim == "1")
                {
                    line += "\nAble to swim";
                }

                string canAttack = GetFieldValue(sessionService, 77, pos_item, "can_attack");
                if (canAttack == "1")
                {
                    line += "\n^FFFF00Enable Combat with License.^FFFFFF";
                }
            }
            catch
            {
                line = string.Empty;
            }

            return line;
        }

        private static string GetFieldValue(ISessionService sessionService, int listIndex, int posItem, string fieldName)
        {
            for (int k = 0; k < sessionService.ListCollection.Lists[listIndex].elementFields.Length; k++)
            {
                if (string.Equals(sessionService.ListCollection.Lists[listIndex].elementFields[k], fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return sessionService.ListCollection.GetValue(listIndex, posItem, k);
                }
            }

            return "0";
        }

        private static string FormatMountedSpeedPercent(string rawValue)
        {
            float value;
            if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.TryParse(rawValue, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return rawValue;
            }

            return (100F + (value * 25F)).ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }
    }
}
