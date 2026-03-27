
using System;
using System.Globalization;

namespace FWEledit
{
    class GOBLIN_EXPPILL_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[122].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[122].elementFields[k] == "level")
                    {
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7000), sessionService.ListCollection.GetValue(122, pos_item, k));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[122].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[122].elementFields[k] == "exp")
                    {
                        string exp = sessionService.ListCollection.GetValue(122, pos_item, k);
                        if (exp != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7079), exp);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[122].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[122].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(122, pos_item, k);
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

