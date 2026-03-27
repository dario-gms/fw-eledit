
using System;
using System.Globalization;

namespace FWEledit
{
    class GOBLIN_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7120), 1);
                string init_strength = "0";
                string init_agility = "0";
                string init_tili = "0";
                string init_energy = "0";
                for (int k = 0; k < sessionService.ListCollection.Lists[119].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[119].elementFields[k] == "init_strength")
                    {
                        init_strength = sessionService.ListCollection.GetValue(119, pos_item, k);
                        if (init_strength != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7058), "+" + init_strength);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[119].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[119].elementFields[k] == "init_agility")
                    {
                        init_agility = sessionService.ListCollection.GetValue(119, pos_item, k);
                        if (init_agility != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7059), "+" + init_agility);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[119].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[119].elementFields[k] == "init_tili")
                    {
                        init_tili = sessionService.ListCollection.GetValue(119, pos_item, k);
                        if (init_tili != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7060), "+" + init_tili);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[119].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[119].elementFields[k] == "init_energy")
                    {
                        init_energy = sessionService.ListCollection.GetValue(119, pos_item, k);
                        if (init_energy != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7061), "+" + init_energy);
                        }
                        break;
                    }
                }
                line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7062), 50);
                line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7063), (100 + Convert.ToInt32(init_tili)));
                line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7064), Convert.ToSingle(1 + 0.02 * Convert.ToInt32(init_energy)).ToString("F2", CultureInfo.CreateSpecificCulture("en-US")));
                line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7065), 20000);
                line += "\n" + Extensions.GetLocalization(sessionService, 7066);
                for (int k = 0; k < sessionService.ListCollection.Lists[119].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[119].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(119, pos_item, k);
                        if (price != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7024) + " " + Convert.ToInt32(price).ToString("N0", CultureInfo.CreateSpecificCulture("zh-CN"));
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[119].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[119].elementFields[k] == "default_skill1")
                    {
                        string default_skill1 = sessionService.ListCollection.GetValue(119, pos_item, k);
                        if (default_skill1 != "0")
                        {
                            line += "\n" + Extensions.SkillName(sessionService, Convert.ToInt32(default_skill1)) + String.Format(Extensions.GetLocalization(sessionService, 7067), 1);
                        }
                        string default_skill2 = sessionService.ListCollection.GetValue(119, pos_item, k + 1);
                        if (default_skill2 != "0")
                        {
                            line += "\n" + Extensions.SkillName(sessionService, Convert.ToInt32(default_skill2)) + String.Format(Extensions.GetLocalization(sessionService, 7067), 1);
                        }
                        string default_skill3 = sessionService.ListCollection.GetValue(119, pos_item, k + 2);
                        if (default_skill3 != "0")
                        {
                            line += "\n" + Extensions.SkillName(sessionService, Convert.ToInt32(default_skill3)) + String.Format(Extensions.GetLocalization(sessionService, 7067), 1);
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

