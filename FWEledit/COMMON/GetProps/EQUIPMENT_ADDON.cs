using System;
using System.Globalization;
using System.Collections.Generic;

namespace sELedit
{
    class EQUIPMENT_ADDON
    {
        private static readonly Dictionary<int, string> SkillTokenCache = new Dictionary<int, string>();

        public static void ResetRuntimeCaches()
        {
            SkillTokenCache.Clear();
        }

        private static int ToIntSafe(string value, int fallback = 0)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static float ToFloatFromParam(string value)
        {
            try
            {
                int raw = ToIntSafe(value, 0);
                return BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
            }
            catch
            {
                return 0f;
            }
        }

        private static string CleanSkillName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "$skill";
            }
            string clean = raw;
            try
            {
                clean = Extensions.ColorClean(clean);
            }
            catch
            { }
            clean = clean.Replace("\r", " ").Replace("\n", " ").Trim();
            if (string.IsNullOrWhiteSpace(clean) || clean.StartsWith("NOT FOUND", StringComparison.OrdinalIgnoreCase))
            {
                return "$skill";
            }
            return clean;
        }

        private static string GetSkillTokenFromParam(string param)
        {
            int skillId = ToIntSafe(param, 0);
            if (skillId <= 0)
            {
                return "$skill";
            }
            string cached;
            if (SkillTokenCache.TryGetValue(skillId, out cached))
            {
                return cached;
            }
            try
            {
                string resolved = CleanSkillName(Extensions.SkillName(skillId));
                SkillTokenCache[skillId] = resolved;
                return resolved;
            }
            catch
            {
                return "$skill";
            }
        }

        private static bool TryGetFwAddonText(int addonType, string param1, string param2, out string text)
        {
            text = string.Empty;
            int p1 = ToIntSafe(param1, 0);
            int p2 = ToIntSafe(param2, 0);
            float f1 = ToFloatFromParam(param1);

            switch (addonType)
            {
                case 0:
                    text = "Physical Attack +" + p1;
                    return true;
                case 89:
                    text = "Accuracy +" + p1;
                    return true;
                case 92:
                    text = "Not using 92";
                    return true;
                case 93:
                    text = "Gear Attack +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 94:
                    text = "Gear Defense +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 95:
                    text = "Max Gear Health +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 96:
                    text = "Max Gear Mana +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 99:
                    text = "Reduces drop rate of Wrath orbs when hit by +" + f1.ToString("F1", CultureInfo.CreateSpecificCulture("en-US")) + "%";
                    return true;
                case 75:
                    text = "+damage taken decreased by " + p1;
                    return true;
                case 1:
                    text = "Health +" + p1;
                    return true;
                case 2:
                    text = "Mana +" + p1;
                    return true;
                case 3:
                    text = "Attack +" + p1;
                    return true;
                case 4:
                    text = "Defense +" + p1;
                    return true;
                case 5:
                    text = "Physical Mastery +" + p1;
                    return true;
                case 6:
                    text = "Earth Mastery +" + p1;
                    return true;
                case 7:
                    text = "Water Mastery +" + p1;
                    return true;
                case 8:
                    text = "Fire Mastery +" + p1;
                    return true;
                case 9:
                    text = "Wind Mastery +" + p1;
                    return true;
                case 10:
                    text = "Light Mastery +" + p1;
                    return true;
                case 11:
                    text = "Darkness Mastery +" + p1;
                    return true;
                case 12:
                    text = "None Mastery +" + p1;
                    return true;
                case 13:
                    text = "Stun Mastery +" + p1;
                    return true;
                case 14:
                    text = "Slow Mastery +" + p1;
                    return true;
                case 15:
                    text = "Immobilize Mastery +" + p1;
                    return true;
                case 16:
                    text = "Sleep Mastery +" + p1;
                    return true;
                case 17:
                    text = "Silent Mastery +" + p1;
                    return true;
                case 18:
                    text = "Disarm Mastery +" + p1;
                    return true;
                case 19:
                case 20:
                    text = "Misc. Mastery +" + p1;
                    return true;
                case 21:
                    text = "Physical Resistance +" + p1;
                    return true;
                case 22:
                    text = "Earth Resistance +" + p1;
                    return true;
                case 23:
                    text = "Water Resistance +" + p1;
                    return true;
                case 24:
                    text = "Fire Resistance +" + p1;
                    return true;
                case 25:
                    text = "Wind Resistance +" + p1;
                    return true;
                case 26:
                    text = "Light Resistance +" + p1;
                    return true;
                case 27:
                    text = "Darkness Resistance +" + p1;
                    return true;
                case 28:
                    text = "None Resistance +" + p1;
                    return true;
                case 29:
                    text = "Stun Resistance +" + p1;
                    return true;
                case 30:
                    text = "Slow Resistance +" + p1;
                    return true;
                case 31:
                    text = "Immobilize Resistance +" + p1;
                    return true;
                case 32:
                    text = "Sleep Resistance +" + p1;
                    return true;
                case 33:
                    text = "Silent Resistance +" + p1;
                    return true;
                case 34:
                    text = "Disarm Resistance +" + p1;
                    return true;
                case 35:
                case 36:
                    text = "Misc. Resistance +" + p1;
                    return true;
                case 37:
                    text = "Health +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 38:
                    text = "Mana +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 39:
                    text = "Attack +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 40:
                    text = "Defense +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 41:
                    text = "Physical Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 42:
                    text = "Earth Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 43:
                    text = "Water Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 44:
                    text = "Fire Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 45:
                    text = "Wind Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 46:
                    text = "Light Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 47:
                    text = "Darkness Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 48:
                    text = "None Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 49:
                    text = "Stun Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 50:
                    text = "Slow Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 51:
                    text = "Immobilize Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 52:
                    text = "Sleep Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 53:
                    text = "Silent Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 54:
                    text = "Disarm Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 55:
                case 56:
                    text = "Misc. Mastery +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 57:
                    text = "Physical Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 58:
                    text = "Earth Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 59:
                    text = "Water Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 60:
                    text = "Fire Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 61:
                    text = "Wind Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 62:
                    text = "Light Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 63:
                    text = "Darkness Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 64:
                    text = "None Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 65:
                    text = "Stun Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 66:
                    text = "Slow Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 67:
                    text = "Immobilize Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 68:
                    text = "Sleep Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 69:
                    text = "Silent Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 70:
                    text = "Disarm Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 71:
                case 72:
                    text = "Misc. Resistance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 90:
                    text = "Evasion +" + p1;
                    return true;
                case 77:
                case 81:
                    text = "Skill " + GetSkillTokenFromParam(param1) + " Lv" + (p2 > 0 ? p2.ToString() : "1");
                    return true;
                case 79:
                    text = "Increases Damage by " + p1;
                    return true;
                case 84:
                    text = "Crit Chance +" + f1.ToString("F1", CultureInfo.CreateSpecificCulture("en-US")) + "%";
                    return true;
                case 85:
                    text = "Crit Damage +" + f1.ToString("F1", CultureInfo.CreateSpecificCulture("en-US")) + "%";
                    return true;
                case 86:
                    text = "Crit Dodge +" + f1.ToString("F1", CultureInfo.CreateSpecificCulture("en-US")) + "%";
                    return true;
                case 87:
                    text = "Crit Defense +" + f1.ToString("F1", CultureInfo.CreateSpecificCulture("en-US")) + "%";
                    return true;
                case 88:
                    text = "Movement Speed +" + f1.ToString("F2", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 104:
                    text = "Physical Attack +" + p1;
                    return true;
                case 105:
                    text = "Earth Attack +" + p1;
                    return true;
                case 106:
                    text = "Water Attack +" + p1;
                    return true;
                case 107:
                    text = "Fire Attack +" + p1;
                    return true;
                case 108:
                    text = "Wind Attack +" + p1;
                    return true;
                case 109:
                    text = "Light Attack +" + p1;
                    return true;
                case 110:
                    text = "Dark Attack +" + p1;
                    return true;
                case 111:
                    text = "No-Attribute Attack +" + p1;
                    return true;
                case 112:
                    text = "Physical Defense +" + p1;
                    return true;
                case 113:
                    text = "Earth Defense +" + p1;
                    return true;
                case 114:
                    text = "Water Defense +" + p1;
                    return true;
                case 115:
                    text = "Fire Defense +" + p1;
                    return true;
                case 116:
                    text = "Wind Defense +" + p1;
                    return true;
                case 117:
                    text = "Light Defense +" + p1;
                    return true;
                case 118:
                    text = "Dark Defense +" + p1;
                    return true;
                case 119:
                    text = "No-Attribute Defense +" + p1;
                    return true;
                case 120:
                    text = "Healing Effect +" + p1;
                    return true;
                case 121:
                    text = "Crit Heal Chance +" + f1.ToString("P0", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 122:
                    text = "Max EXP +" + p1;
                    return true;
                case 123:
                    text = "Physical Mana Cost -" + p1 + "%";
                    return true;
                case 124:
                    text = "Earth Mana Cost -" + p1 + "%";
                    return true;
                case 125:
                    text = "Water Mana Cost -" + p1 + "%";
                    return true;
                case 126:
                    text = "Fire Mana Cost -" + p1 + "%";
                    return true;
                case 127:
                    text = "Wind Mana Cost -" + p1 + "%";
                    return true;
                case 128:
                    text = "Light Mana Cost -" + p1 + "%";
                    return true;
                case 129:
                    text = "Darkness Mana Cost -" + p1 + "%";
                    return true;
                case 130:
                    text = "All skills Mana Cost -" + p1 + "%";
                    return true;
                case 131:
                    text = "Blessing skills Mana Cost -" + p1 + "%";
                    return true;
                case 132:
                    text = "Curse skills Mana Cost -" + p1 + "%";
                    return true;
                case 133:
                    text = "All Masteries + " + p1;
                    return true;
                case 134:
                    text = "All Resistances + " + p1;
                    return true;
                case 135:
                    text = "All Masteries +" + p1 + "%";
                    return true;
                case 136:
                    text = "Resistance Gain +" + p1;
                    return true;
                case 137:
                    text = "Mounted Combat";
                    return true;
                case 138:
                    text = "Riding level +" + p1;
                    return true;
                case 139:
                    text = "Increases Damage by " + p1;
                    return true;
                case 140:
                    text = "Mount Speed +" + (p1 / 100f).ToString("F2", CultureInfo.CreateSpecificCulture("en-US"));
                    return true;
                case 141:
                    text = "Decreases direct damage by " + p1;
                    return true;
                case 142:
                    text = "Decreases reflected damage by " + f1.ToString("F1", CultureInfo.CreateSpecificCulture("en-US")) + "% plus " + p2;
                    return true;
                case 143:
                    text = "Reduces Damage over Time by " + f1.ToString("F1", CultureInfo.CreateSpecificCulture("en-US")) + "%";
                    return true;
                case 144:
                    text = "PVE Intensity +" + p1;
                    return true;
                case 145:
                    text = "PVE Tenacity +" + p1;
                    return true;
                default:
                    return false;
            }
        }

        public static string GetAddonTypeDisplay(string rawType)
        {
            int addonType = ToIntSafe(rawType, int.MinValue);
            switch (addonType)
            {
                case 0: return "Physical Attack +%d";
                case 89: return "Accuracy +%d";
                case 92: return "Not using 92";
                case 93: return "Gear Attack +%.0f%%";
                case 94: return "Gear Defense +%.0f%%";
                case 95: return "Max Gear Health +%.0f%%";
                case 96: return "Max Gear Mana +%.0f%%";
                case 99: return "Reduces drop rate of Wrath orbs when hit by +%.1f%%";
                case 75: return "+damage taken decreased by %d";
                case 1: return "Health +%d";
                case 2: return "Mana +%d";
                case 3: return "Attack +%d";
                case 4: return "Defense +%d";
                case 5: return "Physical Mastery +%d";
                case 6: return "Earth Mastery +%d";
                case 7: return "Water Mastery +%d";
                case 8: return "Fire Mastery +%d";
                case 9: return "Wind Mastery +%d";
                case 10: return "Light Mastery +%d";
                case 11: return "Darkness Mastery +%d";
                case 12: return "None Mastery +%d";
                case 13: return "Stun Mastery +%d";
                case 14: return "Slow Mastery +%d";
                case 15: return "Immobilize Mastery +%d";
                case 16: return "Sleep Mastery +%d";
                case 17: return "Silent Mastery +%d";
                case 18: return "Disarm Mastery +%d";
                case 19: return "Misc. Mastery +%d";
                case 20: return "Misc. Mastery +%d";
                case 21: return "Physical Resistance +%d";
                case 22: return "Earth Resistance +%d";
                case 23: return "Water Resistance +%d";
                case 24: return "Fire Resistance +%d";
                case 25: return "Wind Resistance +%d";
                case 26: return "Light Resistance +%d";
                case 27: return "Darkness Resistance +%d";
                case 28: return "None Resistance +%d";
                case 29: return "Stun Resistance +%d";
                case 30: return "Slow Resistance +%d";
                case 31: return "Immobilize Resistance +%d";
                case 32: return "Sleep Resistance +%d";
                case 33: return "Silent Resistance +%d";
                case 34: return "Disarm Resistance +%d";
                case 35: return "Misc. Resistance +%d";
                case 36: return "Misc. Resistance +%d";
                case 37: return "Health +%.0f%%";
                case 38: return "Mana +%.0f%%";
                case 39: return "Attack +%.0f%%";
                case 40: return "Defense +%.0f%%";
                case 41: return "Physical Mastery +%.0f%%";
                case 42: return "Earth Mastery +%.0f%%";
                case 43: return "Water Mastery +%.0f%%";
                case 44: return "Fire Mastery +%.0f%%";
                case 45: return "Wind Mastery +%.0f%%";
                case 46: return "Light Mastery +%.0f%%";
                case 47: return "Darkness Mastery +%.0f%%";
                case 48: return "None Mastery +%.0f%%";
                case 49: return "Stun Mastery +%.0f%%";
                case 50: return "Slow Mastery +%.0f%%";
                case 51: return "Immobilize Mastery +%.0f%%";
                case 52: return "Sleep Mastery +%.0f%%";
                case 53: return "Silent Mastery +%.0f%%";
                case 54: return "Disarm Mastery +%.0f%%";
                case 55: return "Misc. Mastery +%.0f%%";
                case 56: return "Misc. Mastery +%.0f%%";
                case 57: return "Physical Resistance +%.0f%%";
                case 58: return "Earth Resistance +%.0f%%";
                case 59: return "Water Resistance +%.0f%%";
                case 60: return "Fire Resistance +%.0f%%";
                case 61: return "Wind Resistance +%.0f%%";
                case 62: return "Light Resistance +%.0f%%";
                case 63: return "Darkness Resistance +%.0f%%";
                case 64: return "None Resistance +%.0f%%";
                case 65: return "Stun Resistance +%.0f%%";
                case 66: return "Slow Resistance +%.0f%%";
                case 67: return "Immobilize Resistance +%.0f%%";
                case 68: return "Sleep Resistance +%.0f%%";
                case 69: return "Silent Resistance +%.0f%%";
                case 70: return "Disarm Resistance +%.0f%%";
                case 71: return "Misc. Resistance +%.0f%%";
                case 72: return "Misc. Resistance +%.0f%%";
                case 90: return "Evasion +%d";
                case 77:
                case 81: return "Skill %s Lv%d";
                case 79: return "Increases Damage by %d";
                case 84: return "Crit Chance +%.1f%%";
                case 85: return "Crit Damage +%.1f%%";
                case 86: return "Crit Dodge +%.1f%%";
                case 87: return "Crit Defense +%.1f%%";
                case 88: return "Movement Speed +%.2f";
                case 104: return "Physical Attack +%d";
                case 105: return "Earth Attack +%d";
                case 106: return "Water Attack +%d";
                case 107: return "Fire Attack +%d";
                case 108: return "Wind Attack +%d";
                case 109: return "Light Attack +%d";
                case 110: return "Dark Attack +%d";
                case 111: return "No-Attribute Attack +%d";
                case 112: return "Physical Defense +%d";
                case 113: return "Earth Defense +%d";
                case 114: return "Water Defense +%d";
                case 115: return "Fire Defense +%d";
                case 116: return "Wind Defense +%d";
                case 117: return "Light Defense +%d";
                case 118: return "Dark Defense +%d";
                case 119: return "No-Attribute Defense +%d";
                case 120: return "Healing Effect +%d";
                case 121: return "Crit Heal Chance +%.0f%%";
                case 122: return "Max EXP +%d";
                case 123: return "Physical Mana Cost -%d%%";
                case 124: return "Earth Mana Cost -%d%%";
                case 125: return "Water Mana Cost -%d%%";
                case 126: return "Fire Mana Cost -%d%%";
                case 127: return "Wind Mana Cost -%d%%";
                case 128: return "Light Mana Cost -%d%%";
                case 129: return "Darkness Mana Cost -%d%%";
                case 130: return "All skills Mana Cost -%d%%";
                case 131: return "Blessing skills Mana Cost -%d%%";
                case 132: return "Curse skills Mana Cost -%d%%";
                case 133: return "All Masteries + %d";
                case 134: return "All Resistances + %d";
                case 135: return "All Masteries +%d%%";
                case 136: return "Resistance Gain +%d";
                case 137: return "Mounted Combat";
                case 138: return "Riding level + %d";
                case 139: return "Increases Damage by %d";
                case 140: return "Mount Speed +%.2f";
                case 141: return "Decreases direct damage by %d";
                case 142: return "Decreases reflected damage by %.1f%% plus %d";
                case 143: return "Reduces Damage over Time by %.1f%%";
                case 144: return "PVE Intensity +%d";
                case 145: return "PVE Tenacity +%d";
                default: return rawType;
            }
        }

        public static string GetAddon(string id)
        {
            string line = "";
            try
            {
                string name = "";
                string num_params = "0";
                string param1 = "0";
                string param2 = "0";
                string param3 = "0";
                //for (int k = 0; k < MainWindow.eLC.Lists[0].elementValues.Count; k++)
                //{
                int key = int.Parse(id);
                if (!MainWindow.eLC.addonIndex.ContainsKey(key))
                {
                    return "";
                }
                int k = MainWindow.eLC.addonIndex[key];
                    if (MainWindow.eLC.GetValue(0, k, 0) == id)
                    {
                        for (int t = 0; t < MainWindow.eLC.Lists[0].elementFields.Length; t++)
                        {
                            if (MainWindow.eLC.Lists[0].elementFields[t] == "Name")
                            {
                                name = MainWindow.eLC.GetValue(0, k, t);
                                break;
                            }
                        }
                        for (int t = 0; t < MainWindow.eLC.Lists[0].elementFields.Length; t++)
                        {
                            if (MainWindow.eLC.Lists[0].elementFields[t] == "num_params")
                            {
                                num_params = MainWindow.eLC.GetValue(0, k, t);
                                break;
                            }
                        }
                        for (int t = 0; t < MainWindow.eLC.Lists[0].elementFields.Length; t++)
                        {
                            if (MainWindow.eLC.Lists[0].elementFields[t] == "param1")
                            {
                                param1 = MainWindow.eLC.GetValue(0, k, t);
                                param2 = MainWindow.eLC.GetValue(0, k, t + 1);
                                param3 = MainWindow.eLC.GetValue(0, k, t + 2);
                                break;
                            }
                        }
                        try
                        {
                            int addon_type = 0;
                            bool hasAddonType = false;
                            for (int t = 0; t < MainWindow.eLC.Lists[0].elementFields.Length; t++)
                            {
                                if (MainWindow.eLC.Lists[0].elementFields[t] == "type")
                                {
                                    int parsedType;
                                    if (int.TryParse(MainWindow.eLC.GetValue(0, k, t), out parsedType))
                                    {
                                        addon_type = parsedType;
                                        hasAddonType = true;
                                    }
                                    break;
                                }
                            }
                            if (!hasAddonType && MainWindow.database != null && MainWindow.database.addonslist != null && MainWindow.database.addonslist.ContainsKey(id))
                            {
                                addon_type = Convert.ToInt32(MainWindow.database.addonslist[id].ToString());
                                hasAddonType = true;
                            }
                            if (!hasAddonType)
                            {
                                return "";
                            }
                            string fwLine;
                            if (TryGetFwAddonText(addon_type, param1, param2, out fwLine))
                            {
                                return fwLine;
                            }
                            switch (addon_type)
                            {
                                case 0:
                                    line = Extensions.GetLocalization(7202) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 1:
                                    line = Extensions.GetLocalization(7203) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 2:
                                    line = Extensions.GetLocalization(7202) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 3:
                                    line = Extensions.GetLocalization(7204) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 4:
                                    line = Extensions.GetLocalization(7205) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 5:
                                    line = Extensions.GetLocalization(7204) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 6:
                                    line = Extensions.GetLocalization(7206) + " +" + param1 + "\n" + Extensions.GetLocalization(7202) + " -" + param2;
                                    break;
                                case 7:
                                    line = Extensions.GetLocalization(7202) + " +" + param1 + "\n" + Extensions.GetLocalization(7206) + " -" + param2;
                                    break;
                                case 8:
                                    line = Extensions.GetLocalization(7204) + " +" + param1 + "\n" + Extensions.GetLocalization(7207) + " -" + param2;;
                                    break;
                                case 9:
                                    line = String.Format(Extensions.GetLocalization(7214), "-" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("F2", CultureInfo.CreateSpecificCulture("en-US")));
                                    break;
                                case 10:
                                    line = Extensions.GetLocalization(7213) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("F2", CultureInfo.CreateSpecificCulture("en-US"));
                                    break;
                                case 11:
                                    line = Extensions.GetLocalization(7215) + " -" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 12:
                                    line = Extensions.GetLocalization(7206) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 13:
                                    line = Extensions.GetLocalization(7237) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 14:
                                    line = Extensions.GetLocalization(7207) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 15:
                                    line = Extensions.GetLocalization(7208) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 16:
                                    line = Extensions.GetLocalization(7208) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 17:
                                    line = Extensions.GetLocalization(7209) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 18:
                                    line = Extensions.GetLocalization(7209) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 19:
                                    line = Extensions.GetLocalization(7210) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 20:
                                    line = Extensions.GetLocalization(7210) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 21:
                                    line = Extensions.GetLocalization(7211) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 22:
                                    line = Extensions.GetLocalization(7211) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 23:
                                    line = Extensions.GetLocalization(7212) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 24:
                                    line = Extensions.GetLocalization(7212) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 25:
                                    line = Extensions.GetLocalization(7208) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0") + "\n" + Extensions.GetLocalization(7211) + " -" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 26:
                                    line = Extensions.GetLocalization(7209) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0") + "\n" + Extensions.GetLocalization(7208) + " -" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 27:
                                    line = Extensions.GetLocalization(7210) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0") + "\n" + Extensions.GetLocalization(7212) + " -" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 28:
                                    line = Extensions.GetLocalization(7211) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0") + "\n" + Extensions.GetLocalization(7210) + " -" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 29:
                                    line = Extensions.GetLocalization(7212) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0") + "\n" + Extensions.GetLocalization(7209) + " -" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 30:
                                    line = Extensions.GetLocalization(7208) + " +" + param1 + "\n" + Extensions.GetLocalization(7211) + " -" + param2;
                                    break;
                                case 31:
                                    line = Extensions.GetLocalization(7209) + " +" + param1 + "\n" + Extensions.GetLocalization(7208) + " -" + param2;
                                    break;
                                case 32:
                                    line = Extensions.GetLocalization(7210) + " +" + param1 + "\n" + Extensions.GetLocalization(7212) + " -" + param2;
                                    break;
                                case 33:
                                    line = Extensions.GetLocalization(7211) + " +" + param1 + "\n" + Extensions.GetLocalization(7210) + " -" + param2;
                                    break;
                                case 34:
                                    line = Extensions.GetLocalization(7212) + " +" + param1 + "\n" + Extensions.GetLocalization(7209) + " -" + param2;
                                    break;
                                case 35:
                                    line = Extensions.GetLocalization(7217) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 36:
                                    line = Extensions.GetLocalization(7218) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 37:
                                    line = Extensions.GetLocalization(7219) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 38:
                                    line = Extensions.GetLocalization(7220) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 39:
                                    line = Extensions.GetLocalization(7221) + " +" + Convert.ToInt32(param1) / 2;
                                    break;
                                case 40:
                                    line = Extensions.GetLocalization(7222) + " +" + Convert.ToInt32(param1) / 2;
                                    break;
                                case 41:
                                    line = Extensions.GetLocalization(7223) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 42:
                                    line = Extensions.GetLocalization(7224) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 43:
                                    line = Extensions.GetLocalization(7225) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 44:
                                    line = Extensions.GetLocalization(7226) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 45:
                                    line = Extensions.GetLocalization(7229) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 46:
                                    line = Extensions.GetLocalization(7227) + " +" + param1;
                                    break;
                                case 47:
                                    line = Extensions.GetLocalization(7227) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 48:
                                    line = String.Format(Extensions.GetLocalization(7230), "+" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("F2", CultureInfo.CreateSpecificCulture("en-US")));
                                    break;
                                case 49:
                                    line = Extensions.GetLocalization(7231) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 50:
                                    line = Extensions.GetLocalization(7228) + " +" + param1;
                                    break;
                                case 51:
                                    line = Extensions.GetLocalization(7228) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");//param1%~param2%
                                    break;
                                case 52:
                                    line = Extensions.GetLocalization(7232) + " +" + param1;
                                    break;
                                case 53:
                                    line = Extensions.GetLocalization(7232) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 54:
                                    line = Extensions.GetLocalization(7233) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 55:
                                    line = Extensions.SkillDesc(Convert.ToInt32(param1));
                                    break;
                                case 56:
                                    line = Extensions.GetLocalization(7235) + " -" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 57:
                                    line = Extensions.GetLocalization(7236);
                                    break;
                                case 58:
                                    line = Extensions.GetLocalization(7216) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 59:
                                    line = Extensions.GetLocalization(7239) + " +" + param1;
                                    break;
                                case 60:
                                    line = Extensions.GetLocalization(7240) + " +" + param1;
                                    break;
                                case 61:
                                    line = Extensions.GetLocalization(7238) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 62:
                                    line = Extensions.GetLocalization(7241);
                                    break;
                                case 63:
                                    line = Extensions.GetLocalization(7242) + " +" + param1;
                                    break;
                                case 64:
                                    line = Extensions.GetLocalization(7243) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 65:
                                    line = Extensions.GetLocalization(7244) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 66:
                                    line = Extensions.GetLocalization(7245) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 67:
                                    line = Extensions.GetLocalization(7246) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 68:
                                    line = Extensions.GetLocalization(7247) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 69:
                                    line = Extensions.GetLocalization(7234) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 70:
                                    line = Extensions.GetLocalization(7239) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 71:
                                    line = Extensions.GetLocalization(7240) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 72:
                                    line = Extensions.GetLocalization(7229) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 73:
                                    line = Extensions.GetLocalization(7217) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 74:
                                    line = Extensions.GetLocalization(7218) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 75:
                                    line = Extensions.GetLocalization(7227) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 76:
                                    line = Extensions.GetLocalization(7206) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 77:
                                    line = Extensions.GetLocalization(7207) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 78:
                                    line = Extensions.GetLocalization(7233) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 79:
                                    line = Extensions.GetLocalization(7234) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 80:
                                    line = Extensions.GetLocalization(7215) + " -" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 81:
                                    line = Extensions.GetLocalization(7213) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("F2", CultureInfo.CreateSpecificCulture("en-US"));
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("F2", CultureInfo.CreateSpecificCulture("en-US"));
                                    break;
                                case 82:
                                    line = Extensions.GetLocalization(7222) + " +" + Convert.ToInt32(param1) / 2;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + Convert.ToInt32(param2) / 2;
                                    break;
                                case 83:
                                    line = Extensions.GetLocalization(7237) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 84:
                                    line = Extensions.GetLocalization(7238) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param2)), 0).ToString("P0");
                                    break;
                                case 85:
                                    line = Extensions.GetLocalization(7221) + " +" + Convert.ToInt32(param1) / 2;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + Convert.ToInt32(param2) / 2;
                                    break;
                                case 86:
                                    line = Extensions.GetLocalization(7228) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 87:
                                    line = Extensions.GetLocalization(7203) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 88:
                                    line = Extensions.GetLocalization(7205) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 89:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 90:
                                    line = Extensions.GetLocalization(7248) + " +" + param1;
                                    break;
                                case 91:
                                    line = Extensions.GetLocalization(7249) + " +" + param1;
                                    break;
                                case 92:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    //if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 93:
                                    line = Extensions.GetLocalization(7217) + " +" + param1;
                                    break;
                                case 94:
                                    line = Extensions.GetLocalization(7217) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 95:
                                    line = Extensions.GetLocalization(7223) + " +" + param1;
                                    break;
                                case 96:
                                    line = Extensions.GetLocalization(7224) + " +" + param1;
                                    break;
                                case 97:
                                    line = Extensions.GetLocalization(7225) + " +" + param1;
                                    break;
                                case 98:
                                    line = Extensions.GetLocalization(7226) + " +" + param1;
                                    break;
                                case 99:
                                    line = Extensions.GetLocalization(7218) + " +" + param1;
                                    break;
                                case 100:
                                    line = Extensions.GetLocalization(7202) + " +" + param1;
                                    break;
                                case 101:
                                    line = Extensions.GetLocalization(7203) + " +" + param1;
                                    break;
                                case 102:
                                    line = Extensions.GetLocalization(7204) + " +" + param1;
                                    break;
                                case 103:
                                    line = Extensions.GetLocalization(7205) + " +" + param1;
                                    break;
                                case 104:
                                    line = Extensions.GetLocalization(7206) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 105:
                                    line = Extensions.GetLocalization(7217) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 106:
                                    line = Extensions.GetLocalization(7223) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 107:
                                    line = Extensions.GetLocalization(7224) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 108:
                                    line = Extensions.GetLocalization(7225) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 109:
                                    line = Extensions.GetLocalization(7227) + " +" + param1;
                                    if (Convert.ToInt32(num_params) > 1 && param1 != param2) line += "~" + param2;
                                    break;
                                case 110:
                                    line = Extensions.GetLocalization(7229) + " +" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 111:
                                    line = Extensions.GetLocalization(7239) + " +" + param1;
                                    break;
                                case 112:
                                    line = Extensions.GetLocalization(7240) + " +" + param1;
                                    break;
                                case 113:
                                    line = Extensions.GetLocalization(7215) + " -" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("P0");
                                    break;
                                case 114:
                                    line = Extensions.GetLocalization(7207) + " +" + param1;
                                    break;
                                case 115:
                                    line = String.Format(Extensions.GetLocalization(7050), "+" + BitConverter.ToSingle(BitConverter.GetBytes(Convert.ToInt32(param1)), 0).ToString("F1", CultureInfo.CreateSpecificCulture("en-US")));
                                    break;
                                //case 120-146: Engrave
                                case 150:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 151:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 152:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 153:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 154:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 155:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 156:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 160:
                                    line = Extensions.GetLocalization(7251) + " +" + param1;
                                    break;
                                case 161:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 162:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 163:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 164:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 165:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 166:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 167:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 168:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 169:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 170:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 171:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 172:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 173:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 174:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 175:
                                    line = Extensions.GetLocalization(7252) + " +" + param1;
                                    break;
                                case 176:
                                    line = Extensions.GetLocalization(7253) + " +" + param1;
                                    break;
                                case 200:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 201:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 202:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 203:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 204:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 205:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 206:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 207:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 208:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 209:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 210:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 211:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                case 212:
                                    line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    break;
                                default:
                                    {
                                        return line = Extensions.GetLocalization(7200) + " " + id + " " + Extensions.GetLocalization(7201) + " " + addon_type + " " + param1 + "-" + param2 + "-" + param3;
                                    }
                            }
                        }
                        catch
                        {
                            line = Extensions.GetLocalization(7200) + " " + id + " " + param1 + "-" + param2 + "-" + param3;
                        }
                    }
                
            }
            catch
            {
                line = "";
            }
            return line;
        }
	}
}
