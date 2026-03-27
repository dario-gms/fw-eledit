
using System;
using System.Globalization;

namespace FWEledit
{
    class MEDICINE_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                string hp_add_total = "0";
                string hp_add_time = "0";
                string mp_add_total = "0";
                string mp_add_time = "0";
                for (int k = 0; k < sessionService.ListCollection.Lists[12].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[12].elementFields[k] == "require_level")
                    {
                        string require_level = sessionService.ListCollection.GetValue(12, pos_item, k);
                        if (require_level != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7018), require_level);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[12].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[12].elementFields[k] == "hp_add_total")
                    {
                        hp_add_total = sessionService.ListCollection.GetValue(12, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[12].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[12].elementFields[k] == "hp_add_time")
                    {
                        hp_add_time = sessionService.ListCollection.GetValue(12, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[12].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[12].elementFields[k] == "mp_add_total")
                    {
                        mp_add_total = sessionService.ListCollection.GetValue(12, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[12].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[12].elementFields[k] == "mp_add_time")
                    {
                        mp_add_time = sessionService.ListCollection.GetValue(12, pos_item, k);
                        break;
                    }
                }
                if (hp_add_total != "0" && hp_add_time == "0" && mp_add_total != "0" && mp_add_time == "0")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7025) + String.Format(Extensions.GetLocalization(sessionService, 7026), hp_add_total, mp_add_total);
                }
                if (hp_add_total != "0" && hp_add_time != "0" && mp_add_total == "0" && mp_add_time == "0")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7025) + String.Format(Extensions.GetLocalization(sessionService, 7027), hp_add_time, hp_add_total);
                }
                if (hp_add_total != "0" && hp_add_time == "0" && mp_add_total == "0" && mp_add_time == "0")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7025) + String.Format(Extensions.GetLocalization(sessionService, 7028), hp_add_total);
                }
                if (mp_add_total != "0" && mp_add_time != "0" && hp_add_total == "0" && hp_add_time == "0")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7025) + String.Format(Extensions.GetLocalization(sessionService, 7029), mp_add_time, mp_add_total);
                }
                if (mp_add_total != "0" && mp_add_time == "0" && hp_add_total == "0" && hp_add_time == "0")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7025) + String.Format(Extensions.GetLocalization(sessionService, 7030), mp_add_total);
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[12].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[12].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(12, pos_item, k);
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

