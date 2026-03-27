
using System;
using System.Globalization;

namespace FWEledit
{
    class WEDDING_BOOKCARD_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                string year = "0";
                string month = "0";
                string day = "0";
                for (int k = 0; k < sessionService.ListCollection.Lists[133].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[133].elementFields[k] == "year")
                    {
                        year = sessionService.ListCollection.GetValue(133, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[133].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[133].elementFields[k] == "month")
                    {
                        month = sessionService.ListCollection.GetValue(133, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[133].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[133].elementFields[k] == "day")
                    {
                        day = sessionService.ListCollection.GetValue(133, pos_item, k);
                        break;
                    }
                }
                line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7089), year, month, day);
                for (int k = 0; k < sessionService.ListCollection.Lists[133].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[133].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(133, pos_item, k);
                        if (price != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7024) + " " + Convert.ToInt32(price).ToString("N0", CultureInfo.CreateSpecificCulture("zh-CN"));
                        }
                        break;
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

