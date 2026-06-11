using System;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public enum NpcSellServiceFieldType
    {
        Unknown = 0,
        PageTitle,
        MoneyType,
        GoodsItem
    }

    public static class NpcSellServiceCatalog
    {
        public const int PageCount = 8;
        public const int GoodsPerPage = 48;

        private const int HeaderFieldCount = 2;
        private const int FieldsPerPage = 50;
        private static readonly Regex NumberedFieldRegex = new Regex(
            @"^pages_(?<page>\d+)_(?<kind>page_title|money_type|id_goods_(?<slot>\d+))$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsNpcSellServiceList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null
                || listCollection.Lists == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null)
            {
                return false;
            }

            return string.Equals(NormalizeListName(listCollection.Lists[listIndex].listName), "NPC_SELL_SERVICE", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGetFieldLocation(
            eListCollection listCollection,
            int listIndex,
            int fieldIndex,
            string fieldName,
            out int pageIndex,
            out NpcSellServiceFieldType fieldType,
            out int goodsSlotIndex)
        {
            pageIndex = -1;
            fieldType = NpcSellServiceFieldType.Unknown;
            goodsSlotIndex = -1;

            if (!IsNpcSellServiceList(listCollection, listIndex)
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null)
            {
                return false;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            string normalizedFieldName = (fieldName ?? string.Empty).Trim();
            if (fieldIndex < 0 || fieldIndex >= fields.Length)
            {
                Match fallbackMatch = NumberedFieldRegex.Match(normalizedFieldName);
                if (fallbackMatch.Success)
                {
                    int parsedPage = ParseNumber(fallbackMatch.Groups["page"].Value, -1);
                    pageIndex = NormalizePageIndex(parsedPage);
                    if (pageIndex < 0)
                    {
                        return false;
                    }

                    if (normalizedFieldName.IndexOf("page_title", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        fieldType = NpcSellServiceFieldType.PageTitle;
                        return true;
                    }

                    if (normalizedFieldName.IndexOf("money_type", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        fieldType = NpcSellServiceFieldType.MoneyType;
                        return true;
                    }

                    int parsedSlot = ParseNumber(fallbackMatch.Groups["slot"].Value, -1);
                    if (parsedSlot > 0 && parsedSlot <= GoodsPerPage)
                    {
                        fieldType = NpcSellServiceFieldType.GoodsItem;
                        goodsSlotIndex = parsedSlot - 1;
                        return true;
                    }

                    return false;
                }

                if (string.Equals(normalizedFieldName, "money_type", StringComparison.OrdinalIgnoreCase))
                {
                    fieldType = NpcSellServiceFieldType.MoneyType;
                    return true;
                }

                if (normalizedFieldName.StartsWith("pages_", StringComparison.OrdinalIgnoreCase)
                    && normalizedFieldName.IndexOf("_money_type", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    fieldType = NpcSellServiceFieldType.MoneyType;
                    return true;
                }

                return false;
            }

            normalizedFieldName = (fieldName ?? fields[fieldIndex] ?? string.Empty).Trim();
            Match numberedMatch = NumberedFieldRegex.Match(normalizedFieldName);
            if (numberedMatch.Success)
            {
                int parsedPage = ParseNumber(numberedMatch.Groups["page"].Value, -1);
                if (parsedPage < 0)
                {
                    return false;
                }

                if (normalizedFieldName.IndexOf("page_title", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    pageIndex = NormalizePageIndex(parsedPage);
                    fieldType = NpcSellServiceFieldType.PageTitle;
                    return pageIndex >= 0;
                }

                if (normalizedFieldName.IndexOf("money_type", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    pageIndex = NormalizePageIndex(parsedPage);
                    fieldType = NpcSellServiceFieldType.MoneyType;
                    return pageIndex >= 0;
                }

                int parsedSlot = ParseNumber(numberedMatch.Groups["slot"].Value, -1);
                if (parsedSlot > 0)
                {
                    pageIndex = NormalizePageIndex(parsedPage);
                    fieldType = NpcSellServiceFieldType.GoodsItem;
                    goodsSlotIndex = parsedSlot - 1;
                    return pageIndex >= 0 && goodsSlotIndex >= 0 && goodsSlotIndex < GoodsPerPage;
                }
            }

            int relativeIndex = fieldIndex - HeaderFieldCount;
            if (relativeIndex < 0 || relativeIndex >= PageCount * FieldsPerPage)
            {
                return false;
            }

            pageIndex = relativeIndex / FieldsPerPage;
            int pageFieldOffset = relativeIndex % FieldsPerPage;
            if (pageFieldOffset == 0 && string.Equals(normalizedFieldName, "page_title", StringComparison.OrdinalIgnoreCase))
            {
                fieldType = NpcSellServiceFieldType.PageTitle;
                return true;
            }

            if (pageFieldOffset == 1 && string.Equals(normalizedFieldName, "money_type", StringComparison.OrdinalIgnoreCase))
            {
                fieldType = NpcSellServiceFieldType.MoneyType;
                return true;
            }

            if (pageFieldOffset >= 2 && pageFieldOffset < FieldsPerPage)
            {
                fieldType = NpcSellServiceFieldType.GoodsItem;
                goodsSlotIndex = pageFieldOffset - 2;
                return true;
            }

            return false;
        }

        public static string GetDisplayFieldName(
            eListCollection listCollection,
            int listIndex,
            int fieldIndex,
            string fieldName)
        {
            if (!TryGetFieldLocation(listCollection, listIndex, fieldIndex, fieldName, out int pageIndex, out NpcSellServiceFieldType fieldType, out int goodsSlotIndex))
            {
                return fieldName ?? string.Empty;
            }

            switch (fieldType)
            {
                case NpcSellServiceFieldType.PageTitle:
                    return "page_" + (pageIndex + 1).ToString() + "_title";
                case NpcSellServiceFieldType.MoneyType:
                    return "page_" + (pageIndex + 1).ToString() + "_money_type";
                case NpcSellServiceFieldType.GoodsItem:
                    return "page_" + (pageIndex + 1).ToString() + "_item_" + (goodsSlotIndex + 1).ToString("00");
                default:
                    return fieldName ?? string.Empty;
            }
        }

        public static int GetPageTitleFieldIndex(eListCollection listCollection, int listIndex, int pageIndex)
        {
            return FindFieldIndex(listCollection, listIndex, pageIndex, NpcSellServiceFieldType.PageTitle);
        }

        public static int GetMoneyTypeFieldIndex(eListCollection listCollection, int listIndex, int pageIndex)
        {
            return FindFieldIndex(listCollection, listIndex, pageIndex, NpcSellServiceFieldType.MoneyType);
        }

        public static int GetGoodsFieldIndex(eListCollection listCollection, int listIndex, int pageIndex, int goodsSlotIndex)
        {
            if (!IsNpcSellServiceList(listCollection, listIndex)
                || pageIndex < 0
                || pageIndex >= PageCount
                || goodsSlotIndex < 0
                || goodsSlotIndex >= GoodsPerPage)
            {
                return -1;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            string expectedNumberedName = "pages_" + (pageIndex + 1).ToString() + "_id_goods_" + (goodsSlotIndex + 1).ToString();
            for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
            {
                if (string.Equals(fields[fieldIndex], expectedNumberedName, StringComparison.OrdinalIgnoreCase))
                {
                    return fieldIndex;
                }
            }

            return HeaderFieldCount + (pageIndex * FieldsPerPage) + 2 + goodsSlotIndex;
        }

        private static int FindFieldIndex(eListCollection listCollection, int listIndex, int pageIndex, NpcSellServiceFieldType fieldType)
        {
            if (!IsNpcSellServiceList(listCollection, listIndex)
                || pageIndex < 0
                || pageIndex >= PageCount)
            {
                return -1;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            string expectedName = fieldType == NpcSellServiceFieldType.PageTitle
                ? "pages_" + (pageIndex + 1).ToString() + "_page_title"
                : "pages_" + (pageIndex + 1).ToString() + "_money_type";

            for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
            {
                if (string.Equals(fields[fieldIndex], expectedName, StringComparison.OrdinalIgnoreCase))
                {
                    return fieldIndex;
                }
            }

            return HeaderFieldCount + (pageIndex * FieldsPerPage) + (fieldType == NpcSellServiceFieldType.PageTitle ? 0 : 1);
        }

        private static int NormalizePageIndex(int parsedPage)
        {
            if (parsedPage > 0 && parsedPage <= PageCount)
            {
                return parsedPage - 1;
            }

            if (parsedPage >= 0 && parsedPage < PageCount)
            {
                return parsedPage;
            }

            return -1;
        }

        private static int ParseNumber(string value, int fallback)
        {
            return int.TryParse(value, out int parsed) ? parsed : fallback;
        }

        private static string NormalizeListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return string.Empty;
            }

            string[] split = listName.Split(new[] { " - " }, StringSplitOptions.None);
            return split.Length > 1 ? split[1].Trim() : listName.Trim();
        }
    }
}
