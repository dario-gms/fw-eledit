
using System;
using System.Globalization;

namespace FWEledit
{
    class PROJECTILE_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                string type = "";
                string require_weapon_level_min = "0";
                string require_weapon_level_max = "0";
                string damage_enhance = "0";
                for (int k = 0; k < sessionService.ListCollection.Lists[31].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[31].elementFields[k] == "type")
                    {
                        string value = sessionService.ListCollection.GetValue(31, pos_item, k);
                        if (value != "0")
                        {
                            for (int t = 0; t < sessionService.ListCollection.Lists[30].elementValues.Length; t++)
                            {
                                if (sessionService.ListCollection.GetValue(30, t, 0) == value)
                                {
                                    for (int a = 0; a < sessionService.ListCollection.Lists[30].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[30].elementFields[a] == "Name")
                                        {
                                            type = sessionService.ListCollection.GetValue(30, t, a);
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[31].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[31].elementFields[k] == "require_weapon_level_min")
                    {
                        require_weapon_level_min = sessionService.ListCollection.GetValue(31, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[31].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[31].elementFields[k] == "require_weapon_level_max")
                    {
                        require_weapon_level_max = sessionService.ListCollection.GetValue(31, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[31].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[31].elementFields[k] == "damage_enhance")
                    {
                        damage_enhance = sessionService.ListCollection.GetValue(31, pos_item, k);
                        break;
                    }
                }
                line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7042), require_weapon_level_min, require_weapon_level_max, type);
                if (damage_enhance != "0")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7004) + " +" + damage_enhance;
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[31].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[31].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(31, pos_item, k);
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

