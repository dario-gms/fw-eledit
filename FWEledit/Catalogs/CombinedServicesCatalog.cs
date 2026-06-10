using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public sealed class CombinedServiceOption
    {
        public int SegmentIndex { get; set; }
        public int BitIndex { get; set; }
        public uint Mask { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Label ?? string.Empty;
        }
    }

    public static class CombinedServicesCatalog
    {
        private static readonly string[] SegmentOneLabels =
        {
            "Pet Rename",
            "Pet Academy",
            "Pet Soulcombine",
            "Upgrade Guild",
            "Faction Service",
            "Temple Challenge",
            "Mail Service",
            "Auction",
            "Refine",
            "Soulcare",
            "Pet Rebirth",
            "Battlefield Recruit",
            "Exit Battlefield",
            "Eyrda Leaf Exchange",
            "Guild Storehouse",
            "Talisman Cleanse",
            "Reset Pet Owner",
            "Pet Meld",
            "Business Estimation",
            "Pet Combining",
            "Fortify Gear",
            "Embed Rune",
            "Remove Rune",
            "Select Sub-class",
            "Select Profession",
            "Learn Skill",
            "Engrave Gear",
            "Learn Sub-class Skill",
            "Embed Gem",
            "Remove Gem",
            "Re-identify Gear",
            "Combine Gem"
        };

        private static readonly string[] SegmentTwoLabels =
        {
            "Bid for Guild Base",
            "Relocate Guild Base",
            "Enter My Guild Base",
            "Choose which base to enter",
            "Return to Freedom Harbor",
            "Guild Base Entrance Fee Details",
            "View Guild Base Tax Information",
            "Guild Base Construction Tree",
            "View Guild Trade Run Log",
            "Refine Pet Essence",
            "Mastery and Resistance Training",
            "Treasure Hunting",
            "Pet Meld",
            "Marriage Service",
            "Territory Wars",
            "Pet Skill Binding",
            "Fortify Transfer",
            "Reset Mastery and Resistance",
            "Mount Dye",
            "Reforge",
            "Rune Combination",
            "Change Configuration",
            "Fashion Dye",
            "Claim Gift Card directly",
            "Fashion Upgrades",
            "Space Compass Recharging",
            "Anima training",
            "Pet Identification",
            "Anima Transformation",
            "Gear Augmentation",
            "Fashion Stats Transfer",
            "Change character's name"
        };

        private static readonly string[] SegmentThreeLabels =
        {
            "EXP Expansion Attribute closed",
            "Rename Guild"
        };

        private static readonly List<CombinedServiceOption>[] OptionSegments =
        {
            BuildOptions(0, SegmentOneLabels),
            BuildOptions(1, SegmentTwoLabels),
            BuildOptions(2, SegmentThreeLabels)
        };

        public static bool IsCombinedServicesFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return string.Equals(normalized, "combined_services", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "combined_services2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "combined_services3", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("combined_services_", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryResolveSegmentIndex(
            eListCollection listCollection,
            int listIndex,
            string fieldName,
            out int segmentIndex)
        {
            segmentIndex = -1;
            if (!IsCombinedServicesFieldName(fieldName))
            {
                return false;
            }

            string normalized = (fieldName ?? string.Empty).Trim();
            if (string.Equals(normalized, "combined_services", StringComparison.OrdinalIgnoreCase))
            {
                segmentIndex = 0;
                return true;
            }

            if (string.Equals(normalized, "combined_services2", StringComparison.OrdinalIgnoreCase))
            {
                segmentIndex = 1;
                return true;
            }

            if (string.Equals(normalized, "combined_services3", StringComparison.OrdinalIgnoreCase))
            {
                segmentIndex = 2;
                return true;
            }

            int suffixValue;
            if (!TryParseSuffixIndex(normalized, out suffixValue))
            {
                return false;
            }

            bool zeroBased = HasZeroBasedCombinedServicesFields(listCollection, listIndex);
            segmentIndex = zeroBased ? suffixValue : suffixValue - 1;
            return segmentIndex >= 0 && segmentIndex < OptionSegments.Length;
        }

        public static IList<CombinedServiceOption> GetOptions(
            eListCollection listCollection,
            int listIndex,
            string fieldName)
        {
            int segmentIndex;
            if (!TryResolveSegmentIndex(listCollection, listIndex, fieldName, out segmentIndex))
            {
                return new List<CombinedServiceOption>();
            }

            return OptionSegments[segmentIndex];
        }

        public static string FormatDisplay(
            eListCollection listCollection,
            int listIndex,
            string fieldName,
            string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            uint bits;
            if (!TryParseValue(rawValue, out bits))
            {
                return rawValue ?? string.Empty;
            }

            int segmentIndex;
            if (!TryResolveSegmentIndex(listCollection, listIndex, fieldName, out segmentIndex))
            {
                return rawValue ?? string.Empty;
            }

            List<string> labels = new List<string>();
            uint remaining = bits;
            List<CombinedServiceOption> options = OptionSegments[segmentIndex];
            for (int i = 0; i < options.Count; i++)
            {
                CombinedServiceOption option = options[i];
                if ((bits & option.Mask) != option.Mask)
                {
                    continue;
                }

                labels.Add(option.Label ?? string.Empty);
                remaining &= ~option.Mask;
            }

            if (remaining != 0)
            {
                labels.Add("Unknown flags (0x" + remaining.ToString("X8", CultureInfo.InvariantCulture) + ")");
            }

            if (labels.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", labels.ToArray());
        }

        public static string NormalizeInput(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            uint bits;
            if (!TryParseValue(value, out bits))
            {
                return value.Trim();
            }

            return ToStorageString(bits);
        }

        public static bool TryParseValue(string text, out uint value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return uint.TryParse(trimmed.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
            }

            int signedValue;
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out signedValue)
                || int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out signedValue))
            {
                value = unchecked((uint)signedValue);
                return true;
            }

            return uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                || uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
        }

        public static string ToStorageString(uint value)
        {
            return unchecked((int)value).ToString(CultureInfo.InvariantCulture);
        }

        private static List<CombinedServiceOption> BuildOptions(int segmentIndex, string[] labels)
        {
            List<CombinedServiceOption> options = new List<CombinedServiceOption>();
            for (int i = 0; i < labels.Length; i++)
            {
                string label = labels[i] ?? ("Service " + (segmentIndex * 32 + i).ToString(CultureInfo.InvariantCulture));
                options.Add(new CombinedServiceOption
                {
                    SegmentIndex = segmentIndex,
                    BitIndex = i,
                    Mask = 1u << i,
                    Label = label,
                    Description = "Activated Service: " + label
                });
            }

            return options;
        }

        private static bool HasZeroBasedCombinedServicesFields(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null)
            {
                return false;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], "combined_services_0", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseSuffixIndex(string fieldName, out int suffixValue)
        {
            suffixValue = -1;
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            int underscore = fieldName.LastIndexOf('_');
            if (underscore < 0 || underscore >= fieldName.Length - 1)
            {
                return false;
            }

            return int.TryParse(
                fieldName.Substring(underscore + 1),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out suffixValue);
        }
    }
}
