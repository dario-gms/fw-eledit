
using System;
using System.Globalization;

namespace FWEledit
{
    class PET_FOOD_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[96].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[96].elementFields[k] == "food_type")
                    {
                        line += Extensions.DecodingFoodMask(sessionService, sessionService.ListCollection.GetValue(96, pos_item, k));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[96].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[96].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(96, pos_item, k);
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

