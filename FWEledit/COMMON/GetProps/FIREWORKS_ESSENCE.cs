
using System;
using System.Globalization;

namespace FWEledit
{
    class FIREWORKS_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[98].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[98].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(98, pos_item, k);
                        if (price != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7024) + " " + Convert.ToInt32(price).ToString("N0", CultureInfo.CreateSpecificCulture("zh-CN"));
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[98].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[98].elementFields[k] == "level")
                    {
                        string level = sessionService.ListCollection.GetValue(98, pos_item, k);
                        line += "\n";
                        for (int a = 0; a < Convert.ToInt32(level); a++)
                        {
                            line += Extensions.GetLocalization(sessionService, 7046);
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

