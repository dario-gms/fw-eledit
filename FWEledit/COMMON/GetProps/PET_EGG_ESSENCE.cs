
using System;
using System.Globalization;

namespace FWEledit
{
    class PET_EGG_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[95].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[95].elementFields[k] == "id_pet")
                    {
                        string id_pet = sessionService.ListCollection.GetValue(95, pos_item, k);
                        if (id_pet != "0")
                        {
                            for (int t = 0; t < sessionService.ListCollection.Lists[94].elementValues.Length; t++)
                            {
                                if (sessionService.ListCollection.GetValue(94, t, 0) == id_pet)
                                {
                                    string id_type = "0";
                                    for (int a = 0; a < sessionService.ListCollection.Lists[94].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[94].elementFields[a] == "id_type")
                                        {
                                            id_type = sessionService.ListCollection.GetValue(94, t, a);
                                            break;
                                        }
                                    }
                                    for (int a = 0; a < sessionService.ListCollection.Lists[94].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[94].elementFields[a] == "food_mask")
                                        {
                                            line += Extensions.DecodingFoodMask(sessionService, sessionService.ListCollection.GetValue(94, t, a));
                                            break;
                                        }
                                    }
                                    for (int a = 0; a < sessionService.ListCollection.Lists[95].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[95].elementFields[a] == "level")
                                        {
                                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7048), sessionService.ListCollection.GetValue(95, pos_item, a));
                                            break;
                                        }
                                    }
                                    if (id_type == "8781" || id_type == "8783")
                                    {
                                        for (int a = 0; a < sessionService.ListCollection.Lists[94].elementFields.Length; a++)
                                        {
                                            if (sessionService.ListCollection.Lists[94].elementFields[a] == "speed_a")
                                            {
                                                line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7049), Convert.ToSingle(sessionService.ListCollection.GetValue(94, t, a)).ToString("F2", CultureInfo.CreateSpecificCulture("en-US")));
                                                break;
                                            }
                                        }
                                    }
                                    for (int a = 0; a < sessionService.ListCollection.Lists[94].elementFields.Length; a++)
                                    {
                                        if (sessionService.ListCollection.Lists[94].elementFields[a] == "level_require")
                                        {
                                            string level_require = sessionService.ListCollection.GetValue(94, t, a);
                                            if (level_require != "0")
                                            {
                                                line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7018), level_require);
                                            }
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
                for (int k = 0; k < sessionService.ListCollection.Lists[95].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[95].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(95, pos_item, k);
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

