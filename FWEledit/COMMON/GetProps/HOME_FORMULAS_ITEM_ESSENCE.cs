
using System;
using System.Globalization;

namespace FWEledit
{
    class HOME_FORMULAS_ITEM_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                string name = "";
                string max_produces = "-1";
                string require_produce_type = "0";
                string require_factory_type = "0";
                string require_factory_level = "0";
                string factoryname = "";
                string home_exp = "0";
                string resources_num_1 = "0";
                string resources_num_2 = "0";
                string resources_num_3 = "0";
                string resources_num_4 = "0";
                string price1 = "0";
                for (int k = 0; k < sessionService.ListCollection.Lists[218].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[218].elementFields[k] == "id_recipe")
                    {
                        string id_recipe = sessionService.ListCollection.GetValue(218, pos_item, k);
                        if (id_recipe != "0")
                        {
                            for (int t = 0; t < sessionService.ListCollection.Lists[217].elementValues.Length; t++)
                            {
                                if (sessionService.ListCollection.GetValue(217, t, 0) == id_recipe)
                                {
                                    for (int a = 0; a < sessionService.ListCollection.Lists[217].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[217].elementFields[a] == "id_to_make")
                                        {
                                            string id_to_make = sessionService.ListCollection.GetValue(217, t, a);
                                            if (id_to_make != "0")
                                            {
                                                for (int i = 0; i < sessionService.ListCollection.Lists[223].elementValues.Length; i++)
                                                {
                                                    if (sessionService.ListCollection.GetValue(223, i, 0) == id_to_make)
                                                    {
                                                        for (int f = 0; f < sessionService.ListCollection.Lists[223].elementFields.Length; f++)
                                                        {
                                                            if (sessionService.ListCollection.Lists[223].elementFields[f] == "Name")
                                                            {
                                                                name = sessionService.ListCollection.GetValue(223, i, f);
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
                                    for (int a = 0; a < sessionService.ListCollection.Lists[217].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[217].elementFields[a] == "require_produce_type")
                                        {
                                            require_produce_type = sessionService.ListCollection.GetValue(217, t, a);
                                            break;
                                        }
                                    }
                                    for (int a = 0; a < sessionService.ListCollection.Lists[217].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[217].elementFields[a] == "require_factory_type")
                                        {
                                            require_factory_type = sessionService.ListCollection.GetValue(217, t, a);
                                            break;
                                        }
                                    }
                                    for (int a = 0; a < sessionService.ListCollection.Lists[217].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[217].elementFields[a] == "require_factory_level")
                                        {
                                            require_factory_level = sessionService.ListCollection.GetValue(217, t, a);
                                            break;
                                        }
                                    }
                                    for (int a = 0; a < sessionService.ListCollection.Lists[217].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[217].elementFields[a] == "home_exp")
                                        {
                                            home_exp = sessionService.ListCollection.GetValue(217, t, a);
                                            break;
                                        }
                                    }
                                    for (int a = 0; a < sessionService.ListCollection.Lists[217].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[217].elementFields[a] == "resources_num_1")
                                        {
                                            resources_num_1 = sessionService.ListCollection.GetValue(217, t, a);
                                            resources_num_2 = sessionService.ListCollection.GetValue(217, t, a + 1);
                                            resources_num_3 = sessionService.ListCollection.GetValue(217, t, a + 2);
                                            resources_num_4 = sessionService.ListCollection.GetValue(217, t, a + 3);
                                            break;
                                        }
                                    }
                                    for (int a = 0; a < sessionService.ListCollection.Lists[217].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[217].elementFields[a] == "price")
                                        {
                                            price1 = sessionService.ListCollection.GetValue(217, t, a);
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
                for (int k = 0; k < sessionService.ListCollection.Lists[218].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[218].elementFields[k] == "max_produces")
                    {
                        max_produces = sessionService.ListCollection.GetValue(218, pos_item, k);
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[218].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[218].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(218, pos_item, k);
                        if (price != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7024) + " " + Convert.ToInt32(price).ToString("N0", CultureInfo.CreateSpecificCulture("zh-CN"));
                        }
                        break;
                    }
                }
                if (require_produce_type == "0")
                {
                    if (require_factory_type == "0") factoryname = Extensions.GetLocalization(sessionService, 3070);
                    if (require_factory_type == "1") factoryname = Extensions.GetLocalization(sessionService, 3071);
                    if (require_factory_type == "2") factoryname = Extensions.GetLocalization(sessionService, 3072);
                    if (require_factory_type == "3") factoryname = Extensions.GetLocalization(sessionService, 3073);
                    if (require_factory_type == "4") factoryname = Extensions.GetLocalization(sessionService, 3074);
                }
                if (require_produce_type == "1")
                {
                    factoryname = Extensions.GetLocalization(sessionService, 3075);
                }
                line += "\n\n" + Extensions.GetLocalization(sessionService, 7108) + name;
                if (Convert.ToInt32(max_produces) > 0)
                {
                    line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7109), max_produces);
                }
                line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7110), factoryname, require_factory_level, resources_num_1, resources_num_2, resources_num_3, resources_num_4, price1, home_exp);
            }
            catch
            {
                line = "";
            }
            return line;
        }
	}
}

