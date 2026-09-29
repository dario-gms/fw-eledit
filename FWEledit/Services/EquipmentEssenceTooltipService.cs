using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public sealed class EquipmentEssenceTooltipService
    {
        private const string AddonPackageListName = "ADDON_PACKAGE_CONFIG";
        private const string SuiteListName = "SUITE_ESSENCE";

        public bool TryBuildBasicAddons(ISessionService sessionService, int listIndex, int elementIndex, out string text)
        {
            text = string.Empty;
            if (sessionService == null
                || sessionService.ListCollection == null
                || sessionService.ListCollection.Lists == null
                || listIndex < 0
                || listIndex >= sessionService.ListCollection.Lists.Length
                || elementIndex < 0
                || sessionService.ListCollection.Lists[listIndex] == null
                || sessionService.ListCollection.Lists[listIndex].elementFields == null
                || sessionService.ListCollection.Lists[listIndex].elementValues == null
                || elementIndex >= sessionService.ListCollection.Lists[listIndex].elementValues.Length)
            {
                return false;
            }

            if (!IsEquipmentEssenceList(sessionService.ListCollection, listIndex))
            {
                return false;
            }

            List<string> lines = new List<string>();
            AppendEquipmentHeader(sessionService, listIndex, elementIndex, lines);
            AppendBaseStats(sessionService, listIndex, elementIndex, lines);
            AppendAddonPackages(sessionService, listIndex, elementIndex, lines);
            AppendSuite(sessionService, listIndex, elementIndex, lines, GetItemQualityColorHex(sessionService, listIndex, elementIndex));

            text = JoinLines(lines);
            return !string.IsNullOrWhiteSpace(text);
        }

        private static bool IsEquipmentEssenceList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listCollection.Lists == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }

            string listName = NormalizeListName(listCollection.Lists[listIndex].listName);
            return string.Equals(listName, "EQUIPMENT_ESSENCE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "Equipment", StringComparison.OrdinalIgnoreCase);
        }

        private static void AppendEquipmentHeader(ISessionService sessionService, int listIndex, int elementIndex, List<string> lines)
        {
            string equipType = EquipmentTypeCatalog.FormatDisplay(GetValue(sessionService, listIndex, elementIndex, "equip_type"));
            string equipMask = EquipmentMaskCatalog.FormatDisplay(GetValue(sessionService, listIndex, elementIndex, "equip_mask"));
            string header = CombineTypeAndMask(equipType, equipMask);
            AddLine(lines, Colorize(header, "FFFFFF"));

            int baseStarLevel = GetInt(sessionService, listIndex, elementIndex, "base_star_level");
            if (baseStarLevel > 0)
            {
                AddLine(lines, Colorize("Star Level: " + baseStarLevel.ToString(CultureInfo.InvariantCulture), "FFFFFF"));
            }

            int level = GetInt(sessionService, listIndex, elementIndex, "level");
            if (level > 0)
            {
                AddLine(lines, Colorize("Lv" + level.ToString(CultureInfo.InvariantCulture), "FFFFFF"));
            }

            string characterCombo = GetValue(sessionService, listIndex, elementIndex, "character_combo_id");
            if (!IsZero(characterCombo))
            {
                string decodedCombo = Extensions.DecodingCharacterComboId(sessionService, characterCombo);
                if (!ContainsMissingLocalization(decodedCombo))
                {
                    AddBlock(lines, decodedCombo);
                }
            }

            int requireLevel = GetInt(sessionService, listIndex, elementIndex, "require_level");
            if (requireLevel > 0 && requireLevel != level)
            {
                AddLine(lines, Colorize("Required Level: " + requireLevel.ToString(CultureInfo.InvariantCulture), "FF4040"));
            }

            int requireVipLevel = GetInt(sessionService, listIndex, elementIndex, "require_vip_level");
            if (requireVipLevel > 0)
            {
                AddLine(lines, Colorize("Required VIP Level: " + requireVipLevel.ToString(CultureInfo.InvariantCulture), "FF4040"));
            }
        }

        private static void AppendBaseStats(ISessionService sessionService, int listIndex, int elementIndex, List<string> lines)
        {
            int minDamage = GetInt(sessionService, listIndex, elementIndex, "min_dmg");
            int maxDamage = GetInt(sessionService, listIndex, elementIndex, "dmg_val");
            if (minDamage != 0 || maxDamage != 0)
            {
                AddLine(lines, Colorize("Attack: " + minDamage.ToString(CultureInfo.InvariantCulture) + "-" + maxDamage.ToString(CultureInfo.InvariantCulture), "FFFFFF"));
            }

            AddNumberLine(sessionService, listIndex, elementIndex, lines, "defence", "Defense");
            AddNumberLine(sessionService, listIndex, elementIndex, lines, "hp", "Health");
            AddNumberLine(sessionService, listIndex, elementIndex, lines, "mp", "Mana");
            AddNumberLine(sessionService, listIndex, elementIndex, lines, "attack", "Accuracy");
            AddNumberLine(sessionService, listIndex, elementIndex, lines, "armor", "Evasion");

            float attackRange = GetFloat(sessionService, listIndex, elementIndex, "attack_range");
            if (Math.Abs(attackRange) > 0.0001f)
            {
                AddLine(lines, Colorize("Attack Range: " + attackRange.ToString("0.##", CultureInfo.CreateSpecificCulture("en-US")), "FFFFFF"));
            }

            int refineMaxLevel = GetInt(sessionService, listIndex, elementIndex, "refine_max_level");
            if (refineMaxLevel > 0)
            {
                AddLine(lines, Colorize("Fortify Level: 0/" + refineMaxLevel.ToString(CultureInfo.InvariantCulture), "FFFF00"));
            }

            int canSign = GetInt(sessionService, listIndex, elementIndex, "can_sign");
            if (canSign != 0)
            {
                AddLine(lines, Colorize("Can be Augmented", "FFFF00"));
            }

            int identify = GetInt(sessionService, listIndex, elementIndex, "id_identify");
            if (identify != 0)
            {
                AddLine(lines, Colorize("Unidentified", "FFFF00"));
            }
        }

        private static void AppendAddonPackages(ISessionService sessionService, int listIndex, int elementIndex, List<string> lines)
        {
            string qualityColor = GetItemQualityColorHex(sessionService, listIndex, elementIndex);
            AddAddonPackageLines(sessionService, lines, GetValue(sessionService, listIndex, elementIndex, "id_prefix_addon_package"), string.Empty, qualityColor);
            AddAddonPackageLines(sessionService, lines, GetValue(sessionService, listIndex, elementIndex, "id_postfix_addon_package"), string.Empty, qualityColor);
            AddAddonPackageLines(sessionService, lines, GetValue(sessionService, listIndex, elementIndex, "id_special_addon_package"), "Additional Attribute: ", qualityColor);
            AddAddonPackageLines(sessionService, lines, GetValue(sessionService, listIndex, elementIndex, "id_sign_addon_package"), string.Empty, qualityColor);

            for (int i = 1; i <= 12; i++)
            {
                AddAddonPackageLines(
                    sessionService,
                    lines,
                    GetValue(sessionService, listIndex, elementIndex, "enhanced_prop_package_" + i.ToString(CultureInfo.InvariantCulture)),
                    string.Empty,
                    qualityColor);
            }
        }

        private static void AddAddonPackageLines(ISessionService sessionService, List<string> lines, string packageIdText, string prefix, string qualityColor)
        {
            int packageId;
            if (!int.TryParse((packageIdText ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out packageId) || packageId <= 0)
            {
                return;
            }

            int packageListIndex = FindListIndexByName(sessionService.ListCollection, AddonPackageListName);
            if (packageListIndex < 0)
            {
                return;
            }

            int packageElementIndex = FindElementById(sessionService.ListCollection, packageListIndex, packageId);
            if (packageElementIndex < 0)
            {
                return;
            }

            for (int i = 1; i <= 3; i++)
            {
                string addonId = GetValue(sessionService, packageListIndex, packageElementIndex, "id_addon_prop_" + i.ToString(CultureInfo.InvariantCulture));
                if (IsZero(addonId))
                {
                    continue;
                }

                string addon = CleanTooltipText(EQUIPMENT_ADDON.GetAddon(sessionService, addonId));
                if (!string.IsNullOrWhiteSpace(addon))
                {
                    string color = string.IsNullOrEmpty(prefix) ? "FFFF00" : qualityColor;
                    AddLine(lines, Colorize(prefix + addon, color));
                }
            }
        }

        private static void AppendSuite(ISessionService sessionService, int listIndex, int elementIndex, List<string> lines, string qualityColor)
        {
            int itemId = GetInt(sessionService, listIndex, elementIndex, "id");
            if (itemId <= 0)
            {
                return;
            }

            int suiteListIndex = FindListIndexByName(sessionService.ListCollection, SuiteListName);
            if (suiteListIndex < 0)
            {
                return;
            }

            for (int suiteIndex = 0; suiteIndex < sessionService.ListCollection.Lists[suiteListIndex].elementValues.Length; suiteIndex++)
            {
                if (!SuiteContainsItem(sessionService, suiteListIndex, suiteIndex, itemId))
                {
                    continue;
                }

                string suiteName = GetValue(sessionService, suiteListIndex, suiteIndex, "name");
                int maxEquips = GetInt(sessionService, suiteListIndex, suiteIndex, "max_equips");
                AddLine(lines, string.Empty);
                AddLine(lines, Colorize(CleanTooltipText(suiteName) + "(" + maxEquips.ToString(CultureInfo.InvariantCulture) + ")", "FFFF00"));

                for (int i = 1; i <= 14; i++)
                {
                    int suiteItemId = GetInt(sessionService, suiteListIndex, suiteIndex, "equipments_" + i.ToString(CultureInfo.InvariantCulture) + "_id");
                    if (suiteItemId <= 0)
                    {
                        continue;
                    }

                    string suiteItemName = FindEquipmentNameById(sessionService, suiteItemId);
                    if (!string.IsNullOrWhiteSpace(suiteItemName))
                    {
                        AddLine(lines, Colorize(suiteItemName + " (" + suiteItemId.ToString(CultureInfo.InvariantCulture) + ")", "A8A8A8"));
                    }
                }

                for (int i = 1; i <= 13; i++)
                {
                    string addonId = GetValue(sessionService, suiteListIndex, suiteIndex, "addons_" + i.ToString(CultureInfo.InvariantCulture) + "_id");
                    if (IsZero(addonId))
                    {
                        continue;
                    }

                    string addon = CleanTooltipText(EQUIPMENT_ADDON.GetAddon(sessionService, addonId));
                    if (!string.IsNullOrWhiteSpace(addon))
                    {
                        AddLine(lines, Colorize("Set: " + addon, qualityColor));
                    }
                }

                return;
            }
        }

        private static bool SuiteContainsItem(ISessionService sessionService, int suiteListIndex, int suiteElementIndex, int itemId)
        {
            for (int i = 1; i <= 14; i++)
            {
                int suiteItemId = GetInt(sessionService, suiteListIndex, suiteElementIndex, "equipments_" + i.ToString(CultureInfo.InvariantCulture) + "_id");
                if (suiteItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FindEquipmentNameById(ISessionService sessionService, int itemId)
        {
            eListCollection listCollection = sessionService.ListCollection;
            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (!IsEquipmentEssenceList(listCollection, listIndex))
                {
                    continue;
                }

                int elementIndex = FindElementById(listCollection, listIndex, itemId);
                if (elementIndex >= 0)
                {
                    return CleanTooltipText(GetValue(sessionService, listIndex, elementIndex, "name"));
                }
            }

            return string.Empty;
        }

        private static void AddNumberLine(ISessionService sessionService, int listIndex, int elementIndex, List<string> lines, string fieldName, string label)
        {
            int value = GetInt(sessionService, listIndex, elementIndex, fieldName);
            if (value != 0)
            {
                AddLine(lines, Colorize(label + ": " + value.ToString(CultureInfo.InvariantCulture), "FFFFFF"));
            }
        }

        private static string CombineTypeAndMask(string equipType, string equipMask)
        {
            equipType = CleanTooltipText(equipType);
            equipMask = CleanTooltipText(equipMask);
            if (string.IsNullOrWhiteSpace(equipType) || equipType.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                return equipMask;
            }

            if (string.IsNullOrWhiteSpace(equipMask) || string.Equals(equipMask, "None", StringComparison.OrdinalIgnoreCase))
            {
                return equipType;
            }

            if (equipType.IndexOf(equipMask, StringComparison.OrdinalIgnoreCase) >= 0
                || equipMask.IndexOf(equipType, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return equipMask.IndexOf(equipType, StringComparison.OrdinalIgnoreCase) >= 0 ? equipMask : equipType;
            }

            if (string.Equals(equipType, "Weapon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(equipType, "Armor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(equipType, "Jewelry", StringComparison.OrdinalIgnoreCase))
            {
                return equipType;
            }

            return equipType + " " + equipMask;
        }

        private static int FindListIndexByName(eListCollection listCollection, string name)
        {
            if (listCollection == null || listCollection.Lists == null)
            {
                return -1;
            }

            for (int i = 0; i < listCollection.Lists.Length; i++)
            {
                if (listCollection.Lists[i] == null)
                {
                    continue;
                }

                string listName = NormalizeListName(listCollection.Lists[i].listName);
                if (string.Equals(listName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindElementById(eListCollection listCollection, int listIndex, int id)
        {
            if (listCollection == null
                || listCollection.Lists == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementValues == null)
            {
                return -1;
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementValues.Length; i++)
            {
                int candidate;
                if (int.TryParse(listCollection.GetValue(listIndex, i, 0), NumberStyles.Integer, CultureInfo.InvariantCulture, out candidate)
                    && candidate == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string GetValue(ISessionService sessionService, int listIndex, int elementIndex, string fieldName)
        {
            int fieldIndex = FindFieldIndex(sessionService.ListCollection, listIndex, fieldName);
            if (fieldIndex < 0)
            {
                return string.Empty;
            }

            try
            {
                return sessionService.ListCollection.GetValue(listIndex, elementIndex, fieldIndex) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int FindFieldIndex(eListCollection listCollection, int listIndex, string fieldName)
        {
            if (listCollection == null
                || listCollection.Lists == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null)
            {
                return -1;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetInt(ISessionService sessionService, int listIndex, int elementIndex, string fieldName)
        {
            int value;
            return int.TryParse(GetValue(sessionService, listIndex, elementIndex, fieldName).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : 0;
        }

        private static float GetFloat(ISessionService sessionService, int listIndex, int elementIndex, string fieldName)
        {
            float value;
            string text = GetValue(sessionService, listIndex, elementIndex, fieldName).Trim();
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                ? value
                : 0f;
        }

        private static bool IsZero(string value)
        {
            int parsed;
            return string.IsNullOrWhiteSpace(value)
                || (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed == 0);
        }

        private static void AddBlock(List<string> lines, string block)
        {
            string clean = CleanTooltipText(block);
            if (string.IsNullOrWhiteSpace(clean))
            {
                return;
            }

            string[] blockLines = clean.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < blockLines.Length; i++)
            {
                AddLine(lines, blockLines[i]);
            }
        }

        private static void AddLine(List<string> lines, string line)
        {
            if (lines == null)
            {
                return;
            }

            string clean = CleanTooltipText(line);
            if (clean.Length == 0 && (lines.Count == 0 || lines[lines.Count - 1].Length == 0))
            {
                return;
            }

            lines.Add(clean);
        }

        private static string JoinLines(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                return string.Empty;
            }

            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return string.Join("\n", lines.ToArray()).Trim();
        }

        private static string CleanTooltipText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string clean = value.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
            clean = Regex.Replace(clean, @"\s+\n", "\n");
            clean = Regex.Replace(clean, @"\n\s+", "\n");
            return clean.Trim();
        }

        private static string Colorize(string value, string color)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return "^" + color + value + "^FFFFFF";
        }

        private static string GetItemQualityColorHex(ISessionService sessionService, int listIndex, int elementIndex)
        {
            int quality = GetInt(sessionService, listIndex, elementIndex, "item_quality");
            System.Drawing.Color color;
            if (!ItemQualityCatalog.TryGetColor(quality, out color))
            {
                return "FFFF00";
            }

            return color.R.ToString("X2", CultureInfo.InvariantCulture)
                + color.G.ToString("X2", CultureInfo.InvariantCulture)
                + color.B.ToString("X2", CultureInfo.InvariantCulture);
        }

        private static bool ContainsMissingLocalization(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.IndexOf("NOT FOUND KEY", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NormalizeListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return string.Empty;
            }

            string[] split = listName.Split(new string[] { " - " }, StringSplitOptions.None);
            return split.Length > 1 ? split[1].Trim() : listName.Trim();
        }
    }
}
