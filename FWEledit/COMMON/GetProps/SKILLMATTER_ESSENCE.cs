
using System;
using System.Globalization;

namespace FWEledit
{
    class SKILLMATTER_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[106].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[106].elementFields[k] == "level_required")
                    {
                        string level_required = sessionService.ListCollection.GetValue(106, pos_item, k);
                        if (level_required != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7018), level_required);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[106].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[106].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(106, pos_item, k);
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

