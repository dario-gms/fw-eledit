
using System;
using System.Globalization;

namespace FWEledit
{
    class FASHION_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                string equip_location = "0";
                string level = "0";
                for (int k = 0; k < sessionService.ListCollection.Lists[83].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[83].elementFields[k] == "id_sub_type")
                    {
                        string id_sub_type = sessionService.ListCollection.GetValue(83, pos_item, k);
                        for (int t = 0; t < sessionService.ListCollection.Lists[82].elementValues.Length; t++)
                        {
                            if (sessionService.ListCollection.GetValue(82, t, 0) == id_sub_type)
                            {
                                for (int a = 0; a < sessionService.ListCollection.Lists[82].elementFields.Length; a++)
                                {
                                    if (sessionService.ListCollection.Lists[82].elementFields[a] == "Name")
                                    {
                                        line += "\n" + sessionService.ListCollection.GetValue(82, t, a);
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[83].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[83].elementFields[k] == "equip_location")
                    {
                        equip_location = sessionService.ListCollection.GetValue(83, pos_item, k);
                        break;
                    }
                }
                if (Convert.ToInt32(equip_location) < 9)
                {
                    line += "\n" + Extensions.GetLocalization(sessionService, 7043) + " ???";
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[83].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[83].elementFields[k] == "require_level")
                    {
                        string require_level = sessionService.ListCollection.GetValue(83, pos_item, k);
                        if (require_level != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7018), require_level);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[83].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[83].elementFields[k] == "level")
                    {
                        level = sessionService.ListCollection.GetValue(83, pos_item, k);
                        if (equip_location == "10")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7000), level);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[83].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[83].elementFields[k] == "character_combo_id")
                    {
                        line += Extensions.DecodingCharacterComboId(sessionService, sessionService.ListCollection.GetValue(83, pos_item, k));
                        break;
                    }
                }
                if (equip_location == "10")
                {
                    for (int k = 0; k < sessionService.ListCollection.Lists[83].elementFields.Length; k++)
                    {
                        if (sessionService.ListCollection.Lists[83].elementFields[k] == "action_type")
                        {
                            string action_type = sessionService.ListCollection.GetValue(83, pos_item, k);
                            line += "\n" + Extensions.GetLocalization(sessionService, 7044) + " ";
                            if (action_type == "0")
                            {
                                line += Extensions.GetLocalization(sessionService, 3020);
                            }
                            if (action_type == "1")
                            {
                                line += Extensions.GetLocalization(sessionService, 3021);
                            }
                            if (action_type == "2")
                            {
                                line += Extensions.GetLocalization(sessionService, 3022);
                            }
                            if (action_type == "3")
                            {
                                line += Extensions.GetLocalization(sessionService, 3023);
                            }
                            if (action_type == "4")
                            {
                                line += Extensions.GetLocalization(sessionService, 3024);
                            }
                            if (action_type == "5")
                            {
                                line += Extensions.GetLocalization(sessionService, 3025);
                            }
                            if (action_type == "6")
                            {
                                line += Extensions.GetLocalization(sessionService, 3026);
                            }
                            if (action_type == "7")
                            {
                                line += Extensions.GetLocalization(sessionService, 3027);
                            }
                            if (action_type == "8")
                            {
                                line += Extensions.GetLocalization(sessionService, 3028);
                            }
                            if (action_type == "9")
                            {
                                line += Extensions.GetLocalization(sessionService, 3029);
                            }
                            if (action_type == "10")
                            {
                                line += Extensions.GetLocalization(sessionService, 3030);
                            }
                            if (action_type == "11")
                            {
                                line += Extensions.GetLocalization(sessionService, 3031);
                            }
                            if (action_type == "12")
                            {
                                line += Extensions.GetLocalization(sessionService, 3032);
                            }
                            if (action_type == "13")
                            {
                                line += Extensions.GetLocalization(sessionService, 3033);
                            }
                            if (action_type == "14")
                            {
                                line += Extensions.GetLocalization(sessionService, 3034);
                            }
                            break;
                        }
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[83].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[83].elementFields[k] == "gender")
                    {
                        string gender = sessionService.ListCollection.GetValue(83, pos_item, k);
                        line += "\n" + Extensions.GetLocalization(sessionService, 7045) + " ";
                        if (gender == "0")
                        {
                            line += Extensions.GetLocalization(sessionService, 3040);
                        }
                        if (gender == "1")
                        {
                            line += Extensions.GetLocalization(sessionService, 3041);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[83].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[83].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(83, pos_item, k);
                        if (price != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7024) + " " + Convert.ToInt32(price).ToString("N0", CultureInfo.CreateSpecificCulture("zh-CN"));
                        }
                        break;
                    }
                }
                if (Convert.ToInt32(level) > 0)
                {
                    line += "\n";
                    for (int a = 0; a < Convert.ToInt32(level); a++)
                    {
                        line += Extensions.GetLocalization(sessionService, 7046);
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

