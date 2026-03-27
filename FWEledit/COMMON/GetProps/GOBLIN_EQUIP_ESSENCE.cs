
using System;
using System.Globalization;

namespace FWEledit
{
    class GOBLIN_EQUIP_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[121].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[121].elementFields[k] == "equip_type")
                    {
                        string equip_type = sessionService.ListCollection.GetValue(121, pos_item, k);
                        line += "\n" + Extensions.GetLocalization(sessionService, 7068);
                        if (equip_type == "0")
                        {
                            line += Extensions.GetLocalization(sessionService, 7075);
                        }
                        if (equip_type == "1")
                        {
                            line += Extensions.GetLocalization(sessionService, 7076);
                        }
                        if (equip_type == "2")
                        {
                            line += Extensions.GetLocalization(sessionService, 7077);
                        }
                        if (equip_type == "3")
                        {
                            line += Extensions.GetLocalization(sessionService, 7078);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[121].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[121].elementFields[k] == "req_goblin_level")
                    {
                        string req_goblin_level = sessionService.ListCollection.GetValue(121, pos_item, k);
                        if (req_goblin_level != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7069), req_goblin_level);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[121].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[121].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(121, pos_item, k);
                        if (price != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7024) + " " + Convert.ToInt32(price).ToString("N0", CultureInfo.CreateSpecificCulture("zh-CN"));
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[121].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[121].elementFields[k] == "strength")
                    {
                        string strength = sessionService.ListCollection.GetValue(121, pos_item, k);
                        if (strength != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7058), "+" + strength);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[121].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[121].elementFields[k] == "agility")
                    {
                        string agility = sessionService.ListCollection.GetValue(121, pos_item, k);
                        if (agility != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7059), "+" + agility);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[121].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[121].elementFields[k] == "energy")
                    {
                        string energy = sessionService.ListCollection.GetValue(121, pos_item, k);
                        if (energy != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7061), "+" + energy);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[121].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[121].elementFields[k] == "tili")
                    {
                        string tili = sessionService.ListCollection.GetValue(121, pos_item, k);
                        if (tili != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7060), "+" + tili);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[121].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[121].elementFields[k] == "magic_1")
                    {
                        string magic_1 = sessionService.ListCollection.GetValue(121, pos_item, k);
                        if (magic_1 != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7070), magic_1);
                        }
                        string magic_2 = sessionService.ListCollection.GetValue(121, pos_item, k + 1);
                        if (magic_2 != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7071), magic_2);
                        }
                        string magic_3 = sessionService.ListCollection.GetValue(121, pos_item, k + 2);
                        if (magic_3 != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7072), magic_3);
                        }
                        string magic_4 = sessionService.ListCollection.GetValue(121, pos_item, k + 3);
                        if (magic_4 != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7073), magic_4);
                        } 
                        string magic_5 = sessionService.ListCollection.GetValue(121, pos_item, k + 4);
                        if (magic_5 != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7074), magic_5);
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

