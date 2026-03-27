
using System;
using System.Globalization;

namespace FWEledit
{
    class SHARPENER_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[135].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[135].elementFields[k] == "level")
                    {
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7000), sessionService.ListCollection.GetValue(135, pos_item, k));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[135].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[135].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(135, pos_item, k);
                        if (price != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7024) + " " + Convert.ToInt32(price).ToString("N0", CultureInfo.CreateSpecificCulture("zh-CN"));
                        }
                        break;
                    }
                }
                line += "\n";
                for (int k = 1; k < 4; k++)
                {
                    for (int t = 0; t < sessionService.ListCollection.Lists[135].elementFields.Length; t++)
                    {
                        if (sessionService.ListCollection.Lists[135].elementFields[t] == "addon_" + k)
                        {
                            string addon = sessionService.ListCollection.GetValue(135, pos_item, t);
                            if (addon != "0")
                            {
                                line += "\n" + "^4286f4" + EQUIPMENT_ADDON.GetAddon(sessionService, addon) + "^FFFFFF";
                            }
                            break;
                        }
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[135].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[135].elementFields[k] == "addon_time")
                    {
                        string price = sessionService.ListCollection.GetValue(135, pos_item, k);
                        line += "\n" + Extensions.GetLocalization(sessionService, 7090) + Extensions.ItemPropsSecondsToString(sessionService, Convert.ToUInt32(sessionService.ListCollection.GetValue(135, pos_item, k)));
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

