
using System;
using System.Globalization;

namespace FWEledit
{
    class REFINE_TICKET_ESSENCE
    {
        public static string GetProps(ISessionService sessionService, int pos_item)
        {
            string line = "";
            try
            {
                for (int k = 0; k < sessionService.ListCollection.Lists[107].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[107].elementFields[k] == "binding_only")
                    {
                        string binding_only = sessionService.ListCollection.GetValue(107, pos_item, k);
                        if (binding_only != "0")
                        {
                            line += "\n" + Extensions.GetLocalization(sessionService, 7051);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[107].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[107].elementFields[k] == "require_level_max")
                    {
                        string require_level_max = sessionService.ListCollection.GetValue(107, pos_item, k);
                        if (require_level_max != "0")
                        {
                            line += "\n" + String.Format(Extensions.GetLocalization(sessionService, 7052), require_level_max);
                        }
                        break;
                    }
                }
                for (int k = 0; k < sessionService.ListCollection.Lists[107].elementFields.Length; k++)
                {
                    if (sessionService.ListCollection.Lists[107].elementFields[k] == "price")
                    {
                        string price = sessionService.ListCollection.GetValue(107, pos_item, k);
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

