using System.Drawing;
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
            if (database != null && database.item_color != null && database.item_color.ContainsKey(data.itemId))
            {
                itemID = database.item_color[data.itemId];
            }

            Color color = Helper.getByID(itemID);
            string title = (data.name ?? string.Empty) + " [" + data.itemId + "]";
            string line = (data.basicAdons ?? string.Empty)
                + (data.time ?? string.Empty)
                + (data.addons ?? string.Empty)
                + (data.protect ?? string.Empty)
                + (data.description ?? string.Empty);

            return new ToolPreviewData
            {
                TitleText = title,
                TitleColor = color,
                IconImage = data.img,
                PreviewText = line
            };
        }
    }
}
