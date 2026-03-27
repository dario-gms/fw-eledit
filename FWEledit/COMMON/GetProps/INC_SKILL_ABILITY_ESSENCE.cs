
using System;
using System.Globalization;

namespace FWEledit
{
    class INC_SKILL_ABILITY_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[130].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[130].elementFields[k] == "level_required")
                    {
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7087), sessionService.ListCollection.GetValue(130, pos_item, k));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[130].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[130].elementFields[k] == "inc_ratio")
                    {
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7088), Convert.ToSingle(sessionService.ListCollection.GetValue(130, pos_item, k)).ToString("P0"));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[130].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[130].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(130, pos_item, k);
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

