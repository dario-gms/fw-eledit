
using System;
using System.Globalization;

namespace FWEledit
{
    class DAMAGERUNE_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                string damage_type = "-1";
                string require_weapon_level_min = "0";
                string require_weapon_level_max = "0";
                string damage_increased = "0";
                for (int k = 0; k < sessionService.ListCollection.Lists[17].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[17].elementFields[k] == "damage_type")
                    {
                        damage_type = sessionService.ListCollection.GetValue(17, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[17].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[17].elementFields[k] == "require_weapon_level_min")
                    {
                        require_weapon_level_min = sessionService.ListCollection.GetValue(17, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[17].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[17].elementFields[k] == "require_weapon_level_max")
                    {
                        require_weapon_level_max = sessionService.ListCollection.GetValue(17, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[17].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[17].elementFields[k] == "damage_increased")
                    {
                        damage_increased = sessionService.ListCollection.GetValue(17, pos_item, k);
                        break;
                    }
                }
                if (require_weapon_level_min != "0" || require_weapon_level_max != "0")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7031) + String.Format(Extensions.GetLocalization(sessionService, 7032), require_weapon_level_min, require_weapon_level_max);
                }
                if (damage_increased != "0" && damage_type == "0")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7025) + Extensions.GetLocalization(sessionService, 7004) + " +" + damage_increased;
                }
                if (damage_increased != "0" && damage_type == "1")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7025) + Extensions.GetLocalization(sessionService, 7005) + " +" + damage_increased;
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[17].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[17].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(17, pos_item, k);
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

