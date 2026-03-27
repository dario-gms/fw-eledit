
using System;
using System.Globalization;

namespace FWEledit
{
    class WINGMANWING_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[23].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[23].elementFields[k] == "speed_increase")
                    {
                        string speed_increase = sessionService.ListCollection.GetValue(23, pos_item, k);
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7038), Convert.ToSingle(speed_increase).ToString("F2", CultureInfo.CreateSpecificCulture("en-US")));
                        break;
                    }
                }
                line += "\n" + Extensions.GetLocalization(sessionService, 7017) + " " + Extensions.GetLocalization(sessionService, 1126) + " " + Extensions.GetLocalization(sessionService, 1127);
                for (int k = 0; k < sessionService.ListCollection.Lists[23].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[23].elementFields[k] == "require_player_level_min")
                    {
                        string require_player_level_min = sessionService.ListCollection.GetValue(23, pos_item, k);
                        if (require_player_level_min != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7018), require_player_level_min);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[23].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[23].elementFields[k] == "mp_per_second")
                    {
                        string mp_per_second = sessionService.ListCollection.GetValue(23, pos_item, k);
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7041), mp_per_second);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[23].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[23].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(23, pos_item, k);
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

