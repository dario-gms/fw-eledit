
using System;
using System.Globalization;

namespace FWEledit
{
    class SELL_CERTIFICATE_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[123].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[123].elementFields[k] == "num_sell_item")
                    {
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7080), sessionService.ListCollection.GetValue(123, pos_item, k));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[123].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[123].elementFields[k] == "num_buy_item")
                    {
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7081), sessionService.ListCollection.GetValue(123, pos_item, k));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[123].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[123].elementFields[k] == "max_name_length")
                    {
                        line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7082), sessionService.ListCollection.GetValue(123, pos_item, k));
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[123].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[123].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(123, pos_item, k);
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

