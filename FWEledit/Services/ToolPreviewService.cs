using System.Drawing;
using System.Text.RegularExpressions;
using FWEledit.DDSReader.Utils;

namespace FWEledit
{
    public sealed class ToolPreviewService
    {
        public ToolPreviewData BuildPreview(InfoTool data, CacheSave database)
        {
            if (data == null)
            {
                return new ToolPreviewData();
            }

            int itemID = 0;
            Color color = Color.White;
            if (database != null && database.item_color != null && database.item_color.ContainsKey(data.itemId))
            {
                itemID = database.item_color[data.itemId];
                color = Helper.getByID(itemID);
            }
            else if (data.itemQuality >= 0)
            {
                Color qualityColor;
                if (ItemQualityCatalog.TryGetColor(data.itemQuality, out qualityColor))
                {
                    color = qualityColor;
                }
            }
            string title = BuildTitle(data);
            string line = BuildPreviewText(data);

            return new ToolPreviewData
            {
                TitleText = title,
                TitleColor = color,
                IconImage = data.img,
                PreviewText = line
            };
        }

        private static string BuildTitle(InfoTool data)
        {
            string name = (data.name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
            {
                return "[" + data.itemId + "]";
            }

            return name + " (" + data.itemId + ")";
        }

        private static string BuildPreviewText(InfoTool data)
        {
            string body = JoinNonEmptyBlocks(
                NormalizeBlock(data.basicAdons),
                NormalizeBlock(data.time),
                NormalizeBlock(data.addons),
                BuildProcTypeBlock(data.procTypeValue, data.protect, data.powers));

            string description = FormatDescriptionBlock(data.description);
            return JoinNonEmptyBlocks(body, description);
        }

        private static string BuildProcTypeBlock(uint procTypeValue, string legacyProtect, string legacyPowers)
        {
            string mapped = JoinNonEmptyLines(
                GetProcTypeLine(procTypeValue, 0x0001, "No drop on death"),
                GetProcTypeLine(procTypeValue, 0x0002, "Unable to discard"),
                GetProcTypeLine(procTypeValue, 0x0004, "Unable to sell"),
                GetProcTypeLine(procTypeValue, 0x0010, "Unable to trade"),
                GetProcTypeLine(procTypeValue, 0x0020, "Quest item"),
                GetProcTypeLine(procTypeValue, 0x0040, "Binds when exchanged"),
                GetProcTypeLine(procTypeValue, 0x0080, "Bound"),
                GetProcTypeLine(procTypeValue, 0x0200, "Cannot split or stack"),
                GetProcTypeLine(procTypeValue, 0x0400, "Unable to destroy"),
                GetProcTypeLine(procTypeValue, 0x4000, "Does not disappear after expiry"));

            if (!string.IsNullOrWhiteSpace(mapped))
            {
                return mapped;
            }

            return JoinNonEmptyLines(
                CleanupLegacyTooltipBlock(legacyProtect),
                CleanupLegacyTooltipBlock(legacyPowers));
        }

        private static string GetProcTypeLine(uint procTypeValue, uint flag, string label)
        {
            return (procTypeValue & flag) == flag ? "^FFFF00" + label + "^FFFFFF" : string.Empty;
        }

        private static string FormatDescriptionBlock(string description)
        {
            string normalized = NormalizeBlock(description);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            normalized = Regex.Replace(
                normalized,
                @"(?im)^(contain[s]?:)",
                "^C040FF$1^FFFFFF");

            normalized = Regex.Replace(
                normalized,
                @"(?im)^(right-click to open.*)$",
                "^4DD7FF$1^FFFFFF");

            return normalized;
        }

        private static string CleanupLegacyTooltipBlock(string text)
        {
            string normalized = NormalizeBlock(text);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            normalized = Regex.Replace(normalized, @"(?im)^NOT FOUND KEY \d+\s*$", string.Empty);
            return NormalizeBlock(normalized);
        }

        private static string JoinNonEmptyBlocks(params string[] blocks)
        {
            string result = string.Empty;
            for (int i = 0; i < blocks.Length; i++)
            {
                string block = NormalizeBlock(blocks[i]);
                if (string.IsNullOrWhiteSpace(block))
                {
                    continue;
                }

                result = string.IsNullOrEmpty(result)
                    ? block
                    : result + "\n\n" + block;
            }

            return result;
        }

        private static string JoinNonEmptyLines(params string[] lines)
        {
            string result = string.Empty;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = NormalizeBlock(lines[i]);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                result = string.IsNullOrEmpty(result)
                    ? line
                    : result + "\n" + line;
            }

            return result;
        }

        private static string NormalizeBlock(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Trim('\n', '\t', ' ');
        }
    }
}
