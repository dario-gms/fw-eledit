
using System;
using System.Globalization;

namespace FWEledit
{
    class ARMOR_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[6].elementFields[k] == "id_sub_type")
                    {
                        string id_sub_type = sessionService.ListCollection.GetValue(6, pos_item, k);
                        for (int t = 0; t < sessionService.ListCollection.Lists[5].elementValues.Length; t++)
                        {
                            if (sessionService.ListCollection.GetValue(5, t, 0) == id_sub_type)
                            {
                                for (int a = 0; a < sessionService.ListCollection.Lists[5].elementFields.Length; a++)
                                {
                                    if (sessionService.ListCollection.Lists[5].elementFields[a] == "Name")
                                    {
                                        line += "\n" + sessionService.ListCollection.GetValue(5, t, a);
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[6].elementFields[k] == "level")
                    {
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7000), sessionService.ListCollection.GetValue(6, pos_item, k));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                    {
                        if (sessionService.ListCollection.Lists[6].elementFields[k] == "mp_enhance_low")
                        {
                            string hp_enhance_low = sessionService.ListCollection.GetValue(6, pos_item, k + 2);
                            if (hp_enhance_low != "0")
                            {
                                line += "\n" + Extensions.GetLocalization(sessionService, 7006) + " +" + hp_enhance_low;
                            }
                            string mp_enhance_low = sessionService.ListCollection.GetValue(6, pos_item, k);
                            if (mp_enhance_low != "0")
                            {
                                line += "\n" + Extensions.GetLocalization(sessionService, 7007) + " +" + mp_enhance_low;
                            }
                            string armor_enhance_low = sessionService.ListCollection.GetValue(6, pos_item, k + 4);
                            if (armor_enhance_low != "0")
                            {
                                line += "\n" + Extensions.GetLocalization(sessionService, 7008) + " +" + armor_enhance_low;
                            }
                            break;
                        }
                    }
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[6].elementFields[k] == "defence_low")
                    {
                        string defence_low = sessionService.ListCollection.GetValue(6, pos_item, k);
                        if (defence_low != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7009) + " +" + defence_low;
                        }
                        string magic_defences_1_low = sessionService.ListCollection.GetValue(6, pos_item, k + 2);
                        if (magic_defences_1_low != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7010) + " +" + magic_defences_1_low;
                        }
                        string magic_defences_2_low = sessionService.ListCollection.GetValue(6, pos_item, k + 4);
                        if (magic_defences_2_low != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7011) + " +" + magic_defences_2_low;
                        }
                        string magic_defences_3_low = sessionService.ListCollection.GetValue(6, pos_item, k + 6);
                        if (magic_defences_3_low != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7012) + " +" + magic_defences_3_low;
                        }
                        string magic_defences_4_low = sessionService.ListCollection.GetValue(6, pos_item, k + 8);
                        if (magic_defences_4_low != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7013) + " +" + magic_defences_4_low;
                        }
                        string magic_defences_5_low = sessionService.ListCollection.GetValue(6, pos_item, k + 10);
                        if (magic_defences_5_low != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7014) + " +" + magic_defences_5_low;
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[6].elementFields[k] == "durability_min")
                    {
                        line += "\n" + Extensions.GetLocalization(sessionService, 7015) + " " + sessionService.ListCollection.GetValue(6, pos_item, k) + "/" + sessionService.ListCollection.GetValue(6, pos_item, k + 1);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[6].elementFields[k] == "character_combo_id")
                    {
                        line += Extensions.DecodingCharacterComboId(sessionService, sessionService.ListCollection.GetValue(6, pos_item, k));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[6].elementFields[k] == "require_level")
                    {
                        string require_level = sessionService.ListCollection.GetValue(6, pos_item, k);
                        if (require_level != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7018), require_level);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[6].elementFields[k] == "require_strength")
                    {
                        string require_strength = sessionService.ListCollection.GetValue(6, pos_item, k);
                        if (require_strength != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7019), require_strength);
                        }
                        string require_agility = sessionService.ListCollection.GetValue(6, pos_item, k + 1);
                        if (require_agility != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7020), require_agility);
                        }
                        string require_energy = sessionService.ListCollection.GetValue(6, pos_item, k + 2);
                        if (require_energy != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7021), require_energy);
                        }
                        string require_tili = sessionService.ListCollection.GetValue(6, pos_item, k + 3);
                        if (require_tili != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7022), require_tili);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[6].elementFields[k] == "require_reputation")
                    {
                        string require_reputation = sessionService.ListCollection.GetValue(6, pos_item, k);
                        if (require_reputation != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7023), require_reputation);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[6].elementFields[k] == "fixed_props")
                    {
                        if ("0" != sessionService.ListCollection.GetValue(6, pos_item, k))
                        {
                            string probability_addon_num0 = "0";
                            for (int t = 0; t < sessionService.ListCollection.Lists[6].elementFields.Length; t++)
                            {
                                if (sessionService.ListCollection.Lists[6].elementFields[t] == "probability_addon_num0")
                                {
                                    probability_addon_num0 = sessionService.ListCollection.GetValue(6, pos_item, t);
                                    break;
                                }
                            }
                            if (probability_addon_num0 != "1")
                            {
                                for (int t = 1; t < 33; t++)
                                {
                                    for (int a = 0; a < sessionService.ListCollection.Lists[6].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[6].elementFields[a] == "addons_" + t + "_id_addon")
                                        {
                                            string id_addon = sessionService.ListCollection.GetValue(6, pos_item, a);
                                            if (id_addon != "0")
                                            {
                                                line += "\n" + "^4286f4" + EQUIPMENT_ADDON.GetAddon(sessionService, id_addon) + "^FFFFFF";
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[6].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[6].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(6, pos_item, k);
                        if (price != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7024) + " " + Convert.ToInt32(price).ToString("N0", CultureInfo.CreateSpecificCulture("zh-CN"));
                        }
                        break;
                    }
                }
                bool Suc = false;
                for (int k = 0; k < sessionService.ListCollection.Lists[90].elementValues.Length; k++)
                {
                    for (int a = 1; a < 13; a++)
                    {
                        for (int t = 0; t < sessionService.ListCollection.Lists[90].elementFields.Length; t++)
                        {
                            if (sessionService.ListCollection.Lists[90].elementFields[t] == "equipments_" + a + "_id")
                            {
                                if (Convert.ToInt32(sessionService.ListCollection.GetValue(90, k, t)) == Convert.ToInt32(sessionService.ListCollection.GetValue(6, pos_item, 0)))
                                {
                                    Suc = true;
                                    string name = "";
                                    string max_equips = "0";
                                    for (int n = 0; n < sessionService.ListCollection.Lists[90].elementFields.Length; n++)
                                    {
                                        if (sessionService.ListCollection.Lists[90].elementFields[n] == "Name")
                                        {
                                            name = sessionService.ListCollection.GetValue(90, k, n);
                                            break;
                                        }
                                    }
                                    for (int n = 0; n < sessionService.ListCollection.Lists[90].elementFields.Length; n++)
                                    {
                                        if (sessionService.ListCollection.Lists[90].elementFields[n] == "max_equips")
                                        {
                                            max_equips = sessionService.ListCollection.GetValue(90, k, n);
                                            break;
                                        }
                                    }
                                    line += "\n\n" + name + " (" + max_equips + ")";
                                    break;
                                }
                                break;
                            }
                        }
                    }
                    if (Suc == true) break;
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
