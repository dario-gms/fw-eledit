using System;
using System.Globalization;

namespace FWEledit
{
    class AIRCRAFT_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = string.Empty;
            try
            {
                string requireLevel = GetFieldValue(sessionService, 21, pos_item, "require_level");
                if (requireLevel != "0")
                {
                    line += "\nLv" + requireLevel;
                }

                string requirePlayerFlyLevel = GetFieldValue(sessionService, 21, pos_item, "require_player_fly_level");
                if (requirePlayerFlyLevel != "0")
                {
                    line += "\n^FFFF00Suggested Flying Level " + requirePlayerFlyLevel + "^FFFFFF";
                }

                string startFlyCostTime = GetFieldValue(sessionService, 21, pos_item, "start_fly_cost_time");
                if (startFlyCostTime != "0")
                {
                    line += "\nDeparture Time " + FormatNumber(startFlyCostTime, 0, 1) + " sec";
                }

                string cruiseSpeed = GetFieldValue(sessionService, 21, pos_item, "cruise_speed");
                if (cruiseSpeed != "0")
                {
                    line += "\nCruising Speed " + FormatCruiseSpeedPercent(cruiseSpeed);
                }

                string sprintSpeed = GetFieldValue(sessionService, 21, pos_item, "sprint_speed");
                if (sprintSpeed != "0")
                {
                    line += "\nAccelerating Speed " + FormatSprintSpeedPercent(sprintSpeed);
                }

                string cruiseCostMp = GetFieldValue(sessionService, 21, pos_item, "cruise_cost_mp");
                if (cruiseCostMp != "0")
                {
                    line += "\nCruising Mana Cost " + FormatNumber(cruiseCostMp, 1, 1) + " points/s";
                }

                string sprintCostEnergy = GetFieldValue(sessionService, 21, pos_item, "sprint_cost_energy");
                if (sprintCostEnergy != "0")
                {
                    line += "\nAccelerating Energy Cost " + FormatNumber(sprintCostEnergy, 1, 1) + " points/s";
                }

                string canChangeColor = GetFieldValue(sessionService, 21, pos_item, "can_change_color");
                if (canChangeColor == "0")
                {
                    line += "\n^FFFF00Unable to dye^FFFFFF";
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

        private static string FormatCruiseSpeedPercent(string rawValue)
        {
            float value;
            if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.TryParse(rawValue, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return rawValue;
            }

            return (value * 25F).ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatSprintSpeedPercent(string rawValue)
        {
            float value;
            if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.TryParse(rawValue, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return rawValue;
            }

            return (value * 25F).ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatNumber(string rawValue, int minDecimals, int maxDecimals)
        {
            float value;
            if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.TryParse(rawValue, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            {
                return rawValue;
            }

            string format = maxDecimals <= 0
                ? "0"
                : "0." + new string('0', minDecimals) + new string('#', Math.Max(0, maxDecimals - minDecimals));
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
