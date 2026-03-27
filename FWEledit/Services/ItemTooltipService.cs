using System;

namespace FWEledit
{
    public sealed class ItemTooltipService
    {
        public ItemTooltipResult BuildTooltip(
            eListCollection listCollection,
            int listIndex,
            int currentElementRowIndex,
            int columnIndex,
            int rowIndex,
            string fieldName,
            Func<int, int> resolveFieldIndex)
        {
            ItemTooltipResult result = new ItemTooltipResult();
            if (listCollection == null)
            {
                return result;
            }

            if (listIndex == 0 && currentElementRowIndex > -1 && columnIndex == 2 && rowIndex > 2 && rowIndex < 6)
            {
                try
                {
                    int raw = (int)listCollection.Lists[0].elementValues[currentElementRowIndex][rowIndex];
                    string text = "Float: " + BitConverter.ToSingle(BitConverter.GetBytes(raw), 0).ToString("F6");
                    result.ShowTooltip = true;
                    result.ShowCellTooltips = false;
                    result.Text = text;
                    return result;
                }
                catch
                {
                    return result;
                }
            }

            if (rowIndex > -1
                && !string.IsNullOrWhiteSpace(fieldName)
                && string.Equals(fieldName, "shop_price", StringComparison.OrdinalIgnoreCase)
                && listIndex > -1
                && currentElementRowIndex > -1)
            {
                int fieldIndex = resolveFieldIndex != null ? resolveFieldIndex(rowIndex) : -1;
                if (fieldIndex < 0)
                {
                    return result;
                }
                try
                {
                    int shopPrice = Convert.ToInt32(listCollection.GetValue(listIndex, currentElementRowIndex, fieldIndex));
                    double tmp = shopPrice;
                    double tmp1 = shopPrice * 0.05;
                    if (shopPrice >= 10)
                    {
                        tmp1 = Math.Round(tmp1, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        tmp1 = Math.Round(tmp1);
                    }
                    tmp = shopPrice + tmp1;
                    if (tmp >= 100 && tmp < 1000)
                    {
                        tmp = tmp * 0.1;
                        tmp = Math.Ceiling(tmp);
                        tmp = tmp * 10;
                    }
                    if (tmp >= 1000)
                    {
                        tmp = tmp * 0.01;
                        tmp = Math.Ceiling(tmp);
                        tmp = tmp * 100;
                    }
                    result.ShowTooltip = true;
                    result.ShowCellTooltips = false;
                    result.Text = "In Game Price: " + tmp;
                    return result;
                }
                catch
                {
                    return result;
                }
            }

            return result;
        }
    }
}
