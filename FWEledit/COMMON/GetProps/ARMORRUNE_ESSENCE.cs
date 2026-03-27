
using System;
using System.Globalization;

namespace FWEledit
{
    class ARMORRUNE_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                string damage_type = "-1";
                string require_player_level_max = "0";
                string damage_reduce_percent = "0";
                for (int k = 0; k < sessionService.ListCollection.Lists[19].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[19].elementFields[k] == "damage_type")
                    {
                        damage_type = sessionService.ListCollection.GetValue(19, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[19].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[19].elementFields[k] == "require_player_level_max")
                    {
                        require_player_level_max = sessionService.ListCollection.GetValue(19, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[19].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[19].elementFields[k] == "damage_reduce_percent")
                    {
                        damage_reduce_percent = sessionService.ListCollection.GetValue(19, pos_item, k);
                        break;
                    }
                }
                if (require_player_level_max != "0")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7031) + String.Format(Extensions.GetLocalization(sessionService, 7033), require_player_level_max);
                }
                if (damage_reduce_percent != "0" && damage_type == "0")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7025) + String.Format(Extensions.GetLocalization(sessionService, 7034), "+" + Convert.ToSingle(damage_reduce_percent).ToString("P0"));
                }
                if (damage_reduce_percent != "0" && damage_type == "1")
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7025) + String.Format(Extensions.GetLocalization(sessionService, 7035), "+" + Convert.ToSingle(damage_reduce_percent).ToString("P0"));
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[19].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[19].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(19, pos_item, k);
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

