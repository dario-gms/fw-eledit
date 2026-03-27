
using System;
using System.Globalization;

namespace FWEledit
{
    class UNIVERSAL_TOKEN_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[191].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[191].elementFields[k] == "link_num")
                    {
                        string link_num = sessionService.ListCollection.GetValue(191, pos_item, k);
                        if (link_num != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7104);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[191].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[191].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(191, pos_item, k);
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

